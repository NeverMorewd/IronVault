using System.Runtime.InteropServices.JavaScript;
using IronVault.Input;
using IronVault.Navigation;

namespace IronVault.Browser.Navigation;

/// <summary>
/// Bridges C# navigation events and touch-input feedback to the JavaScript
/// touch-controls overlay so the virtual gamepad is only visible during
/// active gameplay and the knob reacts to finger movement.
/// </summary>
internal static partial class BrowserNavBridge
{
    /// <summary>
    /// Subscribe to NavigationService.GlobalNavigated and sync JS overlay state.
    /// Also wires the knob-update delegate so GameView can push knob position
    /// back to the JS visual without depending on the browser project.
    /// Called once from Program.cs before the Avalonia app starts.
    /// </summary>
    public static void Initialize()
    {
        // Show/hide the HTML overlay when the active screen changes
        NavigationService.GlobalNavigated += (_, screen) =>
        {
            try { SetScreen(screen == AppScreen.Game ? "game" : "menu"); }
            catch { /* touch-controls.js not loaded — ignore */ }
        };

        // Wire knob feedback: GameView calls TouchInputState.KnobUpdated
        // which routes here to update the JS knob position.
        TouchInputState.KnobUpdated = (dx, dy) =>
        {
            try { SetKnob(dx, dy); }
            catch { /* overlay not loaded — ignore */ }
        };
    }

    /// <summary>Maps to <c>window.IronVaultControls.setScreen(screen)</c>.</summary>
    [JSImport("globalThis.IronVaultControls.setScreen")]
    private static partial void SetScreen(string screen);

    /// <summary>Maps to <c>window.IronVaultControls.setKnob(dx, dy)</c>.</summary>
    [JSImport("globalThis.IronVaultControls.setKnob")]
    private static partial void SetKnob(double dx, double dy);
}
