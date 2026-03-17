using Avalonia.Controls;
using IronVault.Core.Engine;
using IronVault.Core.Engine.Systems;
using IronVault.Desktop.ViewModels;

namespace IronVault.Desktop.Views;

/// <summary>
/// Browser root view — mirrors MainWindow's navigation logic but runs as a
/// <see cref="UserControl"/> (required by <c>ISingleViewApplicationLifetime</c>).
/// </summary>
public partial class MainView : UserControl
{
    private readonly GameViewModel _vm = new();

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

        ShowScreen(AppScreen.Menu);
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    private enum AppScreen { Menu, Game, Upgrade }

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
