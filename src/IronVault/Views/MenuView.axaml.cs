using Avalonia.Controls;
using Avalonia.Input;
using IronVault.Audio;
using IronVault.Core.Engine;
using IronVault.Core.Engine.Systems;
using IronVault.Core.Localization;

namespace IronVault.Views;

public partial class MenuView : UserControl
{
    private AIDifficulty _difficulty = AIDifficulty.Normal;
    private GameMode     _mode       = GameMode.Classic;

    /// <summary>Raised when the player clicks DEPLOY.</summary>
    public event EventHandler<(AIDifficulty Difficulty, GameMode Mode)>? StartRequested;

    /// <summary>Raised when the player clicks ABORT / presses Escape.
    /// The host (MainView) decides how to handle exit based on platform.</summary>
    public event EventHandler? ExitRequested;

    public MenuView()
    {
        InitializeComponent();

        // Difficulty buttons
        EasyBtn.Click   += (_, _) => { RetroSound.PlayClick(); SetDifficulty(AIDifficulty.Easy); };
        NormalBtn.Click += (_, _) => { RetroSound.PlayClick(); SetDifficulty(AIDifficulty.Normal); };
        HardBtn.Click   += (_, _) => { RetroSound.PlayClick(); SetDifficulty(AIDifficulty.Hard); };

        // Mode buttons
        ClassicBtn.Click += (_, _) => { RetroSound.PlayClick(); SetMode(GameMode.Classic); };
        DefenseBtn.Click += (_, _) => { RetroSound.PlayClick(); SetMode(GameMode.Defense); };

        // Language toggle
        LangEnBtn.Click += (_, _) => { RetroSound.PlayClick(); I18n.Current = Language.English; RefreshText(); };
        LangZhBtn.Click += (_, _) => { RetroSound.PlayClick(); I18n.Current = Language.Chinese; RefreshText(); };

        // Action buttons
        StartBtn.Click += (_, _) => { RetroSound.PlayClick(); StartRequested?.Invoke(this, (_difficulty, _mode)); };
        ExitBtn.Click  += (_, _) => { RetroSound.PlayClick(); ExitRequested?.Invoke(this, EventArgs.Empty); };

        // Hide the exit button on platforms that don't support programmatic exit
        ExitBtn.IsVisible = !OperatingSystem.IsBrowser()
                         && !OperatingSystem.IsAndroid()
                         && !OperatingSystem.IsIOS();

        // Keyboard navigation
        Focusable = true;
        KeyDown  += OnKeyDown;

        I18n.LanguageChanged += RefreshText;

        RefreshText();
        SetDifficulty(AIDifficulty.Normal);
        SetMode(GameMode.Classic);
    }

    // ── Keyboard navigation ───────────────────────────────────────────────────

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
            case Key.A:
                CycleDifficulty(-1);
                e.Handled = true;
                break;
            case Key.Right:
            case Key.D:
                CycleDifficulty(+1);
                e.Handled = true;
                break;
            case Key.Up:
            case Key.W:
                SetMode(GameMode.Classic);
                e.Handled = true;
                break;
            case Key.Down:
            case Key.S:
                SetMode(GameMode.Defense);
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Space:
                StartRequested?.Invoke(this, (_difficulty, _mode));
                e.Handled = true;
                break;
            case Key.L:
                I18n.Current = I18n.Current == Language.English ? Language.Chinese : Language.English;
                RefreshText();
                e.Handled = true;
                break;
            case Key.Escape:
                ExitRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                break;
        }
    }

    private void CycleDifficulty(int delta)
    {
        var values = new[] { AIDifficulty.Easy, AIDifficulty.Normal, AIDifficulty.Hard };
        int idx = Array.IndexOf(values, _difficulty);
        idx = (idx + delta + values.Length) % values.Length;
        SetDifficulty(values[idx]);
    }

    // ── Text refresh ─────────────────────────────────────────────────────────

    private void RefreshText()
    {
        TitleText.Text       = I18n.T("app.title");
        SubtitleText.Text    = I18n.T("app.subtitle");
        TaglineText.Text     = I18n.T("app.tagline");
        FooterText.Text      = I18n.T("app.footer");
        ModeLabel.Text       = I18n.T("menu.mode");
        DifficultyLabel.Text = I18n.T("menu.difficulty");
        DeployText.Text      = I18n.T("menu.deploy");
        AbortText.Text       = I18n.T("menu.abort");
        LangLabel.Text       = I18n.T("menu.lang");

        LangEnBtn.Content = I18n.Current == Language.English ? "▶ EN ◀" : "  EN  ";
        LangZhBtn.Content = I18n.Current == Language.Chinese ? "▶ 中文 ◀" : "  中文  ";

        SetDifficulty(_difficulty);
        SetMode(_mode);
    }

    // ── Mode selection ────────────────────────────────────────────────────────

    private void SetMode(GameMode m)
    {
        _mode = m;
        ClassicBtn.Content = m == GameMode.Classic ? $"▶ {I18n.T("menu.classic")} ◀" : $"  {I18n.T("menu.classic")}  ";
        DefenseBtn.Content = m == GameMode.Defense ? $"▶ {I18n.T("menu.defense")} ◀" : $"  {I18n.T("menu.defense")}  ";
        ModeDesc.Text = m == GameMode.Classic ? I18n.T("menu.classic.desc") : I18n.T("menu.defense.desc");
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
