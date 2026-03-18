using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using IronVault.Core.Engine;
using IronVault.Navigation;
using IronVault.ViewModels;
using IronVault.Views;

namespace IronVault;

public partial class MainView : UserControl
{
    /// <summary>Exposes the game engine so MainWindow can wire up title-bar updates.</summary>
    public GameEngine Engine { get; }

    public MainView(
        INavigationService nav,
        GameViewModel      vm,
        MenuView           menuView,
        GameView           gameView,
        UpgradeView        upgradeView)
    {
        Engine = vm.Engine;

        InitializeComponent();

        // Insert all screens into the shared host grid (same cell → only IsVisible differs)
        ViewHost.Children.Add(menuView);
        ViewHost.Children.Add(gameView);
        ViewHost.Children.Add(upgradeView);

        // ── Navigation: sync visibility to the active screen ────────────────
        nav.Navigated += (_, screen) =>
        {
            menuView.IsVisible    = screen == AppScreen.Menu;
            gameView.IsVisible    = screen == AppScreen.Game;
            upgradeView.IsVisible = screen == AppScreen.Upgrade;

            switch (screen)
            {
                case AppScreen.Menu:    menuView.Focus();    break;
                case AppScreen.Game:    gameView.Focus();    break;
                case AppScreen.Upgrade: upgradeView.Focus(); break;
            }
        };

        // ── Game-flow events ─────────────────────────────────────────────────
        menuView.StartRequested += (_, args) =>
        {
            vm.StartGame(args.Difficulty, args.Mode);
            nav.NavigateTo(AppScreen.Game);
        };

        upgradeView.ContinueRequested += (_, upgrade) =>
        {
            if (upgrade.HasValue)
                vm.Engine.ApplyPlayerUpgrade(upgrade.Value);
            vm.Engine.ContinueToNextWave();
            nav.NavigateTo(AppScreen.Game);
        };

        gameView.MenuRequested += (_, _) =>
        {
            vm.Stop();
            nav.NavigateTo(AppScreen.Menu);
        };

        vm.Engine.WaveCleared += (_, wave) =>
        {
            upgradeView.Prepare(wave);
            nav.NavigateTo(AppScreen.Upgrade);
        };

        menuView.ExitRequested += (_, _) =>
        {
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.Shutdown();
        };

        // ── Initial screen ───────────────────────────────────────────────────
        nav.NavigateTo(AppScreen.Menu);
    }
}
