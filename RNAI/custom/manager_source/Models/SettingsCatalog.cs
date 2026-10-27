namespace HomunculusManager.Models;

internal enum SettingKind { Toggle, Choice, Number }

internal sealed record SettingDefinition(
    string Key, string Name, string Description, string Section, SettingKind Kind,
    int Min, int Max, string ProfileGroup, string? Parent = null,
    string Unit = "", params string[] Choices)
{
    internal string DisplayValue(int value) => Kind == SettingKind.Number
        ? $"{value:N0}{(Unit.Length == 0 ? "" : " " + Unit)}"
        : value >= 0 && value < Choices.Length ? Choices[value] : value.ToString();
}

internal static class SettingsCatalog
{
    internal static readonly (string Key, string Name, string ShortName)[] Types =
    [
        ("lif", "麗芙 Lif", "麗芙"),
        ("amistr", "阿密斯特爾 Amistr", "阿密斯特爾"),
        ("filir", "飛里樂 Filir", "飛里樂"),
        ("Vanilmirth", "巴尼米樂斯 Vanilmirth", "巴尼米樂斯")
    ];

    internal static readonly SettingDefinition[] All =
    [
        new("AttackMonstersEnabled", "攻擊一般怪物", "是否把一般怪物列入攻擊目標。", "戰鬥行為", SettingKind.Toggle, 0, 1, "exclude", Choices: ["關閉", "允許"]),
        new("CooperativeMode", "協同作戰模式", "決定生命體如何配合主人選擇攻擊目標。", "戰鬥行為", SettingKind.Choice, 0, 3, "combat", Choices: ["被動", "主動", "鎖定主人目標", "內掛鎖定"]),
        new("ContinuousAttackMode", "連續戰鬥模式", "控制擊殺後是否繼續搜尋，以及返回主人的距離。", "戰鬥行為", SettingKind.Choice, 0, 2, "combat", Choices: ["標準", "連續戰鬥", "激進"]),
        new("AutoCounterAttack", "自動反擊", "受到攻擊時是否反擊。", "戰鬥行為", SettingKind.Toggle, 0, 1, "combat", Choices: ["關閉", "開啟"]),
        new("DanceAttackMode", "走位攻擊", "設定檔的走位攻擊開關。", "戰鬥行為", SettingKind.Toggle, 0, 1, "combat", Choices: ["關閉", "開啟"]),
        new("FollowMode", "跟隨模式", "原 Lua 未說明各數值的行為；保留原始數值。", "戰鬥行為", SettingKind.Choice, 0, 2, "combat", Choices: ["模式 0", "模式 1", "模式 2"]),
        new("SentryMode", "哨戒模式", "是否使用哨戒點周圍的搜尋行為。", "戰鬥行為", SettingKind.Toggle, 0, 1, "combat", Choices: ["關閉", "開啟"]),

        new("players", "排除玩家", "1 代表不攻擊玩家；0 代表允許。", "目標過濾", SettingKind.Toggle, 0, 1, "exclude", "AttackAllExclude", Choices: ["允許", "排除"]),
        new("friends", "排除好友", "避免把好友清單中的對象當作目標。", "目標過濾", SettingKind.Toggle, 0, 1, "exclude", "AttackAllExclude", Choices: ["允許", "排除"]),
        new("homunculus", "排除敵方生命體", "是否排除敵方生命體。", "目標過濾", SettingKind.Toggle, 0, 1, "exclude", "AttackAllExclude", Choices: ["允許", "排除"]),
        new("plants", "植物處理", "植物和菇類的攻擊規則。", "目標過濾", SettingKind.Choice, 0, 2, "exclude", "AttackAllExclude", Choices: ["攻擊植物", "排除植物", "排除苗娃"]),
        new("emperium", "華麗金屬", "設定檔中的華麗金屬目標規則。", "目標過濾", SettingKind.Choice, 0, 1, "exclude", "AttackAllExclude", Choices: ["不攻擊", "最高優先"]),
        new("priorityTargets", "副本特殊目標", "原 Lua 對 0/1 的註解互相矛盾，僅顯示原始值。", "目標過濾", SettingKind.Choice, 0, 1, "exclude", "AttackAllExclude", Choices: ["原值 0", "原值 1"]),
        new("smartAttackOrder", "智慧攻擊順序", "主動模式的目標排序設定。", "目標過濾", SettingKind.Toggle, 0, 1, "exclude", "AttackAllExclude", Choices: ["關閉", "開啟"]),

        new("RadiusAggr", "索敵半徑", "搜尋目標的範圍。", "距離與時間", SettingKind.Number, 0, 100, "parameters", Unit: "格"),
        new("FollowDis", "跟隨距離", "生命體與主人保持的距離。", "距離與時間", SettingKind.Number, 0, 30, "parameters", Unit: "格"),
        new("SentryRadius", "哨戒半徑", "哨戒點周圍的搜尋範圍。", "距離與時間", SettingKind.Number, 0, 100, "parameters", Unit: "格"),
        new("PlayerASPD", "角色攻速", "設定檔用於走位節奏的角色 ASPD。", "距離與時間", SettingKind.Number, 0, 200, "parameters", Unit: "ASPD"),
        new("AttackTimeoutMs", "攻擊逾時", "攻擊卡住後放棄目標的時間；Lua 使用毫秒。", "距離與時間", SettingKind.Number, 0, 60000, "parameters", Unit: "毫秒"),
        new("AltT_ToggleMode", "Alt+T 切換模式", "按快捷鍵後切換到的協同作戰模式。", "距離與時間", SettingKind.Choice, 0, 3, "parameters", Choices: ["被動", "主動", "鎖定主人目標", "內掛鎖定"]),
        new("SkillMinSP", "技能最低 SP", "低於此 SP 時不自動施放技能；0 表示不限制。", "技能管理", SettingKind.Number, 0, 100000, "parameters", Unit: "SP")
    ];

    internal static SettingDefinition Find(string key) => All.Single(x => x.Key == key);
}
