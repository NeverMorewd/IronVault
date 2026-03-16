using IronVault.Core.Engine.Components;
using IronVault.Core.Engine.Entities;
using IronVault.Core.Map;

namespace IronVault.Core.Engine.Systems;

public enum AIDifficulty : byte { Easy, Normal, Hard }

public static class AISystem
{
    private static readonly Random _rng = new();

    public static void Update(
        List<TankEntity>   tanks,
        List<BulletEntity> bullets,
        TileMap            map,
        AIDifficulty       difficulty,
        float              dt)
    {
        // Find player tanks for targeting
        TankEntity? player = null;
        foreach (var t in tanks)
            if (t.IsPlayerControlled && t.IsAlive) { player = t; break; }

        // Find base position
        (float baseX, float baseY) = FindBase(map);

        foreach (var tank in tanks)
        {
            if (!tank.IsAlive || tank.IsPlayerControlled) continue;
            UpdateAITank(tank, bullets, player, baseX, baseY, map, difficulty, dt);
        }
    }

    private static void UpdateAITank(
        TankEntity         tank,
        List<BulletEntity> bullets,
        TankEntity?        player,
        float              baseX,
        float              baseY,
        TileMap            map,
        AIDifficulty       difficulty,
        float              dt)
    {
        tank.Velocity.IsMoving = true;

        switch (difficulty)
        {
            case AIDifficulty.Easy:
                UpdateEasy(tank, bullets, map, dt);
                break;
            case AIDifficulty.Normal:
                UpdateNormal(tank, bullets, player, baseX, baseY, map, dt);
                break;
            case AIDifficulty.Hard:
                UpdateHard(tank, bullets, player, baseX, baseY, map, dt);
                break;
        }
    }

    // ── Easy: random movement, random fire ──────────────────────────────────
    private static void UpdateEasy(TankEntity tank, List<BulletEntity> bullets, TileMap map, float dt)
    {
        // Change direction randomly every 1-3 seconds
        if (tank.Weapon.CooldownRemaining <= 0 && _rng.NextDouble() < 0.3 * dt)
            tank.Position.Facing = (Direction)_rng.Next(4);

        // Random fire
        if (_rng.NextDouble() < 0.8 * dt && tank.Weapon.CanFire)
            WeaponSystem.Fire(tank, bullets);
    }

    // ── Normal: aim toward player or base ────────────────────────────────────
    private static void UpdateNormal(TankEntity tank, List<BulletEntity> bullets,
        TankEntity? player, float baseX, float baseY, TileMap map, float dt)
    {
        // 70% chance to chase player, 30% chance to attack base
        float targetX = player != null && _rng.NextDouble() < 0.7
            ? player.Position.X
            : baseX;
        float targetY = player != null && _rng.NextDouble() < 0.7
            ? player.Position.Y
            : baseY;

        Direction preferred = GetDirectionTo(tank.Position.X, tank.Position.Y, targetX, targetY);

        // Occasionally change randomly to avoid getting stuck
        if (_rng.NextDouble() < 0.15 * dt)
            preferred = (Direction)_rng.Next(4);

        tank.Position.Facing = preferred;

        // Fire when facing target
        if (IsAlignedWith(tank, targetX, targetY) && tank.Weapon.CanFire)
            WeaponSystem.Fire(tank, bullets);
        else if (_rng.NextDouble() < 0.5 * dt && tank.Weapon.CanFire)
            WeaponSystem.Fire(tank, bullets);
    }

    // ── Hard: flank player, dodge bullets, prioritize base ──────────────────
    private static void UpdateHard(TankEntity tank, List<BulletEntity> bullets,
        TankEntity? player, float baseX, float baseY, TileMap map, float dt)
    {
        // Prioritize base destruction
        float targetX = baseX, targetY = baseY;
        if (player != null && _rng.NextDouble() < 0.4)
        {
            // Attempt to flank (approach from side)
            float dx = player.Position.X - tank.Position.X;
            float dy = player.Position.Y - tank.Position.Y;
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                // Try to get behind/beside on the longer axis
                targetX = player.Position.X;
                targetY = player.Position.Y - 48;
            }
            else
            {
                targetX = player.Position.X - 48;
                targetY = player.Position.Y;
            }
        }

        Direction preferred = GetDirectionTo(tank.Position.X, tank.Position.Y, targetX, targetY);
        tank.Position.Facing = preferred;

        // Aggressive fire
        if (tank.Weapon.CanFire && _rng.NextDouble() < 2.0 * dt)
            WeaponSystem.Fire(tank, bullets);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
    private static Direction GetDirectionTo(float fromX, float fromY, float toX, float toY)
    {
        float dx = toX - fromX;
        float dy = toY - fromY;
        if (Math.Abs(dx) >= Math.Abs(dy))
            return dx >= 0 ? Direction.Right : Direction.Left;
        return dy >= 0 ? Direction.Down : Direction.Up;
    }

    private static bool IsAlignedWith(TankEntity tank, float targetX, float targetY)
    {
        const float tolerance = TankEntity.Size;
        return tank.Position.Facing switch
        {
            Direction.Up    or Direction.Down  => Math.Abs(tank.Position.X - targetX) < tolerance,
            Direction.Left  or Direction.Right => Math.Abs(tank.Position.Y - targetY) < tolerance,
            _ => false,
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
