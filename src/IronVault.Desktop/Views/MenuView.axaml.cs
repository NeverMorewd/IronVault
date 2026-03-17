using Avalonia.Controls;
using IronVault.Core.Engine.Systems;

namespace IronVault.Desktop.Views;

public partial class MenuView : UserControl
{
    private AIDifficulty _difficulty = AIDifficulty.Normal;

    /// <summary>Raised when the player clicks DEPLOY.  Parameter = chosen difficulty.</summary>
    public event EventHandler<AIDifficulty>? StartRequested;

    public MenuView()
    {
        InitializeComponent();

        EasyBtn.Click   += (_, _) => SetDifficulty(AIDifficulty.Easy);
        NormalBtn.Click += (_, _) => SetDifficulty(AIDifficulty.Normal);
        HardBtn.Click   += (_, _) => SetDifficulty(AIDifficulty.Hard);
        StartBtn.Click  += (_, _) => StartRequested?.Invoke(this, _difficulty);
        ExitBtn.Click   += (_, _) => Environment.Exit(0);

        SetDifficulty(AIDifficulty.Normal); // default selection
    }

    private void SetDifficulty(AIDifficulty d)
    {
        _difficulty = d;

        // Update button labels — prefix the active one with ▶
        EasyBtn.Content   = d == AIDifficulty.Easy   ? "▶ ROOKIE  ◀" : "  ROOKIE  ";
        NormalBtn.Content = d == AIDifficulty.Normal ? "▶ VETERAN ◀" : "  VETERAN ";
        HardBtn.Content   = d == AIDifficulty.Hard   ? "▶ ELITE   ◀" : "  ELITE   ";

        DiffDesc.Text = d switch
        {
            AIDifficulty.Easy   => "ROOKIE — enemies roam randomly, slow fire rate, easy to dodge",
            AIDifficulty.Normal => "VETERAN — A* pathfinding, fires when target is aligned",
            AIDifficulty.Hard   => "ELITE — aggressive A*, bullet dodging, rapid fire rate",
            _                   => ""
        };
    }
}
