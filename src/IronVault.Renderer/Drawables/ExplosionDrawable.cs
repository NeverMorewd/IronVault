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
        double baseSize = _explosion.Size switch { 0 => 12, 1 => 24, _ => 48 };
        double size = GrowShrink(baseSize, frame, maxF);

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
