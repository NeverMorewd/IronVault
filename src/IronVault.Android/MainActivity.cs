using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace IronVault.Android;

[Activity(
    Label = "Iron Vault",
    Theme = "@style/MyTheme.NoActionBar",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<global::IronVault.App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder)
               .WithInterFont()
               .LogToTrace();
}
