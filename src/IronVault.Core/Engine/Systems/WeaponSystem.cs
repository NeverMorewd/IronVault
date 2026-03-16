using IronVault.Core.Engine.Entities;

namespace IronVault.Core.Engine.Systems;

public static class WeaponSystem
{
    public static void Update(List<TankEntity> tanks, List<BulletEntity> bullets, float dt)
    {
        foreach (var tank in tanks)
        {
            if (!tank.IsAlive) continue;

            // Tick cooldown
            if (tank.Weapon.CooldownRemaining > 0)
                tank.Weapon.CooldownRemaining -= dt;

            // Fire check
            bool wantsFire = tank.IsPlayerControlled
                ? tank.Input.Fire
                : false; // AI handled in AISystem

            if (wantsFire && tank.Weapon.CanFire)
                Fire(tank, bullets);
        }
    }

    public static void Fire(TankEntity tank, List<BulletEntity> bullets)
    {
        if (!tank.Weapon.CanFire) return;
        var bullet = BulletEntity.Spawn(tank);
        bullets.Add(bullet);
        tank.Weapon.ActiveBullets++;
        tank.Weapon.CooldownRemaining = tank.Weapon.FireCooldown;
    }
}
