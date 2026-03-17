using IronVault.Core.Localization;

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

/// <summary>Display metadata for one upgrade choice (icon + localised name + description).</summary>
public sealed record UpgradeInfo(string Name, string Desc, string Icon);

/// <summary>Returns localised display info for each <see cref="UpgradeType"/>.</summary>
public static class UpgradeDescriptions
{
    public static UpgradeInfo For(UpgradeType t) => t switch
    {
        UpgradeType.ArmorPlating    => new(I18n.T("upg.armor.name"),  I18n.T("upg.armor.desc"),  "◈"),
        UpgradeType.NitroBoosters   => new(I18n.T("upg.nitro.name"),  I18n.T("upg.nitro.desc"),  "▲"),
        UpgradeType.RapidFireSystem => new(I18n.T("upg.fire.name"),   I18n.T("upg.fire.desc"),   "◎"),
        UpgradeType.DualCannon      => new(I18n.T("upg.dual.name"),   I18n.T("upg.dual.desc"),   "╪"),
        UpgradeType.ArmourPiercing  => new(I18n.T("upg.ap.name"),     I18n.T("upg.ap.desc"),     "◆"),
        UpgradeType.RepairKit       => new(I18n.T("upg.repair.name"), I18n.T("upg.repair.desc"), "✚"),
        _                           => new("UNKNOWN",                  "",                         "?"),
    };
}
