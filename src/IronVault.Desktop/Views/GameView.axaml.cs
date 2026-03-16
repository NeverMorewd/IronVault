using Avalonia.Controls;
using Avalonia.Input;
using IronVault.Core.Engine;
using IronVault.Core.Engine.Entities;
using IronVault.Desktop.ViewModels;

namespace IronVault.Desktop.Views;

public partial class GameView : UserControl
{
    private readonly GameViewModel _vm = new();
    private readonly HashSet<Key> _heldKeys = [];

    public GameView()
    {
        InitializeComponent();

        _vm.FrameTick += OnFrameTick;
        _vm.Engine.StateChanged  += OnStateChanged;
        _vm.Engine.ScoreChanged  += OnScoreChanged;

        GameCanvas.Attach(_vm.Engine);

        StartButton.Click += (_, _) => StartOrRestart();
        PauseButton.Click += (_, _) => _vm.TogglePause();

        Focusable = true;
        KeyDown += OnKeyDown;
        KeyUp   += OnKeyUp;
    }

    private void StartOrRestart()
    {
        _vm.Stop();
        _vm.StartGame();
        PauseButton.IsEnabled = true;
        StartButton.Content   = "[REDEPLOY]";
        Focus();
    }

    private void OnFrameTick(object? sender, float dt)
    {
        // Build input state from held keys
        if (_vm.Engine.Player is { } player)
        {
            player.Input = new TankInput(
                MoveUp:    _heldKeys.Contains(Key.W) || _heldKeys.Contains(Key.Up),
                MoveDown:  _heldKeys.Contains(Key.S) || _heldKeys.Contains(Key.Down),
                MoveLeft:  _heldKeys.Contains(Key.A) || _heldKeys.Contains(Key.Left),
                MoveRight: _heldKeys.Contains(Key.D) || _heldKeys.Contains(Key.Right),
                Fire:      _heldKeys.Contains(Key.Space)
            );
        }

        GameCanvas.Tick(dt);
        UpdateHud();
    }

    private void UpdateHud()
    {
        WaveText.Text    = _vm.Engine.Wave.ToString("D2");
        ScoreText.Text   = _vm.Engine.Score.ToString("D5");
        LivesText.Text   = new string('I', Math.Max(0, _vm.Engine.Lives));
        EnemiesText.Text = _vm.Engine.EnemiesLeft.ToString("D2");

        if (_vm.Engine.Player is { } p && p.Health is { } hp)
            HpBar.Value = hp.Current;
    }

    private void OnStateChanged(object? sender, GameState state)
    {
        if (state is GameState.GameOver or GameState.Victory)
        {
            PauseButton.IsEnabled = false;
            StartButton.Content   = "[REDEPLOY]";
        }
    }

    private void OnScoreChanged(object? sender, int score)
        => ScoreText.Text = score.ToString("D5");

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        _heldKeys.Add(e.Key);

        if (e.Key == Key.P)
            _vm.TogglePause();
        else if (e.Key == Key.Enter && _vm.Engine.State == GameState.NotStarted)
            StartOrRestart();
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
        => _heldKeys.Remove(e.Key);
}
