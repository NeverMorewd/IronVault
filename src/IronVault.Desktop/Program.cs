using Avalonia;
using IronVault;

namespace IronVault.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<global::IronVault.App>()
            .UsePlatformDetect()
            .WithInterFont()
            .WithIronVaultFonts()
            .LogToTrace();
}
