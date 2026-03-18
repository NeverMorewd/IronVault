namespace IronVault.Core.Map;

/// <summary>
/// Represents the game world grid. Each tile is <see cref="TileSize"/> pixels.
/// </summary>
public sealed class TileMap
{
    public const int TileSize    = 24;   // pixels per tile
    public const int DefaultCols = 28;
    public const int DefaultRows = 28;

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
    /// 28×28 Battle City-style layout.
    ///
    /// Key design constraints:
    ///   • Cols 13-14, rows 19-24 are the player spawn channel (kept clear)
    ///   • Four enemy spawns at row 1: cols 4, 10, 18, 23 — each has a clear
    ///     2-tile footprint for the first two rows so SpawnIsUsable always passes
    ///   • Base Eagle at (14, 25); surrounded by brick U-wall and side channel walls
    ///   • Ice patches in left/right corridors and the centre approach
    ///   • Forest clusters for stealth ambushes
    ///   • Water moats to force routing decisions
    /// </summary>
    public static TileMap CreateDefault()
    {
        var map = new TileMap();

        // ── Steel border ──────────────────────────────────────────────────────
        for (int c = 0; c < DefaultCols; c++)
        {
            map[c, 0]               = TileType.Steel;
            map[c, DefaultRows - 1] = TileType.Steel;
        }
        for (int r = 0; r < DefaultRows; r++)
        {
            map[0, r]               = TileType.Steel;
            map[DefaultCols - 1, r] = TileType.Steel;
        }

        // ── Top brick clusters (rows 2-4) ────────────────────────────────────
        // Avoided: spawn cols 4-5, 10-11, 18-19, 23-24 (must be clear in rows 1-3)
        FillRect(map,  2,  2, 2, 3, TileType.Brick);  // cols  2-3
        FillRect(map,  7,  2, 2, 3, TileType.Brick);  // cols  7-8
        FillRect(map, 12,  2, 1, 3, TileType.Brick);  // col  12 (left of centre gap 13-14)
        FillRect(map, 15,  2, 1, 3, TileType.Brick);  // col  15
        FillRect(map, 20,  2, 2, 3, TileType.Brick);  // cols 20-21
        FillRect(map, 25,  2, 2, 3, TileType.Brick);  // cols 25-26

        // ── Forest canopy — top area ──────────────────────────────────────────
        FillRect(map,  9,  3, 2, 2, TileType.Forest); // cols  9-10, rows  3-4
        FillRect(map, 16,  3, 2, 2, TileType.Forest); // cols 16-17, rows  3-4

        // ── Water moats — upper flanks (rows 5-7) ────────────────────────────
        FillRect(map,  5,  5, 2, 3, TileType.Water);  // cols  5-6,  rows  5-7
        FillRect(map, 21,  5, 2, 3, TileType.Water);  // cols 21-22, rows  5-7 (symmetric mirror of 5-6)

        // ── Ice: centre approach (rows 5-7) ──────────────────────────────────
        FillRect(map, 13,  5, 2, 3, TileType.Ice);    // cols 13-14, rows  5-7

        // ── Mid-upper brick clusters (rows 7-10) ─────────────────────────────
        FillRect(map,  2,  7, 2, 3, TileType.Brick);  // cols  2-3
        FillRect(map,  7,  7, 2, 3, TileType.Brick);  // cols  7-8
        // Steel accents among the mid-upper bricks (add threat variety)
        FillRect(map,  6,  9, 1, 2, TileType.Steel);  // col   6, rows  9-10
        FillRect(map, 21,  9, 1, 2, TileType.Steel);  // col  21, rows  9-10
        FillRect(map, 11,  7, 1, 3, TileType.Brick);  // col  11
        FillRect(map, 16,  7, 1, 3, TileType.Brick);  // col  16
        FillRect(map, 19,  7, 2, 3, TileType.Brick);  // cols 19-20
        FillRect(map, 24,  7, 2, 3, TileType.Brick);  // cols 24-25

        // ── Ice corridors — left and right mid (rows 11-13) ──────────────────
        FillRect(map,  3, 11, 2, 3, TileType.Ice);    // cols  3-4,  rows 11-13
        FillRect(map, 23, 11, 2, 3, TileType.Ice);    // cols 23-24, rows 11-13

        // ── Forest — mid flanks and centre ambush ─────────────────────────────
        FillRect(map,  9, 11, 2, 2, TileType.Forest); // cols  9-10, rows 11-12
        FillRect(map, 17, 11, 2, 2, TileType.Forest); // cols 17-18, rows 11-12
        FillRect(map, 13, 12, 2, 2, TileType.Forest); // cols 13-14, rows 12-13 (centre ambush)

        // ── Mid brick clusters (rows 12-15) ──────────────────────────────────
        FillRect(map,  6, 12, 3, 3, TileType.Brick);  // cols  6-8,  rows 12-14
        FillRect(map, 18, 12, 3, 3, TileType.Brick);  // cols 18-20, rows 12-14
        FillRect(map,  2, 14, 2, 2, TileType.Brick);  // cols  2-3,  rows 14-15 (row 13 kept for ice)
        FillRect(map, 24, 14, 2, 2, TileType.Brick);  // cols 24-25, rows 14-15 (row 13 kept for ice)

        // ── Lower water patches (rows 16-17) ─────────────────────────────────
        FillRect(map,  5, 16, 2, 2, TileType.Water);  // cols  5-6,  rows 16-17
        FillRect(map, 21, 16, 2, 2, TileType.Water);  // cols 21-22, rows 16-17

        // ── Lower forest patches (rows 18-19) ────────────────────────────────
        FillRect(map,  9, 18, 2, 2, TileType.Forest); // cols  9-10, rows 18-19
        FillRect(map, 17, 18, 2, 2, TileType.Forest); // cols 17-18, rows 18-19

        // ── Lower brick clusters (rows 16-20) ────────────────────────────────
        // Stay off cols 13-14 (player channel) and cols 10-11 (ally spawn area)
        FillRect(map,  2, 16, 2, 5, TileType.Brick);  // cols  2-3,  rows 16-20
        FillRect(map,  7, 17, 2, 3, TileType.Brick);  // cols  7-8,  rows 17-19
        FillRect(map, 19, 17, 2, 3, TileType.Brick);  // cols 19-20, rows 17-19
        FillRect(map, 23, 16, 2, 5, TileType.Brick);  // cols 23-24, rows 16-20

        // ── Outer base channel walls (rows 17-20) ────────────────────────────
        for (int r = 17; r <= 20; r++)
        {
            map[11, r] = TileType.Brick;
            map[16, r] = TileType.Brick;
        }

        // ── Inner base channel walls (rows 21-24) ────────────────────────────
        // Narrower (col 12 + col 16) so ally tank can still pass via col 10-11
        for (int r = 21; r <= 24; r++)
        {
            map[12, r] = TileType.Brick;
            map[16, r] = TileType.Brick;
        }

        // ── Base protection and Eagle ─────────────────────────────────────────
        // Eagle / HQ at (mid, DefaultRows-3) = (14, 25)
        int mid = DefaultCols / 2;  // 14

        // Top row — fully seals the base from above
        map[mid - 1, DefaultRows - 4] = TileType.Brick; // col 13, row 24
        map[mid,     DefaultRows - 4] = TileType.Brick; // col 14, row 24
        map[mid + 1, DefaultRows - 4] = TileType.Brick; // col 15, row 24

        // Middle row — left and right flanks around the Eagle
        map[mid - 1, DefaultRows - 3] = TileType.Brick; // col 13, row 25
        map[mid + 1, DefaultRows - 3] = TileType.Brick; // col 15, row 25
        map[mid,     DefaultRows - 3] = TileType.Base;  // col 14, row 25  ← Eagle

        // Bottom row — base of the fortification
        map[mid - 1, DefaultRows - 2] = TileType.Brick; // col 13, row 26
        map[mid,     DefaultRows - 2] = TileType.Brick; // col 14, row 26
        map[mid + 1, DefaultRows - 2] = TileType.Brick; // col 15, row 26

        // ── Enemy spawn points (row 1) ────────────────────────────────────────
        // Cols 4-5, 10-11, 18-19, 23-24 are clear in rows 1-3 → SpawnIsUsable passes
        map[ 4, 1] = TileType.Spawn;  // left
        map[10, 1] = TileType.Spawn;  // centre-left
        map[18, 1] = TileType.Spawn;  // centre-right
        map[23, 1] = TileType.Spawn;  // right

        return map;
    }

    private static void FillRect(TileMap map, int col, int row, int w, int h, TileType type)
    {
        for (int r = row; r < row + h && r < map.Rows; r++)
            for (int c = col; c < col + w && c < map.Cols; c++)
                map[c, r] = type;
    }
}
