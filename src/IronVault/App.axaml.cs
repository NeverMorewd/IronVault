using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using Pipboy.Avalonia;

namespace IronVault;

public partial class App : Application
{
    /// <summary>Application-wide IoC container. Available after OnFrameworkInitializationCompleted.</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        PipboyThemeManager.Instance.SetPrimaryColor(Color.Parse("#FFA500"));
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddIronVaultServices();
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(Services.GetRequiredService<MainView>());
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = Services.GetRequiredService<MainView>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
