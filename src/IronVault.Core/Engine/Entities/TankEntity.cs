using IronVault.Core.Engine.Components;

namespace IronVault.Core.Engine.Entities;

public enum TankTeam : byte { Player, Enemy, Ally }
public enum TankTier : byte { Tier1 = 1, Tier2, Tier3, Tier4 }

/// <summary>
/// A single tank entity.  Contains no logic — just data consumed by the Systems.
/// </summary>
public sealed class TankEntity : EntityBase
{
    public TankTeam Team  { get; set; }
    public TankTier Tier  { get; set; } = TankTier.Tier1;
    public int      Level { get; set; } = 1;

    public Position     Position { get; } = new();
    public Health       Health   { get; }
    public Velocity     Velocity { get; } = new();
    public WeaponSystem Weapon   { get; } = new();

    /// <summary>Whether this tank is currently player-controlled.</summary>
    public bool IsPlayerControlled { get; set; }

    /// <summary>Input snapshot for the current frame (player-only).</summary>
    public TankInput Input { get; set; }

    // ── Spawn invincibility ────────────────────────────────────────────────
    public float SpawnInvincibleTimer { get; set; } = 2.0f;
    public bool  IsInvincible  => SpawnInvincibleTimer > 0;
    public bool  BlinkVisible  { get; set; } = true;
    public float BlinkTimer    { get; set; }

    // ── Ice-slide momentum (player only) ──────────────────────────────────
    /// <summary>
    /// Remaining slide momentum while on an ice tile.
    /// 0 = fully stopped; 1 = full-speed slide.
    /// Decays at 2 units/second when the player releases all movement keys.
    /// Cleared immediately when the tank leaves the ice.
    /// </summary>
    public float     IceMomentum    { get; set; }
    /// <summary>Direction the tank was moving when it last touched an ice tile.</summary>
    public Direction IceMomentumDir { get; set; } = Direction.Up;

    /// <summary>Tank body size in pixels (square).</summary>
    public const int Size = 48;

    // ── Factory methods ───────────────────────────────────────────────────

    public static TankEntity CreatePlayer(float x, float y)
        => new(TankTeam.Player, TankTier.Tier1, x, y) { IsPlayerControlled = true };

    public static TankEntity CreateAlly(float x, float y)
        => new(TankTeam.Ally, TankTier.Tier1, x, y);

    /// <summary>
    /// Creates an enemy tank tuned for the given tier:
    /// <list type="table">
    ///   <item><term>Tier1</term><description>1 HP · 64 px/s · P1 bullet · 0.50 s cooldown</description></item>
    ///   <item><term>Tier2</term><description>2 HP · 72 px/s · P1 bullet · 0.40 s cooldown</description></item>
    ///   <item><term>Tier3</term><description>3 HP · 84 px/s · P2 steel-piercing · 0.35 s cooldown</description></item>
    ///   <item><term>Tier4</term><description>4 HP · 56 px/s · P2 · dual-shot · 0.30 s cooldown</description></item>
    /// </list>
    /// </summary>
    public static TankEntity CreateEnemy(TankTier tier, float x, float y)
    {
        var e = new TankEntity(TankTeam.Enemy, tier, x, y);
        switch (tier)
        {
            case TankTier.Tier2:
                e.Weapon.FireCooldown = 0.40f;
                break;
            case TankTier.Tier3:
                e.Weapon.Power        = 2;       // pierces steel
                e.Weapon.FireCooldown = 0.35f;
                e.Weapon.BulletSpeed  = 320f;
                break;
            case TankTier.Tier4:
                e.Weapon.Power        = 2;
                e.Weapon.MaxBullets   = 2;       // two shells simultaneously
                e.Weapon.FireCooldown = 0.30f;
                e.Weapon.BulletSpeed  = 352f;
                break;
        }
        return e;
    }

    // ── Private constructor ───────────────────────────────────────────────

    private TankEntity(TankTeam team, TankTier tier, float x, float y)
    {
        Team           = team;
        Tier           = tier;
        Position.X     = x;
        Position.Y     = y;
        Health         = new Health(HpTable(team, tier));
        Velocity.Speed = SpeedTable(team, tier);
    }

    // ── Stat tables ───────────────────────────────────────────────────────

    private static int HpTable(TankTeam team, TankTier tier) => team switch
    {
        TankTeam.Enemy => tier switch
        {
            TankTier.Tier2 => 2,
            TankTier.Tier3 => 3,
            TankTier.Tier4 => 4,
            _              => 1,
        },
        TankTeam.Ally => 3,
        _             => 3,   // Player
    };

    private static float SpeedTable(TankTeam team, TankTier tier) => team switch
    {
        TankTeam.Enemy => tier switch
        {
            TankTier.Tier2 => 72f,
            TankTier.Tier3 => 84f,
            TankTier.Tier4 => 56f,  // heavy but hard-hitting
            _              => 64f,
        },
        TankTeam.Ally => 80f,
        _             => 96f,   // Player
    };
}

public record struct TankInput(
    bool MoveUp,
    bool MoveDown,
    bool MoveLeft,
    bool MoveRight,
    bool Fire
);
