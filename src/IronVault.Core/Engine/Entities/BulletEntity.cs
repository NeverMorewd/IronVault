using IronVault.Core.Engine.Components;

namespace IronVault.Core.Engine.Entities;

public sealed class BulletEntity : EntityBase
{
    public float X { get; set; }
    public float Y { get; set; }
    public Direction Direction { get; set; }
    public float Speed { get; set; }
    public TankTeam OwnerTeam { get; set; }
    public int OwnerId { get; set; }
    public int Power { get; set; } = 1;

    /// <summary>
    /// Remaining durability. Initialized to <see cref="Power"/> on spawn.
    /// When an opposing bullet hits this one it loses Health equal to that
    /// bullet's Power.  Reaching 0 destroys this bullet mid-air.
    /// <para>
    /// Cancellation table (attacker Power → hits needed to destroy this bullet):
    ///   P1 bullet (Health 1): 1× P1  or  1× P2
    ///   P2 bullet (Health 2): 2× P1  or  1× P2
    /// </para>
    /// </summary>
    public int Health { get; set; } = 1;

    public const int Width  = 6;
    public const int Height = 10;

    public static BulletEntity Spawn(TankEntity owner)
    {
        float bx, by;
        int halfTank = TankEntity.Size / 2;
        // Offset bullet to the front-center of the tank
        switch (owner.Position.Facing)
        {
            case Direction.Up:
                bx = owner.Position.X + halfTank - Width / 2;
                by = owner.Position.Y - Height;
                break;
            case Direction.Down:
                bx = owner.Position.X + halfTank - Width / 2;
                by = owner.Position.Y + TankEntity.Size;
                break;
            case Direction.Left:
                bx = owner.Position.X - Height;
                by = owner.Position.Y + halfTank - Width / 2;
                break;
            default: // Right
                bx = owner.Position.X + TankEntity.Size;
                by = owner.Position.Y + halfTank - Width / 2;
                break;
        }

        return new BulletEntity
        {
            X         = bx,
            Y         = by,
            Direction = owner.Position.Facing,
            Speed     = owner.Weapon.BulletSpeed,
            OwnerTeam = owner.Team,
            OwnerId   = owner.Id,
            Power     = owner.Weapon.Power,
            Health    = owner.Weapon.Power,   // durability = power level
        };
    }
}
