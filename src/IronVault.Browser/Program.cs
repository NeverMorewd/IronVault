using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using IronVault.App;

[assembly: SupportedOSPlatform("browser")]

namespace IronVault.Browser;

internal sealed partial class Program
{
    private static async Task Main(string[] args)
        => await BuildAvaloniaApp().StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .WithInterFont()
            .LogToTrace();
}
