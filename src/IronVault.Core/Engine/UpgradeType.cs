namespace IronVault.Core.Engine;

/// <summary>All available between-wave player upgrades.</summary>
public enum UpgradeType
{
    ArmorPlating,       // +1 max HP, partial heal
    NitroBoosters,      // move speed × 1.15
    RapidFireSystem,    // fire cooldown × 0.80
    DualCannon,         // +1 simultaneous bullet
    ArmourPiercing,     // bullet power → 2 (pierces steel)
    RepairKit,          // restore full HP (no stat bonus)
}

/// <summary>Display metadata for one upgrade choice.</summary>
public sealed record UpgradeInfo(string Name, string Desc, string Icon);

/// <summary>Returns display info for each <see cref="UpgradeType"/>.</summary>
public static class UpgradeDescriptions
{
    public static UpgradeInfo For(UpgradeType t) => t switch
    {
        UpgradeType.ArmorPlating    => new("ARMOR PLATING",    "+1 MAX HP  ·  PARTIAL HEAL",       "◈"),
        UpgradeType.NitroBoosters   => new("NITRO BOOSTERS",   "+15% MOVE SPEED",                  "▲"),
        UpgradeType.RapidFireSystem => new("RAPID FIRE SYS",   "FIRE COOLDOWN  −20%",              "◎"),
        UpgradeType.DualCannon      => new("DUAL CANNON",      "+1 SIMULTANEOUS SHELL",            "╪"),
        UpgradeType.ArmourPiercing  => new("ARMOUR-PIERCING",  "ROUNDS PIERCE STEEL WALLS",        "◆"),
        UpgradeType.RepairKit       => new("FIELD REPAIR KIT", "RESTORE FULL HULL INTEGRITY",      "✚"),
        _                           => new("UNKNOWN",          "",                                  "?"),
    };
}
