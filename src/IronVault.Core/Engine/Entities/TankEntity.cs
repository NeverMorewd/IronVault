using IronVault.Core.Engine.Components;

namespace IronVault.Core.Engine.Entities;

public enum TankTeam : byte { Player, Enemy, Ally }
public enum TankTier : byte { Tier1 = 1, Tier2, Tier3, Tier4 }

public sealed class TankEntity : EntityBase
{
    public TankTeam Team { get; set; }
    public TankTier Tier { get; set; } = TankTier.Tier1;
    public int Level { get; set; } = 1;

    public Position Position { get; } = new();
    public Health Health { get; }
    public Velocity Velocity { get; } = new();
    public WeaponSystem Weapon { get; } = new();

    /// <summary>Whether this tank is currently being controlled as a player.</summary>
    public bool IsPlayerControlled { get; set; }

    /// <summary>Input state for the current frame (player-only).</summary>
    public TankInput Input { get; set; }

    // Spawning / invincibility frames
    public float SpawnInvincibleTimer { get; set; } = 2.0f;
    public bool IsInvincible => SpawnInvincibleTimer > 0;

    /// <summary>Blink state for invincibility visual effect.</summary>
    public bool BlinkVisible { get; set; } = true;

    /// <summary>Accumulated blink timer.</summary>
    public float BlinkTimer { get; set; }

    public static TankEntity CreatePlayer(float x, float y)
        => new(TankTeam.Player, x, y) { IsPlayerControlled = true };

    public static TankEntity CreateEnemy(TankTier tier, float x, float y)
        => new(TankTeam.Enemy, x, y) { Tier = tier };

    public static TankEntity CreateAlly(float x, float y)
        => new(TankTeam.Ally, x, y);

    private TankEntity(TankTeam team, float x, float y)
    {
        Team = team;
        Position.X = x;
        Position.Y = y;
        Health = new Health(GetMaxHp(team));
        Velocity.Speed = GetSpeed(team);
    }

    private static int GetMaxHp(TankTeam team) => team switch
    {
        TankTeam.Enemy => 1,
        TankTeam.Ally  => 3,
        _              => 3,
    };

    private static float GetSpeed(TankTeam team) => team switch
    {
        TankTeam.Enemy => 72f,
        TankTeam.Ally  => 80f,
        _              => 96f,
    };

    /// <summary>Tank body size in pixels (always a square).</summary>
    public const int Size = 48;
}

public record struct TankInput(
    bool MoveUp,
    bool MoveDown,
    bool MoveLeft,
    bool MoveRight,
    bool Fire
);
