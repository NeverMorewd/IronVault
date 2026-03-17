using IronVault.Core.Engine.Components;
using IronVault.Core.Engine.Entities;
using IronVault.Core.Map;

namespace IronVault.Core.Engine.Systems;

public static class MoveSystem
{
    public static void Update(IReadOnlyList<TankEntity> tanks, TileMap map, float dt)
    {
        foreach (var tank in tanks)
        {
            if (!tank.IsAlive) continue;
            if (tank.IsFrozen) continue;   // Clock power-up: enemy freeze

            // ── Invincibility blink ───────────────────────────────────────────
            if (tank.IsInvincible)
            {
                if (tank.SpawnInvincibleTimer > 0) tank.SpawnInvincibleTimer -= dt;
                if (tank.PowerUpInvincibleTimer > 0) tank.PowerUpInvincibleTimer -= dt;
                tank.BlinkTimer += dt;
                if (tank.BlinkTimer >= 0.1f)
                {
                    tank.BlinkTimer  = 0;
                    tank.BlinkVisible = !tank.BlinkVisible;
                }
            }
            else
            {
                tank.BlinkVisible = true;
            }

            // ── Player ice-momentum: slide when no keys held ──────────────────
            if (tank.IsPlayerControlled)
            {
                bool onIce = IsOnIce(tank, map);
                if (!onIce) tank.IceMomentum = 0f;

                var input    = tank.Input;
                bool hasInput = input.MoveUp || input.MoveDown ||
                                input.MoveLeft || input.MoveRight;

                if (!hasInput && onIce && tank.IceMomentum > 0f)
                {
                    tank.IceMomentum = MathF.Max(0f, tank.IceMomentum - 2.0f * dt);
                    if (tank.IceMomentum > 0f)
                    {
                        float slideSpeed = tank.Velocity.Speed * tank.IceMomentum;
                        (float sdx, float sdy) = DirectionDelta(tank.IceMomentumDir);
                        float snx = tank.Position.X + sdx * slideSpeed * dt;
                        float sny = tank.Position.Y + sdy * slideSpeed * dt;

                        if (CanMoveTo(snx, sny, map, tanks, tank))
                        {
                            tank.Position.X  = snx;
                            tank.Position.Y  = sny;
                            tank.Velocity.IsMoving = true;
                        }
                        else
                        {
                            tank.IceMomentum       = 0f;
                            tank.Velocity.IsMoving = false;
                        }
                    }
                    else
                    {
                        tank.Velocity.IsMoving = false;
                    }
                    continue;
                }
            }

            Direction? desiredDir = GetDesiredDirection(tank);
            if (desiredDir is null)
            {
                tank.Velocity.IsMoving = false;
                continue;
            }

            var dir = desiredDir.Value;

            if (tank.Position.Facing != dir)
            {
                if (tank.IsPlayerControlled)
                {
                    // ── Fix 2: player rotates in place — no snap, no displacement ──
                    // On the NEXT frame (still holding the key) the tank will move.
                    tank.Position.Facing   = dir;
                    tank.Velocity.IsMoving = false;
                    continue;
                }
                else
                {
                    // Enemies snap to grid on turn for clean grid-based navigation
                    SnapToGrid(tank);
                    tank.Position.Facing = dir;
                }
            }

            // ── Align perpendicular axis for smooth corridor navigation ────────
            // Replaces the old "snap-on-turn" for the player.  Snaps the axis
            // that is NOT the movement axis to the nearest grid line — but only
            // when the result is passable and doesn't increase tank overlap.
            if (tank.IsPlayerControlled)
                TryAlignPerpAxis(tank, dir, map, tanks);

            float speed = tank.Velocity.Speed;
            (float dx, float dy) = DirectionDelta(dir);
            float nx = tank.Position.X + dx * speed * dt;
            float ny = tank.Position.Y + dy * speed * dt;

            if (CanMoveTo(nx, ny, map, tanks, tank))
            {
                tank.Position.X  = nx;
                tank.Position.Y  = ny;
                tank.Velocity.IsMoving = true;

                if (tank.IsPlayerControlled && IsOnIce(tank, map))
                {
                    tank.IceMomentum    = 1.0f;
                    tank.IceMomentumDir = dir;
                }
            }
            else if (tank.IsPlayerControlled)
            {
                // ── Fix 1 (partial): only try a partial move when the component
                // actually differs from the current value, so we never "succeed"
                // by re-checking the current position (which caused the full lock). ──
                bool moved = false;
                if (dx != 0 && CanMoveTo(nx, tank.Position.Y, map, tanks, tank))
                {
                    tank.Position.X  = nx;
                    moved = true;
                }
                else if (dy != 0 && CanMoveTo(tank.Position.X, ny, map, tanks, tank))
                {
                    tank.Position.Y  = ny;
                    moved = true;
                }

                tank.IceMomentum       = moved ? tank.IceMomentum : 0f;
                tank.Velocity.IsMoving = moved;
            }
            else
            {
                bool turned = TryPerpendicularMove(tank, dir, speed, dt, map, tanks);
                tank.Velocity.IsMoving = turned;
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Snaps the perpendicular axis to the nearest tile boundary so the player
    /// glides smoothly through corridor openings.  Only applied when the result
    /// passes both tile and tank-overlap checks.
    /// </summary>
    private static void TryAlignPerpAxis(TankEntity tank, Direction dir,
        TileMap map, IReadOnlyList<TankEntity> tanks)
    {
        float half = TileMap.TileSize;
        float sx = tank.Position.X, sy = tank.Position.Y;
        float tx, ty;

        if (dir == Direction.Up || dir == Direction.Down)
        {
            tx = MathF.Round(sx / half) * half;
            ty = sy;
        }
        else
        {
            tx = sx;
            ty = MathF.Round(sy / half) * half;
        }

        if (tx == sx && ty == sy) return;  // Already aligned

        // Apply only if the snapped position is fully passable and doesn't
        // increase tank overlap (the escape logic in CanMoveTo handles this).
        if (CanMoveTo(tx, ty, map, tanks, tank))
        {
            tank.Position.X = tx;
            tank.Position.Y = ty;
        }
    }

    private static bool IsOnIce(TankEntity tank, TileMap map)
    {
        float cx = tank.Position.X + TankEntity.Size * 0.5f;
        float cy = tank.Position.Y + TankEntity.Size * 0.5f;
        int col = (int)(cx / TileMap.TileSize);
        int row = (int)(cy / TileMap.TileSize);
        if (col < 0 || col >= map.Cols || row < 0 || row >= map.Rows) return false;
        return map[col, row] == TileType.Ice;
    }

    private static Direction? GetDesiredDirection(TankEntity tank)
    {
        if (!tank.IsPlayerControlled) return tank.Position.Facing;

        var input = tank.Input;
        if (input.MoveUp)    return Direction.Up;
        if (input.MoveDown)  return Direction.Down;
        if (input.MoveLeft)  return Direction.Left;
        if (input.MoveRight) return Direction.Right;
        return null;
    }

    private static bool TryPerpendicularMove(
        TankEntity tank, Direction blocked, float speed, float dt, TileMap map,
        IReadOnlyList<TankEntity> tanks)
    {
        Direction[] perps = blocked is Direction.Up or Direction.Down
            ? [Direction.Left, Direction.Right]
            : [Direction.Up,   Direction.Down];

        if (tank.Id % 2 == 1)
            (perps[0], perps[1]) = (perps[1], perps[0]);

        foreach (var alt in perps)
        {
            (float adx, float ady) = DirectionDelta(alt);
            float anx = tank.Position.X + adx * speed * dt;
            float any = tank.Position.Y + ady * speed * dt;

            if (CanMoveTo(anx, any, map, tanks, tank))
            {
                SnapToGrid(tank);
                tank.Position.Facing = alt;
                tank.Position.X      = anx;
                tank.Position.Y      = any;
                return true;
            }
        }
        return false;
    }

    private static (float dx, float dy) DirectionDelta(Direction dir) => dir switch
    {
        Direction.Up    => (0, -1),
        Direction.Down  => (0,  1),
        Direction.Left  => (-1, 0),
        _               => (1,  0),
    };

    /// <summary>
    /// Checks tile passability AND no harmful tank overlap at (x, y).
    /// </summary>
    private static bool CanMoveTo(float x, float y, TileMap map,
        IReadOnlyList<TankEntity> tanks, TankEntity self)
        => CanMoveTo(x, y, map) && !OverlapsTank(x, y, tanks, self);

    /// <summary>Tile-only passability check (used internally).</summary>
    private static bool CanMoveTo(float x, float y, TileMap map)
    {
        int   size   = TankEntity.Size;
        const float margin = 1f;
        return IsTilePassable(x + margin,        y + margin,        map)
            && IsTilePassable(x + size - margin, y + margin,        map)
            && IsTilePassable(x + margin,        y + size - margin, map)
            && IsTilePassable(x + size - margin, y + size - margin, map);
    }

    private static bool IsTilePassable(float px, float py, TileMap map)
    {
        int col = (int)(px / TileMap.TileSize);
        int row = (int)(py / TileMap.TileSize);
        return map.IsPassable(col, row);
    }

    /// <summary>
    /// Returns true when placing <paramref name="self"/> at (x, y) would cause
    /// a harmful tank-vs-tank overlap.
    ///
    /// Uses strict AABB and an "escape" rule:
    ///   • If the two tanks are NOT currently overlapping → block any new overlap.
    ///   • If they ARE already overlapping (rare bad state after a snap) →
    ///     allow movement only when it REDUCES the overlap area, so the tank
    ///     can always escape rather than being permanently frozen.
    /// </summary>
    private static bool OverlapsTank(float nx, float ny,
        IReadOnlyList<TankEntity> tanks, TankEntity self)
    {
        int size = TankEntity.Size;

        for (int i = 0; i < tanks.Count; i++)
        {
            var other = tanks[i];
            if (ReferenceEquals(other, self) || !other.IsAlive) continue;

            float ox = other.Position.X;
            float oy = other.Position.Y;

            // AABB overlap area at the new position
            float aw = MathF.Max(0, MathF.Min(nx + size, ox + size) - MathF.Max(nx, ox));
            float ah = MathF.Max(0, MathF.Min(ny + size, oy + size) - MathF.Max(ny, oy));
            float newArea = aw * ah;

            if (newArea <= 0) continue;  // No overlap at new position — allow

            // There IS overlap at the new position. Check current overlap.
            float sx = self.Position.X, sy = self.Position.Y;
            float cw = MathF.Max(0, MathF.Min(sx + size, ox + size) - MathF.Max(sx, ox));
            float ch = MathF.Max(0, MathF.Min(sy + size, oy + size) - MathF.Max(sy, oy));
            float curArea = cw * ch;

            // Allow only if the move strictly reduces the existing overlap
            // (escape mode).  Block if overlap is new or not shrinking.
            if (newArea >= curArea) return true;
        }
        return false;
    }

    private static void SnapToGrid(TankEntity tank)
    {
        float half   = TileMap.TileSize;
        tank.Position.X = MathF.Round(tank.Position.X / half) * half;
        tank.Position.Y = MathF.Round(tank.Position.Y / half) * half;
    }
}
