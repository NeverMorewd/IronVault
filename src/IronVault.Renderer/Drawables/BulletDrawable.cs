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

        // Glow outer
        ctx.FillRectangle(
            new SolidColorBrush(Color.FromArgb(80, 255, 200, 0)),
            new Rect(x - 2, y - 2, w + 4, h + 4));

        // Bullet body
        ctx.FillRectangle(DrawColors.BulletBrush, new Rect(x, y, w, h));

        // Bright core
        ctx.FillRectangle(Brushes.White,
            new Rect(x + 1, y + 1, Math.Max(1, w - 2), Math.Max(1, h - 2)));
    }
}
