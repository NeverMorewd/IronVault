using Avalonia;
using Avalonia.Media;
using IronVault.Core.Engine.Components;
using IronVault.Core.Engine.Entities;

namespace IronVault.Renderer.Drawables;

/// <summary>
/// Draws a single tank using pure DrawingContext geometry.
/// Pixel layout: 48×48 bounding box.
/// Tank always drawn with "barrel pointing up" convention, then rotated via transform.
/// </summary>
public sealed class TankDrawable : IDrawable
{
    private readonly TankEntity _tank;

    public TankDrawable(TankEntity tank) { _tank = tank; }

    public void Draw(DrawingContext ctx, uint frameTick)
    {
        if (!_tank.IsAlive) return;
        if (!_tank.BlinkVisible) return;

        double x = _tank.Position.X;
        double y = _tank.Position.Y;
        int s = TankEntity.Size; // 48
        double cx = x + s / 2.0;
        double cy = y + s / 2.0;

        IBrush body = _tank.Team switch
        {
            TankTeam.Player => DrawColors.PlayerBrush,
            TankTeam.Enemy  => DrawColors.EnemyBrush,
            _               => DrawColors.AllyBrush,
        };

        // Rotate canvas around tank center
        double angle = DirectionToRadians(_tank.Position.Facing);
        using (ctx.PushTransform(
            Matrix.CreateTranslation(-cx, -cy)
            * Matrix.CreateRotation(angle)
            * Matrix.CreateTranslation(cx, cy)))
        {
            DrawTankBody(ctx, x, y, s, body, frameTick);
        }

        // Spawn invincibility shield (drawn without rotation)
        if (_tank.IsInvincible)
            DrawShield(ctx, x, y, s, frameTick);
    }

    // Tank body drawn in "facing Up" orientation (barrel points toward decreasing Y)
    private static void DrawTankBody(DrawingContext ctx, double x, double y, int s, IBrush body, uint tick)
    {
        // Left track
        ctx.FillRectangle(Brushes.DimGray, new Rect(x, y + 4, 10, s - 8));
        // Right track
        ctx.FillRectangle(Brushes.DimGray, new Rect(x + s - 10, y + 4, 10, s - 8));

        // Animated track tread marks
        var treadDark = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20));
        int offset = (int)(tick / 4 % 8);
        for (int i = 0; i < 6; i++)
        {
            double ty = y + 4 + (i * 8 + offset) % (s - 8);
            ctx.FillRectangle(treadDark, new Rect(x, ty, 10, 2));
            ctx.FillRectangle(treadDark, new Rect(x + s - 10, ty, 10, 2));
        }

        // Hull
        ctx.FillRectangle(body, new Rect(x + 10, y + 8, s - 20, s - 16));

        // Turret base (dark, centered on hull)
        ctx.FillRectangle(new SolidColorBrush(Color.FromArgb(180, 20, 20, 20)),
                          new Rect(x + 16, y + 18, 16, 16));

        // Barrel — pointing up
        ctx.FillRectangle(new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x30)),
                          new Rect(x + 21, y + 2, 6, 22));

        // Dome highlight (top-left of turret)
        ctx.FillRectangle(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                          new Rect(x + 17, y + 19, 6, 6));
    }

    private static void DrawShield(DrawingContext ctx, double x, double y, int s, uint tick)
    {
        bool bright = (tick / 3 % 2 == 0);
        byte alpha = bright ? (byte)180 : (byte)70;
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(alpha, 0, 200, 255)), 2);

        double cx = x + s / 2.0;
        double cy = y + s / 2.0;
        double r  = s / 2.0 + 5;
        double a  = tick * 0.06;

        var pts = new Point[4];
        for (int i = 0; i < 4; i++)
            pts[i] = new Point(cx + r * Math.Cos(a + i * Math.PI / 2),
                               cy + r * Math.Sin(a + i * Math.PI / 2));

        ctx.DrawLine(pen, pts[0], pts[1]);
        ctx.DrawLine(pen, pts[1], pts[2]);
        ctx.DrawLine(pen, pts[2], pts[3]);
        ctx.DrawLine(pen, pts[3], pts[0]);
    }

    private static double DirectionToRadians(Direction dir) => dir switch
    {
        Direction.Up    => 0,
        Direction.Right => Math.PI / 2.0,
        Direction.Down  => Math.PI,
        Direction.Left  => -Math.PI / 2.0,
        _ => 0,
    };
}
