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

        int frame = _explosion.Frame;
        int maxF  = ExplosionEntity.MaxFrames;
        double t  = frame / (double)maxF; // 0..1 progress

        // Size grows then shrinks
        double baseSize = _explosion.Size switch { 0 => 12, 1 => 24, _ => 48 };
        double size = baseSize * (frame < maxF / 2
            ? 2.0 * frame / maxF
            : 2.0 * (maxF - frame) / maxF) + 4;

        double cx = _explosion.X + size / 2;
        double cy = _explosion.Y + size / 2;

        byte outerAlpha = (byte)(255 * (1.0 - t));
        byte innerAlpha = (byte)(200 * (1.0 - t));

        // Outer ring
        var outerBrush = new SolidColorBrush(Color.FromArgb(outerAlpha, 255, 100, 0));
        ctx.FillRectangle(outerBrush,
            new Rect(cx - size / 2, cy - size / 2, size, size));

        // Inner glow
        double innerSize = size * 0.6;
        var innerBrush = new SolidColorBrush(Color.FromArgb(innerAlpha, 255, 220, 0));
        ctx.FillRectangle(innerBrush,
            new Rect(cx - innerSize / 2, cy - innerSize / 2, innerSize, innerSize));

        // Core flash (first two frames)
        if (frame <= 1)
        {
            double coreSize = size * 0.25;
            ctx.FillRectangle(Brushes.White,
                new Rect(cx - coreSize / 2, cy - coreSize / 2, coreSize, coreSize));
        }
    }
}
