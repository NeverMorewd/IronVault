namespace IronVault.Core.Engine.Components;

/// <summary>
/// Pixel-space position and facing direction.
/// X/Y represent the top-left corner of the entity's bounding box.
/// </summary>
public sealed class Position
{
    public float X { get; set; }
    public float Y { get; set; }
    public Direction Facing { get; set; } = Direction.Up;

    public Position() { }
    public Position(float x, float y, Direction facing = Direction.Up)
    {
        X = x; Y = y; Facing = facing;
    }
}

public enum Direction : byte { Up, Down, Left, Right }
