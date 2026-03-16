namespace IronVault.Core.Engine.Entities;

public enum PowerUpType : byte
{
    Star,       // Tank level up (speed + weapon)
    BulletSpeed,
    ExtraBullet,
    Shield,
    Clock,      // Freeze all enemies
    Shovel,     // Temporarily upgrade base walls to steel
    Life,       // Extra life
}

public sealed class PowerUpEntity : EntityBase
{
    public float X { get; set; }
    public float Y { get; set; }
    public PowerUpType Type { get; set; }

    /// <summary>Blinking timer for visual effect.</summary>
    public float BlinkTimer { get; set; }
    public bool BlinkVisible { get; set; } = true;

    public const int Size = 24;
}
