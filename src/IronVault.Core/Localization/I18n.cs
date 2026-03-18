namespace IronVault.Core.Localization;

/// <summary>
/// Centralised bilingual string table (English / Chinese).
/// All UI text is resolved at runtime so language can be toggled without restart.
/// </summary>
public static class I18n
{
    private static Language _current = Language.English;

    public static Language Current
    {
        get => _current;
        set
        {
            if (_current == value) return;
            _current = value;
            LanguageChanged?.Invoke();
        }
    }

    /// <summary>Fired whenever the active language changes.</summary>
    public static event Action? LanguageChanged;

    /// <summary>Returns the localised string for <paramref name="key"/>.</summary>
    public static string T(string key)
        => _strings.TryGetValue(key, out var pair)
               ? (_current == Language.Chinese ? pair.zh : pair.en)
               : key;

    /// <summary>
    /// Returns the formatted level name for the current language.
    /// English: "STAGE 1", Chinese: "第一关"
    /// </summary>
    public static string FormatLevel(int level)
        => _current == Language.Chinese
               ? $"第{ToChineseNumber(level)}关"
               : $"STAGE {level}";

    private static string ToChineseNumber(int n)
    {
        string[] digits = ["〇", "一", "二", "三", "四", "五", "六", "七", "八", "九"];
        if (n <= 0)  return digits[0];
        if (n < 10)  return digits[n];
        if (n == 10) return "十";
        if (n < 20)  return $"十{digits[n % 10]}";
        if (n % 10 == 0) return $"{digits[n / 10]}十";
        return $"{digits[n / 10]}十{digits[n % 10]}";
    }

    // ── String table ─────────────────────────────────────────────────────────
    // Each entry: (English, Chinese)

    private static readonly Dictionary<string, (string en, string zh)> _strings = new()
    {
        // ── App / shared ────────────────────────────────────────────────────
        ["app.title"]         = ("IRON VAULT",                              "铁  窖  计  划"),
        ["app.subtitle"]      = ("铁  窖  计  划",                          "IRON VAULT"),
        ["app.tagline"]       = ("ARMOURED COMBAT SIMULATION · VECTOR ENGINE", "装甲战斗模拟 · 矢量渲染引擎"),
        ["app.footer"]        = ("© IRON VAULT SIM  ·  PURE VECTOR  ·  AOT", "© 铁窖计划 · 矢量渲染 · AOT"),

        // ── Menu ─────────────────────────────────────────────────────────────
        ["menu.mode"]         = ("GAME MODE",                               "游戏模式"),
        ["menu.classic"]      = ("CLASSIC",                                 "经典模式"),
        ["menu.defense"]      = ("DEFENSE",                                 "防守模式"),
        ["menu.classic.desc"] = ("Survive infinite waves — no victory, only glory",
                                 "无限波次 — 没有胜利，只有荣耀"),
        ["menu.defense.desc"] = ("Survive 10 scripted waves to achieve victory",
                                 "坚守10波进攻 — 全部击退即为胜利"),
        ["menu.difficulty"]   = ("TACTICAL RATING",                         "战术等级"),
        ["menu.deploy"]       = ("▶  DEPLOY FORCES  ◀",                    "▶  出击  ◀"),
        ["menu.abort"]        = ("ABORT MISSION",                           "放弃任务"),
        ["menu.lang"]         = ("LANGUAGE",                                "语言"),

        // ── Difficulty ───────────────────────────────────────────────────────
        ["diff.easy"]         = ("ROOKIE",                                  "新  兵"),
        ["diff.normal"]       = ("VETERAN",                                 "老  兵"),
        ["diff.hard"]         = ("ELITE",                                   "精  英"),
        ["diff.easy.desc"]    = ("Enemies roam randomly · slow fire rate · easy to dodge",
                                 "敌人随机移动 · 射速慢 · 容易躲避"),
        ["diff.normal.desc"]  = ("A* pathfinding · fires when target is aligned",
                                 "A*寻路 · 目标对准时开火"),
        ["diff.hard.desc"]    = ("Aggressive A* · bullet dodging · rapid fire",
                                 "激进AI · 躲避子弹 · 快速射击"),

        // ── HUD ──────────────────────────────────────────────────────────────
        ["hud.title"]         = ("IRON VAULT",                              "铁窖计划"),
        ["hud.status"]        = ("STATUS",                                  "状  态"),
        ["hud.mode"]          = ("MODE",                                    "模式"),
        ["hud.wave"]          = ("WAVE",                                    "波次"),
        ["hud.score"]         = ("SCORE",                                   "得分"),
        ["hud.lives"]         = ("LIVES",                                   "生命"),
        ["hud.enemies"]       = ("ENEMIES",                                 "剩余"),
        ["hud.armor"]         = ("ARMOR",                                   "装甲"),
        ["hud.controls"]      = ("CONTROLS",                                "操作"),
        ["hud.ctrl.move"]     = ("[W/A/S/D]  MOVE",                        "[W/A/S/D]  移动"),
        ["hud.ctrl.fire"]     = ("[SPACE]    FIRE",                        "[空格]     开火"),
        ["hud.ctrl.pause"]    = ("[P]        PAUSE",                       "[P]        暂停"),
        ["hud.ctrl.start"]    = ("[ENTER]    START",                       "[回车]     开始"),
        ["btn.deploy"]        = ("[DEPLOY]",                                "[出击]"),
        ["btn.redeploy"]      = ("[REDEPLOY]",                              "[再战]"),
        ["btn.hold"]          = ("[HOLD]",                                  "[暂停]"),
        ["btn.retreat"]       = ("[RETREAT]",                               "[撤退]"),

        // ── Upgrade screen ────────────────────────────────────────────────────
        ["upg.debrief"]       = ("——  TACTICAL DEBRIEF  ——",               "——  战场总结  ——"),
        ["upg.score"]         = ("SCORE",                                   "得分"),
        ["upg.next_wave"]     = ("NEXT WAVE",                               "下一波"),
        ["upg.select_hdr"]    = ("◆  SELECT FIELD UPGRADE  ◆",             "◆  选择战场升级  ◆"),
        ["upg.select"]        = ("[ SELECT ]",                              "[ 选择 ]"),
        ["upg.skip"]          = ("▷  SKIP — DEPLOY NEXT WAVE  ◁",          "▷  跳过 — 进入下一波  ◁"),
        ["upg.ally"]          = ("◈  ALLY TANK DEPLOYED TO YOUR POSITION", "◈  友军坦克已部署至你的位置"),

        // ── Upgrade names/descriptions ─────────────────────────────────────
        ["upg.armor.name"]    = ("ARMOR PLATING",                           "装甲强化"),
        ["upg.armor.desc"]    = ("+1 MAX HP  ·  PARTIAL HEAL",             "+1最大生命值 · 部分回复"),
        ["upg.nitro.name"]    = ("NITRO BOOSTERS",                          "氮气助推"),
        ["upg.nitro.desc"]    = ("+15% MOVE SPEED",                        "+15% 移动速度"),
        ["upg.fire.name"]     = ("RAPID FIRE SYS",                          "速射系统"),
        ["upg.fire.desc"]     = ("FIRE COOLDOWN  −20%",                    "开火冷却 −20%"),
        ["upg.dual.name"]     = ("DUAL CANNON",                             "双联炮"),
        ["upg.dual.desc"]     = ("+1 SIMULTANEOUS SHELL",                  "+1 同时炮弹数"),
        ["upg.ap.name"]       = ("ARMOUR-PIERCING",                         "穿甲弹"),
        ["upg.ap.desc"]       = ("ROUNDS PIERCE STEEL WALLS",              "子弹可穿透钢墙"),
        ["upg.repair.name"]   = ("FIELD REPAIR KIT",                        "野战修理包"),
        ["upg.repair.desc"]   = ("RESTORE FULL HULL INTEGRITY",            "完全修复战车装甲"),

        // ── HUD extras ───────────────────────────────────────────────────────
        ["hud.effects"]       = ("EFFECTS",                                 "道具效果"),

        // ── Active power-up labels (shown with countdown timer) ───────────────
        ["pu.star"]           = ("★ SHIELD",                               "★ 无敌"),
        ["pu.clock"]          = ("⏸ FREEZE",                               "⏸ 冰冻"),
        ["pu.shovel"]         = ("⬛ FORTRESS",                             "⬛ 要塞"),
        ["pu.boost"]          = ("▲ OVERLOAD",                             "▲ 超速"),

        // ── Game canvas overlays ─────────────────────────────────────────────
        ["game.over"]         = ("GAME OVER",                               "游戏结束"),
        ["game.victory"]      = ("VICTORY!",                                "胜  利！"),
        ["game.paused"]       = ("PAUSED",                                  "已暂停"),
        ["game.title"]        = ("IRON VAULT",                              "铁窖计划"),

        // ── In-game overlays ─────────────────────────────────────────────────
        ["overlay.settings"]     = ("SETTINGS",                             "设  置"),
        ["overlay.resume"]       = ("▶  RESUME",                           "▶  继续"),
        ["overlay.exit.title"]   = ("ABORT MISSION?",                       "中止任务？"),
        ["overlay.exit.menu"]    = ("RETURN TO MENU",                       "返回菜单"),
        ["overlay.exit.quit"]    = ("QUIT GAME",                            "退出游戏"),
        ["overlay.exit.cancel"]  = ("CANCEL",                               "取消"),

        // ── Level selection ───────────────────────────────────────────────────
        ["level.stage"]       = ("STAGE",                                   "第"),
        ["level.suffix"]      = ("",                                        "关"),
        ["level.select"]      = ("SELECT STAGE",                            "选择关卡"),
        ["level.announce"]    = ("STAGE",                                   "第"),
        ["level.back"]        = ("◀  BACK",                                 "◀  返回"),
        ["level.locked"]      = ("LOCKED",                                  "未解锁"),
    };
}
