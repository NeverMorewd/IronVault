# Iron Vault — Code Architecture

This document describes the design of the Iron Vault codebase in enough depth that a new
developer can understand each layer, locate the code they need to change, and extend the game
without breaking existing functionality.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Project Layout](#2-project-layout)
3. [IronVault.Core — Game Engine](#3-ironvaultcore--game-engine)
   - 3.1 [Entity Model](#31-entity-model)
   - 3.2 [ECS Systems](#32-ecs-systems)
   - 3.3 [GameEngine — the coordinator](#33-gameengine--the-coordinator)
   - 3.4 [Wave Scripting](#34-wave-scripting)
   - 3.5 [Upgrade System](#35-upgrade-system)
   - 3.6 [Localisation](#36-localisation)
4. [IronVault.Renderer — Drawing Layer](#4-ironvaultrenderer--drawing-layer)
   - 4.1 [GameCanvas](#41-gamecanvas)
   - 4.2 [Drawable Objects](#42-drawable-objects)
   - 4.3 [DrawColors](#43-drawcolors)
5. [IronVault.Desktop — Windows Host](#5-ironvaultdesktop--windows-host)
   - 5.1 [Entry Point and App Lifetime](#51-entry-point-and-app-lifetime)
   - 5.2 [MainWindow and Navigation](#52-mainwindow-and-navigation)
   - 5.3 [Views](#53-views)
   - 5.4 [GameViewModel and the Game Loop](#54-gameviewmodel-and-the-game-loop)
   - 5.5 [Audio](#55-audio)
6. [IronVault.Browser — WebAssembly Host](#6-ironvaultbrowser--webassembly-host)
   - 6.1 [Entry Point](#61-entry-point)
   - 6.2 [MainView (Shell)](#62-mainview-shell)
   - 6.3 [Shared Code via File Linking](#63-shared-code-via-file-linking)
   - 6.4 [Audio Stub](#64-audio-stub)
7. [Pipboy.Avalonia Integration](#7-pipboyavalonia-integration)
8. [Data Flow for One Frame](#8-data-flow-for-one-frame)
9. [How to Extend the Game](#9-how-to-extend-the-game)
   - 9.1 [Add a new enemy tier](#91-add-a-new-enemy-tier)
   - 9.2 [Add a new power-up](#92-add-a-new-power-up)
   - 9.3 [Add a new upgrade](#93-add-a-new-upgrade)
   - 9.4 [Add a new game mode](#94-add-a-new-game-mode)
   - 9.5 [Add a new map tile type](#95-add-a-new-map-tile-type)
   - 9.6 [Add a new localisation string](#96-add-a-new-localisation-string)
10. [Dependency Rules](#10-dependency-rules)
11. [Build and Deployment](#11-build-and-deployment)

---

## 1. Overview

Iron Vault is a top-down tank shooter inspired by the classic Battle City arcade game.
The design goal is a self-contained codebase that:

- Has no external asset files (all graphics are procedural vector drawing).
- Compiles AOT (Ahead-Of-Time) and ships as both a native desktop executable and a
  WebAssembly browser application from the same source.
- Uses a lightweight Entity-Component-System (ECS) pattern inside a single-threaded
  60 fps game loop driven by `DispatcherTimer`.

---

## 2. Project Layout

```
src/
├── IronVault.Core        # Pure C# — no UI dependency
├── IronVault.Renderer    # Avalonia dependency; wraps Core for drawing
├── IronVault.Desktop     # net10.0 Windows desktop application
└── IronVault.Browser     # net10.0-browser WebAssembly application
```

**Dependency graph** (arrows point from consumer to dependency):

```
IronVault.Desktop  ──►  IronVault.Renderer  ──►  IronVault.Core
IronVault.Browser  ──►  IronVault.Renderer  ──►  IronVault.Core
Both hosts         ──►  Pipboy.Avalonia
```

`IronVault.Core` has no Avalonia dependency. This keeps the game logic portable and
unit-testable without a UI.

---

## 3. IronVault.Core — Game Engine

Located in `src/IronVault.Core/`.

### 3.1 Entity Model

Entities live in `Engine/Entities/`. Every entity is a plain C# class (not an interface)
that aggregates several component objects. The most important entity is `Tank`, which
composes:

| Component | Purpose |
|-----------|---------|
| `PositionComponent` | Grid cell coordinates, pixel offset, facing direction |
| `VelocityComponent` | `IsMoving` flag set by `MoveSystem` each frame |
| `HealthComponent`   | Current and maximum HP |
| `WeaponComponent`   | Fire rate, bullet speed, power, max bullets in flight |
| `TankInput`         | Intended move directions and fire request for the current frame |

Other entities include `Bullet`, `PowerUp`, `Explosion`, and `MapTile`.

### 3.2 ECS Systems

Systems are stateless classes in `Engine/Systems/` that each own one concern.
`GameEngine` calls them in a fixed order every tick.

| System | Responsibility |
|--------|---------------|
| `MoveSystem` | Moves tanks using AABB collision against tiles and other tanks; handles in-place rotation |
| `BulletSystem` | Advances bullets, detects tile and tank hits, spawns explosions |
| `EnemyAISystem` | Chooses movement direction and fires for each enemy tank |
| `AllyAISystem` | Same for ally tanks; includes stuck-detection (resets direction after N frames without movement) |
| `PowerUpSystem` | Checks player overlap with power-up pickups; applies timed effects |
| `ExplosionSystem` | Advances explosion animation frames and removes finished explosions |
| `SpawnSystem` | Spawns new enemy tanks from spawn points up to the wave's simultaneous cap |
| `WaveSystem` | Detects wave-clear and victory conditions; raises events on `GameEngine` |

**Order matters.** `MoveSystem` runs first so that `Velocity.IsMoving` is set correctly
before `AllyAISystem` reads it to detect a stuck condition.

### 3.3 GameEngine — the coordinator

`GameEngine` (in `Engine/GameEngine.cs`) holds the canonical game state:

- `Player` — the player's `Tank` entity (null before the game starts).
- `Enemies`, `Allies`, `Bullets`, `Explosions`, `PowerUps` — entity lists.
- `Map` — the `TileMap` for the current level.
- `State` — a `GameState` enum (`NotStarted`, `Playing`, `Paused`, `WaveComplete`, `GameOver`, `Victory`).
- `Score`, `Lives`, `Wave`, `Mode`, `Difficulty`, `TotalWaves` — scalar game state.

`GameEngine.Tick(float dt)` is called once per frame by `GameViewModel`. It checks the
current state, updates all systems, and raises domain events:

| Event | When |
|-------|------|
| `StateChanged` | Any state transition |
| `ScoreChanged` | Score incremented |
| `WaveCleared`  | All enemies for the wave defeated |
| `ShotFired`    | Any tank fires |
| `HitOccurred`  | A bullet destroys a tile |
| `EnemyDestroyed` | An enemy tank is destroyed |
| `PlayerHurt`   | Player takes damage |
| `PowerUpCollected` | Player picks up a power-up |

### 3.4 Wave Scripting

`DefenseWaveScript` (in `Engine/DefenseWaveScript.cs`) defines the 10-wave Defense
campaign as a static switch expression. Each `WaveScript` record carries:

- `TotalEnemies` — how many spawn in total.
- `MaxSimultaneous` — cap on live enemies at once.
- `TierWeights` — percentage weights for Tier 1-4 enemies (must sum to 100).
- `GrantsAlly` — whether clearing this wave rewards an ally tank.

Classic mode does not use `DefenseWaveScript`; it uses a formula based on the current
wave number and difficulty.

### 3.5 Upgrade System

`UpgradeType` is an enum in `Engine/`. Each value maps to a mutation applied by
`GameEngine.ApplyPlayerUpgrade(UpgradeType)`. Descriptions (icon, name, localised text)
are provided by the `UpgradeDescriptions` helper in `IronVault.Core`, so the Renderer and
both hosts can access them.

### 3.6 Localisation

`I18n` (in `Localization/I18n.cs`) is a static class with a `Dictionary<string, (string En, string Zh)>`
lookup. Calling `I18n.T("key")` returns the string for the current `I18n.Current` language.
Changing `I18n.Current` raises `I18n.LanguageChanged` (an `Action`), which every view
subscribes to in order to refresh its text.

To add a string: add a key-value entry to the dictionary in `I18n.cs`. The key is a
dot-separated path like `"hud.wave"` or `"diff.easy.desc"`.

---

## 4. IronVault.Renderer — Drawing Layer

Located in `src/IronVault.Renderer/`.

### 4.1 GameCanvas

`GameCanvas` (in `Controls/GameCanvas.cs`) is an Avalonia `Control` subclass.
`GameCanvas.Attach(GameEngine)` wires the canvas to an engine instance.
`GameCanvas.Tick(float dt)` is called every frame by the view's `OnFrameTick` handler;
it calls `engine.Tick(dt)` and then `InvalidateVisual()` to schedule a redraw.

`GameCanvas.Render(DrawingContext ctx)` is the Avalonia paint callback. It iterates
all game entities and delegates to a `Drawable` for each.

All drawing uses the Avalonia `DrawingContext` API — no hardware-specific calls, no
`unsafe` blocks — which makes the renderer safe for both the WebAssembly target and AOT
compilation.

### 4.2 Drawable Objects

Every entity type has a corresponding `*Drawable` class in `Drawables/`:

| Drawable | Draws |
|----------|-------|
| `TankDrawable` | Player tank, all four enemy tiers, ally tanks |
| `BulletDrawable` | Active bullets |
| `ExplosionDrawable` | Multi-frame explosion animation |
| `TileDrawable` | All map tile variants |
| `PowerUpDrawable` | Power-up item icons |
| `GridDrawable` | Background grid lines |

Each `*Drawable` has a `Draw(DrawingContext, ...)` static (or instance) method so no
allocation occurs per frame.

Enemy tiers are differentiated entirely by `TankDrawable.DrawEnemyTank(ctx, x, y, s, tick, tier)`:

- **Tier 1** — red-orange hull, single barrel, two blinking red lights.
- **Tier 2** — amber-gold hull, three barrel ribs, roof stripe, amber lights.
- **Tier 3** — dark crimson, side skirt armor plates, shoulder bolts, thick barrel, white strobe.
- **Tier 4** — gunmetal/black, red accent stripe, dual cannon, hazard chevron, four-corner lights.

### 4.3 DrawColors

`DrawColors` (in `Drawables/DrawColors.cs`) is an internal static class that centralises
every `Color` and `SolidColorBrush` used across all drawables. Modifying a color here
changes it everywhere. The palette follows an amber CRT / phosphor aesthetic.

---

## 5. IronVault.Desktop — Windows Host

Located in `src/IronVault.Desktop/`.

### 5.1 Entry Point and App Lifetime

`Program.cs` calls `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)`.
`App.axaml.cs` sets the amber primary color via `PipboyThemeManager` and creates a
`MainWindow` for `IClassicDesktopStyleApplicationLifetime`.

### 5.2 MainWindow and Navigation

`MainWindow` inherits `PipboyWindow` (from Pipboy.Avalonia), which provides the custom
chrome-less title bar and Win11 snap support.

Navigation between screens is managed entirely through `Control.IsVisible` — all three
views live in the same `Grid` cell and only one is visible at a time. This avoids page
transitions and keeps focus management simple.

```
MainWindow
└── Grid
    ├── MenuView     (IsVisible = true  when on menu)
    ├── GameView     (IsVisible = false)
    └── UpgradeView  (IsVisible = false)
```

`TitleBarContent` is set to a `TextBlock` whose text is updated by `RefreshTitleBar()`
whenever `ScoreChanged` or `StateChanged` fires.

### 5.3 Views

| View | File | Purpose |
|------|------|---------|
| `MenuView` | `Views/MenuView.axaml(.cs)` | Mode, difficulty, language selection; raises `StartRequested` |
| `GameView` | `Views/GameView.axaml(.cs)` | HUD, keyboard input, `GameCanvas`, `CrtDisplay` wrapper |
| `UpgradeView` | `Views/UpgradeView.axaml(.cs)` | Wave-clear debrief, three upgrade card buttons |

`GameView.SetViewModel(GameViewModel)` must be called once before the first tick. It wires
up the `FrameTick` event and game engine events to the HUD updater.

### 5.4 GameViewModel and the Game Loop

`GameViewModel` (in `ViewModels/GameViewModel.cs`) owns a single `GameEngine` instance and
a `DispatcherTimer` running at 60 fps. Each timer tick:

1. Calculates `dt` (delta time, capped at 100 ms to prevent spiral-of-death on lag spikes).
2. Raises `FrameTick(dt)` to `GameView`.
3. `GameView.OnFrameTick` applies input, calls `GameCanvas.Tick(dt)`, and updates the HUD.

`StartGame(difficulty, mode)` → `Engine.StartGame()` → starts the timer.
`Stop()` → stops the timer (engine state is preserved for the upgrade screen).

### 5.5 Audio

`Audio/RetroSound.cs` synthesises all sounds procedurally at runtime using 16-bit mono PCM
at 22 050 Hz. It uses two Windows-only mechanisms:

- `PlaySound` via `winmm.dll` P/Invoke for one-shot sounds.
- A `waveOut` loop for the continuous engine-running rumble.

Because this is Windows-only, the class lives in `IronVault.Desktop` and is referenced from
the views using `using IronVault.Desktop.Audio`. The Browser host provides a no-op stub with
the same namespace and method signatures.

---

## 6. IronVault.Browser — WebAssembly Host

Located in `src/IronVault.Browser/`.

### 6.1 Entry Point

`Program.cs` uses `BuildAvaloniaApp().StartBrowserAppAsync()` (async, no `[STAThread]`).
The assembly is decorated with `[assembly: SupportedOSPlatform("browser")]`.

`App.axaml.cs` handles `ISingleViewApplicationLifetime` instead of
`IClassicDesktopStyleApplicationLifetime`, and sets `singleView.MainView = new MainView()`.

### 6.2 MainView (Shell)

`MainView` is a `UserControl` (not a `Window`) that contains the same three views —
`MenuView`, `GameView`, `UpgradeView` — in a `Grid`. Navigation logic is identical to
`MainWindow` in the Desktop project.

### 6.3 Shared Code via File Linking

To avoid duplicating source files the Browser project uses MSBuild `Link` metadata to
compile Desktop source files directly:

```xml
<AvaloniaXaml Include="..\IronVault.Desktop\Views\GameView.axaml"
              Link="Views\GameView.axaml" />
<Compile Include="..\IronVault.Desktop\Views\GameView.axaml.cs"
         Link="Views\GameView.axaml.cs" />
```

Linked files:

| File | Shared via |
|------|-----------|
| `Views/GameView.axaml` + `.cs` | AvaloniaXaml + Compile link |
| `Views/MenuView.axaml`         | AvaloniaXaml link |
| `Views/UpgradeView.axaml` + `.cs` | AvaloniaXaml + Compile link |
| `ViewModels/GameViewModel.cs`  | Compile link |

`Views/MenuView.axaml.cs` is **not** linked because the Desktop version calls
`Environment.Exit(0)` which throws `PlatformNotSupportedException` in WebAssembly.
The Browser project provides its own `MenuView.axaml.cs` that hides the Exit button
instead.

### 6.4 Audio Stub

`Audio/RetroSound.cs` in the Browser project declares the same `IronVault.Desktop.Audio`
namespace and the same public static methods as the Desktop original, but all method bodies
are empty. The linked view code compiles and runs without modification; it just produces no
sound. Web Audio API support can be added later via `IJSRuntime` or Avalonia's JS interop.

---

## 7. Pipboy.Avalonia Integration

[Pipboy.Avalonia](https://github.com/NeverMorewd/Pipboy.Avalonia) is referenced via a local
relative `ProjectReference` (`../../../Pipboy.Avalonia/...`). It must be cloned as a sibling
to the `IronVault` repository.

Iron Vault uses the following controls from Pipboy.Avalonia:

| Control | Where used | Purpose |
|---------|-----------|---------|
| `PipboyWindow` | `MainWindow` (Desktop only) | Custom chrome-less title bar, Win11 snap |
| `PipboyTheme` | `App.axaml` (both hosts) | Amber CRT colour theme; all styles |
| `CrtDisplay` | `GameView.axaml` | Post-processing overlay for `GameCanvas` |
| `SegmentedBar` | `GameView.axaml` | HP bar with discrete segment display |

`PipboyThemeManager.Instance.SetPrimaryColor(Color)` is called in `App.axaml.cs` to apply
the game's signature amber colour (#FFA500) to all theme-derived brushes.

`CrtDisplay` is a `Panel` that renders its child first and then draws scanlines, a moving
scan beam, random static noise, and a vignette on top using `DrawingContext` — fully AOT-
and WASM-compatible.

---

## 8. Data Flow for One Frame

```
DispatcherTimer.Tick
  └─► GameViewModel.OnTimerTick(dt)
        └─► FrameTick event
              └─► GameView.OnFrameTick(dt)
                    ├─► Build TankInput from held-key set
                    ├─► GameCanvas.Tick(dt)
                    │     ├─► GameEngine.Tick(dt)
                    │     │     ├─► MoveSystem.Update()     — sets Velocity.IsMoving
                    │     │     ├─► BulletSystem.Update()
                    │     │     ├─► EnemyAISystem.Update()
                    │     │     ├─► AllyAISystem.Update()   — reads IsMoving for stuck detection
                    │     │     ├─► PowerUpSystem.Update()
                    │     │     ├─► ExplosionSystem.Update()
                    │     │     ├─► SpawnSystem.Update()
                    │     │     └─► WaveSystem.Update()     — may raise WaveCleared / Victory
                    │     └─► InvalidateVisual()
                    │           └─► GameCanvas.Render(DrawingContext)
                    │                 └─► *Drawable.Draw() for each entity
                    ├─► RetroSound management (start/stop engine rumble)
                    └─► UpdateHud()  — writes score, wave, lives, effects to TextBlocks
```

---

## 9. How to Extend the Game

### 9.1 Add a new enemy tier

1. In `IronVault.Core`: add a `Tier5` constant or increase the tier ceiling in `Tank` /
   `DefenseWaveScript`.
2. In `IronVault.Renderer/Drawables/TankDrawable.cs`: add a `DrawEnemyTier5(...)` method
   and route to it from the `tier` switch in `DrawEnemyTank`.
3. Update `DefenseWaveScript` tier-weight arrays to include Tier 5 in later waves.

### 9.2 Add a new power-up

1. Add a value to the `PowerUpType` enum in `IronVault.Core`.
2. Implement the timed-effect logic in `PowerUpSystem.Update()` and add a timer field to
   `GameEngine`.
3. Add a draw case in `PowerUpDrawable`.
4. Expose the timer from `GameEngine` and display it in `GameView.UpdateHud()`.

### 9.3 Add a new upgrade

1. Add a value to `UpgradeType` and implement the mutation in
   `GameEngine.ApplyPlayerUpgrade`.
2. Add the icon/name/description to `UpgradeDescriptions.For(UpgradeType)`.
3. Add the item to the `pool` list in `UpgradeView.GenerateChoices` with any
   availability condition you need.

### 9.4 Add a new game mode

1. Add a value to `GameMode`.
2. Add mode-specific initialisation in `GameEngine.StartGame`.
3. Add a button + description in `MenuView` AXAML and localisation strings.
4. Implement mode-specific win / loss conditions in `WaveSystem`.

### 9.5 Add a new map tile type

1. Add a `TileType` enum value.
2. Add tile behaviour (solid, destructible, traversable) in `MoveSystem` and `BulletSystem`.
3. Add a draw case in `TileDrawable`.
4. Place the tile in the map data in `MapGenerator` or the level files.

### 9.6 Add a new localisation string

Open `src/IronVault.Core/Localization/I18n.cs` and add one entry to the `_strings`
dictionary:

```csharp
["my.new.key"] = ("English text", "中文文本"),
```

Then call `I18n.T("my.new.key")` wherever you need it in a view.

---

## 10. Dependency Rules

These rules keep the project maintainable. Follow them when adding new code.

| Rule | Rationale |
|------|-----------|
| `IronVault.Core` must not reference Avalonia | Core should be testable without a UI runtime |
| `IronVault.Renderer` may only reference `Avalonia` and `IronVault.Core` | Keeps rendering logic isolated |
| Neither Core nor Renderer may reference Desktop or Browser | Prevents circular dependencies |
| `RetroSound` (Desktop) must not be referenced from Core or Renderer | Platform-specific audio must stay in the host layer |
| All drawing must go through `DrawingContext` | Required for AOT and WASM compatibility |

---

## 11. Build and Deployment

### Desktop

```bash
dotnet run  --project src/IronVault.Desktop              # development
dotnet publish src/IronVault.Desktop -c Release          # native executable
```

### Browser (WebAssembly)

Requires `.NET 10 SDK` and the `wasm-tools` workload:

```bash
dotnet workload install wasm-tools
dotnet publish src/IronVault.Browser -c Release -o publish
# Serve the files in publish/wwwroot/ with any static HTTP server.
```

### Continuous Deployment

`.github/workflows/deploy-pages.yml` runs on every push to `main`:

1. Checks out `IronVault` and `Pipboy.Avalonia` as siblings (required by the relative
   `ProjectReference`).
2. Installs the SDK and workload.
3. Publishes the Browser project.
4. Deploys `publish/wwwroot/` to GitHub Pages via `actions/deploy-pages`.

Enable the deployment environment in **Settings → Pages → Source: GitHub Actions** before
triggering the workflow for the first time.
