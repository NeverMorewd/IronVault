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
        double cx = x + s / 2.0;
        double cy = y + s / 2.0;

        // Pulse: alternates every 4 ticks (≈ 15 times/sec at 60 fps)
        bool pulse = (tick / 4 % 2 == 0);

        double r1 =  s / 2.0 + 9;  // outer diamond orbit radius
        double r2 =  s / 2.0 + 3;  // inner diamond orbit radius
        double a1 =  tick * 0.045; // outer: slow clockwise
        double a2 = -tick * 0.070; // inner: faster counter-clockwise

        // ── 1. Soft field glow behind everything ──────────────────────────
        ctx.FillRectangle(
            new SolidColorBrush(Color.FromArgb(18, 0, 180, 255)),
            new Rect(x - 8, y - 8, s + 16, s + 16));

        // ── 2. Outer dashed diamond ───────────────────────────────────────
        byte outerAlpha = pulse ? (byte)210 : (byte)80;
        DrawDashedDiamond(ctx, cx, cy, r1, a1,
            Color.FromArgb(outerAlpha, 0, 200, 255), thickness: 2);

        // ── 3. Inner dashed diamond (opposite spin) ───────────────────────
        byte innerAlpha = pulse ? (byte)130 : (byte)45;
        DrawDashedDiamond(ctx, cx, cy, r2, a2,
            Color.FromArgb(innerAlpha, 140, 230, 255), thickness: 1.5);

        // ── 4. Cross sparks at the 4 outer-diamond vertices ───────────────
        byte sparkAlpha = pulse ? (byte)255 : (byte)140;
        var  sparkBrush = new SolidColorBrush(Color.FromArgb(sparkAlpha, 210, 245, 255));

        for (int i = 0; i < 4; i++)
        {
            double angle = a1 + i * Math.PI / 2;
            double px = cx + r1 * Math.Cos(angle);
            double py = cy + r1 * Math.Sin(angle);

            // Pixel-art "+" cross spark (longer arm on pulse)
            double arm = pulse ? 4.0 : 2.5;
            ctx.FillRectangle(sparkBrush, new Rect(px - 1,   py - arm, 2,       arm * 2)); // vertical
            ctx.FillRectangle(sparkBrush, new Rect(px - arm, py - 1,   arm * 2, 2));       // horizontal
        }
    }

    /// <summary>
    /// Draws a diamond (rotated square) as 3 dashed segments per side,
    /// giving a pixel-art "dashed orbit ring" look.
    /// </summary>
    private static void DrawDashedDiamond(DrawingContext ctx,
        double cx, double cy, double r, double angle,
        Color color, double thickness)
    {
        // Pre-build pen once per call (avoids allocating inside inner loop)
        var pen = new Pen(new SolidColorBrush(color), thickness);

        // 4 corner vertices spaced 90° apart, starting at 'angle'
        var pts = new Point[4];
        for (int i = 0; i < 4; i++)
            pts[i] = new Point(
                cx + r * Math.Cos(angle + i * Math.PI / 2),
                cy + r * Math.Sin(angle + i * Math.PI / 2));

        // 3 dashes per side: draw segments at t ∈ [0.08, 0.42], [0.41, 0.74], [0.75, 0.92]
        ReadOnlySpan<(double t0, double t1)> dashRanges =
        [
            (0.06, 0.38),
            (0.45, 0.72),
            (0.79, 0.94),
        ];

        for (int side = 0; side < 4; side++)
        {
            var p1 = pts[side];
            var p2 = pts[(side + 1) % 4];

            foreach (var (t0, t1) in dashRanges)
            {
                var segStart = new Point(p1.X + (p2.X - p1.X) * t0,
                                         p1.Y + (p2.Y - p1.Y) * t0);
                var segEnd   = new Point(p1.X + (p2.X - p1.X) * t1,
                                         p1.Y + (p2.Y - p1.Y) * t1);
                ctx.DrawLine(pen, segStart, segEnd);
            }
        }
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
