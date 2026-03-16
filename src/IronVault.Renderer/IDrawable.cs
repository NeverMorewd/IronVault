using Avalonia.Media;

namespace IronVault.Renderer;

/// <summary>
/// All game objects that can be rendered implement this interface.
/// Rendering uses only DrawingContext — no bitmaps.
/// </summary>
public interface IDrawable
{
    void Draw(DrawingContext context, uint frameTick);
}
