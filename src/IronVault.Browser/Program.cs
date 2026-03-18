using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using IronVault;
using IronVault.Audio;
using IronVault.Browser.Audio;

[assembly: SupportedOSPlatform("browser")]

namespace IronVault.Browser;

internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        RetroSound.BrowserBackend = new BrowserAudio();
        await BuildAvaloniaApp().StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<global::IronVault.App>()
            .WithInterFont()
            .WithIronVaultFonts()
            .LogToTrace();
}
