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
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            var b = bullets[i];
            if (!b.IsAlive) { bullets.RemoveAt(i); continue; }

            // Move bullet
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

            // Check map bounds
            if (b.X < 0 || b.Y < 0 ||
                b.X + BulletEntity.Width  > map.Cols * TileMap.TileSize ||
                b.Y + BulletEntity.Height > map.Rows * TileMap.TileSize)
            {
                KillBullet(b, bullets, tanks, explosions, i, small: true);
                continue;
            }

            // Check tile collision
            if (CheckTileCollision(b, map, explosions))
            {
                DecrementOwnerBullets(b, tanks);
                b.IsAlive = false;
                bullets.RemoveAt(i);
                continue;
            }

            // Check tank collision
            if (CheckTankCollision(b, tanks, explosions))
            {
                DecrementOwnerBullets(b, tanks);
                b.IsAlive = false;
                bullets.RemoveAt(i);
            }
        }
    }

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
            if (tile == TileType.Empty || tile == TileType.Forest ||
                tile == TileType.Ice   || tile == TileType.Spawn)
                continue;

            if (tile == TileType.Water) continue; // bullets pass over water

            if (tile == TileType.Brick)
            {
                map[col, row] = TileType.Empty;
                // Size=1 (medium) so the brick-break effect is clearly visible
                SpawnExplosion(explosions, col * TileMap.TileSize, row * TileMap.TileSize, 1);
                return true;
            }

            if (tile == TileType.Steel)
            {
                if (b.Power >= 2)
                    map[col, row] = TileType.Empty;
                SpawnExplosion(explosions, col * TileMap.TileSize, row * TileMap.TileSize, 0);
                return true;
            }

            if (tile == TileType.Base)
            {
                // Signal game over will be handled by GameEngine
                map[col, row] = TileType.Empty;
                SpawnExplosion(explosions, col * TileMap.TileSize, row * TileMap.TileSize, 2);
                return true;
            }

            return true; // any other solid tile
        }
        return false;
    }

    private static bool CheckTankCollision(BulletEntity b, List<TankEntity> tanks, List<ExplosionEntity> explosions)
    {
        var bRect = new Rect(b.X, b.Y, BulletEntity.Width, BulletEntity.Height);

        foreach (var tank in tanks)
        {
            if (!tank.IsAlive) continue;
            if (tank.Id == b.OwnerId) continue; // don't hit self
            // Friendly fire: enemy bullets don't hurt enemies; player bullets don't hurt allies
            if (tank.Team == b.OwnerTeam && b.OwnerTeam != TankTeam.Player) continue;
            if (tank.Team == TankTeam.Ally && b.OwnerTeam == TankTeam.Player) continue;
            if (tank.IsInvincible) continue;

            var tRect = new Rect(tank.Position.X, tank.Position.Y, TankEntity.Size, TankEntity.Size);
            if (!bRect.Intersects(tRect)) continue;

            tank.Health.TakeDamage(1);
            if (!tank.Health.IsAlive)
            {
                tank.IsAlive = false;
                SpawnExplosion(explosions, tank.Position.X + TankEntity.Size / 2f - 16,
                               tank.Position.Y + TankEntity.Size / 2f - 16, 2);
            }
            else
            {
                SpawnExplosion(explosions, b.X, b.Y, 1);
            }
            return true;
        }
        return false;
    }

    private static void KillBullet(BulletEntity b, List<BulletEntity> bullets,
        List<TankEntity> tanks, List<ExplosionEntity> explosions, int index, bool small)
    {
        SpawnExplosion(explosions, b.X, b.Y, small ? 0 : 1);
        DecrementOwnerBullets(b, tanks);
        b.IsAlive = false;
        bullets.RemoveAt(index);
    }

    private static void DecrementOwnerBullets(BulletEntity b, List<TankEntity> tanks)
    {
        foreach (var t in tanks)
            if (t.Id == b.OwnerId) { t.Weapon.ActiveBullets--; break; }
    }

    private static void SpawnExplosion(List<ExplosionEntity> explosions, float x, float y, int size)
    {
        explosions.Add(new ExplosionEntity { X = x, Y = y, Size = size });
    }

    private readonly record struct Rect(float X, float Y, float W, float H)
    {
        public bool Intersects(Rect other) =>
            X < other.X + other.W && X + W > other.X &&
            Y < other.Y + other.H && Y + H > other.Y;
    }
}
