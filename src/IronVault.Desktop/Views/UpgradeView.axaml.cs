using Avalonia.Controls;
using IronVault.Core.Engine;
using IronVault.Core.Localization;

namespace IronVault.Desktop.Views;

public partial class UpgradeView : UserControl
{
    private UpgradeType[] _choices = new UpgradeType[3];
    private bool _grantsAlly;

    // Keep the last engine/wave so we can re-populate on language change
    private GameEngine? _lastEngine;
    private int         _lastWave;

    /// <summary>Raised when the player picks an upgrade or skips.  Parameter = null for skip.</summary>
    public event EventHandler<UpgradeType?>? ContinueRequested;

    public UpgradeView()
    {
        InitializeComponent();

        UpBtn0.Click  += (_, _) => ContinueRequested?.Invoke(this, _choices[0]);
        UpBtn1.Click  += (_, _) => ContinueRequested?.Invoke(this, _choices[1]);
        UpBtn2.Click  += (_, _) => ContinueRequested?.Invoke(this, _choices[2]);
        SkipBtn.Click += (_, _) => ContinueRequested?.Invoke(this, null);

        I18n.LanguageChanged += OnLanguageChanged;
        RefreshStaticText();
    }

    /// <summary>
    /// Called by MainWindow just before this view is made visible.
    /// Populates the wave-clear stats and generates 3 upgrade choices.
    /// </summary>
    public void Prepare(int clearedWave, GameEngine engine)
    {
        _lastEngine = engine;
        _lastWave   = clearedWave;

        WaveClearedText.Text = FormatWaveCleared(clearedWave);
        ScoreText.Text       = engine.Score.ToString("D5");
        NextWaveText.Text    = $"{clearedWave + 1:D2}";

        _grantsAlly = WaveScript.ForWave(clearedWave + 1).GrantsAlly;
        AllyRewardBanner.IsVisible = _grantsAlly;

        _choices = GenerateChoices(engine);
        PopulateCards();
        RefreshStaticText();
    }

    // ── Localisation ─────────────────────────────────────────────────────────

    private void OnLanguageChanged()
    {
        RefreshStaticText();
        // Refresh dynamic data if we have it
        if (_lastEngine is not null)
        {
            WaveClearedText.Text = FormatWaveCleared(_lastWave);
            PopulateCards();
        }
    }

    private void RefreshStaticText()
    {
        DebriefLabel.Text   = I18n.T("upg.debrief");
        ScoreLabel.Text     = I18n.T("upg.score");
        NextWaveLabel.Text  = I18n.T("upg.next_wave");
        SelectHeader.Text   = I18n.T("upg.select_hdr");
        SelectHint0.Text    = I18n.T("upg.select");
        SelectHint1.Text    = I18n.T("upg.select");
        SelectHint2.Text    = I18n.T("upg.select");
        SkipText.Text       = I18n.T("upg.skip");
        AllyRewardText.Text = I18n.T("upg.ally");
    }

    private void PopulateCards()
    {
        var slots = new[]
        {
            (UpIcon0, UpName0, UpDesc0),
            (UpIcon1, UpName1, UpDesc1),
            (UpIcon2, UpName2, UpDesc2),
        };

        for (int i = 0; i < 3; i++)
        {
            var info        = UpgradeDescriptions.For(_choices[i]);
            slots[i].Item1.Text = info.Icon;
            slots[i].Item2.Text = info.Name;
            slots[i].Item3.Text = info.Desc;
        }
    }

    private static string FormatWaveCleared(int wave)
        => $"{I18n.T("hud.wave")}  {wave:D2}  {(I18n.Current == Language.Chinese ? "通过" : "CLEARED")}";

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static UpgradeType[] GenerateChoices(GameEngine engine)
    {
        var pool = new List<UpgradeType>
        {
            UpgradeType.ArmorPlating,
            UpgradeType.NitroBoosters,
            UpgradeType.RapidFireSystem,
            UpgradeType.DualCannon,
            UpgradeType.ArmourPiercing,
            UpgradeType.RepairKit,
        };

        var player = engine.Player;

        // Only offer RepairKit when the player is actually damaged
        if (player?.Health.Current >= player?.Health.Max)
            pool.Remove(UpgradeType.RepairKit);

        // Only offer ArmourPiercing before the player has it
        if (player?.Weapon.Power >= 2)
            pool.Remove(UpgradeType.ArmourPiercing);

        // Only offer DualCannon below the shell cap
        if (player?.Weapon.MaxBullets >= 5)
            pool.Remove(UpgradeType.DualCannon);

        // Shuffle → take 3
        var rng = new Random();
        return [..pool.OrderBy(_ => rng.Next()).Take(3)];
    }
}
