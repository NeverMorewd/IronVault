# Iron Vault

A retro-styled armoured combat simulation built with [Avalonia UI](https://avaloniaui.net/).
Pure vector rendering — no sprite sheets, no bitmaps — every tank, bullet, and explosion is
drawn at runtime from geometric primitives. Runs as a native Windows desktop application and
as a WebAssembly browser game deployed to GitHub Pages.

---

## Features

- **Two game modes**
  - *Classic* — clear all enemy waves to achieve victory; the number of waves scales with
    difficulty.
  - *Defense* — survive a scripted 10-wave campaign with escalating enemy tier distributions;
    complete a wave to choose a field upgrade for your tank.

- **Four enemy tiers**
  Each tier has a distinct silhouette, color scheme, armor detail, and barrel configuration so
  you can identify threats at a glance.

- **Power-ups**
  Shield, Freeze, Fortress, and Overload drop randomly from destroyed enemies.

- **Ally tanks**
  Earned by completing specific waves in Defense mode; they navigate the map autonomously using
  a rule-based AI with stuck detection.

- **Field upgrade system** (Defense mode)
  After each wave you choose one of three random upgrades: Armor Plating, Nitro Boosters, Rapid
  Fire, Dual Cannon, Armour Piercing, or Repair Kit.

- **CRT aesthetic**
  The game canvas is wrapped in a CRT post-processing layer (scanlines, animated scan beam,
  static noise, vignette) courtesy of the
  [Pipboy.Avalonia](https://github.com/NeverMorewd/Pipboy.Avalonia) library.

- **Bilingual UI**
  Toggle between English and Chinese at any time from the menu or by pressing `L`.

- **AOT and trim compatible**
  The entire codebase compiles with `IsAotCompatible=true` and `IsTrimmable=true`.

---

## Getting Started

### Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 10.0 or later |
| wasm-tools workload (browser build only) | `dotnet workload install wasm-tools` |

### Clone

```bash
# Iron Vault and Pipboy.Avalonia must be siblings so the relative ProjectReference resolves.
git clone https://github.com/<your-org>/IronVault
git clone https://github.com/NeverMorewd/Pipboy.Avalonia
```

### Run the desktop app

```bash
cd IronVault
dotnet run --project src/IronVault.Desktop
```

### Publish the browser build

```bash
cd IronVault
dotnet publish src/IronVault.Browser -c Release -o publish
# Static web files land in publish/wwwroot/
```

---

## Controls

| Key | Action |
|-----|--------|
| W / A / S / D or Arrow keys | Move tank |
| Space | Fire |
| P | Pause / Resume |
| Enter | Start / Restart |
| Escape | Return to menu |
| L | Toggle language (English / Chinese) |

---

## Project Structure

```
IronVault/
├── src/
│   ├── IronVault.Core/        # Game engine — ECS, physics, AI, wave scripts
│   ├── IronVault.Renderer/    # Avalonia DrawingContext drawables
│   ├── IronVault.Desktop/     # Windows desktop host (PipboyWindow, audio)
│   └── IronVault.Browser/     # WebAssembly browser host
├── docs/
│   └── ARCHITECTURE.md        # Code design document
└── .github/
    └── workflows/
        └── deploy-pages.yml   # GitHub Actions → GitHub Pages
```

For a detailed explanation of every layer, the ECS design, and how to extend the game, read
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

---

## Deployment

Pushing to the `main` branch triggers the GitHub Actions workflow at
`.github/workflows/deploy-pages.yml`. The workflow:

1. Checks out both `IronVault` and `Pipboy.Avalonia` as sibling directories so the relative
   `ProjectReference` path resolves correctly.
2. Installs the .NET 10 SDK and the `wasm-tools` workload.
3. Runs `dotnet publish` on `IronVault.Browser`.
4. Uploads `publish/wwwroot/` as a GitHub Pages artifact and deploys it.

Enable GitHub Pages in your repository settings (**Settings → Pages → Source: GitHub Actions**)
before the first deployment.

---

## License

MIT
