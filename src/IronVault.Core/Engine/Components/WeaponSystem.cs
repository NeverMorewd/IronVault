namespace IronVault.Core.Engine.Components;

public sealed class WeaponSystem
{
    /// <summary>Bullet speed in pixels per second.</summary>
    public float BulletSpeed { get; set; } = 288f;

    /// <summary>Maximum bullets this tank can have in flight simultaneously.</summary>
    public int MaxBullets { get; set; } = 1;

    /// <summary>Current bullets in flight.</summary>
    public int ActiveBullets { get; set; }

    /// <summary>Cooldown between shots in seconds.</summary>
    public float FireCooldown { get; set; } = 0.5f;

    /// <summary>Time remaining until next shot allowed.</summary>
    public float CooldownRemaining { get; set; }

    /// <summary>Bullet power: 1 = damages brick, 2 = damages steel.</summary>
    public int Power { get; set; } = 1;

    public bool CanFire => CooldownRemaining <= 0f && ActiveBullets < MaxBullets;
}
