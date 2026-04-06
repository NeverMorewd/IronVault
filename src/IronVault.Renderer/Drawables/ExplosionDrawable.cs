using Avalonia;
using Avalonia.Media;
using IronVault.Core.Engine.Entities;

namespace IronVault.Renderer.Drawables;

public sealed class ExplosionDrawable : IDrawable
{
    private readonly ExplosionEntity _explosion;

    public ExplosionDrawable(ExplosionEntity explosion) { _explosion = explosion; }

    public void Draw(DrawingContext ctx, uint frameTick)
    {
        if (_explosion.IsFinished) return;

        int    frame = _explosion.Frame;
        int    maxF  = ExplosionEntity.MaxFrames;
        double t     = frame / (double)maxF; // 0 → 1 progress

        switch (_explosion.Type)
        {
            case ExplosionType.Clash:
                DrawClash(ctx, frame, maxF, t);
                break;
            default:
                DrawNormal(ctx, frame, maxF, t);
                break;
        }
    }

    // ── Normal (orange fire) ──────────────────────────────────────────────────

    private void DrawNormal(DrawingContext ctx, int frame, int maxF, double t)
    {
        // Size 2 (tank kill) uses a larger base for more impact
        double baseSize = _explosion.Size switch { 0 => 12, 1 => 24, _ => 64 };
        double size     = GrowShrink(baseSize, frame, maxF);

        // Fixed center (stable across frames — used for debris/shockwave)
        double fx = _explosion.X + baseSize / 2;
        double fy = _explosion.Y + baseSize / 2;

        // Dynamic center (follows size pulse — used for main flame body)
        double cx = _explosion.X + size / 2;
        double cy = _explosion.Y + size / 2;

        byte outerAlpha = AlphaByte(255, t);
        byte innerAlpha = AlphaByte(200, t);

        // Outer ring – orange
        ctx.FillRectangle(
            new SolidColorBrush(Color.FromArgb(outerAlpha, 255, 100, 0)),
            new Rect(cx - size / 2, cy - size / 2, size, size));

        // Inner glow – yellow
        double innerSize = size * 0.6;
        ctx.FillRectangle(
            new SolidColorBrush(Color.FromArgb(innerAlpha, 255, 220, 0)),
            new Rect(cx - innerSize / 2, cy - innerSize / 2, innerSize, innerSize));

        // White core flash – first two frames only
        if (frame <= 1)
        {
            double coreSize = size * 0.25;
            ctx.FillRectangle(Brushes.White,
                new Rect(cx - coreSize / 2, cy - coreSize / 2, coreSize, coreSize));
        }

        // ── Enhanced effects for large (tank kill) explosions ─────────────────
        if (_explosion.Size >= 2)
        {
            // Shockwave ring: expands rapidly beyond the main explosion
            double shockR = baseSize * (0.55 + t * 1.5);
            byte   shockA = (byte)(210 * Math.Clamp(1.0 - t * 1.6, 0, 1));
            if (shockA > 0)
            {
                var shockPen = new Pen(new SolidColorBrush(Color.FromArgb(shockA, 255, 200, 60)), 3);
                ctx.DrawRectangle(null, shockPen,
                    new Rect(fx - shockR / 2, fy - shockR / 2, shockR, shockR));

                // Second thinner outer ring for double-wave effect
                double shock2R = shockR * 1.35;
                byte   shock2A = (byte)(shockA * 0.5);
                var    shock2Pen = new Pen(new SolidColorBrush(Color.FromArgb(shock2A, 255, 160, 0)), 1.5);
                ctx.DrawRectangle(null, shock2Pen,
                    new Rect(fx - shock2R / 2, fy - shock2R / 2, shock2R, shock2R));
            }

            // Debris particles: 8 squares expanding outward at fixed angles
            double spread      = baseSize * (0.25 + t * 1.3);
            byte   debrisAlpha = (byte)(230 * Math.Clamp(1.0 - t * 1.5, 0, 1));
            if (debrisAlpha > 0)
            {
                double debrisSz  = Math.Max(2, 5.5 * (1 - t * 1.4));
                var    debrisBrush = new SolidColorBrush(Color.FromArgb(debrisAlpha, 255, 155, 0));
                double[] angles = [0, 45, 90, 135, 180, 225, 270, 315];
                foreach (var deg in angles)
                {
                    double rad = deg * Math.PI / 180.0;
                    double dx  = Math.Cos(rad) * spread;
                    double dy  = Math.Sin(rad) * spread;
                    ctx.FillRectangle(debrisBrush,
                        new Rect(fx + dx - debrisSz / 2, fy + dy - debrisSz / 2, debrisSz, debrisSz));
                }
            }

            // Secondary re-ignition flash at frame 3 (delayed pop)
            if (frame == 3)
            {
                double flashSz = size * 0.40;
                ctx.FillRectangle(
                    new SolidColorBrush(Color.FromArgb(150, 255, 240, 180)),
                    new Rect(fx - flashSz / 2, fy - flashSz / 2, flashSz, flashSz));
            }
        }
    }

    // ── Clash (electric cyan spark) ───────────────────────────────────────────
    //
    // Rendered as three overlapping effects:
    //   1. Square "ring" in electric blue — grows then shrinks
    //   2. Bright cyan inner square
    //   3. Pixel-art cross rays (+) extending outward — gives the "electric arc" look
    //
    // Distinct from the orange fire so players immediately recognise "bullets cancelled".

    private void DrawClash(DrawingContext ctx, int frame, int maxF, double t)
    {
        // Clash is always medium-ish (baseSize 20)
        const double baseSize = 20;
        double size = GrowShrink(baseSize, frame, maxF);

        double cx = _explosion.X + size / 2;
        double cy = _explosion.Y + size / 2;

        // Alpha stays high longer (spark is brief but intense)
        byte ringAlpha  = AlphaByte(240, t * 0.75);
        byte innerAlpha = AlphaByte(200, t * 0.80);
        byte rayAlpha   = AlphaByte(180, t * 0.70);

        // ── 1. Outer electric ring (deep blue) ────────────────────────────
        ctx.FillRectangle(
            new SolidColorBrush(Color.FromArgb(ringAlpha, 30, 80, 255)),
            new Rect(cx - size / 2, cy - size / 2, size, size));

        // ── 2. Inner cyan square ──────────────────────────────────────────
        double innerSize = size * 0.55;
        ctx.FillRectangle(
            new SolidColorBrush(Color.FromArgb(innerAlpha, 0, 220, 255)),
            new Rect(cx - innerSize / 2, cy - innerSize / 2, innerSize, innerSize));

        // ── 3. Cross rays ─────────────────────────────────────────────────
        double rayLen = size * 1.1;                 // extends slightly beyond outer ring
        double rayW   = Math.Max(2, size * 0.12);  // pixel-art width
        var    rayBrush = new SolidColorBrush(Color.FromArgb(rayAlpha, 150, 230, 255));

        // Horizontal arm
        ctx.FillRectangle(rayBrush,
            new Rect(cx - rayLen / 2, cy - rayW / 2, rayLen, rayW));

        // Vertical arm
        ctx.FillRectangle(rayBrush,
            new Rect(cx - rayW / 2, cy - rayLen / 2, rayW, rayLen));

        // ── 4. Bright white core (first 3 frames only) ───────────────────
        if (frame <= 2)
        {
            double coreSize = size * 0.28;
            ctx.FillRectangle(Brushes.White,
                new Rect(cx - coreSize / 2, cy - coreSize / 2, coreSize, coreSize));
        }

        // ── 5. Purple accent ring (last 2 frames — the "fading spark") ───
        if (frame >= maxF - 2)
        {
            byte purpleAlpha = AlphaByte(120, (t - 0.6) / 0.4);
            double pSize = size * 1.3;
            ctx.FillRectangle(
                new SolidColorBrush(Color.FromArgb(purpleAlpha, 180, 60, 255)),
                new Rect(cx - pSize / 2, cy - pSize / 2, pSize, pSize));
        }
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    /// <summary>Grow for the first half of frames, shrink for the second half.</summary>
    private static double GrowShrink(double baseSize, int frame, int maxF)
        => baseSize * (frame < maxF / 2
            ? 2.0 * frame / maxF
            : 2.0 * (maxF - frame) / maxF) + 4;

    private static byte AlphaByte(int max, double t)
        => (byte)(max * Math.Clamp(1.0 - t, 0, 1));
}
