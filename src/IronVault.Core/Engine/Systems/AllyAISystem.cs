using IronVault.Core.Engine.Components;
using IronVault.Core.Engine.Entities;
using IronVault.Core.Map;

namespace IronVault.Core.Engine.Systems;

/// <summary>
/// Behaviour tree for ally (friendly) tanks.
/// Priority order each frame:
/// <list type="number">
///   <item>Fire if an enemy is within the barrel's line of sight.</item>
///   <item>Chase the nearest enemy if one is within engagement range.</item>
///   <item>Guard the base if an enemy is within threat range of the base.</item>
///   <item>Roam — advance in current facing, pick a new direction when stuck.</item>
/// </list>
/// </summary>
public static class AllyAISystem
{
    private const float EngageRange  = 7 * TileMap.TileSize;   // 168 px — chase enemy
    private const float ThreatRange  = 10 * TileMap.TileSize;  // 240 px — guard base
    private const float RoamInterval = 1.2f;                   // seconds between roam turns

    private sealed class AllyState
    {
        public float RoamTimer  = 0f;
        public int   StuckFrames = 0;
    }

    private static readonly Dictionary<int, AllyState> _states = [];
    private static readonly Random _rng = new();

    // ── Public API ────────────────────────────────────────────────────────────

    public static void Reset() => _states.Clear();

    public static void Cleanup(IEnumerable<int> aliveIds)
    {
        var alive = new HashSet<int>(aliveIds);
        foreach (var key in _states.Keys.Where(k => !alive.Contains(k)).ToList())
            _states.Remove(key);
    }

    public static void Update(List<TankEntity> tanks, TileMap map, float dt)
    {
        // Collect enemies for targeting
        var enemies = new List<TankEntity>();
        foreach (var t in tanks)
            if (t.Team == TankTeam.Enemy && t.IsAlive) enemies.Add(t);

        // Find base pixel position
        (float baseX, float baseY) = FindBase(map);

        foreach (var ally in tanks)
        {
            if (ally.Team != TankTeam.Ally || !ally.IsAlive) continue;

            if (!_states.TryGetValue(ally.Id, out var state))
                _states[ally.Id] = state = new AllyState();

            // ── 1. Find closest enemy ─────────────────────────────────────────
            TankEntity? target = null;
            float       bestDist = float.MaxValue;
            float       ax = ally.Position.X + TankEntity.Size * .5f;
            float       ay = ally.Position.Y + TankEntity.Size * .5f;

            foreach (var e in enemies)
            {
                float ex = e.Position.X + TankEntity.Size * .5f;
                float ey = e.Position.Y + TankEntity.Size * .5f;
                float d  = MathF.Sqrt((ex - ax) * (ex - ax) + (ey - ay) * (ey - ay));
                if (d < bestDist) { bestDist = d; target = e; }
            }

            // ── 2. Decide desired direction ───────────────────────────────────
            Direction desired;

            if (target != null && bestDist <= EngageRange)
            {
                // Chase closest enemy
                desired = DirToward(ally, target.Position.X, target.Position.Y);
                state.RoamTimer = 0;
            }
            else if (target != null && bestDist <= ThreatRange)
            {
                // Guard: move toward base to intercept
                desired = DirToward(ally, baseX, baseY);
            }
            else
            {
                // Roam
                state.RoamTimer -= dt;
                if (state.RoamTimer <= 0f || state.StuckFrames >= 6)
                {
                    state.RoamTimer  = RoamInterval + (float)_rng.NextDouble() * 0.6f;
                    state.StuckFrames = 0;
                    desired = (Direction)_rng.Next(4);
                }
                else
                {
                    desired = ally.Position.Facing;
                }
            }

            // Apply facing direction (MoveSystem will handle actual movement)
            ally.Position.Facing = desired;
            ally.Velocity.IsMoving = true;

            // Track stuck frames (MoveSystem sets IsMoving=false when blocked)
            if (!ally.Velocity.IsMoving) state.StuckFrames++;
            else state.StuckFrames = 0;

            // ── 3. Fire if an enemy is axially aligned ────────────────────────
            if (target != null)
            {
                float tx = target.Position.X + TankEntity.Size * .5f;
                float ty = target.Position.Y + TankEntity.Size * .5f;
                bool aligned = ally.Position.Facing switch
                {
                    Direction.Up    => MathF.Abs(ax - tx) < TankEntity.Size * .6f && ty < ay,
                    Direction.Down  => MathF.Abs(ax - tx) < TankEntity.Size * .6f && ty > ay,
                    Direction.Left  => MathF.Abs(ay - ty) < TankEntity.Size * .6f && tx < ax,
                    Direction.Right => MathF.Abs(ay - ty) < TankEntity.Size * .6f && tx > ax,
                    _ => false
                };
                if (aligned)
                    ally.Input = ally.Input with { Fire = true };
                else
                    ally.Input = ally.Input with { Fire = false };
            }
            else
            {
                ally.Input = ally.Input with { Fire = false };
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Direction DirToward(TankEntity from, float tx, float ty)
    {
        float fx = from.Position.X + TankEntity.Size * .5f;
        float fy = from.Position.Y + TankEntity.Size * .5f;
        float dx = tx - fx;
        float dy = ty - fy;
        // Dominant axis
        if (MathF.Abs(dx) >= MathF.Abs(dy))
            return dx > 0 ? Direction.Right : Direction.Left;
        else
            return dy > 0 ? Direction.Down : Direction.Up;
    }

    private static (float x, float y) FindBase(TileMap map)
    {
        for (int r = 0; r < map.Rows; r++)
            for (int c = 0; c < map.Cols; c++)
                if (map[c, r] == TileType.Base)
                    return (c * TileMap.TileSize + TileMap.TileSize * .5f,
                            r * TileMap.TileSize + TileMap.TileSize * .5f);
        return (map.Cols * TileMap.TileSize * .5f, map.Rows * TileMap.TileSize * .5f);
    }
}
