using Avalonia.Controls;
using IronVault.Core.Engine;
using IronVault.Core.Engine.Systems;
using IronVault.Core.Localization;

namespace IronVault.Desktop.Views;

public partial class MenuView : UserControl
{
    private AIDifficulty _difficulty = AIDifficulty.Normal;
    private GameMode     _mode       = GameMode.Classic;

    /// <summary>Raised when the player clicks DEPLOY.  Parameter = (difficulty, mode).</summary>
    public event EventHandler<(AIDifficulty Difficulty, GameMode Mode)>? StartRequested;

    public MenuView()
    {
        InitializeComponent();

        // Difficulty buttons
        EasyBtn.Click   += (_, _) => SetDifficulty(AIDifficulty.Easy);
        NormalBtn.Click += (_, _) => SetDifficulty(AIDifficulty.Normal);
        HardBtn.Click   += (_, _) => SetDifficulty(AIDifficulty.Hard);

        // Mode buttons
        ClassicBtn.Click += (_, _) => SetMode(GameMode.Classic);
        DefenseBtn.Click += (_, _) => SetMode(GameMode.Defense);

        // Language toggle
        LangEnBtn.Click += (_, _) => { I18n.Current = Language.English; RefreshText(); };
        LangZhBtn.Click += (_, _) => { I18n.Current = Language.Chinese; RefreshText(); };

        // Action buttons
        StartBtn.Click += (_, _) => StartRequested?.Invoke(this, (_difficulty, _mode));
        ExitBtn.Click  += (_, _) => Environment.Exit(0);

        // Subscribe to language changes
        I18n.LanguageChanged += RefreshText;

        // Initial state
        RefreshText();
        SetDifficulty(AIDifficulty.Normal);
        SetMode(GameMode.Classic);
    }

    // ── Text refresh ─────────────────────────────────────────────────────────

    private void RefreshText()
    {
        TitleText.Text      = I18n.T("app.title");
        SubtitleText.Text   = I18n.T("app.subtitle");
        TaglineText.Text    = I18n.T("app.tagline");
        FooterText.Text     = I18n.T("app.footer");
        ModeLabel.Text      = I18n.T("menu.mode");
        DifficultyLabel.Text = I18n.T("menu.difficulty");
        DeployText.Text     = I18n.T("menu.deploy");
        AbortText.Text      = I18n.T("menu.abort");
        LangLabel.Text      = I18n.T("menu.lang");

        // Language button labels — mark the active one
        LangEnBtn.Content = I18n.Current == Language.English ? "▶ EN ◀" : "  EN  ";
        LangZhBtn.Content = I18n.Current == Language.Chinese ? "▶ 中文 ◀" : "  中文  ";

        // Re-apply current selections so descriptions refresh in new language
        SetDifficulty(_difficulty);
        SetMode(_mode);
    }

    // ── Mode selection ────────────────────────────────────────────────────────

    private void SetMode(GameMode m)
    {
        _mode = m;

        ClassicBtn.Content = m == GameMode.Classic ? $"▶ {I18n.T("menu.classic")} ◀" : $"  {I18n.T("menu.classic")}  ";
        DefenseBtn.Content = m == GameMode.Defense ? $"▶ {I18n.T("menu.defense")} ◀" : $"  {I18n.T("menu.defense")}  ";

        ModeDesc.Text = m == GameMode.Classic
            ? I18n.T("menu.classic.desc")
            : I18n.T("menu.defense.desc");
    }

    // ── Difficulty selection ──────────────────────────────────────────────────

    private void SetDifficulty(AIDifficulty d)
    {
        _difficulty = d;

        EasyBtn.Content   = d == AIDifficulty.Easy   ? $"▶ {I18n.T("diff.easy")} ◀"   : $"  {I18n.T("diff.easy")}  ";
        NormalBtn.Content = d == AIDifficulty.Normal ? $"▶ {I18n.T("diff.normal")} ◀" : $"  {I18n.T("diff.normal")}  ";
        HardBtn.Content   = d == AIDifficulty.Hard   ? $"▶ {I18n.T("diff.hard")} ◀"   : $"  {I18n.T("diff.hard")}  ";

        DiffDesc.Text = d switch
        {
            AIDifficulty.Easy   => I18n.T("diff.easy.desc"),
            AIDifficulty.Normal => I18n.T("diff.normal.desc"),
            AIDifficulty.Hard   => I18n.T("diff.hard.desc"),
            _                   => ""
        };
    }
}
