using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Pipboy.Avalonia;
using IronVault.Core.Engine;

namespace IronVault;

public partial class MainWindow : PipboyWindow
{
    private readonly TextBlock _titleBarText = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        FontSize          = 11,
        Opacity           = 0.75,
    };

    // Natural (non-fullscreen) window dimensions — keep in sync with AXAML Width/Height.
    private const double NormalWidth  = 858;
    private const double NormalHeight = 940;

    public MainWindow(MainView mainView)
    {
        InitializeComponent();
        Content = mainView;
        TitleBarContent = _titleBarText;

        // F11 full-screen toggle — Windows desktop only.
        // Other platforms (Browser/Android/iOS) manage their own viewport.
        if (OperatingSystem.IsWindows() && !OperatingSystem.IsBrowser())
        {
            KeyDown += OnWindowKeyDown;
        }

        var eng = mainView.Engine;
        eng.ScoreChanged += (_, _) => RefreshTitleBar(eng);
        eng.StateChanged += (_, _) => RefreshTitleBar(eng);

        RefreshTitleBar(eng);
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.F11) return;
        e.Handled = true;

        if (WindowState == WindowState.FullScreen)
        {
            WindowState = WindowState.Normal;
            Width       = NormalWidth;
            Height      = NormalHeight;
        }
        else
        {
            WindowState = WindowState.FullScreen;
        }
    }

    private void RefreshTitleBar(GameEngine eng)
    {
        _titleBarText.Text = eng.State switch
        {
            GameState.Playing      => $"WAVE {eng.Wave:D2}  ·  {eng.Score:D5}  ·  ×{eng.Lives}",
            GameState.Paused       => $"WAVE {eng.Wave:D2}  ·  {eng.Score:D5}  ·  ×{eng.Lives}  ·  ⏸",
            GameState.WaveComplete => $"WAVE {eng.Wave:D2}  COMPLETE  ·  {eng.Score:D5}",
            GameState.GameOver     => "——  GAME OVER  ——",
            GameState.Victory      => "——  VICTORY  ——",
            _                      => "铁  窖  计  划",
        };
    }
}
