using IronVault.Core.Engine.Entities;

namespace IronVault.Core.Engine.Systems;

/// <summary>
/// Animates power-up blink timers and checks whether the player tank
/// has walked over a power-up.  Collected power-ups are removed from
/// the list and reported via <paramref name="onPickup"/> so the caller
/// (GameEngine) can apply the appropriate effect.
/// </summary>
public static class PowerUpSystem
{
    private const float BlinkPeriod  = 0.22f;   // seconds per blink half-cycle
    private const float LifeSpan     = 18f;      // seconds before a power-up despawns

    public static void Update(
        List<PowerUpEntity> powerUps,
        List<TankEntity>    tanks,
        float               dt,
        Action<PowerUpType> onPickup)
    {
        // Find the (single) player tank
        TankEntity? player = null;
        foreach (var t in tanks)
            if (t.IsPlayerControlled && t.IsAlive) { player = t; break; }

        for (int i = powerUps.Count - 1; i >= 0; i--)
        {
            var pu = powerUps[i];
            if (!pu.IsAlive) { powerUps.RemoveAt(i); continue; }

            // Lifespan countdown — despawn after 18 s
            pu.LifeTimer += dt;
            if (pu.LifeTimer >= LifeSpan)
            {
                pu.IsAlive = false;
                powerUps.RemoveAt(i);
                continue;
            }

            // Blink faster in the last 4 seconds
            float period = pu.LifeTimer >= LifeSpan - 4f ? BlinkPeriod * 0.4f : BlinkPeriod;
            pu.BlinkTimer += dt;
            if (pu.BlinkTimer >= period)
            {
                pu.BlinkTimer    = 0;
                pu.BlinkVisible  = !pu.BlinkVisible;
            }

            // Pickup collision: player AABB vs power-up tile
            if (player is null) continue;

            bool overlapX = player.Position.X < pu.X + PowerUpEntity.Size
                         && player.Position.X + TankEntity.Size > pu.X;
            bool overlapY = player.Position.Y < pu.Y + PowerUpEntity.Size
                         && player.Position.Y + TankEntity.Size > pu.Y;

            if (overlapX && overlapY)
            {
                pu.IsAlive = false;
                onPickup(pu.Type);
                powerUps.RemoveAt(i);
            }
        }
    }
}
