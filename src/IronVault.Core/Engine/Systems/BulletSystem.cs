using IronVault.Core.Engine.Components;
using IronVault.Core.Engine.Entities;
using IronVault.Core.Map;

namespace IronVault.Core.Engine.Systems;

public static class BulletSystem
{
    public static void Update(
        List<BulletEntity>    bullets,
        List<TankEntity>      tanks,
        List<ExplosionEntity> explosions,
        TileMap               map,
        float                 dt)
    {
        // ── Phase 1: Move all live bullets ───────────────────────────────────
        foreach (var b in bullets)
        {
            if (!b.IsAlive) continue;
            float dx = 0, dy = 0;
            switch (b.Direction)
            {
                case Direction.Up:    dy = -1; break;
                case Direction.Down:  dy =  1; break;
                case Direction.Left:  dx = -1; break;
                case Direction.Right: dx =  1; break;
            }
            b.X += dx * b.Speed * dt;
            b.Y += dy * b.Speed * dt;
        }

        // ── Phase 2: Bullet-vs-bullet cancellation ───────────────────────────
        CheckBulletCollisions(bullets, tanks, explosions);

        // ── Phase 3: Tile / tank collision (sweep dead bullets out) ──────────
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            var b = bullets[i];

            // Already killed by bullet-vs-bullet this frame
            if (!b.IsAlive) { bullets.RemoveAt(i); continue; }

            // Out of bounds
            if (b.X < 0 || b.Y < 0 ||
                b.X + BulletEntity.Width  > map.Cols * TileMap.TileSize ||
                b.Y + BulletEntity.Height > map.Rows * TileMap.TileSize)
            {
                SpawnExplosion(explosions, b.X, b.Y, 0, ExplosionType.Normal);
                DecrementOwnerBullets(b, tanks);
                b.IsAlive = false;
                bullets.RemoveAt(i);
                continue;
            }

            // Tile collision
            if (CheckTileCollision(b, map, explosions))
            {
                DecrementOwnerBullets(b, tanks);
                b.IsAlive = false;
                bullets.RemoveAt(i);
                continue;
            }

            // Tank collision
            if (CheckTankCollision(b, tanks, explosions))
            {
                DecrementOwnerBullets(b, tanks);
                b.IsAlive = false;
                bullets.RemoveAt(i);
            }
        }
    }

    // ── Bullet-vs-bullet ─────────────────────────────────────────────────────

    /// <summary>
    /// Checks every opposing pair of live bullets for overlap.
    /// On contact each bullet loses Health equal to the other's Power.
    /// Clash explosion spawns at midpoint; destroyed bullets are flagged dead
    /// so Phase 3 will sweep them out.
    /// </summary>
    private static void CheckBulletCollisions(
        List<BulletEntity>    bullets,
        List<TankEntity>      tanks,
        List<ExplosionEntity> explosions)
    {
        for (int i = 0; i < bullets.Count - 1; i++)
        {
            var a = bullets[i];
            if (!a.IsAlive) continue;

            for (int j = i + 1; j < bullets.Count; j++)
            {
                var b = bullets[j];
                if (!b.IsAlive) continue;

                // Only opposing factions cancel each other
                if (!AreOpposing(a.OwnerTeam, b.OwnerTeam)) continue;

                // Broad-phase: cheap center-distance check.
                // Threshold 16 px gives a reliable window even at high speed
                // (two P2 bullets closing at ~576 px/s move ≈9.6 px per frame).
                if (!BulletsOverlap(a, b, threshold: 16f)) continue;

                // ── Apply mutual damage ──────────────────────────────────────
                int aPower = a.Power;   // capture before potential kill
                int bPower = b.Power;

                a.Health -= bPower;
                b.Health -= aPower;

                // Clash spark at midpoint
                float mx = (a.X + b.X) / 2f + BulletEntity.Width  / 2f;
                float my = (a.Y + b.Y) / 2f + BulletEntity.Height / 2f;
                SpawnExplosion(explosions, mx - 12, my - 12, 1, ExplosionType.Clash);

                // Kill bullets that ran out of health
                if (a.Health <= 0)
                {
                    DecrementOwnerBullets(a, tanks);
                    a.IsAlive = false;
                }
                if (b.Health <= 0)
                {
                    DecrementOwnerBullets(b, tanks);
                    b.IsAlive = false;
                }

                // Once a is dead there is nothing left to compare it against
                if (!a.IsAlive) break;
            }
        }
    }

    /// <summary>
    /// True when the two bullets belong to enemy factions
    /// (Player/Ally count as friendly; Enemy is the opposing side).
    /// </summary>
    private static bool AreOpposing(TankTeam a, TankTeam b)
    {
        bool aEnemy = a == TankTeam.Enemy;
        bool bEnemy = b == TankTeam.Enemy;
        return aEnemy != bEnemy;
    }

    /// <summary>
    /// Circle-distance overlap test using bullet centres.
    /// Generous threshold compensates for high bullet speeds.
    /// </summary>
    private static bool BulletsOverlap(BulletEntity a, BulletEntity b, float threshold)
    {
        float acx = a.X + BulletEntity.Width  / 2f;
        float acy = a.Y + BulletEntity.Height / 2f;
        float bcx = b.X + BulletEntity.Width  / 2f;
        float bcy = b.Y + BulletEntity.Height / 2f;

        float dx = acx - bcx;
        float dy = acy - bcy;
        return dx * dx + dy * dy < threshold * threshold;
    }

    // ── Tile collision ────────────────────────────────────────────────────────

    private static bool CheckTileCollision(BulletEntity b, TileMap map, List<ExplosionEntity> explosions)
    {
        // Sample the bullet's four corners
        float[] xs = [b.X, b.X + BulletEntity.Width - 1];
        float[] ys = [b.Y, b.Y + BulletEntity.Height - 1];

        foreach (var py in ys)
        foreach (var px in xs)
        {
            int col = (int)(px / TileMap.TileSize);
            int row = (int)(py / TileMap.TileSize);
            if (!map.InBounds(col, row)) continue;

            var tile = map[col, row];
            if (tile == TileType.Empty  || tile == TileType.Forest ||
                tile == TileType.Ice    || tile == TileType.Spawn)
                continue;

            if (tile == TileType.Water) continue; // bullets fly over water

            if (tile == TileType.Brick)
            {
                map[col, row] = TileType.Empty;
                SpawnExplosion(explosions, col * TileMap.TileSize, row * TileMap.TileSize,
                               1, ExplosionType.Normal);
                return true;
            }

            if (tile == TileType.Steel)
            {
                if (b.Power >= 2)
                    map[col, row] = TileType.Empty;
                SpawnExplosion(explosions, col * TileMap.TileSize, row * TileMap.TileSize,
                               0, ExplosionType.Normal);
                return true;
            }

            if (tile == TileType.Base)
            {
                map[col, row] = TileType.Empty;
                SpawnExplosion(explosions, col * TileMap.TileSize, row * TileMap.TileSize,
                               2, ExplosionType.Normal);
                return true;
            }

            return true; // any other solid tile
        }
        return false;
    }

    // ── Tank collision ────────────────────────────────────────────────────────

    private static bool CheckTankCollision(BulletEntity b, List<TankEntity> tanks,
                                           List<ExplosionEntity> explosions)
    {
        var bRect = new Rect(b.X, b.Y, BulletEntity.Width, BulletEntity.Height);

        foreach (var tank in tanks)
        {
            if (!tank.IsAlive) continue;
            if (tank.Id == b.OwnerId) continue;
            if (tank.Team == b.OwnerTeam && b.OwnerTeam != TankTeam.Player) continue;
            if (tank.Team == TankTeam.Ally && b.OwnerTeam == TankTeam.Player) continue;
            if (tank.IsInvincible) continue;

            var tRect = new Rect(tank.Position.X, tank.Position.Y, TankEntity.Size, TankEntity.Size);
            if (!bRect.Intersects(tRect)) continue;

            tank.Health.TakeDamage(1);
            if (!tank.Health.IsAlive)
            {
                tank.IsAlive = false;
                SpawnExplosion(explosions,
                               tank.Position.X + TankEntity.Size / 2f - 16,
                               tank.Position.Y + TankEntity.Size / 2f - 16,
                               2, ExplosionType.Normal);
            }
            else
            {
                SpawnExplosion(explosions, b.X, b.Y, 1, ExplosionType.Normal);
            }
            return true;
        }
        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void DecrementOwnerBullets(BulletEntity b, List<TankEntity> tanks)
    {
        foreach (var t in tanks)
            if (t.Id == b.OwnerId) { t.Weapon.ActiveBullets--; break; }
    }

    private static void SpawnExplosion(List<ExplosionEntity> explosions,
                                       float x, float y, int size, ExplosionType type)
    {
        explosions.Add(new ExplosionEntity { X = x, Y = y, Size = size, Type = type });
    }

    private readonly record struct Rect(float X, float Y, float W, float H)
    {
        public bool Intersects(Rect other) =>
            X < other.X + other.W && X + W > other.X &&
            Y < other.Y + other.H && Y + H > other.Y;
    }
}
