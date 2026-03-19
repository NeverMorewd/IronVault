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

    // ── Map theme names (20 designs, cycling) ────────────────────────────────

    private static readonly string[] _themeEn =
    [
        "Deployment",   "Block Wall",     "River Crossing", "Forest Haven",
        "Steel Curtain","Ice Rink",       "Fortress",       "Canal City",
        "Mixed Terrain","Checkerboard",   "Labyrinth",      "Island Defense",
        "Steel Grid",   "Forest Ambush",  "Frozen Tundra",  "Trench Warfare",
        "Pillbox",      "Canyon",         "Siege Walls",    "Grand Battle",
    ];

    private static readonly string[] _themeZh =
    [
        "基本部署", "砖墙迷城", "渡河战役", "丛林隐蔽",
        "钢铁壁垒", "冰原滑行", "要塞攻坚", "运河城市",
        "混合地形", "棋盘格局", "迷宫围困", "岛屿防线",
        "钢格战场", "丛林伏击", "冰封苔原", "壕沟战争",
        "碉堡防御", "峡谷通道", "围城攻势", "大  决  战",
    ];

    // ── Constructor ───────────────────────────────────────────────────────────

    public LevelSelectView()
    {
        InitializeComponent();

        DeployBtn.Click += (_, _) => { RetroSound.PlayClick(); ConfirmSelection(); };
        BackBtn.Click   += (_, _) => { RetroSound.PlayClick(); BackRequested?.Invoke(this, EventArgs.Empty); };

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
            int lvl = i; // capture for closure
            var btn = new Button
            {
                Width   = 56,
                Height  = 44,   // ≥44px touch target (Apple / Material guideline)
                Margin  = new Avalonia.Thickness(2),
                Padding = new Avalonia.Thickness(2, 0),
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                FontSize = 11,
                Content  = lvl.ToString(),
                Tag      = lvl,
            };
            // Click = select only (Enter / Deploy button confirms)
            btn.Click += (_, _) =>
            {
                RetroSound.PlayClick();
                _selectedLevel = lvl;
                UpdateSelection();
            };
            LevelGrid.Children.Add(btn);
        }
        UpdateSelection();
    }

    // ── Selection state ───────────────────────────────────────────────────────

    private void UpdateSelection()
    {
        foreach (var child in LevelGrid.Children)
        {
            if (child is not Button btn || btn.Tag is not int lvl) continue;
            if (lvl == _selectedLevel)
                btn.Classes.Add("accent");
            else
                btn.Classes.Remove("accent");
        }

        UpdateInfoBar();
    }

    private void UpdateInfoBar()
    {
        int themeIdx = ((_selectedLevel - 1) % 20); // 0-based
        bool zh = I18n.Current == Language.Chinese;

        StageNameText.Text = I18n.FormatLevel(_selectedLevel);
        ThemeNameText.Text = zh ? _themeZh[themeIdx] : _themeEn[themeIdx];
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
        DeployBtn.Content = I18n.Current == Language.Chinese
            ? "▶  出击"
            : "▶  DEPLOY";

        HintText.Text = I18n.Current == Language.Chinese
            ? "方向键 / WASD 移动光标  ·  回车 / 出击按钮 确认"
            : "Arrow keys / WASD to navigate  ·  Enter or DEPLOY to launch";

        UpdateInfoBar();
    }
}
