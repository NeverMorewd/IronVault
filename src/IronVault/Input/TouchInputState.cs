namespace IronVault.Input;

/// <summary>
/// Shared in-memory store for touch-driven input.
/// Written by the browser JS interop layer via [JSExport], read every frame
/// in GameView.OnFrameTick and OR-ed with keyboard input.
/// WASM runs single-threaded, so plain fields are safe without Interlocked.
/// </summary>
public static class TouchInputState
{
    public static bool Up, Down, Left, Right, Fire;

    public static void Reset()
        => Up = Down = Left = Right = Fire = false;
}
