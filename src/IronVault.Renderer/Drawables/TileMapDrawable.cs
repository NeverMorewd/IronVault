using Avalonia;
using Avalonia.Media;
using IronVault.Core.Map;

namespace IronVault.Renderer.Drawables;

/// <summary>Renders the entire tile map using DrawingContext pixel blocks.</summary>
public sealed class TileMapDrawable : IDrawable
{
    private readonly TileMap _map;

    public TileMapDrawable(TileMap map) { _map = map; }

    /// <summary>
    /// Ground pass — draws every tile EXCEPT Forest.
    /// Call this before rendering tanks / bullets so that entities appear
    /// above ground terrain but below the forest canopy.
    /// </summary>
    public void Draw(DrawingContext ctx, uint frameTick)
    {
        int ts = TileMap.TileSize;

        for (int r = 0; r < _map.Rows; r++)
        for (int c = 0; c < _map.Cols; c++)
        {
            var tile = _map[c, r];
            // Empty, Spawn and Forest are skipped in the ground pass.
            // Forest is drawn separately in DrawCanopy() so it overlays entities.
            if (tile == TileType.Empty  ||
                tile == TileType.Spawn  ||
                tile == TileType.Forest) continue;

            DrawTile(ctx, tile, new Rect(c * ts, r * ts, ts, ts), frameTick);
        }
    }

    /// <summary>
    /// Canopy pass — draws only Forest tiles.
    /// Call this AFTER rendering all game entities (tanks, bullets, explosions)
    /// so the forest canopy is composited on top, hiding anything beneath it.
    /// </summary>
    public void DrawCanopy(DrawingContext ctx, uint frameTick)
    {
        int ts = TileMap.TileSize;

        for (int r = 0; r < _map.Rows; r++)
        for (int c = 0; c < _map.Cols; c++)
        {
            if (_map[c, r] != TileType.Forest) continue;
            DrawForest(ctx, new Rect(c * ts, r * ts, ts, ts));
        }
    }

    private static void DrawTile(DrawingContext ctx, TileType tile, Rect rect, uint tick)
    {
        switch (tile)
        {
            case TileType.Brick:
                DrawBrick(ctx, rect);
                break;

            case TileType.Steel:
                DrawSteel(ctx, rect);
                break;

            case TileType.Water:
                DrawWater(ctx, rect, tick);
                break;

            case TileType.Forest:
                DrawForest(ctx, rect);
                break;

            case TileType.Ice:
                DrawIce(ctx, rect, tick);
                break;

            case TileType.Base:
                DrawBase(ctx, rect, tick);
                break;
        }
    }

    private static void DrawBrick(DrawingContext ctx, Rect r)
    {
        // Fill
        ctx.FillRectangle(DrawColors.BrickBrush, r);
        double x = r.X, y = r.Y, w = r.Width, h = r.Height;
        var darkPen = new Pen(DrawColors.BrickDarkBrush, 1);

        // Mortar lines — simple grid giving brick-like texture
        // Horizontal rows
        ctx.DrawLine(darkPen, new Point(x, y + h * 0.5), new Point(x + w, y + h * 0.5));
        // Offset vertical joints
        ctx.DrawLine(darkPen, new Point(x + w * 0.25, y),        new Point(x + w * 0.25, y + h * 0.5));
        ctx.DrawLine(darkPen, new Point(x + w * 0.75, y),        new Point(x + w * 0.75, y + h * 0.5));
        ctx.DrawLine(darkPen, new Point(x + w * 0.5,  y + h * 0.5), new Point(x + w * 0.5,  y + h));
    }

    private static void DrawSteel(DrawingContext ctx, Rect r)
    {
        ctx.FillRectangle(DrawColors.SteelBrush, r);
        double x = r.X, y = r.Y, w = r.Width, h = r.Height;
        var darkPen = new Pen(DrawColors.SteelDarkBrush, 1);
        var lightPen = new Pen(Brushes.White, 1);
        // Corner highlight
        ctx.DrawLine(lightPen, new Point(x, y),         new Point(x + w - 1, y));
        ctx.DrawLine(lightPen, new Point(x, y),         new Point(x, y + h - 1));
        ctx.DrawLine(darkPen,  new Point(x + w - 1, y), new Point(x + w - 1, y + h - 1));
        ctx.DrawLine(darkPen,  new Point(x, y + h - 1), new Point(x + w - 1, y + h - 1));
        // Cross
        ctx.DrawLine(darkPen, new Point(x + w * 0.5, y + 2),   new Point(x + w * 0.5, y + h - 2));
        ctx.DrawLine(darkPen, new Point(x + 2, y + h * 0.5),   new Point(x + w - 2,   y + h * 0.5));
    }

    private static void DrawWater(DrawingContext ctx, Rect r, uint tick)
    {
        // Animate between two water frames using tick
        var bg = (tick / 8 % 2 == 0) ? DrawColors.Water1Brush : DrawColors.Water2Brush;
        var fg = (tick / 8 % 2 == 0) ? DrawColors.Water2Brush : DrawColors.Water1Brush;

        ctx.FillRectangle(bg, r);
        double x = r.X, y = r.Y, w = r.Width, h = r.Height;
        // Wave stripes
        ctx.FillRectangle(fg, new Rect(x + 2, y + 2, w - 4, 4));
        ctx.FillRectangle(fg, new Rect(x + 4, y + 10, w - 8, 4));
        ctx.FillRectangle(fg, new Rect(x + 2, y + 18, w - 4, 4));
    }

    private static void DrawIce(DrawingContext ctx, Rect r, uint tick)
    {
        double x = r.X, y = r.Y, w = r.Width, h = r.Height;

        // Subtle shimmer animation: alternate between two very close shades
        bool shimmer = (tick / 14 % 2 == 0);
        var iceBase    = new SolidColorBrush(Color.FromRgb(0xB8, 0xE4, 0xF8)); // muted ice blue
        var iceShimmer = new SolidColorBrush(Color.FromRgb(0xD0, 0xF0, 0xFF)); // brighter shimmer frame
        ctx.FillRectangle(shimmer ? iceShimmer : iceBase, r);

        // Crystal fracture lines — star/spider-web pattern
        var crackPen  = new Pen(new SolidColorBrush(Color.FromArgb(100, 120, 180, 220)), 1);
        var crackPen2 = new Pen(new SolidColorBrush(Color.FromArgb(60,  160, 210, 240)), 1);

        // Primary diagonal crack (top-left → bottom-right)
        ctx.DrawLine(crackPen, new Point(x + 3,     y + 3),     new Point(x + w - 3, y + h - 3));
        // Counter diagonal (top-right → bottom-left)
        ctx.DrawLine(crackPen2, new Point(x + w - 4, y + 3),    new Point(x + 4,     y + h - 3));
        // Horizontal stress vein (slightly offset from centre for realism)
        ctx.DrawLine(crackPen2, new Point(x + 2, y + h * 0.42), new Point(x + w - 2, y + h * 0.58));

        // Ice-crystal glint dots at fixed positions (simulate specular reflection)
        byte glintA = shimmer ? (byte)210 : (byte)100;
        var  glint  = new SolidColorBrush(Color.FromArgb(glintA, 255, 255, 255));
        ctx.FillRectangle(glint, new Rect(x + 3,     y + 3,     3, 3)); // top-left
        ctx.FillRectangle(glint, new Rect(x + w - 6, y + h - 6, 3, 3)); // bottom-right
        ctx.FillRectangle(glint, new Rect(x + w - 5, y + 3,     2, 2)); // top-right
        ctx.FillRectangle(glint, new Rect(x + 4,     y + h - 5, 2, 2)); // bottom-left
    }

    private static void DrawForest(DrawingContext ctx, Rect r)
    {
        ctx.FillRectangle(DrawColors.ForestDarkBrush, r);
        double x = r.X, y = r.Y, w = r.Width, h = r.Height;
        // Tree dot pattern
        ctx.FillRectangle(DrawColors.ForestBrush, new Rect(x + 4,  y + 2,  8, 8));
        ctx.FillRectangle(DrawColors.ForestBrush, new Rect(x + 14, y + 6,  6, 6));
        ctx.FillRectangle(DrawColors.ForestBrush, new Rect(x + 2,  y + 14, 6, 6));
        ctx.FillRectangle(DrawColors.ForestBrush, new Rect(x + 12, y + 14, 8, 8));
    }

    private static void DrawBase(DrawingContext ctx, Rect r, uint tick)
    {
        double x = r.X, y = r.Y, w = r.Width, h = r.Height;

        // Slow pulse: alternates every 30 frames (~0.5 s at 60 fps)
        bool pulse = (tick / 30 % 2 == 0);

        // Background — bright yellow / amber alternating
        var bg = pulse ? DrawColors.BaseBrush : DrawColors.AmberBrush;
        ctx.FillRectangle(bg, r);

        // Thick cross — 40 % of tile size (≈ 9–10 px on a 24 px tile)
        ctx.FillRectangle(Brushes.Black, new Rect(x + w * 0.30, y + 1,        w * 0.40, h - 2));  // vertical
        ctx.FillRectangle(Brushes.Black, new Rect(x + 1,        y + h * 0.30, w - 2,    h * 0.40)); // horizontal

        // Bright centre dot — contrasts with the black cross
        var dot = pulse ? DrawColors.AmberBrush : DrawColors.BaseBrush;
        ctx.FillRectangle(dot, new Rect(x + w * 0.37, y + h * 0.37, w * 0.26, h * 0.26));

        // 2 px amber border — makes the tile stand out against adjacent bricks
        ctx.DrawRectangle(null, new Pen(pulse ? DrawColors.AmberBrush : DrawColors.BaseBrush, 2),
            new Rect(x + 1, y + 1, w - 2, h - 2));
    }
}
