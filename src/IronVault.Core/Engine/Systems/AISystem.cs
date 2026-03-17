using IronVault.Core.Engine.Components;
using IronVault.Core.Engine.Entities;
using IronVault.Core.Map;

namespace IronVault.Core.Engine.Systems;

public enum AIDifficulty : byte { Easy, Normal, Hard }

/// <summary>
/// Drives all non-player tanks.  Three difficulty tiers:
/// <list type="bullet">
///   <item>Easy   — random roaming, opportunistic fire</item>
///   <item>Normal — A* toward player (70%) or base (30%), fires when aligned</item>
///   <item>Hard   — A* toward base, bullet-dodging, aggressive fire rate</item>
/// </list>
/// Each tank gets its own <see cref="AiState"/> so movement timers, paths
/// and stuck counters are fully independent.
/// </summary>
public static class AISystem
{
    // ── Per-tank transient state ──────────────────────────────────────────
    private sealed class AiState
    {
        /// Current A* path (tile coords).  Mutated as waypoints are reached.
        public List<(int col, int row)> Path = [];

        /// Seconds until the next forced repath.
        public float RepathTimer = 0f;

        /// Seconds left before the Easy AI may change direction again.
        public float MoveTimer = 0f;

        /// Consecutive frames where MoveSystem couldn't move this tank.
        public int StuckFrames = 0;
    }

    private static readonly Dictionary<int, AiState> _states = [];
    private static readonly Random _rng = new();

    // ── Public API ────────────────────────────────────────────────────────

    public static void Update(
        List<TankEntity>   tanks,
        List<BulletEntity> bullets,
        TileMap            map,
        AIDifficulty       difficulty,
        float              dt)
    {
        TankEntity? player = null;
        foreach (var t in tanks)
            if (t.IsPlayerControlled && t.IsAlive) { player = t; break; }

        (float baseX, float baseY) = FindBase(map);

        foreach (var tank in tanks)
        {
            if (!tank.IsAlive || tank.IsPlayerControlled || tank.IsFrozen) continue;

            if (!_states.TryGetValue(tank.Id, out var state))
                _states[tank.Id] = state = new AiState();

            // Stuck detection: MoveSystem already ran and set IsMoving.
            // Capture the result before we reset it.
            if (!tank.Velocity.IsMoving)
                state.StuckFrames++;
            else
                state.StuckFrames = 0;

            // AI always wants to move
            tank.Velocity.IsMoving = true;

            // Tier 3/4 enemies always use at least Normal AI behaviour
            var effectiveDiff = difficulty;
            if (tank.Tier >= TankTier.Tier3 && effectiveDiff == AIDifficulty.Easy)
                effectiveDiff = AIDifficulty.Normal;
            if (tank.Tier == TankTier.Tier4 && effectiveDiff == AIDifficulty.Normal)
                effectiveDiff = AIDifficulty.Hard;

            switch (effectiveDiff)
            {
                case AIDifficulty.Easy:
                    UpdateEasy(tank, bullets, map, state, dt);
                    break;
                case AIDifficulty.Normal:
                    UpdateNormal(tank, bullets, player, baseX, baseY, map, state, dt);
                    break;
                default:
                    UpdateHard(tank, bullets, player, baseX, baseY, map, state, dt);
                    break;
            }
        }
    }

    /// <summary>
    /// Remove state entries for tanks that are no longer alive.
    /// Call this from GameEngine after dead-tank cleanup.
    /// </summary>
    public static void Cleanup(IEnumerable<int> aliveIds)
    {
        var alive = new HashSet<int>(aliveIds);
        foreach (var key in _states.Keys.ToList())
            if (!alive.Contains(key))
                _states.Remove(key);
    }

    /// <summary>Call on new game / reset to wipe all transient AI state.</summary>
    public static void Reset() => _states.Clear();

    // ═══════════════════════════════════════════════════════════════════════
    // EASY — random roaming, opportunistic fire
    // ═══════════════════════════════════════════════════════════════════════
    private static void UpdateEasy(
        TankEntity tank, List<BulletEntity> bullets,
        TileMap map, AiState state, float dt)
    {
        // Direction change: every 1-2 seconds OR immediately when stuck
        state.MoveTimer -= dt;
        bool changeDir = state.MoveTimer <= 0 || state.StuckFrames >= 6;
        if (changeDir)
        {
            tank.Position.Facing = (Direction)_rng.Next(4);
            state.MoveTimer  = 1.0f + (float)_rng.NextDouble();  // 1–2 s
            state.StuckFrames = 0;
        }

        // Random fire — about once every 2 s
        if (tank.Weapon.CanFire && _rng.NextDouble() < 0.5 * dt)
            WeaponSystem.Fire(tank, bullets);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NORMAL — A* toward player (70 %) or base (30 %), fires when aligned
    // ═══════════════════════════════════════════════════════════════════════
    private static void UpdateNormal(
        TankEntity tank, List<BulletEntity> bullets,
        TankEntity? player, float baseX, float baseY,
        TileMap map, AiState state, float dt)
    {
        // Pick target
        bool chasePlayer = player != null && _rng.NextDouble() < 0.7;
        float targetX = chasePlayer ? player!.Position.X : baseX;
        float targetY = chasePlayer ? player!.Position.Y : baseY;

        int goalCol = (int)(targetX / TileMap.TileSize);
        int goalRow = (int)(targetY / TileMap.TileSize);

        // Repath when timer expires or tank is stuck
        state.RepathTimer -= dt;
        bool needRepath = state.RepathTimer <= 0 || (state.StuckFrames >= 8 && state.Path.Count == 0);
        if (needRepath)
        {
            int startCol = (int)((tank.Position.X + TankEntity.Size / 2f) / TileMap.TileSize);
            int startRow = (int)((tank.Position.Y + TankEntity.Size / 2f) / TileMap.TileSize);
            state.Path        = FindPath(startCol, startRow, goalCol, goalRow, map);
            state.RepathTimer = 2.0f;
            state.StuckFrames = 0;
        }

        // Follow path or fall back to direct direction
        Direction dir = state.Path.Count > 0
            ? FollowPath(tank, state.Path)
            : GetDirectionTo(tank.Position.X, tank.Position.Y, targetX, targetY);

        tank.Position.Facing = dir;

        // Fire when on the same axis as the target, plus occasional random shots
        if (IsAlignedWith(tank, targetX, targetY) && tank.Weapon.CanFire)
            WeaponSystem.Fire(tank, bullets);
        else if (tank.Weapon.CanFire && _rng.NextDouble() < 0.4 * dt)
            WeaponSystem.Fire(tank, bullets);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HARD — base-first A*, bullet dodging, aggressive fire
    // ═══════════════════════════════════════════════════════════════════════
    private static void UpdateHard(
        TankEntity tank, List<BulletEntity> bullets,
        TankEntity? player, float baseX, float baseY,
        TileMap map, AiState state, float dt)
    {
        // ── Bullet dodging (highest priority) ────────────────────────────
        Direction? dodge = GetDodgeDirection(tank, bullets);
        if (dodge.HasValue)
        {
            tank.Position.Facing = dodge.Value;
            // Still fire aggressively while dodging
            if (tank.Weapon.CanFire) WeaponSystem.Fire(tank, bullets);
            return;
        }

        // ── Pick target: 65 % base, 35 % flanking position near player ───
        float targetX = baseX, targetY = baseY;
        if (player != null && _rng.NextDouble() < 0.35)
        {
            // Approach from the side that puts us perpendicular to the player
            float dx = player.Position.X - tank.Position.X;
            float dy = player.Position.Y - tank.Position.Y;
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                targetX = player.Position.X;
                targetY = player.Position.Y - TankEntity.Size * 2;
            }
            else
            {
                targetX = player.Position.X - TankEntity.Size * 2;
                targetY = player.Position.Y;
            }
        }

        int goalCol = (int)(targetX / TileMap.TileSize);
        int goalRow = (int)(targetY / TileMap.TileSize);

        // Repath more frequently than Normal
        state.RepathTimer -= dt;
        bool needRepath = state.RepathTimer <= 0 || state.StuckFrames >= 5;
        if (needRepath)
        {
            int startCol = (int)((tank.Position.X + TankEntity.Size / 2f) / TileMap.TileSize);
            int startRow = (int)((tank.Position.Y + TankEntity.Size / 2f) / TileMap.TileSize);
            state.Path        = FindPath(startCol, startRow, goalCol, goalRow, map);
            state.RepathTimer = 1.0f;
            state.StuckFrames = 0;
        }

        Direction dir = state.Path.Count > 0
            ? FollowPath(tank, state.Path)
            : GetDirectionTo(tank.Position.X, tank.Position.Y, targetX, targetY);

        tank.Position.Facing = dir;

        // Aggressive fire: fire whenever possible
        if (tank.Weapon.CanFire && _rng.NextDouble() < 3.0 * dt)
            WeaponSystem.Fire(tank, bullets);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // A* PATHFINDER
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a list of tile (col,row) waypoints from start to the nearest
    /// passable cell adjacent to the goal.  Empty list when unreachable.
    /// </summary>
    private static List<(int col, int row)> FindPath(
        int startCol, int startRow,
        int goalCol,  int goalRow,
        TileMap map)
    {
        // Snap goal to nearest passable tile
        var goal = FindNearestPassable(goalCol, goalRow, map);

        var start = (startCol, startRow);
        if (start == goal) return [];

        var openSet  = new PriorityQueue<(int col, int row), float>();
        var gCost    = new Dictionary<(int, int), float>(64);
        var cameFrom = new Dictionary<(int, int), (int, int)>(64);

        openSet.Enqueue(start, 0f);
        gCost[start] = 0f;

        int  iterations = 0;
        bool found      = false;

        while (openSet.Count > 0 && iterations++ < 800)
        {
            var current = openSet.Dequeue();

            if (current == goal) { found = true; break; }

            foreach (var next in Neighbors(current.col, current.row, map))
            {
                float ng = gCost[current] + 1f;
                if (!gCost.TryGetValue(next, out float eg) || ng < eg)
                {
                    gCost[next]    = ng;
                    cameFrom[next] = current;
                    float h = Math.Abs(next.col - goal.col) + Math.Abs(next.row - goal.row);
                    openSet.Enqueue(next, ng + h);
                }
            }
        }

        if (!found || !cameFrom.ContainsKey(goal)) return [];

        // Reconstruct path
        var path = new List<(int, int)>(16);
        var node = goal;
        while (node != start)
        {
            path.Add(node);
            if (!cameFrom.TryGetValue(node, out var prev)) break;
            node = prev;
        }
        path.Reverse();
        return path;
    }

    private static IEnumerable<(int col, int row)> Neighbors(int col, int row, TileMap map)
    {
        if (map.IsPassable(col - 1, row)) yield return (col - 1, row);
        if (map.IsPassable(col + 1, row)) yield return (col + 1, row);
        if (map.IsPassable(col, row - 1)) yield return (col, row - 1);
        if (map.IsPassable(col, row + 1)) yield return (col, row + 1);
    }

    private static (int col, int row) FindNearestPassable(int col, int row, TileMap map)
    {
        if (map.IsPassable(col, row)) return (col, row);
        // Check direct neighbours first, then expand
        (int, int)[] candidates =
        [
            (col, row - 1), (col, row + 1),
            (col - 1, row), (col + 1, row),
            (col - 1, row - 1), (col + 1, row - 1),
            (col - 1, row + 1), (col + 1, row + 1),
        ];
        foreach (var c in candidates)
            if (map.IsPassable(c.Item1, c.Item2)) return c;
        return (col, row); // fallback (may be impassable, path will be empty)
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PATH FOLLOWING
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Advances the path when the tank reaches each waypoint and returns the
    /// direction toward the next one.
    /// </summary>
    private static Direction FollowPath(TankEntity tank, List<(int col, int row)> path)
    {
        if (path.Count == 0) return tank.Position.Facing;

        int tankCol = (int)((tank.Position.X + TankEntity.Size / 2f) / TileMap.TileSize);
        int tankRow = (int)((tank.Position.Y + TankEntity.Size / 2f) / TileMap.TileSize);

        // Advance past waypoints we've already reached
        while (path.Count > 0 && path[0].col == tankCol && path[0].row == tankRow)
            path.RemoveAt(0);

        if (path.Count == 0) return tank.Position.Facing;

        var (wpCol, wpRow) = path[0];
        float targetX = wpCol * TileMap.TileSize + TileMap.TileSize / 2f;
        float targetY = wpRow * TileMap.TileSize + TileMap.TileSize / 2f;

        return GetDirectionTo(
            tank.Position.X + TankEntity.Size / 2f,
            tank.Position.Y + TankEntity.Size / 2f,
            targetX, targetY);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BULLET DODGE (Hard only)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a dodge direction if an opposing bullet is heading toward this
    /// tank within 3 tiles, otherwise null.
    /// </summary>
    private static Direction? GetDodgeDirection(TankEntity tank, List<BulletEntity> bullets)
    {
        const float danger = 3 * TileMap.TileSize; // 72 px
        const float align  = 20f;                   // lateral alignment window

        float tx = tank.Position.X + TankEntity.Size / 2f;
        float ty = tank.Position.Y + TankEntity.Size / 2f;

        foreach (var b in bullets)
        {
            if (!b.IsAlive) continue;
            // Only dodge bullets from the player / ally side
            if (b.OwnerTeam == TankTeam.Enemy) continue;

            float bx = b.X + BulletEntity.Width  / 2f;
            float by = b.Y + BulletEntity.Height / 2f;

            bool heading = b.Direction switch
            {
                Direction.Up    => Math.Abs(bx - tx) < align && by > ty  && by - ty < danger,
                Direction.Down  => Math.Abs(bx - tx) < align && by < ty  && ty - by < danger,
                Direction.Left  => Math.Abs(by - ty) < align && bx > tx  && bx - tx < danger,
                Direction.Right => Math.Abs(by - ty) < align && bx < tx  && tx - bx < danger,
                _               => false,
            };
            if (!heading) continue;

            // Dodge perpendicularly, moving away from the bullet's side
            return b.Direction is Direction.Up or Direction.Down
                ? (bx < tx ? Direction.Right : Direction.Left)
                : (by < ty ? Direction.Down  : Direction.Up);
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UTILITIES
    // ═══════════════════════════════════════════════════════════════════════

    private static Direction GetDirectionTo(float fromX, float fromY, float toX, float toY)
    {
        float dx = toX - fromX;
        float dy = toY - fromY;
        return Math.Abs(dx) >= Math.Abs(dy)
            ? (dx >= 0 ? Direction.Right : Direction.Left)
            : (dy >= 0 ? Direction.Down  : Direction.Up);
    }

    private static bool IsAlignedWith(TankEntity tank, float targetX, float targetY)
    {
        const float tolerance = TankEntity.Size;
        return tank.Position.Facing switch
        {
            Direction.Up   or Direction.Down  => Math.Abs(tank.Position.X - targetX) < tolerance,
            Direction.Left or Direction.Right => Math.Abs(tank.Position.Y - targetY) < tolerance,
            _                                 => false,
        };
    }

    private static (float x, float y) FindBase(TileMap map)
    {
        for (int r = 0; r < map.Rows; r++)
            for (int c = 0; c < map.Cols; c++)
                if (map[c, r] == TileType.Base)
                    return (c * TileMap.TileSize, r * TileMap.TileSize);
        return (map.Cols * TileMap.TileSize / 2f, map.Rows * TileMap.TileSize - TileMap.TileSize);
    }
}
