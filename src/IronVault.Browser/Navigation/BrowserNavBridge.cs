using System.Runtime.InteropServices.JavaScript;
using IronVault.Navigation;

namespace IronVault.Browser.Navigation;

/// <summary>
/// Bridges C# navigation events to the JavaScript touch-controls overlay
/// so the virtual gamepad is only visible during active gameplay.
/// </summary>
internal static partial class BrowserNavBridge
{
    /// <summary>
    /// Subscribe to NavigationService.GlobalNavigated and sync JS overlay state.
    /// Called once from Program.cs before the Avalonia app starts.
    /// </summary>
    public static void Initialize()
    {
        NavigationService.GlobalNavigated += (_, screen) =>
        {
            try { SetScreen(screen == AppScreen.Game ? "game" : "menu"); }
            catch { /* touch-controls.js not loaded or setScreen unavailable */ }
        };
    }

    /// <summary>Maps to <c>window.IronVaultControls.setScreen(screen)</c>.</summary>
    [JSImport("globalThis.IronVaultControls.setScreen")]
    private static partial void SetScreen(string screen);
}
