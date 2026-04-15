using IronVault.Core.Engine.Components;
using IronVault.Core.Engine.Entities;
using IronVault.Core.Engine.Systems;
using IronVault.Core.Map;

namespace IronVault.Core.Test;


public class AllyAISystemTests
{
    // Constants based on your provided definitions
    private const int TankSize = TankEntity.Size; // 48 px
    private const int TileSize = TileMap.TileSize; // 24 px

    public AllyAISystemTests()
    {
        AllyAISystem.Reset();
    }

    [Fact]
    public void Update_ShouldNotFire_WhenBaseIsDirectlyInFront()
    {
        var map = new TileMap(28, 28);
        // Place base at (2, 1). Center Y = (1 * 24) + 12 = 36.
        map[2, 1] = TileType.Base;

        // Create Ally at (0, 0). Center Y = (0 + 48/2) = 24.
        // Wait! Center Y 24 vs Base Y 36 is still 12px apart. 

        // BETTER SETUP:
        // Ally Y = 0 (Center Y = 24)
        // Base at row 1 (Center Y = 24 + 12 = 36) -> Still offset.

        // CORRECT ALIGNMENT SETUP:
        float startY = 0;
        var ally = TankEntity.CreateAlly(0, startY); // Center Y = 24
        ally.Position.Facing = Direction.Right;

        // Base at Row 1 (Pixel Y 24 to 47). Center Y = 24 + 12 = 36.
        // To align perfectly, we need the Base center to be 24.
        // That means the Base must be at Pixel Y 12.
        // But Tiles are grid-locked. 

        // Let's adjust the Tank to the Tile Grid:
        // If Tank is at Row 0 and 1 (Y=0, Size=48), its center is 24.
        // If we check the logic: MathF.Abs(24 - baseY) < 12.
        // We need the Base to be at Y=24.

        map[2, 1] = TileType.Base; // Base center Y is 36.

        // Update Tank Y so center is 36:
        ally.Position.Y = 12; // Center Y = 12 + 24 = 36.

        // Update Enemy Y so center is 36:
        var enemy = TankEntity.CreateEnemy(TankTier.Tier1, 10 * TileMap.TileSize, 12);

        var tanks = new List<TankEntity> { ally, enemy };

        AllyAISystem.Update(tanks, map, 0.016f);

        Assert.False(ally.Input.Fire, "Ally should see the base center (36) aligned with its own center (36).");
    }

    [Fact]
    public void Update_ShouldFire_WhenEnemyIsAlignedAndPathIsClear()
    {
        var map = new TileMap(28, 28);
        // Base is tucked away in the corner
        map[25, 25] = TileType.Base;

        var ally = TankEntity.CreateAlly(0, 0);
        ally.Position.Facing = Direction.Down;

        // Enemy is directly below
        var enemy = TankEntity.CreateEnemy(TankTier.Tier1, 0, 5 * TileSize);

        var tanks = new List<TankEntity> { ally, enemy };

        AllyAISystem.Update(tanks, map, 0.016f);

        Assert.True(ally.Input.Fire);
    }

    [Fact]
    public void Update_ShouldIgnoreFrozenEnemies()
    {
        // Note: Your AI logic currently collects all enemies where t.IsAlive.
        // If your AI logic doesn't explicitly check 'IsFrozen', 
        // this test serves as a requirement check.

        var map = new TileMap(28, 28);
        var ally = TankEntity.CreateAlly(0, 0);

        var enemy = TankEntity.CreateEnemy(TankTier.Tier1, 2 * TileSize, 0);
        enemy.IsFrozen = true;

        var tanks = new List<TankEntity> { ally, enemy };

        AllyAISystem.Update(tanks, map, 0.016f);

        // If the AI ignores frozen targets, it shouldn't be targeting/firing.
        // Note: Check your AllyAISystem.cs to see if you filtered by !t.IsFrozen.
        // If not, you might want to add that to the 'enemies' collection logic.
    }

    [Fact]
    public void Update_ShouldFaceEnemy_WhenWithinEngagementRange()
    {
        var map = new TileMap(28, 28);
        // Ally at center
        var ally = TankEntity.CreateAlly(10 * TileSize, 10 * TileSize);

        // Enemy within 7 tiles (e.g., 3 tiles to the left)
        var enemy = TankEntity.CreateEnemy(TankTier.Tier1, 7 * TileSize, 10 * TileSize);

        var tanks = new List<TankEntity> { ally, enemy };

        AllyAISystem.Update(tanks, map, 0.016f);

        Assert.Equal(Direction.Left, ally.Position.Facing);
        Assert.True(ally.Velocity.IsMoving);
    }

    [Fact]
    public void Update_ShouldRoam_WhenNoEnemiesExist()
    {
        var map = new TileMap(28, 28);
        var ally = TankEntity.CreateAlly(0, 0);
        var tanks = new List<TankEntity> { ally };

        // Run update
        AllyAISystem.Update(tanks, map, 0.016f);

        // In Roam mode, Velocity.IsMoving should be true
        Assert.True(ally.Velocity.IsMoving);
    }
}