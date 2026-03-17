using IronVault.Core.Engine.Entities;

namespace IronVault.Core.Engine;

/// <summary>
/// 10-wave scripted compositions for Defense mode.
/// Waves escalate in enemy count, tier distribution, and simultaneous cap.
/// Wave 3 / 5 / 7 / 9 / 10 grant an ally tank reward.
/// </summary>
public static class DefenseWaveScript
{
    public const int TotalWaves = 10;

    public static WaveScript ForWave(int wave) => wave switch
    {
        1  => new() { Wave = 1,  TotalEnemies = 15, MaxSimultaneous = 3, GrantsAlly = false, TierWeights = [80, 20,  0,  0] },
        2  => new() { Wave = 2,  TotalEnemies = 16, MaxSimultaneous = 3, GrantsAlly = false, TierWeights = [60, 35,  5,  0] },
        3  => new() { Wave = 3,  TotalEnemies = 18, MaxSimultaneous = 4, GrantsAlly = true,  TierWeights = [40, 45, 15,  0] },
        4  => new() { Wave = 4,  TotalEnemies = 18, MaxSimultaneous = 4, GrantsAlly = false, TierWeights = [25, 40, 30,  5] },
        5  => new() { Wave = 5,  TotalEnemies = 20, MaxSimultaneous = 5, GrantsAlly = true,  TierWeights = [15, 30, 40, 15] },
        6  => new() { Wave = 6,  TotalEnemies = 20, MaxSimultaneous = 5, GrantsAlly = false, TierWeights = [10, 20, 45, 25] },
        7  => new() { Wave = 7,  TotalEnemies = 22, MaxSimultaneous = 5, GrantsAlly = true,  TierWeights = [ 5, 15, 45, 35] },
        8  => new() { Wave = 8,  TotalEnemies = 22, MaxSimultaneous = 6, GrantsAlly = false, TierWeights = [ 5, 10, 40, 45] },
        9  => new() { Wave = 9,  TotalEnemies = 24, MaxSimultaneous = 6, GrantsAlly = true,  TierWeights = [ 0,  5, 35, 60] },
        10 => new() { Wave = 10, TotalEnemies = 25, MaxSimultaneous = 6, GrantsAlly = true,  TierWeights = [ 0,  0, 30, 70] }, // boss wave — all heavy
        _  => new() { Wave = wave, TotalEnemies = 25, MaxSimultaneous = 6, GrantsAlly = false, TierWeights = [0, 0, 20, 80] },
    };
}
