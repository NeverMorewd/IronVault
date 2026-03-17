# Iron Vault — 铁窖计划

[![Play Online](https://img.shields.io/badge/Play%20Online-WebAssembly-brightgreen?logo=googlechrome&logoColor=white)](https://nevermowd.github.io/IronVault/)
[![Built with Claude AI](https://img.shields.io/badge/Built%20with-Claude%20AI-blueviolet?logo=anthropic&logoColor=white)](https://www.anthropic.com/claude)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A retro armoured-combat game built with [Avalonia UI](https://avaloniaui.net/). Every tank, bullet, and explosion is drawn from pure vector geometry — no sprites, no bitmaps. Play it directly in your browser via WebAssembly, or run it natively on the desktop.

> **🎮 [Play now in your browser →](https://nevermowd.github.io/IronVault/)**

---

## Gameplay

You command a single tank on a tile-based battlefield. Destroy all enemy tanks to advance through waves. Enemy tanks come in four tiers — each with a distinct silhouette and color scheme — and grow progressively more dangerous as waves escalate.

### Game Modes

| Mode | Description |
|------|-------------|
| **Classic** | Survive all waves. Difficulty controls wave count and enemy composition. |
| **Defense** | A scripted 10-wave campaign. Clear a wave to pick a **Field Upgrade** for your tank. |

### Power-ups

Destroyed enemies randomly drop one of four power-ups:

| Icon | Name | Effect |
|------|------|--------|
| ★ | Shield | Your tank becomes invulnerable temporarily |
| ⏸ | Freeze | All enemies stop moving and firing |
| ⬛ | Fortress | Reinforces your base walls |
| ▲ | Overload | Increases bullet speed and penetration |

### Field Upgrades (Defense mode)

After each wave, choose one of three random upgrades:

**Armor Plating** · **Nitro Boosters** · **Rapid Fire** · **Dual Cannon** · **Armour Piercing** · **Repair Kit**

### Ally Tanks

In Defense mode certain waves grant you an allied tank. Allies navigate the map autonomously and engage enemies without any input required.

---

## Controls

| Key | Action |
|-----|--------|
| `W A S D` / Arrow keys | Move |
| `Space` | Fire |
| `P` | Pause / Resume |
| `Enter` | Start / Restart |
| `Esc` | Return to menu |
| `L` | Toggle English / 中文 |

---

## Run Locally

```bash
# Both repos must be cloned as siblings
git clone https://github.com/NeverMorewd/IronVault
git clone https://github.com/NeverMorewd/Pipboy.Avalonia

cd IronVault
dotnet run --project src/IronVault.Desktop
```

---

## License

MIT
