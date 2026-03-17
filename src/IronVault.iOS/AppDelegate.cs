using Avalonia;
using Avalonia.iOS;
using Foundation;
using IronVault.App;
using UIKit;

namespace IronVault.iOS;

[Register("AppDelegate")]
public class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder)
               .WithInterFont()
               .LogToTrace();
}
