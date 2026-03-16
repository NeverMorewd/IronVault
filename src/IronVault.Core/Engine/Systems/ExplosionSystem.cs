using IronVault.Core.Engine.Entities;

namespace IronVault.Core.Engine.Systems;

public static class ExplosionSystem
{
    public static void Update(List<ExplosionEntity> explosions, float dt)
    {
        for (int i = explosions.Count - 1; i >= 0; i--)
        {
            var e = explosions[i];
            e.FrameTimer += dt;
            if (e.FrameTimer >= ExplosionEntity.FrameDuration)
            {
                e.FrameTimer = 0;
                e.Frame++;
            }
            if (e.IsFinished)
                explosions.RemoveAt(i);
        }
    }
}
