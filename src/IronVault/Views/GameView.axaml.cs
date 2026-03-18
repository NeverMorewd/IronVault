using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using IronVault.Audio;
using IronVault.ViewModels;
using IronVault.Core.Engine;
using IronVault.Core.Engine.Entities;
using IronVault.Core.Localization;

namespace IronVault.Views;

public partial class GameView : UserControl
{
    private GameViewModel? _vm;
    private readonly HashSet<Key> _heldKeys = [];

    // Tracks whether an overlay auto-paused the game so we can resume on close.
    private bool _pausedByOverlay;

    /// <summary>Raised when the player confirms returning to the main menu.</summary>
    public event EventHandler? MenuRequested;

    public GameView()
    {
        InitializeComponent();

        // Status-bar icon buttons
        SettingsBtn.Click += (_, _) => ShowSettingsOverlay();
        CloseBtn.Click    += (_, _) => ShowExitOverlay();

        // Settings overlay
        StartButton.Click += (_, _) => StartOrRestart();
        ResumeBtn.Click   += (_, _) => HideSettingsOverlay();

        // Exit overlay
        ExitToMenuBtn.Click += (_, _) => RequestMenu();
        ExitQuitBtn.Click   += (_, _) => QuitApp();
        ExitCancelBtn.Click += (_, _) => HideExitOverlay();

        // Quit only makes sense on desktop
        ExitQuitBtn.IsVisible = !OperatingSystem.IsBrowser()
                             && !OperatingSystem.IsAndroid()
                             && !OperatingSystem.IsIOS();

        Focusable = true;
        KeyDown += OnKeyDown;
        KeyUp   += OnKeyUp;

        I18n.LanguageChanged += RefreshText;
        RefreshText();
    }

    /// <summary>
    /// Called by MainView after construction to wire up the shared ViewModel.
    /// Must be called before the first frame tick.
    /// </summary>
    public void SetViewModel(GameViewModel vm)
    {
        _vm = vm;
        vm.FrameTick               += OnFrameTick;
        vm.Engine.StateChanged     += OnStateChanged;
        vm.Engine.ScoreChanged     += OnScoreChanged;
        vm.Engine.ShotFired        += (_, _) => RetroSound.PlayShoot();
        vm.Engine.HitOccurred      += (_, _) => RetroSound.PlayExplosion();
        vm.Engine.EnemyDestroyed   += (_, _) => RetroSound.PlayEnemyDestroyed();
        vm.Engine.PlayerHurt       += (_, _) => RetroSound.PlayPlayerHurt();
        vm.Engine.PowerUpCollected += (_, _) => RetroSound.PlayPowerUp();
        GameCanvas.Attach(vm.Engine);
    }

    // ── Overlay management ────────────────────────────────────────────────────

    private void ShowSettingsOverlay()
    {
        if (_vm?.Engine.State == GameState.Playing)
        {
            _vm.TogglePause();
            _pausedByOverlay = true;
        }
        SettingsOverlay.IsVisible = true;
        ExitOverlay.IsVisible     = false;
    }

    private void HideSettingsOverlay()
    {
        SettingsOverlay.IsVisible = false;
        if (_pausedByOverlay)
        {
            _pausedByOverlay = false;
            _vm?.TogglePause();
        }
        Focus();
    }

    private void ShowExitOverlay()
    {
        if (_vm?.Engine.State == GameState.Playing)
        {
            _vm.TogglePause();
            _pausedByOverlay = true;
        }
        ExitOverlay.IsVisible     = true;
        SettingsOverlay.IsVisible = false;
    }

    private void HideExitOverlay()
    {
        ExitOverlay.IsVisible = false;
        if (_pausedByOverlay)
        {
            _pausedByOverlay = false;
            _vm?.TogglePause();
        }
        Focus();
    }

    private void RequestMenu()
    {
        RetroSound.StopMovement();
        RetroSound.PlayClick();
        _vm?.Stop();
        _pausedByOverlay          = false;
        ExitOverlay.IsVisible     = false;
        SettingsOverlay.IsVisible = false;
        MenuRequested?.Invoke(this, EventArgs.Empty);
    }

    private static void QuitApp()
        => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
               ?.Shutdown();

    // ── Localisation ─────────────────────────────────────────────────────────

    private void RefreshText()
    {
        TitleText.Text         = I18n.T("hud.title");
        ModeLabel.Text         = I18n.T("hud.mode");
        WaveLabel.Text         = I18n.T("hud.wave");
        ScoreLabel.Text        = I18n.T("hud.score");
        LivesLabel.Text        = I18n.T("hud.lives");
        EnemiesLabel.Text      = I18n.T("hud.enemies");
        ArmorLabel.Text        = I18n.T("hud.armor");
        EffectsLabel.Text      = I18n.T("hud.effects");
        ControlsLabel.Text     = I18n.T("hud.controls");
        CtrlMove.Text          = I18n.T("hud.ctrl.move");
        CtrlFire.Text          = I18n.T("hud.ctrl.fire");
        CtrlPause.Text         = I18n.T("hud.ctrl.pause");
        CtrlStart.Text         = I18n.T("hud.ctrl.start");
        SettingsTitleText.Text = I18n.T("overlay.settings");
        ResumeBtn.Content      = I18n.T("overlay.resume");
        ExitTitleText.Text     = I18n.T("overlay.exit.title");
        ExitToMenuBtn.Content  = I18n.T("overlay.exit.menu");
        ExitQuitBtn.Content    = I18n.T("overlay.exit.quit");
        ExitCancelBtn.Content  = I18n.T("overlay.exit.cancel");

        bool isStarted = _vm?.Engine.State != GameState.NotStarted;
        StartButton.Content = isStarted ? I18n.T("btn.redeploy") : I18n.T("btn.deploy");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void StartOrRestart()
    {
        if (_vm is null) return;
        RetroSound.StopMovement();
        _vm.Stop();
        _vm.StartGame();
        _pausedByOverlay          = false;
        SettingsOverlay.IsVisible = false;
        StartButton.Content       = I18n.T("btn.redeploy");
        Focus();
    }

    private void OnFrameTick(object? sender, float dt)
    {
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

        bool engineRunning = _vm?.Engine is { State: GameState.Playing } eng
                          && eng.Player  is { IsAlive: true, Velocity.IsMoving: true };

        if (engineRunning)
            RetroSound.StartMovement();
        else
            RetroSound.StopMovement();

        UpdateHud();
    }

    private void UpdateHud()
    {
        if (_vm is null) return;
        var eng = _vm.Engine;

        ModeText.Text = eng.Mode == GameMode.Defense
            ? I18n.T("menu.defense")
            : I18n.T("menu.classic");

        WaveText.Text = eng.TotalWaves > 0
            ? $"{eng.Wave:D2}/{eng.TotalWaves}"
            : eng.Wave.ToString("D2");

        ScoreText.Text   = eng.Score.ToString("D5");
        LivesText.Text   = new string('I', Math.Max(0, eng.Lives));
        EnemiesText.Text = eng.EnemiesLeft.ToString("D2");

        if (eng.Player is { } p && p.Health is { } hp)
        {
            HpBar.Maximum      = hp.Max;
            HpBar.SegmentCount = hp.Max;
            HpBar.Value        = hp.Current;
        }

        bool starOn    = eng.StarTimer        > 0;
        bool clockOn   = eng.ClockTimer       > 0;
        bool shovelOn  = eng.ShovelTimer      > 0;
        bool boostOn   = eng.BulletBoostTimer > 0;
        bool anyEffect = starOn || clockOn || shovelOn || boostOn;

        EffectsPanel.IsVisible     = anyEffect;
        StarEffectText.IsVisible   = starOn;
        ClockEffectText.IsVisible  = clockOn;
        ShovelEffectText.IsVisible = shovelOn;
        BoostEffectText.IsVisible  = boostOn;

        if (starOn)   StarEffectText.Text   = $"{I18n.T("pu.star")}  {eng.StarTimer:F1}s";
        if (clockOn)  ClockEffectText.Text  = $"{I18n.T("pu.clock")} {eng.ClockTimer:F1}s";
        if (shovelOn) ShovelEffectText.Text = $"{I18n.T("pu.shovel")}{eng.ShovelTimer:F1}s";
        if (boostOn)  BoostEffectText.Text  = $"{I18n.T("pu.boost")} {eng.BulletBoostTimer:F1}s";
    }

    private void OnStateChanged(object? sender, GameState state)
    {
        if (state is GameState.GameOver or GameState.Victory or GameState.WaveComplete)
            StartButton.Content = I18n.T("btn.redeploy");

        if (state != GameState.Playing)
            RetroSound.StopMovement();

        if (state == GameState.GameOver) RetroSound.PlayGameOver();
        if (state == GameState.Victory)  RetroSound.PlayVictory();
    }

    private void OnScoreChanged(object? sender, int score)
        => ScoreText.Text = score.ToString("D5");

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        _heldKeys.Add(e.Key);

        switch (e.Key)
        {
            case Key.P:
                // If settings overlay is open, P resumes; otherwise toggle pause normally
                if (SettingsOverlay.IsVisible)
                    HideSettingsOverlay();
                else
                    _vm?.TogglePause();
                break;

            case Key.Enter when _vm?.Engine.State == GameState.NotStarted:
                StartOrRestart();
                break;

            case Key.Escape:
                if (SettingsOverlay.IsVisible)
                    HideSettingsOverlay();
                else if (ExitOverlay.IsVisible)
                    HideExitOverlay();
                else
                    ShowExitOverlay();
                break;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
        => _heldKeys.Remove(e.Key);
}
