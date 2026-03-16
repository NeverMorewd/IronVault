namespace IronVault.Core.Map;

public enum TileType : byte
{
    Empty = 0,
    Brick = 1,      // Destroyable wall
    Steel = 2,      // Indestructible (requires upgraded bullet)
    Water = 3,      // Impassable for tanks, passable for bullets
    Forest = 4,     // Passable, hides tanks
    Ice = 5,        // Slippery surface
    Base = 6,       // Player HQ — game over if destroyed
    Spawn = 7,      // Enemy spawn point (invisible in-game)
}
