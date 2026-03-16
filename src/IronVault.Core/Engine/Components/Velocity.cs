namespace IronVault.Core.Engine.Components;

public sealed class Velocity
{
    /// <summary>Speed in pixels per second.</summary>
    public float Speed { get; set; }
    public bool IsMoving { get; set; }

    public Velocity(float speed = 96f) { Speed = speed; }
}
