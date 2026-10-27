using System.Text.RegularExpressions;

namespace HomunculusManager.Services;

internal sealed record SkillEntry(int Id, int Level, int Delay, int Index);

internal static class SkillService
{
    internal static bool IsHomunculusSkill(int id) => id == 0 || id is < 8201 or > 8240;

    internal static int MaxLevel(int id) => id switch
    {
        8027 => 1,
        8018 or 8020 or 8022 or 8023 or 8026 or 8028 or 8032 or 8033 or 8035
            or 8036 or 8037 or 8038 or 8039 or 8040 or 8043 => 5,
        _ => 10
    };

    internal static IReadOnlyList<SkillEntry> Read(string text, string type)
    {
        var entries = Ranges(text, type);
        return entries.Select((range, index) => new SkillEntry(
            Required(text, range, "id"), Required(text, range, "lv"), Required(text, range, "delay"), index)).ToArray();
    }

    internal static void SaveDelay(string path, string type, int index, int id, int level, int delay)
    {
        if (delay is < 0 or > 600000) throw new ArgumentOutOfRangeException(nameof(delay));
        var original = LuaConfigService.Open(path);
        var ranges = Ranges(original.Text, type);
        if (index < 0 || index >= ranges.Count || Required(original.Text, ranges[index], "id") != id ||
            Required(original.Text, ranges[index], "lv") != level)
            throw new IOException("技能順序或內容已改變，請重新載入。 ");
        var field = FindField(original.Text, ranges[index], "delay");
        if (field.Value == delay) return;
        var updated = original.Text[..field.Start] + delay + original.Text[(field.Start + field.Length)..];
        ListService.AtomicWrite(path, original, updated);
    }

    internal static void SaveLevel(string path, string type, int index, int id, int currentLevel, int newLevel)
    {
        if (id <= 0) throw new ArgumentException("普通攻擊沒有可設定的技能等級。", nameof(id));
        if (newLevel < 1 || newLevel > MaxLevel(id))
            throw new ArgumentOutOfRangeException(nameof(newLevel), $"等級需介於 1–{MaxLevel(id)}。");
        var original = LuaConfigService.Open(path);
        var ranges = Ranges(original.Text, type);
        if (index < 0 || index >= ranges.Count || Required(original.Text, ranges[index], "id") != id ||
            Required(original.Text, ranges[index], "lv") != currentLevel)
            throw new IOException("技能順序或內容已改變，請重新載入。");
        var field = FindField(original.Text, ranges[index], "lv");
        if (field.Value == newLevel) return;
        var skillConfig = LuaConfigService.ExtractBlock(original.Text, type, "SkillConfig");
        if (Regex.IsMatch(skillConfig, @"\bSkill_" + id + "_lv" + currentLevel + @"\s*="))
            throw new InvalidOperationException("此技能有綁定目前等級的 SkillConfig；請先檢查該等級專用設定，避免變更後失效。");
        var updated = original.Text[..field.Start] + newLevel + original.Text[(field.Start + field.Length)..];
        ListService.AtomicWrite(path, original, updated);
    }

    internal static bool? ReadAutoEnabled(string text, string type, int id, int level)
        => FindAutoEnabled(text, type, id, level)?.Enabled;

    internal static void SaveAutoEnabled(string path, string type, int id, int level,
        bool expected, bool enabled)
    {
        if (id <= 0 || level <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        var original = LuaConfigService.Open(path);
        var field = FindAutoEnabled(original.Text, type, id, level)
            ?? throw new FormatException($"技能 {id} 沒有可編輯的自動施放開關。");
        if (field.Enabled != expected) throw new IOException("技能開關已被其他程式修改，請重新載入。");
        if (expected == enabled) return;
        var updated = original.Text[..field.Start] + (enabled ? "true" : "false")
            + original.Text[(field.Start + field.Length)..];
        ListService.AtomicWrite(path, original, updated);
    }

    private static (int Start, int Length, bool Enabled)? FindAutoEnabled(string text, string type, int id, int level)
    {
        if (id <= 0 || level <= 0) return null;
        var root = LuaConfigService.FindBlock(text, "HomConfig", 0, text.Length);
        var hom = LuaConfigService.FindBlock(text, type, root.Open + 1, root.Close);
        var config = LuaConfigService.FindBlock(text, "SkillConfig", hom.Open + 1, hom.Close);
        foreach (var key in new[] { $"Skill_{id}_lv{level}", $"Skill_{id}" })
        {
            var pattern = @"(?m)^[ \t]*" + Regex.Escape(key) + @"[ \t]*=[ \t]*\{";
            var entries = Regex.Matches(text[(config.Open + 1)..config.Close], pattern).Cast<Match>()
                .Where(m => LuaConfigService.GetBraceDepth(text, config.Open + 1, config.Open + 1 + m.Index) == 0)
                .ToArray();
            if (entries.Length == 0) continue;
            if (entries.Length != 1) throw new FormatException($"技能設定 {key} 不唯一。");
            var block = LuaConfigService.FindBlock(text, key, config.Open + 1, config.Close);
            var start = block.Open + 1;
            var toggles = Regex.Matches(text[start..block.Close], @"\benabled\s*=\s*(?<value>true|false)\b").Cast<Match>()
                .Where(m => LuaConfigService.GetBraceDepth(text, start, start + m.Index) == 0)
                .ToArray();
            if (toggles.Length != 1) throw new FormatException($"技能設定 {key} 的 enabled 不唯一或不存在。");
            var value = toggles[0].Groups["value"];
            return (start + value.Index, value.Length, value.Value == "true");
        }
        return null;
    }

    private static List<(int Start, int End)> Ranges(string text, string type)
    {
        var root = LuaConfigService.FindBlock(text, "HomConfig", 0, text.Length);
        var hom = LuaConfigService.FindBlock(text, type, root.Open + 1, root.Close);
        var block = LuaConfigService.FindBlock(text, "Skill", hom.Open + 1, hom.Close);
        var result = new List<(int, int)>();
        for (var i = block.Open + 1; i < block.Close; i++)
        {
            if (text[i] != '{' || LuaConfigService.GetBraceDepth(text, block.Open + 1, i) != 0) continue;
            var end = LuaConfigService.FindMatchingBrace(text, i, block.Close);
            result.Add((i + 1, end));
            i = end;
        }
        return result;
    }

    private static int Required(string text, (int Start, int End) range, string key)
        => FindField(text, range, key).Value;

    private static (int Start, int Length, int Value) FindField(string text, (int Start, int End) range, string key)
    {
        var pattern = @"\b" + Regex.Escape(key) + @"\s*=\s*(?<value>\d+)\b";
        var matches = Regex.Matches(text[range.Start..range.End], pattern).Cast<Match>()
            .Where(x => LuaConfigService.GetBraceDepth(text, range.Start, range.Start + x.Index) == 0).ToArray();
        if (matches.Length != 1) throw new FormatException($"技能欄位 {key} 不唯一或不存在");
        var group = matches[0].Groups["value"];
        return (range.Start + group.Index, group.Length, int.Parse(group.Value));
    }
}
