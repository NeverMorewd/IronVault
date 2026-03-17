using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using IronVault.Desktop.Views;
using Pipboy.Avalonia;

namespace IronVault.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        // Amber CRT color — keeps the military aesthetic consistent with the desktop build.
        PipboyThemeManager.Instance.SetPrimaryColor(Color.Parse("#FFA500"));
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
