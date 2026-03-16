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

            // Player: direction from input; AI: always wants to move in current facing
            Direction? desiredDir = GetDesiredDirection(tank);
            if (desiredDir is null)
            {
                tank.Velocity.IsMoving = false;
                continue;
            }

            var dir = desiredDir.Value;

            // When turning, snap to tile grid to avoid off-axis drift
            if (tank.Position.Facing != dir)
            {
                SnapToGrid(tank);
                tank.Position.Facing = dir;
            }

            float speed = tank.Velocity.Speed;
            (float dx, float dy) = DirectionDelta(dir);
            float nx = tank.Position.X + dx * speed * dt;
            float ny = tank.Position.Y + dy * speed * dt;

            if (CanMoveTo(nx, ny, map))
            {
                tank.Position.X = nx;
                tank.Position.Y = ny;
                tank.Velocity.IsMoving = true;
            }
            else if (tank.IsPlayerControlled)
            {
                // Player: slide along the wall on one axis
                if (CanMoveTo(nx, tank.Position.Y, map))
                {
                    tank.Position.X = nx;
                    tank.Velocity.IsMoving = true;
                }
                else if (CanMoveTo(tank.Position.X, ny, map))
                {
                    tank.Position.Y = ny;
                    tank.Velocity.IsMoving = true;
                }
                else
                {
                    tank.Velocity.IsMoving = false;
                }
            }
            else
            {
                // AI: try perpendicular directions to avoid walls automatically.
                // Alternate left/right preference per tank ID to prevent convoy lock-step.
                bool turned = TryPerpendicularMove(tank, dir, speed, dt, map);
                tank.Velocity.IsMoving = turned;
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Direction? GetDesiredDirection(TankEntity tank)
    {
        if (!tank.IsPlayerControlled)
            // AI always wants to advance in current facing direction
            return tank.Position.Facing;

        var input = tank.Input;
        if (input.MoveUp)    return Direction.Up;
        if (input.MoveDown)  return Direction.Down;
        if (input.MoveLeft)  return Direction.Left;
        if (input.MoveRight) return Direction.Right;
        return null;
    }

    private static bool TryPerpendicularMove(
        TankEntity tank, Direction blocked, float speed, float dt, TileMap map)
    {
        Direction[] perps = blocked is Direction.Up or Direction.Down
            ? [Direction.Left, Direction.Right]
            : [Direction.Up,   Direction.Down];

        // Alternate preference by tank ID so not all tanks turn the same way
        if (tank.Id % 2 == 1)
            (perps[0], perps[1]) = (perps[1], perps[0]);

        foreach (var alt in perps)
        {
            (float adx, float ady) = DirectionDelta(alt);
            float anx = tank.Position.X + adx * speed * dt;
            float any = tank.Position.Y + ady * speed * dt;

            if (CanMoveTo(anx, any, map))
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

    private static void SnapToGrid(TankEntity tank)
    {
        float half = TileMap.TileSize;
        tank.Position.X = MathF.Round(tank.Position.X / half) * half;
        tank.Position.Y = MathF.Round(tank.Position.Y / half) * half;
    }
}
