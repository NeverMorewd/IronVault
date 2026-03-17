using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using IronVault.Core.Engine;
using IronVault.Core.Engine.Entities;
using IronVault.Core.Localization;
using IronVault.Core.Map;
using IronVault.Renderer.Drawables;

namespace IronVault.Renderer.Controls;

/// <summary>
/// The main game rendering control.
/// Subscribes to the game loop and draws all entities each frame.
/// No bitmaps — all rendering via DrawingContext.
/// </summary>
public sealed class GameCanvas : Control
{
    private GameEngine? _engine;
    private uint _frameTick;
    private TileMapDrawable? _mapDrawable;
    // Track the map object we built the drawable from.
    // GameEngine.Reset() creates a brand-new TileMap instance, so we must
    // detect the reference change and rebuild the drawable accordingly.
    private IronVault.Core.Map.TileMap? _trackedMap;

    // ── Public API ───────────────────────────────────────────────────────────
    public void Attach(GameEngine engine)
    {
        _engine = engine;
        RebuildMapDrawable();
    }

    public void Detach()
    {
        _engine = null;
        _mapDrawable = null;
        _trackedMap = null;
    }

    /// <summary>Called each game tick to advance and repaint.</summary>
    public void Tick(float dt)
    {
        // Rebuild drawable whenever the engine swaps in a new TileMap (e.g. after Reset).
        if (_engine != null && !ReferenceEquals(_engine.Map, _trackedMap))
            RebuildMapDrawable();

        _engine?.Tick(dt);
        _frameTick++;
        InvalidateVisual();
    }

    private void RebuildMapDrawable()
    {
        if (_engine is null) return;
        _trackedMap  = _engine.Map;
        _mapDrawable = new TileMapDrawable(_trackedMap);
    }

    // ── Sizing ───────────────────────────────────────────────────────────────
    protected override Size MeasureOverride(Size availableSize)
    {
        int cols = _engine?.Map.Cols ?? TileMap.DefaultCols;
        int rows = _engine?.Map.Rows ?? TileMap.DefaultRows;
        return new Size(cols * TileMap.TileSize, rows * TileMap.TileSize);
    }

    // ── Rendering ────────────────────────────────────────────────────────────
    public override void Render(DrawingContext ctx)
    {
        if (_engine is null)
        {
            RenderPlaceholder(ctx);
            return;
        }

        var mapW = _engine.Map.Cols * TileMap.TileSize;
        var mapH = _engine.Map.Rows * TileMap.TileSize;

        // 1. Background fill
        ctx.FillRectangle(DrawColors.BackgroundBrush, new Rect(0, 0, mapW, mapH));

        // 2. Ground tiles (Forest is intentionally skipped here — drawn last as canopy)
        _mapDrawable?.Draw(ctx, _frameTick);

        // 3. Power-ups (below tanks)
        foreach (var pu in _engine.PowerUps)
            new PowerUpDrawable(pu).Draw(ctx, _frameTick);

        // 4. Tanks
        foreach (var tank in _engine.Tanks)
            new TankDrawable(tank).Draw(ctx, _frameTick);

        // 5. Bullets
        foreach (var bullet in _engine.Bullets)
            new BulletDrawable(bullet).Draw(ctx, _frameTick);

        // 6. Explosions
        foreach (var exp in _engine.Explosions)
            new ExplosionDrawable(exp).Draw(ctx, _frameTick);

        // 7. Forest canopy — rendered AFTER all entities so tanks/bullets inside
        //    forest appear beneath the foliage (stealth mechanic, classic Battle City)
        _mapDrawable?.DrawCanopy(ctx, _frameTick);

        // 8. Game state overlay
        if (_engine.State == GameState.GameOver)
            DrawOverlay(ctx, mapW, mapH, I18n.T("game.over"),    Color.FromRgb(200, 0, 0));
        else if (_engine.State == GameState.Victory)
            DrawOverlay(ctx, mapW, mapH, I18n.T("game.victory"), Color.FromRgb(255, 215, 0));
        else if (_engine.State == GameState.Paused)
            DrawOverlay(ctx, mapW, mapH, I18n.T("game.paused"),  Color.FromRgb(255, 165, 0));
        else if (_engine.State == GameState.NotStarted)
            DrawOverlay(ctx, mapW, mapH, I18n.T("game.title"),   Color.FromRgb(255, 165, 0));
    }

    private static void RenderPlaceholder(DrawingContext ctx)
    {
        ctx.FillRectangle(DrawColors.BackgroundBrush,
            new Rect(0, 0, TileMap.DefaultCols * TileMap.TileSize, TileMap.DefaultRows * TileMap.TileSize));
        var ft = new FormattedText(
            "NO ENGINE ATTACHED",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas"),
            16,
            DrawColors.AmberBrush);
        ctx.DrawText(ft, new Point(40, 300));
    }

    private static void DrawOverlay(DrawingContext ctx, double w, double h, string text, Color color)
    {
        // Semi-transparent dark overlay
        ctx.FillRectangle(
            new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
            new Rect(0, 0, w, h));

        // Centered text
        var brush = new SolidColorBrush(color);
        var ft = new FormattedText(
            text,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas", FontStyle.Normal, FontWeight.Bold),
            36,
            brush);

        double tx = (w - ft.Width) / 2;
        double ty = (h - ft.Height) / 2;
        ctx.DrawText(ft, new Point(tx, ty));
    }
}
