using Avalonia;
using Avalonia.Media;
using IronVault.Core.Engine.Entities;

namespace IronVault.Renderer.Drawables;

public sealed class PowerUpDrawable : IDrawable
{
    private readonly PowerUpEntity _powerUp;

    public PowerUpDrawable(PowerUpEntity powerUp) { _powerUp = powerUp; }

    public void Draw(DrawingContext ctx, uint frameTick)
    {
        if (!_powerUp.IsAlive || !_powerUp.BlinkVisible) return;

        double x = _powerUp.X;
        double y = _powerUp.Y;
        int s = PowerUpEntity.Size;

        // Background
        ctx.FillRectangle(DrawColors.AmberDimBrush, new Rect(x, y, s, s));
        var border = new Pen(DrawColors.AmberBrush, 2);
        ctx.DrawRectangle(null, border, new Rect(x + 1, y + 1, s - 2, s - 2));

        // Icon based on type
        DrawIcon(ctx, _powerUp.Type, x, y, s);
    }

    private static void DrawIcon(DrawingContext ctx, PowerUpType type, double x, double y, int s)
    {
        var pen = new Pen(DrawColors.AmberBrush, 2);
        double cx = x + s / 2.0;
        double cy = y + s / 2.0;

        switch (type)
        {
            case PowerUpType.Star:
                DrawStar(ctx, cx, cy, 9, pen);
                break;

            case PowerUpType.BulletSpeed:
                // Arrow pointing up
                ctx.DrawLine(pen, new Point(cx, y + 4),  new Point(cx, y + s - 4));
                ctx.DrawLine(pen, new Point(cx, y + 4),  new Point(cx - 5, y + 10));
                ctx.DrawLine(pen, new Point(cx, y + 4),  new Point(cx + 5, y + 10));
                break;

            case PowerUpType.ExtraBullet:
                // Double bullet dots
                ctx.FillRectangle(DrawColors.AmberBrush, new Rect(cx - 7, cy - 3, 5, 6));
                ctx.FillRectangle(DrawColors.AmberBrush, new Rect(cx + 2, cy - 3, 5, 6));
                break;

            case PowerUpType.Shield:
                // Shield shape
                ctx.DrawLine(pen, new Point(cx - 6, y + 4), new Point(cx + 6, y + 4));
                ctx.DrawLine(pen, new Point(cx + 6, y + 4), new Point(cx + 6, cy));
                ctx.DrawLine(pen, new Point(cx - 6, y + 4), new Point(cx - 6, cy));
                ctx.DrawLine(pen, new Point(cx + 6, cy),    new Point(cx, y + s - 4));
                ctx.DrawLine(pen, new Point(cx - 6, cy),    new Point(cx, y + s - 4));
                break;

            case PowerUpType.Clock:
                // Clock circle
                ctx.DrawEllipse(null, pen, new Point(cx, cy), 7, 7);
                ctx.DrawLine(pen, new Point(cx, cy), new Point(cx, cy - 5));
                ctx.DrawLine(pen, new Point(cx, cy), new Point(cx + 4, cy));
                break;

            case PowerUpType.Shovel:
                // Shovel blade
                ctx.FillRectangle(DrawColors.AmberBrush, new Rect(cx - 5, y + s - 10, 10, 6));
                ctx.DrawLine(pen, new Point(cx, y + s - 10), new Point(cx, y + 4));
                break;

            case PowerUpType.Life:
                // Heart shape approximation
                ctx.FillRectangle(DrawColors.AmberBrush, new Rect(cx - 5, cy - 2, 10, 8));
                ctx.FillRectangle(DrawColors.AmberBrush, new Rect(cx - 7, cy - 5, 5, 5));
                ctx.FillRectangle(DrawColors.AmberBrush, new Rect(cx + 2, cy - 5, 5, 5));
                break;
        }
    }

    private static void DrawStar(DrawingContext ctx, double cx, double cy, double r, Pen pen)
    {
        double innerR = r * 0.45;
        var pts = new Point[10];
        for (int i = 0; i < 10; i++)
        {
            double angle = i * Math.PI / 5 - Math.PI / 2;
            double radius = i % 2 == 0 ? r : innerR;
            pts[i] = new Point(cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle));
        }
        for (int i = 0; i < 10; i++)
            ctx.DrawLine(pen, pts[i], pts[(i + 1) % 10]);
    }
}
