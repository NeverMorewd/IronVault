# Iron Vault — 铁窖计划

[![Play Online](https://img.shields.io/badge/Play%20Online-WebAssembly-brightgreen?logo=googlechrome&logoColor=white)](https://nevermorewd.github.io/IronVault/)
[![Built with Claude AI](https://img.shields.io/badge/Built%20with-Claude%20AI-blueviolet?logo=anthropic&logoColor=white)](https://www.anthropic.com/claude)
[![Powered by Pipboy.Avalonia](https://img.shields.io/badge/UI-Pipboy.Avalonia-orange?logo=dotnet&logoColor=white)](https://github.com/NeverMorewd/Pipboy.Avalonia)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A retro armoured-combat game built with [Avalonia UI](https://avaloniaui.net/) and styled with the [**Pipboy.Avalonia**](https://github.com/NeverMorewd/Pipboy.Avalonia) CRT theme library. Every tank, bullet, and explosion is drawn from pure vector geometry — no sprites, no bitmaps. Play it directly in your browser via WebAssembly, or run it natively on the desktop.

> **🎮 [Play now in your browser →](https://nevermorewd.github.io/IronVault/)**

---

## Gameplay

You command a single tank on a tile-based battlefield. Choose from **100 hand-crafted stages**, each with a unique map layout. Destroy all enemy tanks to advance through waves. Enemy tanks come in four tiers — each with a distinct silhouette and color scheme — and grow progressively more dangerous as waves escalate.

### Game Modes

| Mode | Description |
|------|-------------|
| **Classic** | Survive infinite waves across your chosen stage. No victory — only glory. |
| **Defense** | A scripted 10-wave campaign. Clear a wave to pick a **Field Upgrade** for your tank. |

### Stages & Level Select

100 preset stages are available from a dedicated **Stage Select** screen (accessible before each deployment). Each stage features an intentionally designed Battle City-style map — symmetric corridors, strategic water channels, forest ambush pockets, steel fortifications, and ice fields. A bilingual stage announcement ("STAGE 1 / 第一关") is displayed at the start of each new game.

### Difficulty

| Rating | Enemy Bullet Speed | Fire Rate |
|--------|--------------------|-----------|
| **Rookie (新兵)** | ~160 px/s — very easy to dodge | Half of base |
| **Veteran (老兵)** | ~200 px/s — manageable | 30 % slower than base |
| **Elite (精英)** | 288–352 px/s — close to player speed | 15 % faster than base |

Player bullet speed is always **288 px/s** regardless of difficulty.

### Power-ups

Destroyed enemies randomly drop one of seven power-ups:

| Icon | Name | Effect |
|------|------|--------|
| ★ | Shield | Your tank becomes invulnerable temporarily |
| ⏸ | Freeze | All enemies stop moving and firing |
| ⬛ | Fortress | Reinforces your base walls with steel |
| ▲ | Overload | Increases bullet speed for a limited time |
| ❤ | Life | Grants an extra life (max 5) |
| ◎ | Repair | Fully restores your hull integrity |
| ⚡ | Extra Shell | Permanently adds one simultaneous bullet |

### Field Upgrades (Defense mode)

After each wave, choose one of three random upgrades:

**Armor Plating** · **Nitro Boosters** · **Rapid Fire** · **Dual Cannon** · **Armour Piercing** · **Repair Kit**

### Ally Tanks

In Defense mode certain waves grant you an allied tank. Allies navigate the map autonomously using A* pathfinding and engage enemies without any input required.

---

## Controls

| Key | Action |
|-----|--------|
| `W A S D` / Arrow keys | Move / navigate menus |
| `Space` | Fire |
| `Enter` | Confirm / Start |
| `P` | Pause / Resume |
| `Esc` | Exit overlay / return |
| `L` | Toggle English / 中文 |

---

## UI Theme — Pipboy.Avalonia

The entire UI is themed with [**Pipboy.Avalonia**](https://github.com/NeverMorewd/Pipboy.Avalonia) — a custom Avalonia theme library that delivers a retro amber CRT terminal aesthetic.

| Component | Role in Iron Vault |
|-----------|--------------------|
| `PipboyTheme` | Global amber colour palette, button styles, panel borders |
| `PipboyWindow` | Chrome-less title bar with Win11 snap support (desktop only) |
| `CrtDisplay` | Post-processing layer over the game canvas: scanlines, scan beam, static noise, vignette |
| `SegmentedBar` | Discrete HP bar in the HUD |

The primary accent colour is set to amber `#FFA500` at startup via `PipboyThemeManager`.

---

## Architecture

Iron Vault is split into four projects:

```
IronVault.Core        — pure C#, zero UI dependency (engine, ECS, maps, localisation)
IronVault.Renderer    — Avalonia DrawingContext rendering (tanks, tiles, bullets, FX)
IronVault.Desktop     — .NET 10 desktop host (Window, audio, DI container)
IronVault.Browser     — .NET 10 WebAssembly host (single-view, audio stub)
```

All graphics are drawn with Avalonia's `DrawingContext` API — no bitmaps, no hardware-specific code — making the renderer AOT- and WASM-safe. See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the full design breakdown.

---

## Run Locally

```bash
# Pipboy.Avalonia must be cloned as a sibling to IronVault
git clone https://github.com/NeverMorewd/IronVault
git clone https://github.com/NeverMorewd/Pipboy.Avalonia

cd IronVault
dotnet run --project src/IronVault.Desktop
```

### WebAssembly (browser)

```bash
dotnet workload install wasm-tools
dotnet publish src/IronVault.Browser -c Release -o publish
# Serve publish/wwwroot/ with any static HTTP server
```

---

## License

MIT
