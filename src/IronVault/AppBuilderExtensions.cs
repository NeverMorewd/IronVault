using Avalonia;
using Avalonia.Media.Fonts;

namespace IronVault;

public static class AppBuilderExtensions
{
    /// <summary>
    /// Registers the embedded Noto Sans SC font so Chinese characters render
    /// correctly on all platforms (especially WASM which has no system fonts).
    /// </summary>
    public static AppBuilder WithIronVaultFonts(this AppBuilder builder)
        => builder.ConfigureFonts(fontManager =>
        {
            fontManager.AddFontCollection(new EmbeddedFontCollection(
                new Uri("fonts:IronVault", UriKind.Absolute),
                new Uri("avares://IronVault/Assets/Fonts", UriKind.Absolute)));
        });
}
