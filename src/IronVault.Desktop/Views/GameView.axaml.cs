using Avalonia.Controls;
using Avalonia.Input;
using IronVault.Core.Engine;
using IronVault.Core.Engine.Entities;
using IronVault.Desktop.ViewModels;

namespace IronVault.Desktop.Views;

public partial class GameView : UserControl
{
    private GameViewModel? _vm;
    private readonly HashSet<Key> _heldKeys = [];

    public GameView()
    {
        InitializeComponent();

        StartButton.Click += (_, _) => StartOrRestart();
        PauseButton.Click += (_, _) => _vm?.TogglePause();

        Focusable = true;
        KeyDown += OnKeyDown;
        KeyUp   += OnKeyUp;
    }

    /// <summary>
    /// Called by MainWindow after construction to wire up the shared ViewModel.
    /// Must be called before the first frame tick.
    /// </summary>
    public void SetViewModel(GameViewModel vm)
    {
        _vm = vm;
        vm.FrameTick           += OnFrameTick;
        vm.Engine.StateChanged += OnStateChanged;
        vm.Engine.ScoreChanged += OnScoreChanged;
        GameCanvas.Attach(vm.Engine);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void StartOrRestart()
    {
        if (_vm is null) return;
        _vm.Stop();
        _vm.StartGame();          // uses Normal difficulty (menu already started game with chosen difficulty)
        PauseButton.IsEnabled = true;
        StartButton.Content   = "[REDEPLOY]";
        Focus();
    }

    private void OnFrameTick(object? sender, float dt)
    {
        // Build input state from currently held keys
        if (_vm?.Engine.Player is { } player)
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
        if (_vm is null) return;
        var eng = _vm.Engine;

        WaveText.Text    = eng.Wave.ToString("D2");
        ScoreText.Text   = eng.Score.ToString("D5");
        LivesText.Text   = new string('I', Math.Max(0, eng.Lives));
        EnemiesText.Text = eng.EnemiesLeft.ToString("D2");

        if (eng.Player is { } p && p.Health is { } hp)
        {
            // Keep Maximum + SegmentCount in sync so ArmorPlating upgrades show correctly
            HpBar.Maximum      = hp.Max;
            HpBar.SegmentCount = hp.Max;
            HpBar.Value        = hp.Current;
        }
    }

    private void OnStateChanged(object? sender, GameState state)
    {
        if (state is GameState.GameOver or GameState.Victory or GameState.WaveComplete)
        {
            PauseButton.IsEnabled = false;
            StartButton.Content   = "[REDEPLOY]";
        }
        else if (state == GameState.Playing)
        {
            PauseButton.IsEnabled = true;
        }
    }

    private void OnScoreChanged(object? sender, int score)
        => ScoreText.Text = score.ToString("D5");

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        _heldKeys.Add(e.Key);

        if (e.Key == Key.P)
            _vm?.TogglePause();
        else if (e.Key == Key.Enter && _vm?.Engine.State == GameState.NotStarted)
            StartOrRestart();
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
        => _heldKeys.Remove(e.Key);
}
