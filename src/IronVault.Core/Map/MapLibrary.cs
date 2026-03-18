namespace IronVault.Core.Map;

/// <summary>
/// Provides 20 hand-crafted Battle City-inspired level maps (plus cycling for levels 21-100).
/// All maps share the same structural constraints: steel border, four enemy spawns,
/// Eagle base at (14,25) with surrounding brick protection, clear spawn zones and player channel.
/// </summary>
public static class MapLibrary
{
    public const int TotalLevels = 100;

    /// <summary>
    /// Returns the TileMap for the given level (1-100).
    /// Levels 21-100 cycle through maps 1-20.
    /// </summary>
    public static TileMap CreateLevel(int level)
    {
        level = Math.Clamp(level, 1, TotalLevels);
        int mapIndex = ((level - 1) % 20) + 1;
        return mapIndex switch
        {
            1  => Level01(),
            2  => Level02(),
            3  => Level03(),
            4  => Level04(),
            5  => Level05(),
            6  => Level06(),
            7  => Level07(),
            8  => Level08(),
            9  => Level09(),
            10 => Level10(),
            11 => Level11(),
            12 => Level12(),
            13 => Level13(),
            14 => Level14(),
            15 => Level15(),
            16 => Level16(),
            17 => Level17(),
            18 => Level18(),
            19 => Level19(),
            20 => Level20(),
            _  => Level01(),
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a 28×28 map pre-filled with the structural elements every level shares:
    /// steel border, enemy spawn markers, Eagle base, and the 8 surrounding brick tiles.
    /// </summary>
    private static TileMap BaseMap()
    {
        var map = new TileMap();

        // Steel border (row 0, row 27, col 0, col 27)
        for (int c = 0; c < TileMap.DefaultCols; c++)
        {
            map[c, 0]                       = TileType.Steel;
            map[c, TileMap.DefaultRows - 1] = TileType.Steel;
        }
        for (int r = 0; r < TileMap.DefaultRows; r++)
        {
            map[0, r]                       = TileType.Steel;
            map[TileMap.DefaultCols - 1, r] = TileType.Steel;
        }

        // Enemy spawn markers (row 1, cols 4/10/18/23)
        map[ 4, 1] = TileType.Spawn;
        map[10, 1] = TileType.Spawn;
        map[18, 1] = TileType.Spawn;
        map[23, 1] = TileType.Spawn;

        // Eagle base at (col 14, row 25)
        map[14, 25] = TileType.Base;

        // 8 surrounding protection bricks
        map[13, 24] = TileType.Brick;
        map[14, 24] = TileType.Brick;
        map[15, 24] = TileType.Brick;
        map[13, 25] = TileType.Brick;
        map[15, 25] = TileType.Brick;
        map[13, 26] = TileType.Brick;
        map[14, 26] = TileType.Brick;
        map[15, 26] = TileType.Brick;

        return map;
    }

    /// <summary>
    /// Fills a rectangular region with the given tile type.
    /// Silently clamps to map bounds so callers never need to worry about overflow.
    /// </summary>
    private static void Fill(TileMap map, int col, int row, int w, int h, TileType type)
    {
        for (int r = row; r < row + h && r < map.Rows; r++)
            for (int c = col; c < col + w && c < map.Cols; c++)
                map[c, r] = type;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 1 — "Deployment"
    // Simple symmetric layout easing players in.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level01()
    {
        var map = BaseMap();

        // Corner brick blocks
        Fill(map,  2,  2, 2, 2, TileType.Brick);  // top-left corner
        Fill(map, 24,  2, 2, 2, TileType.Brick);  // top-right corner

        // Mid-upper brick clusters
        Fill(map,  6,  5, 2, 3, TileType.Brick);  // left cluster
        Fill(map, 20,  5, 2, 3, TileType.Brick);  // right cluster

        // Centre brick flanks
        Fill(map, 12,  5, 2, 3, TileType.Brick);  // centre-left
        Fill(map, 15,  5, 2, 3, TileType.Brick);  // centre-right

        // Outer side mid bricks
        Fill(map,  2, 10, 2, 4, TileType.Brick);  // left wall section
        Fill(map, 24, 10, 2, 4, TileType.Brick);  // right wall section

        // Centre forest ambush patch
        Fill(map, 12, 11, 2, 2, TileType.Forest);

        // Base approach channel guards
        for (int r = 17; r <= 20; r++)
        {
            map[11, r] = TileType.Brick;
            map[17, r] = TileType.Brick;
        }

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 2 — "Block Wall"
    // Dense symmetric 2×3 brick blocks with water and forest accents.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level02()
    {
        var map = BaseMap();

        // Row-band 1 (rows 2-4)
        Fill(map,  2,  2, 2, 3, TileType.Brick);
        Fill(map,  7,  2, 2, 3, TileType.Brick);
        Fill(map, 20,  2, 2, 3, TileType.Brick);
        Fill(map, 24,  2, 2, 3, TileType.Brick);

        // Centre columns row-band 1
        Fill(map, 13,  2, 1, 3, TileType.Brick);
        Fill(map, 15,  2, 1, 3, TileType.Brick);

        // Row-band 2 (rows 7-9)
        Fill(map,  2,  7, 2, 3, TileType.Brick);
        Fill(map,  7,  7, 2, 3, TileType.Brick);
        Fill(map, 20,  7, 2, 3, TileType.Brick);
        Fill(map, 24,  7, 2, 3, TileType.Brick);

        // Row-band 3 (rows 12-14)
        Fill(map,  2, 12, 2, 3, TileType.Brick);
        Fill(map,  7, 12, 2, 3, TileType.Brick);
        Fill(map, 20, 12, 2, 3, TileType.Brick);
        Fill(map, 24, 12, 2, 3, TileType.Brick);

        // Water patches
        Fill(map,  5, 10, 2, 2, TileType.Water);
        Fill(map, 22, 10, 2, 2, TileType.Water);

        // Centre forest
        Fill(map, 12,  9, 2, 2, TileType.Forest);

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 3 — "River Crossing"
    // Horizontal water channel with a centre ford, flanked by steel sentinels.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level03()
    {
        var map = BaseMap();

        // Horizontal water channel rows 9-10 (left bank)
        Fill(map,  1,  9, 12, 2, TileType.Water);  // cols 1-12
        // Horizontal water channel rows 9-10 (right bank) — gap at cols 13-15
        Fill(map, 16,  9, 11, 2, TileType.Water);  // cols 16-26

        // Vertical water upper-left
        Fill(map,  5,  5, 2, 3, TileType.Water);   // cols 5-6, rows 5-7
        // Vertical water upper-right
        Fill(map, 22,  5, 2, 3, TileType.Water);   // cols 22-23, rows 5-7

        // Brick blocks
        Fill(map,  2,  3, 2, 2, TileType.Brick);   // top-left
        Fill(map, 24,  3, 2, 2, TileType.Brick);   // top-right
        Fill(map,  8,  3, 2, 2, TileType.Brick);   // inner-left
        Fill(map, 18,  3, 2, 2, TileType.Brick);   // inner-right

        // Lower brick walls (rows 12-17)
        Fill(map,  2, 12, 2, 6, TileType.Brick);
        Fill(map, 24, 12, 2, 6, TileType.Brick);

        // Steel sentinels at crossing
        map[11,  6] = TileType.Steel;
        map[11,  7] = TileType.Steel;
        map[11,  8] = TileType.Steel;
        map[17,  6] = TileType.Steel;
        map[17,  7] = TileType.Steel;
        map[17,  8] = TileType.Steel;

        // Forest patches lower mid
        Fill(map,  9, 14, 2, 2, TileType.Forest);
        Fill(map, 17, 14, 2, 2, TileType.Forest);

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 4 — "Forest Haven"
    // Dense forest across the map; sparse brick walls create structure.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level04()
    {
        var map = BaseMap();

        // Upper forest clusters
        Fill(map,  2,  5, 2, 4, TileType.Forest);
        Fill(map,  7,  3, 2, 3, TileType.Forest);
        Fill(map,  9,  7, 2, 3, TileType.Forest);
        Fill(map, 17,  3, 2, 3, TileType.Forest);
        Fill(map, 19,  7, 2, 3, TileType.Forest);
        Fill(map, 24,  5, 2, 4, TileType.Forest);

        // Centre forest
        Fill(map, 12,  7, 2, 2, TileType.Forest);
        Fill(map, 15,  7, 2, 2, TileType.Forest);

        // Lower forest clusters
        Fill(map,  2, 13, 2, 4, TileType.Forest);
        Fill(map,  7, 13, 2, 3, TileType.Forest);
        Fill(map, 19, 13, 2, 3, TileType.Forest);
        Fill(map, 24, 13, 2, 4, TileType.Forest);

        // Sparse brick walls
        Fill(map,  5,  2, 2, 2, TileType.Brick);
        Fill(map, 21,  2, 2, 2, TileType.Brick);
        Fill(map, 10, 11, 2, 2, TileType.Brick);
        Fill(map, 17, 11, 2, 2, TileType.Brick);

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 5 — "Steel Curtain"
    // Indestructible steel lines dominate; brick provides secondary cover.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level05()
    {
        var map = BaseMap();

        // Horizontal steel lines
        for (int c = 2; c <= 10; c++) map[c,  5] = TileType.Steel;
        for (int c = 17; c <= 25; c++) map[c, 5] = TileType.Steel;

        // Vertical steel flanks
        for (int r = 6; r <= 10; r++) map[ 6, r] = TileType.Steel;
        for (int r = 6; r <= 10; r++) map[21, r] = TileType.Steel;

        // Steel corner blocks
        Fill(map,  2,  8, 2, 2, TileType.Steel);
        Fill(map, 24,  8, 2, 2, TileType.Steel);

        // Brick maze sections
        Fill(map,  8,  3, 2, 2, TileType.Brick);
        Fill(map, 18,  3, 2, 2, TileType.Brick);
        Fill(map,  2, 12, 2, 4, TileType.Brick);
        Fill(map, 24, 12, 2, 4, TileType.Brick);
        Fill(map,  8, 12, 2, 4, TileType.Brick);
        Fill(map, 18, 12, 2, 4, TileType.Brick);

        // Single steel pillboxes
        map[ 8,  7] = TileType.Steel;
        map[19,  7] = TileType.Steel;
        map[ 8, 14] = TileType.Steel;
        map[19, 14] = TileType.Steel;

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 6 — "Ice Rink"
    // Vast ice fields punish momentum; brick borders keep tanks in check.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level06()
    {
        var map = BaseMap();

        // Large ice fields
        Fill(map,  2,  6, 10, 3, TileType.Ice);   // cols 2-11, rows 6-8
        Fill(map, 16,  6, 10, 3, TileType.Ice);   // cols 16-25, rows 6-8
        Fill(map,  5, 12,  7, 3, TileType.Ice);   // cols 5-11, rows 12-14
        Fill(map, 16, 12,  7, 3, TileType.Ice);   // cols 16-22, rows 12-14

        // Brick border walls (thin)
        for (int r = 5; r <= 15; r++)
        {
            map[1, r] = TileType.Brick;
            map[2, r] = TileType.Brick;
            map[25, r] = TileType.Brick;
            map[26, r] = TileType.Brick;
        }

        // Scattered upper brick blocks
        Fill(map,  3,  3, 2, 2, TileType.Brick);
        Fill(map, 23,  3, 2, 2, TileType.Brick);
        Fill(map, 10,  3, 2, 2, TileType.Brick);
        Fill(map, 16,  3, 2, 2, TileType.Brick);

        // Steel accents
        for (int r =  9; r <= 11; r++) map[ 6, r] = TileType.Steel;
        for (int r =  9; r <= 11; r++) map[21, r] = TileType.Steel;
        for (int r =  6; r <=  8; r++) map[13, r] = TileType.Steel;

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 7 — "Fortress"
    // Heavy brick/steel fortress architecture; narrow approach lanes.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level07()
    {
        var map = BaseMap();

        // Outer thin brick walls (left and right flanks, rows 2-18)
        for (int r = 2; r <= 18; r++)
        {
            map[2, r] = TileType.Brick;
            map[3, r] = TileType.Brick;
            map[24, r] = TileType.Brick;
            map[25, r] = TileType.Brick;
        }

        // Interior fortress — upper-left L-shape
        for (int c = 5; c <= 9; c++) map[c, 5] = TileType.Brick;
        for (int r = 5; r <= 9; r++) map[5, r] = TileType.Brick;

        // Interior fortress — upper-right L-shape (mirrored)
        for (int c = 19; c <= 23; c++) map[c, 5] = TileType.Brick;
        for (int r = 5; r <= 9; r++) map[23, r] = TileType.Brick;

        // Centre steel fort (cols 11-16, rows 8-11, gap at cols 13-14)
        for (int r = 8; r <= 11; r++)
        {
            for (int c = 11; c <= 16; c++)
            {
                if (c == 13 || c == 14) continue;  // keep player channel clear
                map[c, r] = TileType.Steel;
            }
        }

        // Lower flanks
        Fill(map,  2, 13, 2, 5, TileType.Brick);
        Fill(map, 24, 13, 2, 5, TileType.Brick);

        // Cross walls
        Fill(map,  7, 13, 2, 4, TileType.Brick);
        Fill(map, 19, 13, 2, 4, TileType.Brick);

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 8 — "Canal City"
    // Interlocking water channels with steel bridge crossings.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level08()
    {
        var map = BaseMap();

        // Vertical water upper-left
        for (int r = 2; r <= 8; r++) map[5, r] = TileType.Water;
        // Vertical water upper-right
        for (int r = 2; r <= 8; r++) map[22, r] = TileType.Water;

        // Horizontal water channel rows 8-9 (with gap at cols 13-14)
        for (int c = 6; c <= 21; c++)
        {
            if (c == 13 || c == 14) continue;
            map[c, 8] = TileType.Water;
            map[c, 9] = TileType.Water;
        }

        // Vertical channels lower
        for (int r = 11; r <= 17; r++) map[ 8, r] = TileType.Water;
        for (int r = 11; r <= 17; r++) map[19, r] = TileType.Water;

        // Brick islands between upper water
        Fill(map,  6,  3, 2, 5, TileType.Brick);
        Fill(map, 20,  3, 2, 5, TileType.Brick);

        // Steel bridges at channel crossings
        map[11, 8] = TileType.Steel;
        map[11, 9] = TileType.Steel;
        map[16, 8] = TileType.Steel;
        map[16, 9] = TileType.Steel;

        // Forest mid-lower
        Fill(map,  9, 12, 2, 3, TileType.Forest);
        Fill(map, 17, 12, 2, 3, TileType.Forest);

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 9 — "Mixed Terrain"
    // Every terrain type represented in strategic zones.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level09()
    {
        var map = BaseMap();

        // Upper bricks
        Fill(map,  2,  2, 2, 3, TileType.Brick);
        Fill(map, 24,  2, 2, 3, TileType.Brick);

        // Upper water
        Fill(map,  6,  3, 3, 3, TileType.Water);
        Fill(map, 19,  3, 3, 3, TileType.Water);

        // Upper forest
        Fill(map, 10,  4, 2, 3, TileType.Forest);
        Fill(map, 16,  4, 2, 3, TileType.Forest);

        // Mid ice corridors
        Fill(map,  3,  9, 3, 3, TileType.Ice);
        Fill(map, 22,  9, 3, 3, TileType.Ice);

        // Mid steel
        for (int r = 8; r <= 10; r++) map[ 7, r] = TileType.Steel;
        for (int r = 8; r <= 10; r++) map[20, r] = TileType.Steel;

        // Mid bricks
        Fill(map,  8,  8, 2, 4, TileType.Brick);
        Fill(map, 18,  8, 2, 4, TileType.Brick);

        // Centre forest
        Fill(map, 12, 11, 2, 3, TileType.Forest);
        Fill(map, 15, 11, 2, 3, TileType.Forest);

        // Lower water moats
        Fill(map,  5, 14, 2, 3, TileType.Water);
        Fill(map, 21, 14, 2, 3, TileType.Water);

        // Lower bricks
        Fill(map,  2, 14, 2, 5, TileType.Brick);
        Fill(map, 24, 14, 2, 5, TileType.Brick);

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 10 — "Checkerboard"
    // Regular 2×2 brick grid with forest and steel accents.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level10()
    {
        var map = BaseMap();

        // 2×2 brick blocks at grid positions (skipping spawn zones and player channel)
        int[] blockCols = { 2, 6, 20, 24 };
        int[] blockColsFull = { 2, 6, 11, 16, 20, 24 };
        int[] blockRows2 = { 2 };
        int[] blockRowsFull = { 6, 10, 14 };

        foreach (int c in blockCols)
            Fill(map, c, 2, 2, 2, TileType.Brick);

        foreach (int r in blockRowsFull)
            foreach (int c in blockColsFull)
                Fill(map, c, r, 2, 2, TileType.Brick);

        // Forest centre patch
        Fill(map, 13,  7, 2, 2, TileType.Forest);

        // Single steel pillboxes
        map[ 4,  4] = TileType.Steel;
        map[22,  4] = TileType.Steel;
        map[ 4, 12] = TileType.Steel;
        map[22, 12] = TileType.Steel;

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 11 — "Labyrinth"
    // Brick wall maze with carefully placed gaps; requires navigational skill.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level11()
    {
        var map = BaseMap();

        // Horizontal wall segments
        for (int c =  2; c <= 10; c++) map[c,  4] = TileType.Brick;
        for (int c = 16; c <= 25; c++) map[c,  4] = TileType.Brick;
        for (int c =  5; c <= 12; c++) map[c,  8] = TileType.Brick;
        for (int c = 16; c <= 22; c++) map[c,  8] = TileType.Brick;
        for (int c =  2; c <=  8; c++) map[c, 12] = TileType.Brick;
        for (int c = 19; c <= 25; c++) map[c, 12] = TileType.Brick;
        for (int c =  5; c <= 11; c++) map[c, 16] = TileType.Brick;
        for (int c = 17; c <= 23; c++) map[c, 16] = TileType.Brick;

        // Vertical wall segments
        for (int r = 5; r <= 7; r++) map[ 3, r] = TileType.Brick;
        for (int r = 5; r <= 7; r++) map[25, r] = TileType.Brick;
        for (int r = 9; r <= 11; r++) map[11, r] = TileType.Brick;
        for (int r = 9; r <= 11; r++) map[17, r] = TileType.Brick;
        for (int r = 13; r <= 15; r++) map[ 4, r] = TileType.Brick;
        for (int r = 13; r <= 15; r++) map[24, r] = TileType.Brick;

        // Steel accents
        map[ 8, 5] = TileType.Steel;
        map[ 8, 6] = TileType.Steel;
        map[19, 5] = TileType.Steel;
        map[19, 6] = TileType.Steel;

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 12 — "Island Defense"
    // Water moat surrounds a central brick island; flanks have forest cover.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level12()
    {
        var map = BaseMap();

        // Outer vertical water sentinels
        for (int r = 3; r <= 17; r++) map[ 5, r] = TileType.Water;
        for (int r = 3; r <= 17; r++) map[22, r] = TileType.Water;

        // Central water moat (rectangle outline, cols 9-18, rows 9-14)
        for (int c = 9; c <= 18; c++)
        {
            map[c,  9] = TileType.Water;
            map[c, 14] = TileType.Water;
        }
        for (int r = 9; r <= 14; r++)
        {
            map[ 9, r] = TileType.Water;
            map[18, r] = TileType.Water;
        }

        // Brick island inside moat (rows 10-13, cols 10-17, with entry gaps)
        for (int r = 10; r <= 13; r++)
            for (int c = 10; c <= 17; c++)
            {
                // Leave entry gaps on north (col 13-14 row 9 already water, use col 13 row 10 gap)
                if (r == 10 && (c == 13 || c == 14)) continue;
                map[c, r] = TileType.Brick;
            }

        // Forest outside moat
        Fill(map,  6,  6, 2, 3, TileType.Forest);
        Fill(map, 20,  6, 2, 3, TileType.Forest);

        // Brick flanks
        Fill(map,  2,  5, 2, 10, TileType.Brick);
        Fill(map, 24,  5, 2, 10, TileType.Brick);

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 13 — "Steel Grid"
    // Vertical and horizontal steel lines form a crosshatch grid.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level13()
    {
        var map = BaseMap();

        // Vertical steel columns
        for (int r = 2; r <= 17; r++) map[ 6, r] = TileType.Steel;
        for (int r = 2; r <= 17; r++) map[21, r] = TileType.Steel;

        // Horizontal steel rows (left half)
        for (int c = 2; c <= 11; c++) map[c,  6] = TileType.Steel;
        for (int c = 2; c <= 11; c++) map[c, 12] = TileType.Steel;
        // Horizontal steel rows (right half)
        for (int c = 17; c <= 25; c++) map[c,  6] = TileType.Steel;
        for (int c = 17; c <= 25; c++) map[c, 12] = TileType.Steel;

        // Brick clusters in grid cells
        Fill(map,  2,  2, 3, 4, TileType.Brick);
        Fill(map,  7,  2, 4, 4, TileType.Brick);
        Fill(map, 17,  2, 3, 4, TileType.Brick);
        Fill(map, 22,  2, 4, 4, TileType.Brick);
        Fill(map,  2,  7, 3, 5, TileType.Brick);
        Fill(map, 22,  7, 4, 5, TileType.Brick);

        // Forest patches
        Fill(map,  8,  8, 2, 3, TileType.Forest);
        Fill(map, 17,  8, 2, 3, TileType.Forest);

        // Ice at top centre
        Fill(map, 13,  3, 2, 3, TileType.Ice);

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 14 — "Forest Ambush"
    // Very dense forest; brick walls carve narrow ambush corridors.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level14()
    {
        var map = BaseMap();

        // Large forest blocks — upper
        Fill(map,  2,  4, 3, 7, TileType.Forest);
        Fill(map, 23,  4, 3, 7, TileType.Forest);
        Fill(map,  7,  2, 3, 6, TileType.Forest);
        Fill(map, 18,  2, 3, 6, TileType.Forest);

        // Centre forest flanking the gap
        Fill(map, 11,  5, 2, 4, TileType.Forest);
        Fill(map, 15,  5, 2, 4, TileType.Forest);

        // Lower forest blocks
        Fill(map,  2, 13, 3, 6, TileType.Forest);
        Fill(map, 23, 13, 3, 6, TileType.Forest);
        Fill(map,  7, 12, 2, 5, TileType.Forest);
        Fill(map, 19, 12, 2, 5, TileType.Forest);

        // Brick walls creating narrow passages
        for (int r = 2; r <= 10; r++) map[ 5, r] = TileType.Brick;
        for (int r = 2; r <= 10; r++) map[22, r] = TileType.Brick;
        for (int r = 9; r <= 12; r++) map[10, r] = TileType.Brick;
        for (int r = 9; r <= 12; r++) map[17, r] = TileType.Brick;

        // Steel pillboxes
        map[ 6, 11] = TileType.Steel;
        map[21, 11] = TileType.Steel;

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 15 — "Frozen Tundra"
    // Vast ice fields blanket the middle; brick borders and islands for cover.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level15()
    {
        var map = BaseMap();

        // Large ice fields (gap at cols 13-14 for player corridor)
        Fill(map,  2,  5, 11, 11, TileType.Ice);  // cols 2-12, rows 5-15
        Fill(map, 15,  5, 11, 11, TileType.Ice);  // cols 15-25, rows 5-15

        // Brick border walls around ice field
        for (int r = 4; r <= 16; r++)
        {
            map[1, r] = TileType.Brick;
            map[2, r] = TileType.Brick;
            map[25, r] = TileType.Brick;
            map[26, r] = TileType.Brick;
        }

        // Brick islands in the ice
        Fill(map,  4,  7, 2, 3, TileType.Brick);
        Fill(map, 22,  7, 2, 3, TileType.Brick);
        Fill(map,  4, 12, 2, 3, TileType.Brick);
        Fill(map, 22, 12, 2, 3, TileType.Brick);

        // Steel accents
        for (int r = 8; r <= 10; r++) map[ 8, r] = TileType.Steel;
        for (int r = 8; r <= 10; r++) map[19, r] = TileType.Steel;

        // Upper brick clusters
        Fill(map,  3,  2, 2, 2, TileType.Brick);
        Fill(map, 10,  2, 2, 2, TileType.Brick);
        Fill(map, 16,  2, 2, 2, TileType.Brick);
        Fill(map, 23,  2, 2, 2, TileType.Brick);

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 16 — "Trench Warfare"
    // Horizontal brick trenches span the map; gaps force tactical routing.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level16()
    {
        var map = BaseMap();

        // Trench at row 4 — gaps at cols 6-7 and cols 13-14 (centre always open)
        for (int c = 1; c <= 26; c++)
        {
            if (c == 6 || c == 7 || c == 13 || c == 14) continue;
            map[c, 4] = TileType.Brick;
        }

        // Trench at row 7 — gaps at cols 20-21 and cols 13-14; water in left and right segments
        for (int c = 1; c <= 26; c++)
        {
            if (c == 13 || c == 14 || c == 20 || c == 21) continue;
            map[c, 7] = (c >= 2 && c <= 5) || (c >= 22 && c <= 25) ? TileType.Water : TileType.Brick;
        }

        // Trench at row 10 — gaps at cols 6-7 and cols 13-14
        for (int c = 1; c <= 26; c++)
        {
            if (c == 6 || c == 7 || c == 13 || c == 14) continue;
            map[c, 10] = TileType.Brick;
        }

        // Trench at row 13 — gaps at cols 20-21 and cols 13-14
        for (int c = 1; c <= 26; c++)
        {
            if (c == 13 || c == 14 || c == 20 || c == 21) continue;
            map[c, 13] = TileType.Brick;
        }

        // Trench at row 16 — gaps at cols 6-7 and cols 13-14
        for (int c = 1; c <= 26; c++)
        {
            if (c == 6 || c == 7 || c == 13 || c == 14) continue;
            map[c, 16] = TileType.Brick;
        }

        // Vertical brick connectors between some trenches
        for (int r = 4; r <= 7; r++) map[ 3, r] = TileType.Brick;
        for (int r = 7; r <= 10; r++) map[24, r] = TileType.Brick;
        for (int r = 10; r <= 13; r++) map[ 9, r] = TileType.Brick;
        for (int r = 13; r <= 16; r++) map[18, r] = TileType.Brick;

        // Forest behind upper trench
        Fill(map,  2,  5, 2, 2, TileType.Forest);
        Fill(map, 24,  5, 2, 2, TileType.Forest);

        // Steel at key junctions
        map[ 8,  4] = TileType.Steel;
        map[19,  4] = TileType.Steel;
        map[ 8, 13] = TileType.Steel;
        map[19, 13] = TileType.Steel;

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 17 — "Pillbox Defense"
    // Scattered steel pillboxes with adjacent brick cover and water barriers.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level17()
    {
        var map = BaseMap();

        // Steel pillbox grid (row 3)
        int[] pillCols = { 3, 7, 11, 17, 21, 25 };
        foreach (int c in pillCols) map[c, 3] = TileType.Steel;

        // Steel pillbox grid (row 7)
        foreach (int c in pillCols) map[c, 7] = TileType.Steel;

        // Steel pillbox grid (row 11)
        foreach (int c in pillCols) map[c, 11] = TileType.Steel;

        // Steel pillbox grid (row 15) — skip col 11 (near player channel)
        int[] pillCols15 = { 3, 7, 17, 21, 25 };
        foreach (int c in pillCols15) map[c, 15] = TileType.Steel;

        // Adjacent 2×2 brick blocks
        Fill(map,  4,  4, 2, 2, TileType.Brick);
        Fill(map,  8,  4, 2, 2, TileType.Brick);
        Fill(map, 18,  4, 2, 2, TileType.Brick);
        Fill(map, 22,  4, 2, 2, TileType.Brick);
        Fill(map,  4, 12, 2, 2, TileType.Brick);
        Fill(map, 22, 12, 2, 2, TileType.Brick);

        // Water barriers centre
        Fill(map, 12,  6, 4, 2, TileType.Water);

        // Forest lower mid
        Fill(map,  8, 13, 2, 3, TileType.Forest);
        Fill(map, 17, 13, 2, 3, TileType.Forest);

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 18 — "Canyon"
    // Two long horizontal walls create a narrow canyon; outside is forested.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level18()
    {
        var map = BaseMap();

        // Top canyon wall (row 7) with gaps at cols 5-6, 13-14, 21-22
        for (int c = 1; c <= 26; c++)
        {
            if (c == 5 || c == 6 || c == 13 || c == 14 || c == 21 || c == 22) continue;
            map[c, 7] = TileType.Brick;
        }

        // Bottom canyon wall (row 16) with the same gaps
        for (int c = 1; c <= 26; c++)
        {
            if (c == 5 || c == 6 || c == 13 || c == 14 || c == 21 || c == 22) continue;
            map[c, 16] = TileType.Brick;
        }

        // Inside canyon bricks
        Fill(map,  2,  8, 2, 3, TileType.Brick);
        Fill(map, 24,  8, 2, 3, TileType.Brick);

        // Inside canyon steel columns
        for (int r = 8; r <= 15; r++) map[ 9, r] = TileType.Steel;
        for (int r = 8; r <= 15; r++) map[18, r] = TileType.Steel;

        // Outside canyon (above) — forest
        Fill(map,  2,  2, 3, 5, TileType.Forest);
        Fill(map, 23,  2, 3, 5, TileType.Forest);

        // Outside canyon (below) — water
        Fill(map,  2, 17, 3, 4, TileType.Water);
        Fill(map, 23, 17, 3, 4, TileType.Water);

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 19 — "Siege Walls"
    // Concentric brick/steel rings converge on the base; chokepoint steel gates.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level19()
    {
        var map = BaseMap();

        // Outer ring — single brick row at row 3 (full span)
        for (int c = 2; c <= 25; c++) map[c, 3] = TileType.Brick;

        // Inner ring — row 7, gaps at cols 13-14
        for (int c = 4; c <= 23; c++)
        {
            if (c == 13 || c == 14) continue;
            map[c, 7] = TileType.Brick;
        }

        // Inner-inner ring — row 11, gaps at 9-10, 13-14, 17-18
        for (int c = 6; c <= 21; c++)
        {
            if (c == 9 || c == 10 || c == 13 || c == 14 || c == 17 || c == 18) continue;
            map[c, 11] = TileType.Brick;
        }

        // Vertical connectors between outer and inner rings
        for (int r = 3; r <= 7; r++) map[ 4, r] = TileType.Brick;
        for (int r = 3; r <= 7; r++) map[23, r] = TileType.Brick;

        // Vertical connectors between inner and inner-inner rings
        for (int r = 7; r <= 11; r++) map[ 6, r] = TileType.Brick;
        for (int r = 7; r <= 11; r++) map[21, r] = TileType.Brick;

        // Steel at chokepoints
        map[ 5,  4] = TileType.Steel;
        map[22,  4] = TileType.Steel;
        map[ 7,  8] = TileType.Steel;
        map[20,  8] = TileType.Steel;

        // Forest flanks
        Fill(map,  2,  8, 2, 5, TileType.Forest);
        Fill(map, 24,  8, 2, 5, TileType.Forest);

        // Lower approach walls
        Fill(map,  8, 14, 2, 4, TileType.Brick);
        Fill(map, 18, 14, 2, 4, TileType.Brick);
        for (int r = 14; r <= 17; r++) map[11, r] = TileType.Brick;
        for (int r = 14; r <= 17; r++) map[16, r] = TileType.Brick;

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level 20 — "Grand Battle"
    // All terrain types in a complex multi-sector strategic layout.
    // ─────────────────────────────────────────────────────────────────────────
    private static TileMap Level20()
    {
        var map = BaseMap();

        // Upper sector — brick fortresses
        Fill(map,  2,  2, 3, 4, TileType.Brick);
        Fill(map, 23,  2, 3, 4, TileType.Brick);

        // Upper sector — water pools
        Fill(map,  6,  2, 3, 3, TileType.Water);
        Fill(map, 19,  2, 3, 3, TileType.Water);

        // Upper forest
        Fill(map,  9,  3, 3, 4, TileType.Forest);
        Fill(map, 16,  3, 3, 4, TileType.Forest);

        // Mid steel spine (cols 13/15, rows 6-11, skipping col 14 for passage)
        for (int r = 6; r <= 11; r++)
        {
            map[13, r] = TileType.Steel;
            map[15, r] = TileType.Steel;
        }

        // Mid ice wings
        Fill(map,  3,  8, 5, 4, TileType.Ice);
        Fill(map, 20,  8, 5, 4, TileType.Ice);

        // Mid forest
        Fill(map,  8,  9, 3, 4, TileType.Forest);
        Fill(map, 17,  9, 3, 4, TileType.Forest);

        // Lower water moats
        Fill(map,  5, 14, 3, 3, TileType.Water);
        Fill(map, 20, 14, 3, 3, TileType.Water);

        // Lower bricks — outer flanks
        Fill(map,  2, 13, 2, 6, TileType.Brick);
        Fill(map, 24, 13, 2, 6, TileType.Brick);

        // Lower bricks — inner
        Fill(map,  9, 14, 2, 4, TileType.Brick);
        Fill(map, 17, 14, 2, 4, TileType.Brick);

        // Channel bricks — narrowing approach to base
        for (int r = 17; r <= 21; r++) map[11, r] = TileType.Brick;
        for (int r = 17; r <= 21; r++) map[16, r] = TileType.Brick;

        return map;
    }
}
