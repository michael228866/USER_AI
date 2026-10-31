namespace HomunculusManager.Models;

internal enum SettingKind { Toggle, Choice, Number }

internal sealed record SettingDefinition(
    string Key, string Name, string Description, string Section, SettingKind Kind,
    int Min, int Max, string ProfileGroup, string? Parent = null,
    string Unit = "", params string[] Choices)
{
    internal bool IsBoolean => Key is "DanceEnabled" or "DanceAttack" or "UseHopDance";
    internal bool AppliesTo(string type) => (type is "eira" or "bayeri" or "sera" or "dieter" or "eleanor") ? Key is not ("PlayerASPD" or "DanceAttack") : (Key != "ASPD" &&
        (Key is not ("DanceEnabled" or "DanceAttack" or "SkillThrottleMs")
        || type is "lif" or "Vanilmirth"));
    internal bool InLegacyProfile => LegacyProfileKeys.Contains(Key);
    internal int Scale => Key == "DancePhase" ? 100 : 1;
    // Lua keeps milliseconds; fields shown in seconds convert on display and input.
    internal int UiScale => Scale * (Unit == "秒" ? 1000 : 1);
    private static readonly HashSet<string> LegacyProfileKeys = ["AttackMonstersEnabled", "CooperativeMode", "ContinuousAttackMode", "AutoCounterAttack", "FollowMode", "SentryMode", "players", "friends", "homunculus", "plants", "emperium", "priorityTargets", "smartAttackOrder", "RadiusAggr", "FollowDis", "SentryRadius", "PlayerASPD", "AttackTimeoutMs", "AltT_ToggleMode", "SkillMinSP"];
    internal string DisplayValue(int value) => Kind == SettingKind.Number
        ? $"{value / (double)UiScale:0.###}{(Unit.Length == 0 ? "" : " " + Unit)}"
        : value >= 0 && value < Choices.Length ? Choices[value] : value.ToString();
}

internal static class SettingsCatalog
{
    internal static readonly (string Key, string Name, string ShortName)[] Types =
    [
        ("lif", "麗芙(少女)", "麗芙(少女)"),
        ("amistr", "阿密斯特爾(綿羊)", "阿密斯特爾(綿羊)"),
        ("filir", "飛里樂(小鳥)", "飛里樂(小鳥)"),
        ("Vanilmirth", "巴尼米樂斯(果凍)", "巴尼米樂斯(果凍)"),
        ("eira", "艾拉(妖精)", "艾拉(妖精)"),
        ("bayeri", "巴耶里(獨角獸)", "巴耶里(獨角獸)"),
        ("sera", "莎拉(蜜蜂)", "莎拉(蜜蜂)"),
        ("dieter", "迪特爾(燒瓶)", "迪特爾(燒瓶)"),
        ("eleanor", "艾蕾諾亞(野貓)", "艾蕾諾亞(野貓)")
    ];

    internal static readonly SettingDefinition[] All =
    [
        new("AttackMonstersEnabled", "攻擊一般怪物", "關閉後不主動打怪，只會反擊或打主人指定的目標。", "戰鬥行為", SettingKind.Toggle, 0, 1, "exclude", Choices: ["關閉", "允許"]),
        new("CooperativeMode", "協同作戰模式", "決定生命體如何配合主人選擇攻擊目標。", "戰鬥行為", SettingKind.Choice, 0, 3, "combat", Choices: ["被動", "主動", "鎖定主人目標", "內掛鎖定"]),
        new("ContinuousAttackMode", "連續戰鬥模式", "標準：離主人超過 9 格就返回，打完先回主人；連續：超過 13 格才返回；激進：不強制返回。", "戰鬥行為", SettingKind.Choice, 0, 2, "combat", Choices: ["標準", "連續戰鬥", "激進"]),
        new("AutoCounterAttack", "自動反擊", "怪物或玩家攻擊主人或生命體時反擊，不受排除與協同模式限制。", "戰鬥行為", SettingKind.Toggle, 0, 1, "combat", Choices: ["關閉", "開啟"]),
        new("FollowMode", "跟隨模式", "0 一般跟隨；1 站在主人腳下，不追擊，只打已在攻擊範圍內或主人身邊 3 格內的怪。", "移動與跟隨", SettingKind.Choice, 0, 2, "combat", Choices: ["一般跟隨", "站在主人腳下", "站在主人腳下"]),
        new("SentryMode", "哨戒模式", "開啟後 Alt+右鍵點地面設為哨兵點，守在那裡攻擊哨戒半徑內的敵人；Alt+T 取消。", "戰鬥行為", SettingKind.Toggle, 0, 1, "combat", Choices: ["關閉", "開啟"]),

        new("players", "排除玩家", "1 代表不攻擊玩家；0 代表允許。", "攻擊目標與優先權", SettingKind.Toggle, 0, 1, "exclude", "AttackAllExclude", Choices: ["允許", "排除"]),
        new("friends", "排除好友", "避免把好友清單中的對象當作目標。", "攻擊目標與優先權", SettingKind.Toggle, 0, 1, "exclude", "AttackAllExclude", Choices: ["允許", "排除"]),
        new("homunculus", "排除敵方生命體", "是否排除敵方生命體。", "攻擊目標與優先權", SettingKind.Toggle, 0, 1, "exclude", "AttackAllExclude", Choices: ["允許", "排除"]),
        new("plants", "植物處理", "植物和菇類的攻擊規則。", "攻擊目標與優先權", SettingKind.Choice, 0, 2, "exclude", "AttackAllExclude", Choices: ["攻擊植物", "排除植物", "排除苗娃"]),
        new("emperium", "華麗金屬", "設定檔中的華麗金屬目標規則。", "攻擊目標與優先權", SettingKind.Choice, 0, 1, "exclude", "AttackAllExclude", Choices: ["不攻擊", "最高優先"]),
        new("priorityTargets", "副本特殊目標", "0 使用 HomCfg.lua 的 PriorityTargets 優先權重；1 關閉。", "攻擊目標與優先權", SettingKind.Choice, 0, 1, "exclude", "AttackAllExclude", Choices: ["使用優先權重", "關閉"]),
        new("smartAttackOrder", "智慧攻擊順序", "開啟時依 SmartAttackPriorityOrder 清單順序優先攻擊，越前面越優先。", "攻擊目標與優先權", SettingKind.Toggle, 0, 1, "exclude", "AttackAllExclude", Choices: ["關閉", "開啟"]),

        new("RadiusAggr", "索敵半徑", "搜尋目標的範圍。", "攻擊目標與優先權", SettingKind.Number, 0, 100, "parameters", Unit: "格"),
        new("FollowDis", "跟隨距離", "生命體與主人保持的距離。", "移動與跟隨", SettingKind.Number, 0, 30, "parameters", Unit: "格"),
        new("SentryRadius", "哨戒半徑", "哨兵點周圍的搜尋與追擊範圍。", "移動與跟隨", SettingKind.Number, 0, 100, "parameters", Unit: "格"),
        new("PlayerASPD", "攻速 ASPD", "舞步與攻擊節奏使用的攻速。", "移動與跟隨", SettingKind.Number, 1, 193, "parameters", Unit: "ASPD"),
        new("AttackTimeoutMs", "攻擊逾時", "攻擊卡住後放棄目標的時間；Lua 使用毫秒。", "戰鬥行為", SettingKind.Number, 0, 60000, "parameters", Unit: "毫秒"),
        new("AltT_ToggleMode", "Alt+T 切換模式", "連按兩下 Alt+T 時切換到的協同作戰模式；選「不切換」就停用這個快捷鍵。", "戰鬥行為", SettingKind.Choice, 0, 4, "parameters", Choices: ["被動", "主動", "鎖定主人目標", "內掛鎖定", "不切換"]),
        new("SkillMinSP", "技能最低 SP", "低於此 SP 時不自動施放技能；0 表示不限制。", "技能管理", SettingKind.Number, 0, 100000, "parameters", Unit: "SP")
        ,new("MoveDelay", "移動延遲", "兩次移動指令間的間隔。", "移動與跟隨", SettingKind.Number, 0, 60000, "parameters", Unit: "毫秒")
        ,new("AutoReturnDelay", "自動返回延遲", "Alt+右鍵移動到定點後，等多久自動回到主人身邊；0 表示不自動返回。", "移動與跟隨", SettingKind.Number, 0, 60000, "parameters", Unit: "毫秒")
        ,new("DanceMinSP", "走位最低 SP", "低於此 SP 時停止走位攻擊。", "移動與跟隨", SettingKind.Number, 0, 100000, "parameters", Unit: "SP")
        ,new("DanceActivationDelay", "走位啟動延遲", "載入後等待走位攻擊啟動。", "移動與跟隨", SettingKind.Number, 0, 60000, "parameters", Unit: "毫秒")
        ,new("DanceCombatDelay", "走位戰鬥延遲", "鎖定目標後等待走位攻擊。", "移動與跟隨", SettingKind.Number, 0, 60000, "parameters", Unit: "毫秒")
        ,new("MVPMode", "MVP 模式", "開啟時畫面上的 MVP 怪最優先攻擊並鎖定；MVP 種類清單在 HomCfg.lua 的 MVPTargets。", "戰鬥行為", SettingKind.Toggle, 0, 1, "combat", Choices: ["關閉", "開啟"])
        ,new("SkillMode", "技能使用方式", "1 正常；2 僅玩家；3 不用技能。", "技能管理", SettingKind.Choice, 1, 3, "combat", Choices: ["", "正常", "只對玩家", "停用"])
        ,new("AutoBlacklist", "自動黑名單", "玩家攻擊主人或生命體時自動加入黑名單（最多 10 筆）。", "黑白名單", SettingKind.Toggle, 0, 1, "combat", Choices: ["關閉", "開啟"])
        ,new("BlacklistPriority", "黑名單優先", "黑名單上的玩家或怪物種類優先攻擊。", "黑白名單", SettingKind.Toggle, 0, 1, "combat", Choices: ["關閉", "開啟"])
        ,new("BlacklistOnly", "僅攻擊黑名單", "只攻擊黑名單上的對象（反擊不受限）。", "黑白名單", SettingKind.Toggle, 0, 1, "combat", Choices: ["關閉", "開啟"])
        ,new("mercenary", "排除傭兵", "目標排除表中的傭兵值。", "攻擊目標與優先權", SettingKind.Toggle, 0, 1, "exclude", "AttackAllExclude", Choices: ["允許", "排除"])
        ,new("ALT_RightClick_Move", "Alt 右鍵移動", "控制 Alt 右鍵移動快捷鍵。", "進階設定", SettingKind.Toggle, 0, 1, "hotkeys", "HotkeySettings", Choices: ["關閉", "開啟"])
        ,new("ALT_RightClick_FirstPoint", "框選加朋友", "Alt+右鍵點第一個角、Alt+Shift+右鍵點第二個角，把範圍內的玩家加為朋友。", "進階設定", SettingKind.Toggle, 0, 1, "hotkeys", "HotkeySettings", Choices: ["關閉", "開啟"])
        ,new("ALT_SHIFT_FullView_FirstPoint", "全視野加朋友", "Alt+Shift+右鍵在地面連點兩次（5 秒內），把畫面內所有玩家加為朋友。", "進階設定", SettingKind.Toggle, 0, 1, "hotkeys", "HotkeySettings", Choices: ["關閉", "開啟"])
        ,new("ALT_SHIFT_SingleAdd", "單一加入好友", "控制加入單一好友快捷鍵。", "進階設定", SettingKind.Toggle, 0, 1, "hotkeys", "HotkeySettings", Choices: ["關閉", "開啟"])
        ,new("ALT_SHIFT_CounterAttack", "反擊快捷鍵", "控制切換反擊快捷鍵。", "進階設定", SettingKind.Toggle, 0, 1, "hotkeys", "HotkeySettings", Choices: ["關閉", "開啟"])
        ,new("StuckDetectionEnabled", "卡住偵測", "移動卡住時嘗試恢復。", "移動與跟隨", SettingKind.Toggle, 0, 1, "parameters", Choices: ["關閉", "開啟"])
        ,new("StuckTimeoutMs", "卡住逾時", "判定卡住的等待時間。", "移動與跟隨", SettingKind.Number, 0, 120000, "parameters", Unit: "毫秒")
        ,new("StuckEarlyWarningTime", "卡住預警", "提前嘗試修正移動。", "移動與跟隨", SettingKind.Number, 0, 120000, "parameters", Unit: "毫秒")
        ,new("StuckRecoveryAttemptInterval", "脫困間隔", "兩次脫困嘗試間的時間。", "移動與跟隨", SettingKind.Number, 0, 120000, "parameters", Unit: "毫秒")
        ,new("StuckRecoveryMode", "脫困模式", "0 隨機移動；1 返回主人。", "移動與跟隨", SettingKind.Choice, 0, 1, "parameters", Choices: ["隨機移動", "返回主人"])
        ,new("ActiveModeRefreshInterval", "主動刷新間隔", "主動與內掛模式重新搜尋目標的週期。", "進階設定", SettingKind.Number, 0, 120000, "parameters", Unit: "毫秒")
        ,new("SmartMoveEnabled", "智慧移動", "啟用繞路與卡點處理。", "移動與跟隨", SettingKind.Toggle, 0, 1, "parameters", Choices: ["關閉", "開啟"])
        ,new("SmartMoveStuckThreshold", "智慧移動卡住門檻", "移動位置未變的判定時間。", "移動與跟隨", SettingKind.Number, 0, 120000, "parameters", Unit: "毫秒")
        ,new("SmartMoveBypassDistance", "繞路距離", "首次繞路偏移格數。", "移動與跟隨", SettingKind.Number, 0, 100, "parameters", Unit: "格")
        ,new("SmartMoveMaxBypassDistance", "最大繞路距離", "多次卡住後的最大偏移。", "移動與跟隨", SettingKind.Number, 0, 100, "parameters", Unit: "格")
        ,new("SmartMoveTimeout", "智慧移動逾時", "繞路失敗後放棄目標的時間。", "移動與跟隨", SettingKind.Number, 0, 120000, "parameters", Unit: "毫秒")
        ,new("AttackTimeoutEnabled", "攻擊逾時偵測", "攻擊過久時放棄目標。", "戰鬥行為", SettingKind.Toggle, 0, 1, "parameters", Choices: ["關閉", "開啟"])
        ,new("TemporarySkipDuration", "暫時跳過目標", "放棄目標後重新鎖定前的等待時間。", "戰鬥行為", SettingKind.Number, 0, 120000, "parameters", Unit: "毫秒")
        ,new("DanceEnabled", "跳舞攻擊", "自行選擇是否跳舞；每種生命體獨立儲存。", "移動與跟隨", SettingKind.Toggle, 0, 1, "parameters", Choices: ["關閉", "開啟"])
        ,new("ASPD", "舞步 ASPD", "舞步節奏使用的攻速。", "移動與跟隨", SettingKind.Number, 0, 200, "parameters", Unit: "ASPD")
        ,new("DancePhase", "舞步比例（進階）", "輸入 0.25 代表 25%；無設定時預設 0.25。這不是已驗證的最佳值。", "移動與跟隨", SettingKind.Number, 1, 49, "parameters")
        ,new("DanceAttack", "水平抖動", "水平左右移動攻擊；Lua true/false。", "移動與跟隨", SettingKind.Toggle, 0, 1, "parameters", Choices: ["關閉", "開啟"])
        ,new("UseHopDance", "Hop 舞步", "靠近、普攻、回位；Lua true/false。", "移動與跟隨", SettingKind.Toggle, 0, 1, "parameters", Choices: ["關閉", "開啟"])
        ,new("SkillThrottleMs", "技能節流", "最短技能施放間隔。", "技能管理", SettingKind.Number, 0, 60000, "parameters", Unit: "秒")
    ];

    internal static SettingDefinition Find(string key) => All.Single(x => x.Key == key);
    internal static IEnumerable<SettingDefinition> ForType(string type) => All.Where(x => x.AppliesTo(type));
}
