using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using IronVault.App.ViewModels;
using IronVault.Core.Engine;
using IronVault.Core.Engine.Systems;

namespace IronVault.App;

public partial class MainView : UserControl
{
    private readonly GameViewModel _vm = new();

    /// <summary>Exposes the game engine so MainWindow can wire up title-bar updates.</summary>
    public GameEngine Engine => _vm.Engine;

    private enum AppScreen { Menu, Game, Upgrade }

    public MainView()
    {
        InitializeComponent();

        GameView.SetViewModel(_vm);

        MenuView.StartRequested       += OnMenuStart;
        UpgradeView.ContinueRequested += OnUpgradeContinue;
        GameView.MenuRequested        += OnGameMenuRequested;

        _vm.Engine.WaveCleared += (_, wave) =>
        {
            UpgradeView.Prepare(wave, _vm.Engine);
            ShowScreen(AppScreen.Upgrade);
        };

        // On desktop platforms shut the window; on Browser/Android/iOS the OS owns
        // the app lifecycle so we just ignore the exit request.
        MenuView.ExitRequested += (_, _) =>
        {
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.Shutdown();
        };

        ShowScreen(AppScreen.Menu);
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    private void ShowScreen(AppScreen screen)
    {
        MenuView.IsVisible    = screen == AppScreen.Menu;
        GameView.IsVisible    = screen == AppScreen.Game;
        UpgradeView.IsVisible = screen == AppScreen.Upgrade;

        switch (screen)
        {
            case AppScreen.Menu:    MenuView.Focus();    break;
            case AppScreen.Game:    GameView.Focus();    break;
            case AppScreen.Upgrade: UpgradeView.Focus(); break;
        }
    }

    private void OnGameMenuRequested(object? sender, EventArgs _)
    {
        _vm.Stop();
        ShowScreen(AppScreen.Menu);
    }

    private void OnMenuStart(object? sender, (AIDifficulty Difficulty, GameMode Mode) args)
    {
        _vm.StartGame(args.Difficulty, args.Mode);
        ShowScreen(AppScreen.Game);
    }

    private void OnUpgradeContinue(object? sender, UpgradeType? upgrade)
    {
        if (upgrade.HasValue)
            _vm.Engine.ApplyPlayerUpgrade(upgrade.Value);

        _vm.Engine.ContinueToNextWave();
        ShowScreen(AppScreen.Game);
    }
}
