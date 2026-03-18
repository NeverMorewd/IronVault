using Avalonia.Controls;
using Avalonia.Input;
using IronVault.Audio;
using IronVault.Core.Map;
using IronVault.Core.Localization;

namespace IronVault.Views;

public partial class LevelSelectView : UserControl
{
    private int _selectedLevel = 1;

    /// <summary>Raised when the player confirms a level. Parameter = level number (1-100).</summary>
    public event EventHandler<int>? LevelSelected;

    /// <summary>Raised when the player presses Back.</summary>
    public event EventHandler? BackRequested;

    public LevelSelectView()
    {
        InitializeComponent();

        BackBtn.Click += (_, _) => { RetroSound.PlayClick(); BackRequested?.Invoke(this, EventArgs.Empty); };

        Focusable = true;
        KeyDown  += OnKeyDown;

        I18n.LanguageChanged += RefreshText;
        RefreshText();
        BuildGrid();
    }

    // ── Called when this view becomes visible ────────────────────────────────

    public void Activate(int startLevel = 1)
    {
        _selectedLevel = Math.Clamp(startLevel, 1, MapLibrary.TotalLevels);
        UpdateSelection();
        Focus();
    }

    // ── Grid construction ────────────────────────────────────────────────────

    private void BuildGrid()
    {
        LevelGrid.Children.Clear();
        for (int i = 1; i <= MapLibrary.TotalLevels; i++)
        {
            int lvl = i; // capture
            var btn = new Button
            {
                Width   = 44,
                Height  = 32,
                Margin  = new Avalonia.Thickness(2),
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Content = lvl.ToString(),
                Tag     = lvl,
            };
            btn.Click += (_, _) =>
            {
                RetroSound.PlayClick();
                _selectedLevel = lvl;
                UpdateSelection();
                ConfirmSelection();
            };
            LevelGrid.Children.Add(btn);
        }
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        foreach (var child in LevelGrid.Children)
        {
            if (child is Button btn && btn.Tag is int lvl)
            {
                if (lvl == _selectedLevel)
                    btn.Classes.Add("accent");
                else
                    btn.Classes.Remove("accent");
            }
        }
    }

    private void ConfirmSelection()
    {
        LevelSelected?.Invoke(this, _selectedLevel);
    }

    // ── Keyboard navigation ───────────────────────────────────────────────────

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
            case Key.A:
                _selectedLevel = Math.Max(1, _selectedLevel - 1);
                UpdateSelection();
                e.Handled = true;
                break;

            case Key.Right:
            case Key.D:
                _selectedLevel = Math.Min(MapLibrary.TotalLevels, _selectedLevel + 1);
                UpdateSelection();
                e.Handled = true;
                break;

            case Key.Up:
            case Key.W:
                _selectedLevel = Math.Max(1, _selectedLevel - 10);
                UpdateSelection();
                e.Handled = true;
                break;

            case Key.Down:
            case Key.S:
                _selectedLevel = Math.Min(MapLibrary.TotalLevels, _selectedLevel + 10);
                UpdateSelection();
                e.Handled = true;
                break;

            case Key.Enter:
            case Key.Space:
                RetroSound.PlayClick();
                ConfirmSelection();
                e.Handled = true;
                break;

            case Key.Escape:
                RetroSound.PlayClick();
                BackRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                break;
        }
    }

    // ── Localisation ─────────────────────────────────────────────────────────

    private void RefreshText()
    {
        SelectLabel.Text = I18n.T("level.select");
        BackText.Text    = I18n.T("level.back");

        HintText.Text = I18n.Current == Language.Chinese
            ? "方向键移动 · 回车确认出击"
            : "Arrow keys to navigate · ENTER to deploy";
    }
}
