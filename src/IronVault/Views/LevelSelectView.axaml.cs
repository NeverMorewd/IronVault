using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
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
    //
    // Layout of LevelGrid (an Avalonia Grid control):
    //
    //        col 0    col 1  col 2  ... col 10
    //  row 0  [    ]   [  1 ] [  2 ] ... [ 10 ]   ← column-number labels
    //  row 1  [  A ]   [ 01 ] [ 02 ] ... [ 10 ]   ← Zone A (levels 1-10)
    //  row 2  [  B ]   [ 11 ] [ 12 ] ... [ 20 ]   ← Zone B (levels 11-20)
    //  ...
    //  row 10 [  J ]   [ 91 ] [ 92 ] ... [100 ]   ← Zone J (levels 91-100)
    //
    // Column 0  = zone-letter labels (A-J), skipped for row 0 (corner spacer).
    // Columns 1-10 = level buttons.
    // Rows 1-10 = one zone per row.

    private void BuildGrid()
    {
        LevelGrid.Children.Clear();
        LevelGrid.ColumnDefinitions.Clear();
        LevelGrid.RowDefinitions.Clear();

        const int cols  = 10;
        const int rows  = 10;
        const int btnW  = 58;
        const int btnH  = 52;
        const int lblW  = 26;   // zone-letter column width

        // Columns: col 0 = zone labels, cols 1-10 = levels
        LevelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(lblW) });
        for (int c = 0; c < cols; c++)
            LevelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Rows: row 0 = col-number labels, rows 1-10 = zones
        LevelGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int r = 0; r < rows; r++)
            LevelGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // ── Row 0: column-number labels ──────────────────────────────────────
        // Corner spacer (col 0, row 0)
        var corner = new TextBlock { Width = lblW, FontSize = 9 };
        Grid.SetRow(corner, 0); Grid.SetColumn(corner, 0);
        LevelGrid.Children.Add(corner);

        for (int c = 0; c < cols; c++)
        {
            var lbl = new TextBlock
            {
                Text              = (c + 1).ToString(),
                Width             = btnW + 4,   // match button slot width (btn + 2px margin × 2)
                FontSize          = 9,
                TextAlignment     = TextAlignment.Center,
                Opacity           = 0.45,
                Margin            = new Thickness(0, 0, 0, 3),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            Grid.SetRow(lbl, 0); Grid.SetColumn(lbl, c + 1);
            LevelGrid.Children.Add(lbl);
        }

        // ── Rows 1-10: zone labels + level buttons ────────────────────────────
        for (int r = 0; r < rows; r++)
        {
            char zoneLetter = (char)('A' + r);

            // Zone label (col 0)
            var zoneLbl = new TextBlock
            {
                Text                = zoneLetter.ToString(),
                Width               = lblW,
                FontSize            = 11,
                FontWeight          = FontWeight.Bold,
                Opacity             = 0.50,
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(0, 2, 4, 2),
            };
            Grid.SetRow(zoneLbl, r + 1); Grid.SetColumn(zoneLbl, 0);
            LevelGrid.Children.Add(zoneLbl);

            // Level buttons (cols 1-10)
            for (int c = 0; c < cols; c++)
            {
                int lvl     = r * cols + c + 1;
                int captured = lvl;

                var btn = new Button
                {
                    Width   = btnW,
                    Height  = btnH,
                    Margin  = new Thickness(2),
                    Padding = new Thickness(2, 0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment   = VerticalAlignment.Center,
                    FontSize = 12,
                    Content  = lvl.ToString("D2"),
                    Tag      = lvl,
                };
                btn.Click += (_, _) =>
                {
                    RetroSound.PlayClick();
                    _selectedLevel = captured;
                    UpdateSelection();
                };
                Grid.SetRow(btn, r + 1); Grid.SetColumn(btn, c + 1);
                LevelGrid.Children.Add(btn);
            }
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
        int themeIdx = (_selectedLevel - 1) % 20;
        bool zh      = I18n.Current == Language.Chinese;

        StageNameText.Text = I18n.FormatLevel(_selectedLevel);
        ThemeNameText.Text = zh ? _themeZh[themeIdx] : _themeEn[themeIdx];

        // Grid coordinate: row letter A-J, column number 1-10
        int row = (_selectedLevel - 1) / 10;   // 0-9  → A-J
        int col = (_selectedLevel - 1) % 10 + 1; // 1-10
        CoordText.Text = $"{(char)('A' + row)}{col}";
    }

    private void ConfirmSelection()
        => LevelSelected?.Invoke(this, _selectedLevel);

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
            ? "方向键 / WASD 移动光标  ·  Enter 或 出击 确认"
            : "Arrow keys / WASD to navigate  ·  Enter or DEPLOY to launch";

        UpdateInfoBar();
    }
}
