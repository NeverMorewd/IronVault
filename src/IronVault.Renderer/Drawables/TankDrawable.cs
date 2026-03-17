using Avalonia;
using Avalonia.Media;
using IronVault.Core.Engine.Components;
using IronVault.Core.Engine.Entities;

namespace IronVault.Renderer.Drawables;

/// <summary>
/// Draws a single tank using pure DrawingContext geometry.
/// Pixel layout: 48×48 bounding box.
/// Each team has its own visual design; all are drawn "barrel up" then
/// rotated into the actual facing direction.
///
/// Enemy tiers have distinct visual profiles:
///   Tier1 — red-orange  · basic hull  · single cannon  · 2 red warning lights
///   Tier2 — amber-gold  · extra ribs  · reinforced barrel · 2 amber lights
///   Tier3 — dark crimson · side skirts · thicker barrel   · 2 white strobes
///   Tier4 — gunmetal/black · dual barrel · hazard stripe  · 4 red lights (boss)
/// </summary>
public sealed class TankDrawable : IDrawable
{
    private readonly TankEntity _tank;

    public TankDrawable(TankEntity tank) { _tank = tank; }

    public void Draw(DrawingContext ctx, uint frameTick)
    {
        if (!_tank.IsAlive)    return;
        if (!_tank.BlinkVisible) return;

        double x  = _tank.Position.X;
        double y  = _tank.Position.Y;
        int    s  = TankEntity.Size; // 48
        double cx = x + s / 2.0;
        double cy = y + s / 2.0;

        // Rotate the whole canvas around the tank centre, then draw in "Up" orientation
        double angle = DirectionToRadians(_tank.Position.Facing);
        using (ctx.PushTransform(
            Matrix.CreateTranslation(-cx, -cy)
            * Matrix.CreateRotation(angle)
            * Matrix.CreateTranslation(cx, cy)))
        {
            switch (_tank.Team)
            {
                case TankTeam.Player: DrawPlayerTank(ctx, x, y, s, frameTick); break;
                case TankTeam.Enemy:  DrawEnemyTank (ctx, x, y, s, frameTick, _tank.Tier); break;
                default:              DrawAllyTank  (ctx, x, y, s, frameTick); break;
            }
        }

        // Shield is drawn without rotation (world-space)
        if (_tank.IsInvincible)
            DrawShield(ctx, x, y, s, frameTick);

        // Ice slide effect — shown whenever the player carries non-zero momentum
        if (_tank.IsPlayerControlled && _tank.IceMomentum > 0.05f)
            DrawIceSlide(ctx, x, y, s, _tank.IceMomentum, frameTick);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PLAYER TANK  —  sleek wedge hull · dual-rail gun · engine glow
    // Colour: gold / yellow   Silhouette: tapered front, wide rear
    // ═══════════════════════════════════════════════════════════════════════════
    private static void DrawPlayerTank(DrawingContext ctx, double x, double y, int s, uint tick)
    {
        // ── Brushes ──────────────────────────────────────────────────────────
        var trackBody  = new SolidColorBrush(Color.FromRgb(0x32, 0x32, 0x32));
        var trackDark  = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x18));
        var wheel      = new SolidColorBrush(Color.FromRgb(0x78, 0x78, 0x78));
        var hull       = DrawColors.PlayerBrush;                               // bright yellow
        var hullLight  = new SolidColorBrush(Color.FromRgb(0xFF, 0xF0, 0x50));// lighter yellow
        var hullShade  = new SolidColorBrush(Color.FromRgb(0xBB, 0x8A, 0x00));// dark gold
        var turret     = new SolidColorBrush(Color.FromRgb(0xCC, 0x9A, 0x00));// darker gold
        var turretRing = new SolidColorBrush(Color.FromRgb(0xAA, 0x78, 0x00));
        var barrel     = new SolidColorBrush(Color.FromRgb(0x28, 0x28, 0x28));
        var barrelTip  = new SolidColorBrush(Color.FromRgb(0x48, 0x48, 0x48));
        var glint      = new SolidColorBrush(Color.FromArgb(110, 255, 255, 255));

        int treadOff = (int)(tick / 4 % 8);

        // ── Tracks ───────────────────────────────────────────────────────────
        ctx.FillRectangle(trackBody, new Rect(x,      y + 3, 10, s - 6));
        ctx.FillRectangle(trackBody, new Rect(x + 38, y + 3, 10, s - 6));

        for (int i = 0; i < 6; i++)
        {
            double ty = y + 3 + (i * 8 + treadOff) % (s - 6);
            ctx.FillRectangle(trackDark, new Rect(x,      ty, 10, 2));
            ctx.FillRectangle(trackDark, new Rect(x + 38, ty, 10, 2));
        }

        // Road-wheels (4 per side — gives a tank "suspension" feel)
        for (int i = 0; i < 4; i++)
        {
            double wy = y + 5 + i * 10;
            ctx.FillRectangle(wheel, new Rect(x + 1,      wy, 8, 4));
            ctx.FillRectangle(wheel, new Rect(x + 39, wy, 8, 4));
        }

        // ── Hull (tapered: narrow front → wide rear) ─────────────────────────
        // Front wedge section
        ctx.FillRectangle(hull,      new Rect(x + 14, y + 4,  20, 8));
        // Mid-front
        ctx.FillRectangle(hull,      new Rect(x + 12, y + 12, 24, 10));
        // Main body
        ctx.FillRectangle(hull,      new Rect(x + 10, y + 22, 28, 14));
        // Rear plate (shaded)
        ctx.FillRectangle(hullShade, new Rect(x + 10, y + 36, 28,  8));

        // Accent stripe (bright band at mid-hull join)
        ctx.FillRectangle(hullLight, new Rect(x + 12, y + 22, 24, 2));

        // Side armour notches
        ctx.FillRectangle(hullShade, new Rect(x + 10, y + 27, 3, 7));
        ctx.FillRectangle(hullShade, new Rect(x + 35, y + 27, 3, 7));

        // ── Turret (slightly offset toward front) ────────────────────────────
        ctx.FillRectangle(turret,     new Rect(x + 14, y + 14, 20, 20));
        ctx.FillRectangle(turretRing, new Rect(x + 17, y + 17, 14, 14));
        ctx.FillRectangle(turret,     new Rect(x + 19, y + 19, 10, 10));

        // ── Dual-rail barrel ─────────────────────────────────────────────────
        ctx.FillRectangle(barrel,    new Rect(x + 20, y,      3, 18)); // left rail
        ctx.FillRectangle(barrel,    new Rect(x + 25, y,      3, 18)); // right rail
        ctx.FillRectangle(barrelTip, new Rect(x + 19, y,     10,  4)); // muzzle tip plate
        ctx.FillRectangle(barrelTip, new Rect(x + 20, y + 15, 8,  3)); // breech connector

        // ── Dome glint ───────────────────────────────────────────────────────
        ctx.FillRectangle(glint, new Rect(x + 17, y + 17, 5, 5));

        // ── Engine exhaust glow (rear, animated heartbeat) ───────────────────
        bool eng = (tick / 6 % 2 == 0);
        ctx.FillRectangle(
            new SolidColorBrush(Color.FromArgb(eng ? (byte)160 : (byte)70, 255, 140, 0)),
            new Rect(x + 15, y + 40, 18, 4));
        if (eng)
            ctx.FillRectangle(
                new SolidColorBrush(Color.FromArgb(60, 255, 220, 0)),
                new Rect(x + 12, y + 42, 24, 2));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ENEMY TANK  —  tier-differentiated visuals
    //
    //  Tier1  red-orange  · basic boxy hull   · single fat barrel  · 2 red blink lights
    //  Tier2  amber-gold  · reinforced hull   · barrel ribs ×3     · 2 amber lights + roof stripe
    //  Tier3  dark crimson· side skirt armour · extra-thick barrel · 2 white strobes + shoulder bolts
    //  Tier4  gunmetal    · black/red scheme  · dual cannon ports  · 4-corner red lights + hazard band
    // ═══════════════════════════════════════════════════════════════════════════
    private static void DrawEnemyTank(DrawingContext ctx, double x, double y, int s, uint tick, TankTier tier)
    {
        int treadOff = (int)(tick / 4 % 8);

        switch (tier)
        {
            case TankTier.Tier2: DrawEnemyTier2(ctx, x, y, s, tick, treadOff); break;
            case TankTier.Tier3: DrawEnemyTier3(ctx, x, y, s, tick, treadOff); break;
            case TankTier.Tier4: DrawEnemyTier4(ctx, x, y, s, tick, treadOff); break;
            default:             DrawEnemyTier1(ctx, x, y, s, tick, treadOff); break;
        }
    }

    // ── Tier 1: basic red-orange ─────────────────────────────────────────────
    private static void DrawEnemyTier1(DrawingContext ctx, double x, double y, int s, uint tick, int treadOff)
    {
        var trackBody  = new SolidColorBrush(Color.FromRgb(0x72, 0x32, 0x10));
        var trackDark  = new SolidColorBrush(Color.FromRgb(0x48, 0x1A, 0x06));
        var hull       = DrawColors.EnemyBrush;                                 // red-orange
        var hullBrow   = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0x00));
        var hullShade  = new SolidColorBrush(Color.FromRgb(0xAA, 0x22, 0x00));
        var turret     = new SolidColorBrush(Color.FromRgb(0xBB, 0x28, 0x00));
        var turretDark = new SolidColorBrush(Color.FromRgb(0x88, 0x14, 0x00));
        var barrel     = new SolidColorBrush(Color.FromRgb(0x30, 0x10, 0x00));
        var rivet      = new SolidColorBrush(Color.FromRgb(0x55, 0x20, 0x00));
        var glint      = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));

        DrawEnemyTracks(ctx, x, y, s, trackBody, trackDark, treadOff, 12);
        DrawEnemyHull(ctx, x, y, hull, hullBrow, hullShade, rivet, turret, turretDark, barrel, rivet, glint);

        // Warning lights — 2 blinking red
        bool  on     = (tick / 5 % 2 == 0);
        byte  core   = on ? (byte)255 : (byte)50;
        byte  halo   = on ? (byte)100 : (byte)20;
        DrawLight(ctx, x + 10, y + 5, Color.FromArgb(halo, 255, 0, 0), Color.FromArgb(core, 255, 0, 0));
        DrawLight(ctx, x + 33, y + 5, Color.FromArgb(halo, 255, 0, 0), Color.FromArgb(core, 255, 0, 0));
    }

    // ── Tier 2: amber-gold, reinforced ───────────────────────────────────────
    private static void DrawEnemyTier2(DrawingContext ctx, double x, double y, int s, uint tick, int treadOff)
    {
        var trackBody  = new SolidColorBrush(Color.FromRgb(0x60, 0x38, 0x00));
        var trackDark  = new SolidColorBrush(Color.FromRgb(0x38, 0x20, 0x00));
        var hull       = new SolidColorBrush(Color.FromRgb(0xCC, 0x70, 0x00));  // amber
        var hullBrow   = new SolidColorBrush(Color.FromRgb(0xFF, 0xA0, 0x00));  // bright amber
        var hullShade  = new SolidColorBrush(Color.FromRgb(0x88, 0x44, 0x00));
        var turret     = new SolidColorBrush(Color.FromRgb(0xAA, 0x60, 0x00));
        var turretDark = new SolidColorBrush(Color.FromRgb(0x77, 0x40, 0x00));
        var barrel     = new SolidColorBrush(Color.FromRgb(0x28, 0x18, 0x00));
        var rivet      = new SolidColorBrush(Color.FromRgb(0x66, 0x44, 0x00));
        var glint      = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));

        DrawEnemyTracks(ctx, x, y, s, trackBody, trackDark, treadOff, 12);
        DrawEnemyHull(ctx, x, y, hull, hullBrow, hullShade, rivet, turret, turretDark, barrel, rivet, glint);

        // Extra: reinforcement rib across mid-hull
        ctx.FillRectangle(hullShade, new Rect(x + 12, y + 22, 24, 2));
        // Barrel ribs ×3 (more than Tier1)
        ctx.FillRectangle(rivet, new Rect(x + 20, y + 4,  8, 2));
        ctx.FillRectangle(rivet, new Rect(x + 20, y + 8,  8, 2));
        ctx.FillRectangle(rivet, new Rect(x + 20, y + 12, 8, 2));
        // Roof stripe
        ctx.FillRectangle(hullBrow, new Rect(x + 16, y + 14, 16, 2));

        // Lights — 2 amber
        bool  on   = (tick / 5 % 2 == 0);
        byte  core = on ? (byte)255 : (byte)60;
        byte  halo = on ? (byte)110 : (byte)25;
        DrawLight(ctx, x + 10, y + 5, Color.FromArgb(halo, 255, 140, 0), Color.FromArgb(core, 255, 160, 0));
        DrawLight(ctx, x + 33, y + 5, Color.FromArgb(halo, 255, 140, 0), Color.FromArgb(core, 255, 160, 0));
    }

    // ── Tier 3: dark crimson, heavy ───────────────────────────────────────────
    private static void DrawEnemyTier3(DrawingContext ctx, double x, double y, int s, uint tick, int treadOff)
    {
        var trackBody  = new SolidColorBrush(Color.FromRgb(0x30, 0x10, 0x10));
        var trackDark  = new SolidColorBrush(Color.FromRgb(0x1A, 0x08, 0x08));
        var hull       = new SolidColorBrush(Color.FromRgb(0x99, 0x00, 0x11));  // dark crimson
        var hullBrow   = new SolidColorBrush(Color.FromRgb(0xCC, 0x00, 0x22));  // brighter crimson
        var hullShade  = new SolidColorBrush(Color.FromRgb(0x55, 0x00, 0x0A));
        var turret     = new SolidColorBrush(Color.FromRgb(0x88, 0x00, 0x11));
        var turretDark = new SolidColorBrush(Color.FromRgb(0x55, 0x00, 0x08));
        var barrel     = new SolidColorBrush(Color.FromRgb(0x20, 0x08, 0x08));
        var rivet      = new SolidColorBrush(Color.FromRgb(0x44, 0x10, 0x10));
        var skirt      = new SolidColorBrush(Color.FromRgb(0x66, 0x00, 0x0A));
        var glint      = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));

        DrawEnemyTracks(ctx, x, y, s, trackBody, trackDark, treadOff, 12);
        DrawEnemyHull(ctx, x, y, hull, hullBrow, hullShade, rivet, turret, turretDark, barrel, rivet, glint);

        // Side skirt armour plates (over the tracks)
        ctx.FillRectangle(skirt, new Rect(x,      y + 14, 12, 22));
        ctx.FillRectangle(skirt, new Rect(x + 36, y + 14, 12, 22));
        // Skirt bolts
        ctx.FillRectangle(rivet, new Rect(x + 1,  y + 18, 3, 3));
        ctx.FillRectangle(rivet, new Rect(x + 1,  y + 27, 3, 3));
        ctx.FillRectangle(rivet, new Rect(x + 44, y + 18, 3, 3));
        ctx.FillRectangle(rivet, new Rect(x + 44, y + 27, 3, 3));

        // Thick barrel (7 wide instead of 6)
        ctx.FillRectangle(barrel, new Rect(x + 20, y,     8, 18));  // thicker tube
        ctx.FillRectangle(turret, new Rect(x + 19, y + 14, 10, 4)); // wider mantlet

        // Shoulder bolts on turret ring
        ctx.FillRectangle(rivet, new Rect(x + 14, y + 20, 3, 3));
        ctx.FillRectangle(rivet, new Rect(x + 31, y + 20, 3, 3));

        // Lights — 2 white strobe (brighter)
        bool  on   = (tick / 4 % 2 == 0);
        byte  core = on ? (byte)255 : (byte)80;
        byte  halo = on ? (byte)120 : (byte)30;
        DrawLight(ctx, x + 10, y + 5, Color.FromArgb(halo, 240, 240, 255), Color.FromArgb(core, 255, 255, 255));
        DrawLight(ctx, x + 33, y + 5, Color.FromArgb(halo, 240, 240, 255), Color.FromArgb(core, 255, 255, 255));
    }

    // ── Tier 4: gunmetal boss, dual cannon ───────────────────────────────────
    private static void DrawEnemyTier4(DrawingContext ctx, double x, double y, int s, uint tick, int treadOff)
    {
        var trackBody  = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22));
        var trackDark  = new SolidColorBrush(Color.FromRgb(0x10, 0x10, 0x10));
        var hull       = new SolidColorBrush(Color.FromRgb(0x28, 0x28, 0x28));  // near-black
        var hullBrow   = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40));  // dark grey
        var hullShade  = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14));
        var hullAccent = new SolidColorBrush(Color.FromRgb(0xCC, 0x00, 0x00));  // deep red accent
        var turret     = new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x30));
        var turretDark = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x18));
        var barrel     = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x18));
        var rivet      = new SolidColorBrush(Color.FromRgb(0x50, 0x50, 0x50));
        var glint      = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));

        DrawEnemyTracks(ctx, x, y, s, trackBody, trackDark, treadOff, 12);

        // ── Hull (same shape, gunmetal colours) ──────────────────────────────
        ctx.FillRectangle(hullBrow,  new Rect(x + 12, y + 4,  24, 6));
        ctx.FillRectangle(hull,      new Rect(x + 12, y + 10, 24, 28));
        ctx.FillRectangle(hullShade, new Rect(x + 12, y + 38, 24,  6));
        ctx.FillRectangle(hullShade, new Rect(x + 12, y + 10, 24,  2));

        // Red accent stripe across mid-hull (boss identifier)
        ctx.FillRectangle(hullAccent, new Rect(x + 12, y + 20, 24, 3));

        // Rivets
        ctx.FillRectangle(rivet, new Rect(x + 12, y + 11, 3, 3));
        ctx.FillRectangle(rivet, new Rect(x + 33, y + 11, 3, 3));
        ctx.FillRectangle(rivet, new Rect(x + 12, y + 35, 3, 3));
        ctx.FillRectangle(rivet, new Rect(x + 33, y + 35, 3, 3));

        // ── Heavy turret ─────────────────────────────────────────────────────
        ctx.FillRectangle(turret,     new Rect(x + 13, y + 13, 22, 22));
        ctx.FillRectangle(turretDark, new Rect(x + 16, y + 16, 16, 16));
        ctx.FillRectangle(turret,     new Rect(x + 18, y + 18, 12, 12));
        ctx.FillRectangle(turretDark, new Rect(x + 19, y + 19, 10, 2));
        ctx.FillRectangle(turretDark, new Rect(x + 19, y + 19, 2,  10));

        // ── Dual cannon (two side-by-side barrels) ───────────────────────────
        ctx.FillRectangle(barrel,     new Rect(x + 18, y,      5, 18));  // left barrel
        ctx.FillRectangle(barrel,     new Rect(x + 25, y,      5, 18));  // right barrel
        ctx.FillRectangle(hullAccent, new Rect(x + 18, y + 5,  5, 2));   // left band
        ctx.FillRectangle(hullAccent, new Rect(x + 25, y + 5,  5, 2));   // right band
        ctx.FillRectangle(hullAccent, new Rect(x + 18, y + 10, 5, 2));   // left band 2
        ctx.FillRectangle(hullAccent, new Rect(x + 25, y + 10, 5, 2));   // right band 2
        ctx.FillRectangle(turret,     new Rect(x + 17, y + 13, 14, 4));  // mantlet

        // Hazard chevron on rear plate (diagonal warning stripe)
        for (int i = 0; i < 4; i++)
            ctx.FillRectangle(hullAccent,
                new Rect(x + 13 + i * 6, y + 38, 3, 6));

        // ── Dome glint ───────────────────────────────────────────────────────
        ctx.FillRectangle(glint, new Rect(x + 18, y + 17, 5, 5));

        // ── Four corner red lights (aggressive identifier) ────────────────────
        bool on   = (tick / 3 % 2 == 0);    // faster blink rate for boss
        byte core = on ? (byte)255 : (byte)40;
        byte halo = on ? (byte)120 : (byte)15;
        DrawLight(ctx, x + 10, y + 5,  Color.FromArgb(halo, 255, 0, 0), Color.FromArgb(core, 255, 0, 0));
        DrawLight(ctx, x + 33, y + 5,  Color.FromArgb(halo, 255, 0, 0), Color.FromArgb(core, 255, 0, 0));
        DrawLight(ctx, x + 10, y + 36, Color.FromArgb(halo, 255, 0, 0), Color.FromArgb(core, 255, 0, 0));
        DrawLight(ctx, x + 33, y + 36, Color.FromArgb(halo, 255, 0, 0), Color.FromArgb(core, 255, 0, 0));
    }

    // ── Shared helpers for enemy tank geometry ────────────────────────────────

    private static void DrawEnemyTracks(DrawingContext ctx, double x, double y, int s,
        SolidColorBrush body, SolidColorBrush dark, int treadOff, int trackW)
    {
        ctx.FillRectangle(body, new Rect(x,           y + 2, trackW,     s - 4));
        ctx.FillRectangle(body, new Rect(x + s - trackW, y + 2, trackW, s - 4));

        for (int i = 0; i < 6; i++)
        {
            double ty = y + 2 + (i * 8 + treadOff) % (s - 4);
            ctx.FillRectangle(dark, new Rect(x,           ty, trackW, 2));
            ctx.FillRectangle(dark, new Rect(x + s - trackW, ty, trackW, 2));
        }
    }

    private static void DrawEnemyHull(DrawingContext ctx, double x, double y,
        SolidColorBrush hull, SolidColorBrush hullBrow, SolidColorBrush hullShade,
        SolidColorBrush rivet, SolidColorBrush turret, SolidColorBrush turretDark,
        SolidColorBrush barrel, SolidColorBrush ribColor, SolidColorBrush glint)
    {
        // Hull
        ctx.FillRectangle(hullBrow,  new Rect(x + 12, y + 4,  24, 6));
        ctx.FillRectangle(hull,      new Rect(x + 12, y + 10, 24, 28));
        ctx.FillRectangle(hullShade, new Rect(x + 12, y + 38, 24,  6));
        ctx.FillRectangle(hullShade, new Rect(x + 12, y + 10, 24,  2));

        // Corner rivets
        ctx.FillRectangle(rivet, new Rect(x + 12, y + 11, 3, 3));
        ctx.FillRectangle(rivet, new Rect(x + 33, y + 11, 3, 3));
        ctx.FillRectangle(rivet, new Rect(x + 12, y + 35, 3, 3));
        ctx.FillRectangle(rivet, new Rect(x + 33, y + 35, 3, 3));

        // Turret
        ctx.FillRectangle(turret,     new Rect(x + 14, y + 14, 20, 20));
        ctx.FillRectangle(turretDark, new Rect(x + 17, y + 17, 14, 14));
        ctx.FillRectangle(turret,     new Rect(x + 18, y + 18, 12, 12));
        ctx.FillRectangle(turretDark, new Rect(x + 19, y + 19, 10,  2));
        ctx.FillRectangle(turretDark, new Rect(x + 19, y + 19,  2, 10));

        // Single fat barrel
        ctx.FillRectangle(barrel,  new Rect(x + 21, y,      6, 18));
        ctx.FillRectangle(turret,  new Rect(x + 20, y + 14, 8,  4));
        // Barrel ribs ×2
        ctx.FillRectangle(ribColor, new Rect(x + 20, y + 6,  8, 2));
        ctx.FillRectangle(ribColor, new Rect(x + 20, y + 11, 8, 2));

        // Dome glint
        ctx.FillRectangle(glint, new Rect(x + 17, y + 17, 5, 5));
    }

    private static void DrawLight(DrawingContext ctx, double lx, double ly,
        Color haloColor, Color coreColor)
    {
        ctx.FillRectangle(new SolidColorBrush(haloColor), new Rect(lx,     ly,     7, 7));
        ctx.FillRectangle(new SolidColorBrush(coreColor), new Rect(lx + 2, ly + 1, 4, 4));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ALLY TANK  —  same sleek wedge as player · cyan palette · "+" team emblem
    // Colour: electric cyan   Silhouette: identical to player (friendly recognition)
    // ═══════════════════════════════════════════════════════════════════════════
    private static void DrawAllyTank(DrawingContext ctx, double x, double y, int s, uint tick)
    {
        // ── Brushes ──────────────────────────────────────────────────────────
        var trackBody  = new SolidColorBrush(Color.FromRgb(0x00, 0x28, 0x38));
        var trackDark  = new SolidColorBrush(Color.FromRgb(0x00, 0x14, 0x1E));
        var wheel      = new SolidColorBrush(Color.FromRgb(0x30, 0x90, 0xAA));
        var hull       = DrawColors.AllyBrush;                                 // cyan
        var hullLight  = new SolidColorBrush(Color.FromRgb(0x60, 0xEE, 0xFF));
        var hullShade  = new SolidColorBrush(Color.FromRgb(0x00, 0x88, 0xAA));
        var turret     = new SolidColorBrush(Color.FromRgb(0x00, 0x80, 0xAA));
        var turretRing = new SolidColorBrush(Color.FromRgb(0x00, 0x60, 0x88));
        var barrel     = new SolidColorBrush(Color.FromRgb(0x10, 0x28, 0x30));
        var barrelTip  = new SolidColorBrush(Color.FromRgb(0x20, 0x44, 0x50));
        var emblem     = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255));
        var glint      = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));

        int treadOff = (int)(tick / 4 % 8);

        // ── Tracks (same 10 px as player) ────────────────────────────────────
        ctx.FillRectangle(trackBody, new Rect(x,      y + 3, 10, s - 6));
        ctx.FillRectangle(trackBody, new Rect(x + 38, y + 3, 10, s - 6));

        for (int i = 0; i < 6; i++)
        {
            double ty = y + 3 + (i * 8 + treadOff) % (s - 6);
            ctx.FillRectangle(trackDark, new Rect(x,      ty, 10, 2));
            ctx.FillRectangle(trackDark, new Rect(x + 38, ty, 10, 2));
        }

        for (int i = 0; i < 4; i++)
        {
            double wy = y + 5 + i * 10;
            ctx.FillRectangle(wheel, new Rect(x + 1,  wy, 8, 4));
            ctx.FillRectangle(wheel, new Rect(x + 39, wy, 8, 4));
        }

        // ── Hull (same tapered shape as player) ──────────────────────────────
        ctx.FillRectangle(hull,      new Rect(x + 14, y + 4,  20, 8));
        ctx.FillRectangle(hull,      new Rect(x + 12, y + 12, 24, 10));
        ctx.FillRectangle(hull,      new Rect(x + 10, y + 22, 28, 14));
        ctx.FillRectangle(hullShade, new Rect(x + 10, y + 36, 28,  8));
        ctx.FillRectangle(hullLight, new Rect(x + 12, y + 22, 24,  2));
        ctx.FillRectangle(hullShade, new Rect(x + 10, y + 27,  3,  7));
        ctx.FillRectangle(hullShade, new Rect(x + 35, y + 27,  3,  7));

        // ── Turret ───────────────────────────────────────────────────────────
        ctx.FillRectangle(turret,     new Rect(x + 14, y + 14, 20, 20));
        ctx.FillRectangle(turretRing, new Rect(x + 17, y + 17, 14, 14));
        ctx.FillRectangle(turret,     new Rect(x + 19, y + 19, 10, 10));

        // ── Team emblem: white "+" cross on turret ────────────────────────────
        ctx.FillRectangle(emblem, new Rect(x + 22, y + 16,  4, 12)); // vertical bar
        ctx.FillRectangle(emblem, new Rect(x + 16, y + 21, 16,  4)); // horizontal bar

        // ── Dual-rail barrel (same as player) ────────────────────────────────
        ctx.FillRectangle(barrel,    new Rect(x + 20, y,      3, 18));
        ctx.FillRectangle(barrel,    new Rect(x + 25, y,      3, 18));
        ctx.FillRectangle(barrelTip, new Rect(x + 19, y,     10,  4));
        ctx.FillRectangle(barrelTip, new Rect(x + 20, y + 15, 8,  3));

        // ── Dome glint ───────────────────────────────────────────────────────
        ctx.FillRectangle(glint, new Rect(x + 17, y + 17, 5, 5));

        // ── Forward sensor scan light (ally identifier, pulses slowly) ───────
        bool scan = (tick / 10 % 2 == 0);
        ctx.FillRectangle(
            new SolidColorBrush(Color.FromArgb(scan ? (byte)150 : (byte)50, 0, 220, 255)),
            new Rect(x + 16, y + 8, 16, 3));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RESPAWN SHIELD  —  dual counter-rotating dashed diamonds + corner sparks
    // ═══════════════════════════════════════════════════════════════════════════
    private static void DrawShield(DrawingContext ctx, double x, double y, int s, uint tick)
    {
        double cx = x + s / 2.0;
        double cy = y + s / 2.0;

        bool pulse = (tick / 4 % 2 == 0);

        double r1 =  s / 2.0 + 9;
        double r2 =  s / 2.0 + 3;
        double a1 =  tick * 0.045;
        double a2 = -tick * 0.070;

        // 1. Soft field glow
        ctx.FillRectangle(
            new SolidColorBrush(Color.FromArgb(18, 0, 180, 255)),
            new Rect(x - 8, y - 8, s + 16, s + 16));

        // 2. Outer dashed diamond
        DrawDashedDiamond(ctx, cx, cy, r1, a1,
            Color.FromArgb(pulse ? (byte)210 : (byte)80, 0, 200, 255), 2);

        // 3. Inner dashed diamond (counter-spin)
        DrawDashedDiamond(ctx, cx, cy, r2, a2,
            Color.FromArgb(pulse ? (byte)130 : (byte)45, 140, 230, 255), 1.5);

        // 4. Cross sparks at outer vertices
        byte  sparkAlpha = pulse ? (byte)255 : (byte)140;
        var   sparkBrush = new SolidColorBrush(Color.FromArgb(sparkAlpha, 210, 245, 255));
        double arm = pulse ? 4.0 : 2.5;

        for (int i = 0; i < 4; i++)
        {
            double ang = a1 + i * Math.PI / 2;
            double px  = cx + r1 * Math.Cos(ang);
            double py  = cy + r1 * Math.Sin(ang);
            ctx.FillRectangle(sparkBrush, new Rect(px - 1,   py - arm, 2,       arm * 2));
            ctx.FillRectangle(sparkBrush, new Rect(px - arm, py - 1,   arm * 2, 2));
        }
    }

    private static void DrawDashedDiamond(DrawingContext ctx,
        double cx, double cy, double r, double angle,
        Color color, double thickness)
    {
        var pen = new Pen(new SolidColorBrush(color), thickness);

        var pts = new Point[4];
        for (int i = 0; i < 4; i++)
            pts[i] = new Point(
                cx + r * Math.Cos(angle + i * Math.PI / 2),
                cy + r * Math.Sin(angle + i * Math.PI / 2));

        ReadOnlySpan<(double t0, double t1)> dashes =
        [
            (0.06, 0.38),
            (0.45, 0.72),
            (0.79, 0.94),
        ];

        for (int side = 0; side < 4; side++)
        {
            var p1 = pts[side];
            var p2 = pts[(side + 1) % 4];
            foreach (var (t0, t1) in dashes)
            {
                ctx.DrawLine(pen,
                    new Point(p1.X + (p2.X - p1.X) * t0, p1.Y + (p2.Y - p1.Y) * t0),
                    new Point(p1.X + (p2.X - p1.X) * t1, p1.Y + (p2.Y - p1.Y) * t1));
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ICE SLIDE EFFECT  —  cyan glow border + corner crystal sparks
    // ═══════════════════════════════════════════════════════════════════════════
    private static void DrawIceSlide(DrawingContext ctx,
        double x, double y, int s, float momentum, uint tick)
    {
        byte borderAlpha = (byte)(momentum * 170);
        byte sparkAlpha  = (byte)(momentum * 220);

        // 1. Thin cyan glow border around the tank bounding box
        var borderPen = new Pen(
            new SolidColorBrush(Color.FromArgb(borderAlpha, 0, 210, 255)), 1.5);
        double pad = 4;
        ctx.DrawLine(borderPen, new Point(x - pad,     y - pad),     new Point(x + s + pad, y - pad));
        ctx.DrawLine(borderPen, new Point(x - pad,     y + s + pad), new Point(x + s + pad, y + s + pad));
        ctx.DrawLine(borderPen, new Point(x - pad,     y - pad),     new Point(x - pad,     y + s + pad));
        ctx.DrawLine(borderPen, new Point(x + s + pad, y - pad),     new Point(x + s + pad, y + s + pad));

        // 2. Inner soft field tint (very translucent)
        ctx.FillRectangle(
            new SolidColorBrush(Color.FromArgb((byte)(momentum * 28), 0, 180, 255)),
            new Rect(x - pad, y - pad, s + pad * 2, s + pad * 2));

        // 3. Corner ice-crystal sparks (blink at 8-tick intervals)
        bool blink = (tick / 8 % 2 == 0);
        if (blink)
        {
            var spark = new SolidColorBrush(Color.FromArgb(sparkAlpha, 200, 240, 255));
            double arm = 2.5 + momentum * 3.0;
            DrawIceSpark(ctx, spark, x - pad,     y - pad,     arm);
            DrawIceSpark(ctx, spark, x + s + pad, y - pad,     arm);
            DrawIceSpark(ctx, spark, x - pad,     y + s + pad, arm);
            DrawIceSpark(ctx, spark, x + s + pad, y + s + pad, arm);
        }

        // 4. Momentum bar — tiny horizontal strip at the bottom of the tank
        double barW = (s - 8) * momentum;
        ctx.FillRectangle(
            new SolidColorBrush(Color.FromArgb(160, 0, 200, 255)),
            new Rect(x + 4, y + s + pad + 2, barW, 2));
    }

    /// <summary>Draws a '+' cross spark at (cx, cy) with the given arm length.</summary>
    private static void DrawIceSpark(DrawingContext ctx, SolidColorBrush brush,
        double cx, double cy, double arm)
    {
        ctx.FillRectangle(brush, new Rect(cx - 1,   cy - arm, 2,       arm * 2));
        ctx.FillRectangle(brush, new Rect(cx - arm, cy - 1,   arm * 2, 2));
    }

    private static double DirectionToRadians(Direction dir) => dir switch
    {
        Direction.Up    =>  0,
        Direction.Right =>  Math.PI / 2.0,
        Direction.Down  =>  Math.PI,
        Direction.Left  => -Math.PI / 2.0,
        _               =>  0,
    };
}
