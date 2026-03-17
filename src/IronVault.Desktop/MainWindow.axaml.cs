using Avalonia.Controls;
using Avalonia.Layout;
using Pipboy.Avalonia;
using IronVault.Core.Engine;
using IronVault.Core.Engine.Systems;
using IronVault.Desktop.ViewModels;

namespace IronVault.Desktop;

public partial class MainWindow : PipboyWindow
{
    private readonly GameViewModel _vm = new();

    // A single TextBlock reused as TitleBarContent — we just mutate its Text.
    private readonly TextBlock _titleBarText = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        FontSize          = 11,
        Opacity           = 0.75,
    };

    public MainWindow()
    {
        InitializeComponent();

        TitleBarContent = _titleBarText;

        // Pass the shared ViewModel to the views that need it
        GameView.SetViewModel(_vm);

        // Wire navigation events from the sub-views
        MenuView.StartRequested       += OnMenuStart;
        UpgradeView.ContinueRequested += OnUpgradeContinue;
        GameView.MenuRequested        += OnGameMenuRequested;

        // Keep title bar updated when score or state changes mid-play
        _vm.Engine.ScoreChanged += (_, _) => RefreshTitleBar();
        _vm.Engine.StateChanged += (_, _) => RefreshTitleBar();

        // When the engine reports a wave clear → jump to upgrade screen
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

        RefreshTitleBar();

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

    // ── Title bar ────────────────────────────────────────────────────────────

    private void RefreshTitleBar()
    {
        var eng = _vm.Engine;
        _titleBarText.Text = eng.State switch
        {
            GameState.Playing      => $"WAVE {eng.Wave:D2}  ·  {eng.Score:D5}  ·  ×{eng.Lives}",
            GameState.Paused       => $"WAVE {eng.Wave:D2}  ·  {eng.Score:D5}  ·  ×{eng.Lives}  ·  ⏸",
            GameState.WaveComplete => $"WAVE {eng.Wave:D2}  COMPLETE  ·  {eng.Score:D5}",
            GameState.GameOver     => "——  GAME OVER  ——",
            GameState.Victory      => "——  VICTORY  ——",
            _                      => "铁  窖  计  划",   // NotStarted / menu
        };
    }
}
