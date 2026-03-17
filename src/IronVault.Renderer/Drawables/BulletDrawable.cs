using Avalonia;
using Avalonia.Media;
using IronVault.Core.Engine.Components;
using IronVault.Core.Engine.Entities;

namespace IronVault.Renderer.Drawables;

public sealed class BulletDrawable : IDrawable
{
    private readonly BulletEntity _bullet;

    public BulletDrawable(BulletEntity bullet) { _bullet = bullet; }

    public void Draw(DrawingContext ctx, uint frameTick)
    {
        if (!_bullet.IsAlive) return;

        bool horizontal = _bullet.Direction is Direction.Left or Direction.Right;
        double w = horizontal ? BulletEntity.Height : BulletEntity.Width;
        double h = horizontal ? BulletEntity.Width  : BulletEntity.Height;

        double x = _bullet.X;
        double y = _bullet.Y;

        // A bullet is "weakened" when it has taken partial cancellation damage
        // (Health < Power) but survived because it had higher power than the attacker.
        bool weakened = _bullet.Health < _bullet.Power;

        if (weakened)
        {
            // ── Weakened state: dim red-orange, no bright core ─────────────
            // The bullet is cracked / partially cancelled — warn the player visually.
            // Outer flicker glow (red-ish)
            ctx.FillRectangle(
                new SolidColorBrush(Color.FromArgb(60, 255, 60, 0)),
                new Rect(x - 2, y - 2, w + 4, h + 4));

            // Body: dark orange-red
            ctx.FillRectangle(
                new SolidColorBrush(Color.FromRgb(200, 80, 0)),
                new Rect(x, y, w, h));

            // Crack-line across the centre (1 px)
            double cx = x + w / 2;
            double cy = y + h / 2;
            ctx.FillRectangle(
                new SolidColorBrush(Color.FromArgb(180, 255, 200, 0)),
                horizontal
                    ? new Rect(cx - 1, y + 1, 2, h - 2)   // vertical crack on horizontal bullet
                    : new Rect(x + 1, cy - 1, w - 2, 2)); // horizontal crack on vertical bullet
        }
        else
        {
            // ── Normal state ─────────────────────────────────────────────────
            // Glow outer (colour varies by power)
            Color glowColor = _bullet.Power >= 2
                ? Color.FromArgb(90, 255, 80, 0)   // P2: brighter orange glow
                : Color.FromArgb(80, 255, 200, 0);  // P1: standard yellow glow

            ctx.FillRectangle(
                new SolidColorBrush(glowColor),
                new Rect(x - 2, y - 2, w + 4, h + 4));

            // Bullet body
            ctx.FillRectangle(DrawColors.BulletBrush, new Rect(x, y, w, h));

            // Bright white core — P2 bullets get a slightly larger core
            double margin = _bullet.Power >= 2 ? 0.5 : 1;
            ctx.FillRectangle(Brushes.White,
                new Rect(x + margin, y + margin,
                         Math.Max(1, w - margin * 2),
                         Math.Max(1, h - margin * 2)));
        }
    }
}
