using System.Runtime.InteropServices.JavaScript;
using IronVault.Input;

namespace IronVault.Browser.Input;

/// <summary>
/// Exports C# input setters to JavaScript via [JSExport].
/// Called from touch-controls.js:
///   const asm = await dotnetRuntime.getAssemblyExports('IronVault.Browser');
///   asm.IronVault.Browser.Input.BrowserInput.SetMove(up, down, left, right);
///   asm.IronVault.Browser.Input.BrowserInput.SetFire(fire);
/// </summary>
public static partial class BrowserInput
{
    /// <summary>Update all four movement directions at once.</summary>
    [JSExport]
    public static void SetMove(bool up, bool down, bool left, bool right)
    {
        TouchInputState.Up    = up;
        TouchInputState.Down  = down;
        TouchInputState.Left  = left;
        TouchInputState.Right = right;
    }

    /// <summary>Set the fire button pressed/released state.</summary>
    [JSExport]
    public static void SetFire(bool fire)
        => TouchInputState.Fire = fire;

    /// <summary>Release all touch inputs (called on screen change / app blur).</summary>
    [JSExport]
    public static void ReleaseAll()
        => TouchInputState.Reset();
}
