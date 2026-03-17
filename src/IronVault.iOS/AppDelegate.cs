using Avalonia;
using Avalonia.iOS;
using Foundation;
using IronVault;
using UIKit;

namespace IronVault.iOS;

[Register("AppDelegate")]
public class AppDelegate : AvaloniaAppDelegate<global::IronVault.App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder)
               .WithInterFont()
               .WithIronVaultFonts()
               .LogToTrace();
}
