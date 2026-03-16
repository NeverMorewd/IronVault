using Avalonia.Media;

namespace IronVault.Renderer.Drawables;

/// <summary>Centralized color palette — amber/phosphor CRT aesthetic.</summary>
internal static class DrawColors
{
    // Primary palette — amber CRT
    public static readonly Color Amber      = Color.FromRgb(0xFF, 0xA5, 0x00);
    public static readonly Color AmberDim   = Color.FromRgb(0xAA, 0x60, 0x00);
    public static readonly Color AmberDark  = Color.FromRgb(0x55, 0x30, 0x00);
    public static readonly Color Background = Color.FromRgb(0x0A, 0x08, 0x00);
    public static readonly Color GridLine   = Color.FromRgb(0x1A, 0x14, 0x00);

    // Tile colors
    public static readonly Color Brick      = Color.FromRgb(0xCC, 0x44, 0x00);
    public static readonly Color BrickDark  = Color.FromRgb(0x88, 0x22, 0x00);
    public static readonly Color Steel      = Color.FromRgb(0x88, 0x88, 0x88);
    public static readonly Color SteelDark  = Color.FromRgb(0x44, 0x44, 0x44);
    public static readonly Color Water1     = Color.FromRgb(0x00, 0x44, 0xAA);
    public static readonly Color Water2     = Color.FromRgb(0x00, 0x22, 0x66);
    public static readonly Color Forest     = Color.FromRgb(0x00, 0x55, 0x00);
    public static readonly Color ForestDark = Color.FromRgb(0x00, 0x33, 0x00);
    public static readonly Color Ice        = Color.FromRgb(0xCC, 0xEE, 0xFF);
    public static readonly Color Base       = Color.FromRgb(0xFF, 0xCC, 0x00);

    // Team colors
    public static readonly Color PlayerColor = Color.FromRgb(0xFF, 0xE0, 0x00);   // bright yellow
    public static readonly Color EnemyColor  = Color.FromRgb(0xFF, 0x44, 0x00);   // red-orange
    public static readonly Color AllyColor   = Color.FromRgb(0x00, 0xCC, 0xFF);   // cyan

    // Bullet / explosion
    public static readonly Color BulletColor     = Color.FromRgb(0xFF, 0xFF, 0xCC);
    public static readonly Color ExplosionOuter  = Color.FromRgb(0xFF, 0x66, 0x00);
    public static readonly Color ExplosionInner  = Color.FromRgb(0xFF, 0xFF, 0x00);
    public static readonly Color ExplosionCore   = Color.FromRgb(0xFF, 0xFF, 0xFF);

    // Brushes (frequently used)
    public static readonly SolidColorBrush BackgroundBrush   = new(Background);
    public static readonly SolidColorBrush AmberBrush        = new(Amber);
    public static readonly SolidColorBrush AmberDimBrush     = new(AmberDim);
    public static readonly SolidColorBrush GridLineBrush     = new(GridLine);
    public static readonly SolidColorBrush BulletBrush       = new(BulletColor);
    public static readonly SolidColorBrush PlayerBrush       = new(PlayerColor);
    public static readonly SolidColorBrush EnemyBrush        = new(EnemyColor);
    public static readonly SolidColorBrush AllyBrush         = new(AllyColor);
    public static readonly SolidColorBrush ExplosionOutBrush = new(ExplosionOuter);
    public static readonly SolidColorBrush ExplosionInBrush  = new(ExplosionInner);
    public static readonly SolidColorBrush ExplosionCoreBrush= new(ExplosionCore);
    public static readonly SolidColorBrush BrickBrush        = new(Brick);
    public static readonly SolidColorBrush BrickDarkBrush    = new(BrickDark);
    public static readonly SolidColorBrush SteelBrush        = new(Steel);
    public static readonly SolidColorBrush SteelDarkBrush    = new(SteelDark);
    public static readonly SolidColorBrush Water1Brush       = new(Water1);
    public static readonly SolidColorBrush Water2Brush       = new(Water2);
    public static readonly SolidColorBrush ForestBrush       = new(Forest);
    public static readonly SolidColorBrush ForestDarkBrush   = new(ForestDark);
    public static readonly SolidColorBrush IceBrush          = new(Ice);
    public static readonly SolidColorBrush BaseBrush         = new(Base);
}
