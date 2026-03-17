namespace IronVault.Core.Map;

/// <summary>
/// Represents the game world grid. Each tile is <see cref="TileSize"/> pixels.
/// </summary>
public sealed class TileMap
{
    public const int TileSize = 24;   // pixels per tile
    public const int DefaultCols = 26;
    public const int DefaultRows = 26;

    public int Cols { get; }
    public int Rows { get; }

    private readonly TileType[] _tiles;

    public TileMap(int cols = DefaultCols, int rows = DefaultRows)
    {
        Cols = cols;
        Rows = rows;
        _tiles = new TileType[cols * rows];
    }

    public TileType this[int col, int row]
    {
        get => _tiles[row * Cols + col];
        set => _tiles[row * Cols + col] = value;
    }

    public bool InBounds(int col, int row) => col >= 0 && col < Cols && row >= 0 && row < Rows;

    public bool IsPassable(int col, int row, bool isBullet = false)
    {
        if (!InBounds(col, row)) return false;
        return this[col, row] switch
        {
            TileType.Empty  => true,
            TileType.Forest => true,
            TileType.Ice    => true,
            TileType.Spawn  => true,   // Spawn is a marker tile — invisible and always passable
            TileType.Water  => isBullet,
            _               => false,
        };
    }

    public bool IsDestructible(int col, int row)
    {
        if (!InBounds(col, row)) return false;
        return this[col, row] == TileType.Brick;
    }

    /// <summary>
    /// Default Battle City-style layout.
    /// Key design constraints:
    ///   • Cols 12-13 rows 16-22 are fully clear → player spawn zone
    ///   • Spawn tiles at top row are passable and face DOWN
    ///   • Base surrounded by a U-shaped brick wall with side channel walls
    /// </summary>
    public static TileMap CreateDefault()
    {
        var map = new TileMap();

        // ── Steel border ──────────────────────────────────────────────────────
        for (int c = 0; c < DefaultCols; c++)
        {
            map[c, 0] = TileType.Steel;
            map[c, DefaultRows - 1] = TileType.Steel;
        }
        for (int r = 0; r < DefaultRows; r++)
        {
            map[0, r] = TileType.Steel;
            map[DefaultCols - 1, r] = TileType.Steel;
        }

        // ── Brick clusters (mirrored, avoiding col 12-13 rows 16-22) ─────────
        // Top row clusters
        FillRect(map, 2,  2, 3, 3, TileType.Brick);
        FillRect(map, 11, 2, 2, 3, TileType.Brick);
        FillRect(map, 15, 2, 2, 3, TileType.Brick);   // split center top: leave col 13 clear
        FillRect(map, 21, 2, 3, 3, TileType.Brick);

        // Mid-row clusters
        FillRect(map, 2,  9, 3, 3, TileType.Brick);
        FillRect(map, 9,  9, 3, 3, TileType.Brick);
        FillRect(map, 15, 9, 3, 3, TileType.Brick);
        FillRect(map, 21, 9, 3, 3, TileType.Brick);

        // Lower clusters — stay off cols 12-13, rows 16-22 (player channel)
        FillRect(map, 2,  16, 3, 4, TileType.Brick);
        FillRect(map, 7,  16, 3, 3, TileType.Brick);
        FillRect(map, 17, 16, 3, 3, TileType.Brick);
        FillRect(map, 21, 16, 3, 4, TileType.Brick);

        // ── Water patches ─────────────────────────────────────────────────────
        FillRect(map, 6,  6,  3, 3, TileType.Water);
        FillRect(map, 17, 6,  3, 3, TileType.Water);
        FillRect(map, 6,  16, 2, 2, TileType.Water);
        FillRect(map, 18, 16, 2, 2, TileType.Water);

        // ── Forest clusters ───────────────────────────────────────────────────
        FillRect(map, 8,  3,  2, 2, TileType.Forest);
        FillRect(map, 16, 20, 2, 2, TileType.Forest);
        FillRect(map, 10, 13, 2, 2, TileType.Forest);

        // ── Ice patches ───────────────────────────────────────────────────────
        // Left corridor (between brick rows 9-11 and 16-19)
        FillRect(map,  3, 13, 3, 3, TileType.Ice);
        // Right corridor (symmetric)
        FillRect(map, 19, 13, 3, 3, TileType.Ice);
        // Centre-top approach: the column the player naturally charges up
        FillRect(map, 12,  6, 2, 3, TileType.Ice);

        // ── Base protection (bottom-center) ───────────────────────────────────
        // Eagle / HQ at (13, 23)
        int mid = DefaultCols / 2;  // 13

        // Side channel walls protecting the base approach (cols 11 and 15, rows 18-22)
        for (int r = 18; r <= 22; r++)
        {
            map[11, r] = TileType.Brick;
            map[15, r] = TileType.Brick;
        }

        // U-shaped wall directly around the base
        map[mid - 1, DefaultRows - 3] = TileType.Brick; // col 12, row 23
        map[mid + 1, DefaultRows - 3] = TileType.Brick; // col 14, row 23
        map[mid,     DefaultRows - 3] = TileType.Base;  // col 13, row 23  ← Eagle

        map[mid - 1, DefaultRows - 2] = TileType.Brick; // col 12, row 24
        map[mid,     DefaultRows - 2] = TileType.Brick; // col 13, row 24
        map[mid + 1, DefaultRows - 2] = TileType.Brick; // col 14, row 24

        // ── Enemy spawn points (top strip, facing DOWN) ───────────────────────
        map[5,  1] = TileType.Spawn;   // left — clear of left brick cluster
        map[13, 1] = TileType.Spawn;   // center — clear after brick shift
        map[19, 1] = TileType.Spawn;   // right — clear of right brick cluster

        return map;
    }

    private static void FillRect(TileMap map, int col, int row, int w, int h, TileType type)
    {
        for (int r = row; r < row + h && r < map.Rows; r++)
            for (int c = col; c < col + w && c < map.Cols; c++)
                map[c, r] = type;
    }
}
