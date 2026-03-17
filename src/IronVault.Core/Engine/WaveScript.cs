using IronVault.Core.Engine.Entities;

namespace IronVault.Core.Engine;

/// <summary>
/// Defines the enemy composition and rewards for a single wave.
/// Higher waves contain more Tier3/4 enemies and may grant ally tanks.
/// </summary>
public sealed class WaveScript
{
    public int   Wave             { get; init; }
    public int   TotalEnemies     { get; init; }
    public int   MaxSimultaneous  { get; init; }
    /// <summary>Whether the player receives an ally tank after clearing this wave.</summary>
    public bool  GrantsAlly       { get; init; }

    /// <summary>
    /// Relative spawn probability per tier index (index 0 = Tier1 … index 3 = Tier4).
    /// Values need not sum to any specific total.
    /// </summary>
    public int[] TierWeights      { get; init; } = [100, 0, 0, 0];

    // ── Factory ──────────────────────────────────────────────────────────────

    public static WaveScript ForWave(int wave) => wave switch
    {
        1 => new() { Wave = 1,  TotalEnemies = 20, MaxSimultaneous = 4, GrantsAlly = false, TierWeights = [70, 30,  0,  0] },
        2 => new() { Wave = 2,  TotalEnemies = 22, MaxSimultaneous = 4, GrantsAlly = false, TierWeights = [50, 40, 10,  0] },
        3 => new() { Wave = 3,  TotalEnemies = 24, MaxSimultaneous = 5, GrantsAlly = true,  TierWeights = [30, 40, 20, 10] },
        4 => new() { Wave = 4,  TotalEnemies = 26, MaxSimultaneous = 5, GrantsAlly = false, TierWeights = [20, 30, 30, 20] },
        5 => new() { Wave = 5,  TotalEnemies = 28, MaxSimultaneous = 6, GrantsAlly = true,  TierWeights = [10, 20, 40, 30] },
        6 => new() { Wave = 6,  TotalEnemies = 30, MaxSimultaneous = 6, GrantsAlly = false, TierWeights = [ 5, 15, 45, 35] },
        _ => new()
        {
            Wave            = wave,
            TotalEnemies    = 20 + wave * 2,
            MaxSimultaneous = Math.Min(8, 4 + wave / 2),
            GrantsAlly      = wave % 3 == 0,
            TierWeights     = [ 5, 10, 40, 45],
        }
    };

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Returns a random tier sampled from <see cref="TierWeights"/>.</summary>
    public TankTier RollTier(Random rng)
    {
        int total = 0;
        foreach (var w in TierWeights) total += w;

        int roll = rng.Next(total);
        int acc  = 0;
        for (int i = 0; i < TierWeights.Length; i++)
        {
            acc += TierWeights[i];
            if (roll < acc) return (TankTier)(i + 1);
        }
        return TankTier.Tier1;
    }
}
