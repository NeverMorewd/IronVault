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

            // Invincibility blink
            if (tank.IsInvincible)
            {
                tank.SpawnInvincibleTimer -= dt;
                tank.BlinkTimer += dt;
                if (tank.BlinkTimer >= 0.1f)
                {
                    tank.BlinkTimer = 0;
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

                var input = tank.Input;
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
                            tank.Position.X = snx;
                            tank.Position.Y = sny;
                            tank.Velocity.IsMoving = true;
                        }
                        else
                        {
                            tank.IceMomentum = 0f;
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
            // ─────────────────────────────────────────────────────────────────

            Direction? desiredDir = GetDesiredDirection(tank);
            if (desiredDir is null)
            {
                tank.Velocity.IsMoving = false;
                continue;
            }

            var dir = desiredDir.Value;

            if (tank.Position.Facing != dir)
            {
                if (!tank.IsPlayerControlled || tank.IceMomentum <= 0f)
                    SnapToGrid(tank);
                tank.Position.Facing = dir;
            }

            float speed = tank.Velocity.Speed;
            (float dx, float dy) = DirectionDelta(dir);
            float nx = tank.Position.X + dx * speed * dt;
            float ny = tank.Position.Y + dy * speed * dt;

            if (CanMoveTo(nx, ny, map, tanks, tank))
            {
                tank.Position.X = nx;
                tank.Position.Y = ny;
                tank.Velocity.IsMoving = true;

                if (tank.IsPlayerControlled && IsOnIce(tank, map))
                {
                    tank.IceMomentum    = 1.0f;
                    tank.IceMomentumDir = dir;
                }
            }
            else if (tank.IsPlayerControlled)
            {
                if (CanMoveTo(nx, tank.Position.Y, map, tanks, tank))
                {
                    tank.Position.X = nx;
                    tank.Velocity.IsMoving = true;
                }
                else if (CanMoveTo(tank.Position.X, ny, map, tanks, tank))
                {
                    tank.Position.Y = ny;
                    tank.Velocity.IsMoving = true;
                }
                else
                {
                    tank.IceMomentum = 0f;
                    tank.Velocity.IsMoving = false;
                }
            }
            else
            {
                bool turned = TryPerpendicularMove(tank, dir, speed, dt, map, tanks);
                tank.Velocity.IsMoving = turned;
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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
        if (!tank.IsPlayerControlled)
            return tank.Position.Facing;

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
                tank.Position.X = anx;
                tank.Position.Y = any;
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

    /// <summary>Checks tile passability AND no overlap with other alive tanks.</summary>
    private static bool CanMoveTo(float x, float y, TileMap map,
        IReadOnlyList<TankEntity> tanks, TankEntity self)
        => CanMoveTo(x, y, map) && !OverlapsTank(x, y, tanks, self);

    /// <summary>Tile-only passability check (used internally).</summary>
    private static bool CanMoveTo(float x, float y, TileMap map)
    {
        int size = TankEntity.Size;
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
    /// Returns true if placing a tank at (x,y) would overlap any other alive tank.
    /// Uses AABB with a small 2-pixel gap tolerance.
    /// </summary>
    private static bool OverlapsTank(float x, float y,
        IReadOnlyList<TankEntity> tanks, TankEntity self)
    {
        const float gap = 2f;
        float halfOverlap = TankEntity.Size - gap;
        float cx = x + TankEntity.Size * 0.5f;
        float cy = y + TankEntity.Size * 0.5f;

        for (int i = 0; i < tanks.Count; i++)
        {
            var other = tanks[i];
            if (ReferenceEquals(other, self) || !other.IsAlive) continue;

            float ocx = other.Position.X + TankEntity.Size * 0.5f;
            float ocy = other.Position.Y + TankEntity.Size * 0.5f;

            if (MathF.Abs(cx - ocx) < halfOverlap && MathF.Abs(cy - ocy) < halfOverlap)
                return true;
        }
        return false;
    }

    private static void SnapToGrid(TankEntity tank)
    {
        float half = TileMap.TileSize;
        tank.Position.X = MathF.Round(tank.Position.X / half) * half;
        tank.Position.Y = MathF.Round(tank.Position.Y / half) * half;
    }
}
