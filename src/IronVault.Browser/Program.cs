using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;

[assembly: SupportedOSPlatform("browser")]

namespace IronVault.Browser;

internal sealed partial class Program
{
    private static async Task Main(string[] args)
        => await BuildAvaloniaApp().StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<global::IronVault.App>()
            .WithInterFont()
            .LogToTrace();
}
