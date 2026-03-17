using Avalonia.Controls;
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

    public MainWindow()
    {
        InitializeComponent();

        TitleBarContent = _titleBarText;

        var eng = RootView.Engine;
        eng.ScoreChanged += (_, _) => RefreshTitleBar(eng);
        eng.StateChanged += (_, _) => RefreshTitleBar(eng);

        RefreshTitleBar(eng);
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
