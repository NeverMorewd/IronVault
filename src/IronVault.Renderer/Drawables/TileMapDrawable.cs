using Avalonia;
using Avalonia.Media;
using IronVault.Core.Map;

namespace IronVault.Renderer.Drawables;

/// <summary>Renders the entire tile map using DrawingContext pixel blocks.</summary>
public sealed class TileMapDrawable : IDrawable
{
    private readonly TileMap _map;

    public TileMapDrawable(TileMap map) { _map = map; }

    public void Draw(DrawingContext ctx, uint frameTick)
    {
        int ts = TileMap.TileSize;

        for (int r = 0; r < _map.Rows; r++)
        for (int c = 0; c < _map.Cols; c++)
        {
            var tile = _map[c, r];
            if (tile == TileType.Empty || tile == TileType.Spawn) continue;

            var rect = new Rect(c * ts, r * ts, ts, ts);
            DrawTile(ctx, tile, rect, frameTick);
        }
    }

    private static void DrawTile(DrawingContext ctx, TileType tile, Rect rect, uint tick)
    {
        int ts = TileMap.TileSize;
        double h = rect.X;
        double v = rect.Y;

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
                ctx.FillRectangle(DrawColors.IceBrush, rect);
                // Crack lines
                ctx.DrawLine(new Pen(DrawColors.SteelBrush, 1),
                    new Point(h + 4, v + 4), new Point(h + ts - 4, v + ts - 4));
                break;

            case TileType.Base:
                DrawBase(ctx, rect);
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

    private static void DrawBase(DrawingContext ctx, Rect r)
    {
        // Eagle/HQ symbol — a stylized star/base icon
        ctx.FillRectangle(DrawColors.BaseBrush, r);
        double x = r.X, y = r.Y, w = r.Width, h = r.Height;
        var darkPen = new Pen(DrawColors.BrickDarkBrush, 2);

        // Draw a simple "eagle" cross shape
        ctx.FillRectangle(Brushes.Black, new Rect(x + w * 0.4, y + 2, w * 0.2, h - 4));
        ctx.FillRectangle(Brushes.Black, new Rect(x + 2, y + h * 0.4, w - 4, h * 0.2));
        // Diagonal arms
        ctx.DrawLine(darkPen, new Point(x + 2, y + 2),         new Point(x + 8, y + 8));
        ctx.DrawLine(darkPen, new Point(x + w - 2, y + 2),     new Point(x + w - 8, y + 8));
        ctx.DrawLine(darkPen, new Point(x + 2, y + h - 2),     new Point(x + 8, y + h - 8));
        ctx.DrawLine(darkPen, new Point(x + w - 2, y + h - 2), new Point(x + w - 8, y + h - 8));
    }
}
