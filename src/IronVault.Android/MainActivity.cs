using Android.Content.PM;
using Avalonia.Android;

namespace IronVault.Android;


[Activity(Label = "Iron Vault", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", MainLauncher = true, Exported = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
