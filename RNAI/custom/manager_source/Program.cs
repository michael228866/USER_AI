using System.Text;
using System.Text.RegularExpressions;

namespace HomunculusManager;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--self-test")
        {
            ConfigEditor.SelfTest();
            Console.WriteLine("PASS");
            return;
        }
        if (args.Length == 2 && args[0] == "--check")
        {
            try
            {
                ConfigEditor.CheckFile(args[1]);
                Console.WriteLine("PASS");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.ExitCode = 1;
            }
            return;
        }
        if (args.Length == 2 && args[0] == "--preview")
        {
            ApplicationConfiguration.Initialize();
            using var form = new MainForm();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-10000, -10000);
            form.Show();
            Application.DoEvents();
            form.PerformLayout();
            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(args[1]);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed record Setting(string Key, string Label, int Min, int Max, string Description, string? Parent = null);

internal static class Settings
{
    internal static readonly (string Key, string Label)[] Types =
    [
        ("lif", "麗芙（補師）"),
        ("amistr", "阿密斯特爾（羊）"),
        ("filir", "飛里樂（鳥）"),
        ("Vanilmirth", "巴尼米樂斯（果凍）")
    ];

    internal static readonly Setting[] Fields =
    [
        new("AttackMonstersEnabled", "攻擊一般怪物", 0, 1, "0 不攻擊；1 允許攻擊一般怪物。"),
        new("CooperativeMode", "協同作戰模式", 0, 3, "0 被動；1 主動；2 鎖定主人目標；3 內掛鎖定。"),
        new("ContinuousAttackMode", "連續戰鬥模式", 0, 2, "0 標準；1 連續搜尋；2 激進搜尋。"),
        new("AutoCounterAttack", "自動反擊", 0, 1, "0 關閉；1 被攻擊時反擊。"),
        new("DanceAttackMode", "走位攻擊模式", 0, 1, "0 關閉；1 啟用走位攻擊。"),
        new("FollowMode", "跟隨模式", 0, 2, "控制生命體的跟隨行為；目前設定通常為 0。"),
        new("SentryMode", "哨戒模式", 0, 1, "0 關閉；1 在哨戒點周圍尋找敵人。"),
        new("players", "排除玩家", 0, 1, "0 可攻擊玩家；1 不攻擊玩家。", "AttackAllExclude"),
        new("friends", "排除好友", 0, 1, "0 不排除好友；1 不攻擊好友。", "AttackAllExclude"),
        new("homunculus", "排除生命體", 0, 1, "0 可攻擊敵方生命體；1 排除。", "AttackAllExclude"),
        new("plants", "植物處理模式", 0, 2, "0 攻擊植物；1 排除；2 排除苗娃。", "AttackAllExclude"),
        new("emperium", "華麗金屬處理模式", 0, 1, "0 不攻擊；1 最高優先攻擊華麗金屬。", "AttackAllExclude"),
        new("priorityTargets", "優先目標", 0, 1, "0 優先攻擊副本特殊怪物；1 關閉。", "AttackAllExclude"),
        new("smartAttackOrder", "智慧攻擊順序", 0, 1, "0 關閉；1 啟用主動模式的智能排序。", "AttackAllExclude"),
        new("RadiusAggr", "索敵半徑", 0, 100, "搜尋敵人的範圍，單位為格。"),
        new("FollowDis", "跟隨距離", 0, 30, "生命體與主人保持的距離，單位為格。"),
        new("SentryRadius", "哨戒半徑", 0, 100, "哨戒點周圍的搜尋範圍，單位為格。"),
        new("PlayerASPD", "角色攻速", 0, 200, "用於計算走位攻擊節奏的 ASPD。"),
        new("SkillMinSP", "技能最低 SP", 0, 100000, "低於此 SP 時不自動施放技能；0 不限制。"),
        new("AttackTimeoutMs", "攻擊逾時", 0, 60000, "攻擊卡住後放棄目標的時間，單位為毫秒。"),
        new("AltT_ToggleMode", "Alt+T 切換模式", 0, 3, "0 被動；1 主動；2 鎖定主人目標；3 內掛鎖定。")
    ];
}

internal static class ConfigEditor
{
    private static readonly Regex Assignment = new(@"(?m)^(?<prefix>[ \t]*)(?<key>[A-Za-z_][A-Za-z_0-9]*)[ \t]*=[ \t]*(?<value>\d+)(?<suffix>[ \t]*,?[ \t]*(?:--[^\r\n]*)?)\r?$", RegexOptions.Compiled);

    internal static Dictionary<string, int> Read(string source, string type)
    {
        var result = new Dictionary<string, int>();
        foreach (var setting in Settings.Fields)
        {
            var range = FieldRange(source, type, setting);
            var matches = Assignment.Matches(source[range.Start..range.End]);
            var match = matches.Cast<Match>().SingleOrDefault(m => m.Groups["key"].Value == setting.Key)
                ?? throw new FormatException($"找不到唯一的設定：{type}.{setting.Key}");
            result[setting.Key] = int.Parse(match.Groups["value"].Value);
        }
        return result;
    }

    internal static string Update(string source, string type, IReadOnlyDictionary<string, int> values)
    {
        foreach (var setting in Settings.Fields)
        {
            if (!values.TryGetValue(setting.Key, out var value) || value < setting.Min || value > setting.Max)
                throw new ArgumentOutOfRangeException(setting.Key, $"{setting.Label} 超出範圍");
            var range = FieldRange(source, type, setting);
            var segment = source[range.Start..range.End];
            var count = 0;
            var changed = Assignment.Replace(segment, match =>
            {
                if (match.Groups["key"].Value != setting.Key) return match.Value;
                count++;
                var number = match.Groups["value"];
                var offset = number.Index - match.Index;
                return match.Value[..offset] + value + match.Value[(offset + number.Length)..];
            });
            if (count != 1) throw new FormatException($"找不到唯一的設定：{type}.{setting.Key}");
            source = source[..range.Start] + changed + source[range.End..];
        }
        return source;
    }

    private static (int Start, int End) FieldRange(string source, string type, Setting setting)
    {
        var outer = FindBlock(source, "HomConfig", 0, source.Length);
        var hom = FindBlock(source, type, outer.Start, outer.End);
        return setting.Parent is null ? hom : FindBlock(source, setting.Parent, hom.Start, hom.End);
    }

    private static (int Start, int End) FindBlock(string source, string key, int start, int end)
    {
        var match = Regex.Match(source[start..end], @"(?m)^\s*" + Regex.Escape(key) + @"\s*=\s*\{");
        if (!match.Success) throw new FormatException($"找不到設定區塊：{key}");
        var open = start + match.Index + match.Length - 1;
        var depth = 0;
        var inQuote = '\0';
        var comment = false;
        for (var i = open; i < end; i++)
        {
            var c = source[i];
            if (c == '\n') { comment = false; continue; }
            if (comment) continue;
            if (inQuote != '\0')
            {
                if (c == '\\') { i++; continue; }
                if (c == inQuote) inQuote = '\0';
                continue;
            }
            if (c == '-' && i + 1 < end && source[i + 1] == '-') { comment = true; i++; continue; }
            if (c is '\'' or '"') { inQuote = c; continue; }
            if (c == '{') depth++;
            if (c == '}')
            {
                depth--;
                if (depth == 0) return (open + 1, i);
            }
        }
        throw new FormatException($"設定區塊未關閉：{key}");
    }

    internal static void Save(string path, string content)
    {
        var temp = path + ".manager.tmp";
        try
        {
            File.WriteAllText(temp, content, new UTF8Encoding(false));
            File.Replace(temp, path, path + ".manager.bak");
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    internal static void SelfTest()
    {
        var source = "HomConfig = {\n  lif = {\n    AttackMonstersEnabled = 1, -- keep\n    AttackAllExclude = { players = 1, },\n  },\n  amistr = { AttackMonstersEnabled = 0, },\n}";
        var outer = FindBlock(source, "HomConfig", 0, source.Length);
        var lif = FindBlock(source, "lif", outer.Start, outer.End);
        if (!source[lif.Start..lif.End].Contains("AttackMonstersEnabled = 1")) throw new Exception("Block scan failed");
    }

    internal static void CheckFile(string path)
    {
        var source = File.ReadAllText(path);
        foreach (var type in Settings.Types)
        {
            var values = Read(source, type.Key);
            var updated = Update(source, type.Key, values);
            if (updated != source) throw new Exception("未修改值時不應改動設定檔：" + type.Key);
            var changed = new Dictionary<string, int>(values)
            {
                ["AttackMonstersEnabled"] = 1 - values["AttackMonstersEnabled"]
            };
            updated = Update(source, type.Key, changed);
            if (Read(updated, type.Key)["AttackMonstersEnabled"] != changed["AttackMonstersEnabled"])
                throw new Exception("修改值未正確寫入：" + type.Key);
            foreach (var other in Settings.Types.Where(other => other.Key != type.Key))
                if (!Read(updated, other.Key).SequenceEqual(Read(source, other.Key)))
                    throw new Exception("修改影響其他生命體：" + type.Key);
        }
    }
}
