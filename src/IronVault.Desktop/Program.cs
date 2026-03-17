using Avalonia;
using IronVaultApp = IronVault.App.App;

namespace IronVault.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<IronVaultApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
