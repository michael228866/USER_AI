using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HomunculusManager.Models;

namespace HomunculusManager.Services;

internal sealed record ConfigDocument(byte[] Bytes, string Text, Encoding Encoding, byte[] Preamble);

internal static class LuaConfigService
{
    private static readonly Regex NumberLine = new(@"(?m)^[ \t]*(?<key>[A-Za-z_][A-Za-z_0-9]*)[ \t]*=[ \t]*(?<value>true|false|-?\d+(?:\.\d+)?)(?=[ \t]*(?:,|--|\r?$))", RegexOptions.Compiled);

    internal static ConfigDocument Open(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Encoding encoding;
        byte[] preamble;
        if (bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
            (encoding, preamble) = (new UTF8Encoding(false, true), [0xEF, 0xBB, 0xBF]);
        else if (bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE }))
            (encoding, preamble) = (new UnicodeEncoding(false, false, true), [0xFF, 0xFE]);
        else if (bytes.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }))
            (encoding, preamble) = (new UnicodeEncoding(true, false, true), [0xFE, 0xFF]);
        else
            (encoding, preamble) = (new UTF8Encoding(false, true), []);
        var text = encoding.GetString(bytes, preamble.Length, bytes.Length - preamble.Length);
        return new ConfigDocument(bytes, text, encoding, preamble);
    }

    internal static Dictionary<string, int> Read(ConfigDocument document, string type)
    {
        var values = new Dictionary<string, int>();
        foreach (var field in SettingsCatalog.ForType(type))
            values[field.Key] = ReadValue(document.Text, type, field);
        return values;
    }

    internal static int ReadValue(string text, string type, SettingDefinition field)
    {
        var match = FindValue(text, type, field);
        var value = ParseValue(match.Groups["value"].Value, field);
        if (value < field.Min || value > field.Max)
            throw new FormatException($"{type}.{field.Key}={value} 超出允許範圍 {field.Min}–{field.Max}");
        return value;
    }

    internal static string Update(string text, string type, IReadOnlyDictionary<string, int> changes)
    {
        if (changes.Count == 0) return text;
        var edits = new List<(int Start, int Length, string Value)>();
        foreach (var (key, value) in changes)
        {
            var field = SettingsCatalog.Find(key);
            if (value < field.Min || value > field.Max)
                throw new ArgumentOutOfRangeException(key, value, $"允許範圍 {field.Min}–{field.Max}");
            var match = FindValue(text, type, field);
            var group = match.Groups["value"];
            if (ParseValue(group.Value, field) != value)
                edits.Add((group.Index, group.Length, FormatValue(value, field)));
        }
        foreach (var edit in edits.OrderByDescending(x => x.Start))
            text = text[..edit.Start] + edit.Value + text[(edit.Start + edit.Length)..];
        return text;
    }

    // The game's Lua cannot parse a UTF-8 BOM, so it is dropped on save; UTF-16 keeps its mark.
    internal static byte[] SavedPreamble(ConfigDocument document) =>
        document.Encoding is UTF8Encoding ? [] : document.Preamble;

    internal static ConfigDocument Save(string path, ConfigDocument original, string type, IReadOnlyDictionary<string, int> changes)
    {
        var current = File.ReadAllBytes(path);
        if (!current.AsSpan().SequenceEqual(original.Bytes))
            throw new IOException("設定檔已被其他程式修改。請重新載入後再儲存。");
        var updated = Update(original.Text, type, changes);
        if (updated == original.Text) return original;
        var preamble = SavedPreamble(original);
        var bytes = preamble.Concat(original.Encoding.GetBytes(updated)).ToArray();
        var temp = path + ".manager." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(true);
            }
            File.Replace(temp, path, path + ".manager.bak");
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
        return new ConfigDocument(bytes, updated, original.Encoding, preamble);
    }

    internal static string ExtractBlock(string text, string type, string block)
    {
        var range = TypeRange(text, type);
        var found = FindBlock(text, block, range.Start, range.End);
        return text[found.Open..(found.Close + 1)];
    }

    internal static string GetUnmanagedFields(string text, string type)
    {
        var range = TypeRange(text, type);
        var known = SettingsCatalog.All.Where(f => f.Parent is null).Select(f => f.Key).ToHashSet();
        var result = new List<string>();
        foreach (Match match in NumberLine.Matches(text[range.Start..range.End]))
        {
            var lineStart = range.Start + match.Index;
            if (GetBraceDepth(text, range.Start, lineStart) != 0) continue;
            var key = match.Groups["key"].Value;
            if (!known.Contains(key)) result.Add($"{key} = {match.Groups["value"].Value}");
        }
        return string.Join(Environment.NewLine, result.Distinct());
    }

    internal static void Check(string path)
    {
        var document = Open(path);
        foreach (var type in SettingsCatalog.Types)
        {
            var values = Read(document, type.Key);
            if (Update(document.Text, type.Key, values) != document.Text)
                throw new Exception($"未變更時意外改寫 {type.Key}");
            ExtractBlock(document.Text, type.Key, "SkillConfig");
            ExtractBlock(document.Text, type.Key, "Skill");
        }
    }

    private static Match FindValue(string text, string type, SettingDefinition field)
    {
        var range = TypeRange(text, type);
        if (field.Parent is not null)
        {
            var parent = FindBlock(text, field.Parent, range.Start, range.End);
            range = (parent.Open + 1, parent.Close);
        }
        var matches = NumberLine.Matches(text[range.Start..range.End]).Cast<Match>()
            .Where(m => m.Groups["key"].Value == field.Key && GetBraceDepth(text, range.Start, range.Start + m.Index) == 0)
            .ToArray();
        if (matches.Length != 1) throw new FormatException($"找不到唯一的欄位：{type}.{field.Key}");
        var match = matches[0];
        var absolute = range.Start + match.Index;
        var result = NumberLine.Match(text, absolute);
        if (!result.Success || result.Index != absolute) throw new FormatException($"欄位定位失敗：{type}.{field.Key}");
        return result;
    }

    private static int ParseValue(string raw, SettingDefinition field)
    {
        if (field.IsBoolean)
            return raw switch { "true" => 1, "false" => 0, _ => throw new FormatException($"{field.Key} 需要 true/false") };
        var number = decimal.Parse(raw, CultureInfo.InvariantCulture) * field.Scale;
        if (number != decimal.Truncate(number)) throw new FormatException($"{field.Key} 精度不支援");
        return checked((int)number);
    }

    private static string FormatValue(int value, SettingDefinition field) => field.IsBoolean
        ? value == 1 ? "true" : "false"
        : (value / (decimal)field.Scale).ToString(CultureInfo.InvariantCulture);

    private static (int Start, int End) TypeRange(string text, string type)
    {
        var root = FindBlock(text, "HomConfig", 0, text.Length);
        var hom = FindBlock(text, type, root.Open + 1, root.Close);
        return (hom.Open + 1, hom.Close);
    }

    internal static (int Open, int Close) FindBlock(string text, string key, int start, int end)
    {
        var pattern = @"(?m)^[ \t]*" + Regex.Escape(key) + @"[ \t]*=[ \t]*\{";
        var matches = Regex.Matches(text[start..end], pattern).Cast<Match>()
            .Where(m => GetBraceDepth(text, start, start + m.Index) == 0).ToArray();
        if (matches.Length != 1) throw new FormatException($"找不到唯一的區塊：{key}");
        var open = start + matches[0].Index + matches[0].Length - 1;
        return (open, FindMatchingBrace(text, open, end));
    }

    internal static int FindMatchingBrace(string text, int open, int end)
    {
        var depth = 0;
        for (var i = open; i < end; i++)
        {
            if (text[i] is '\'' or '"') { i = SkipString(text, i, end); continue; }
            if (text[i] == '-' && i + 1 < end && text[i + 1] == '-') { i = SkipComment(text, i, end); continue; }
            if (text[i] == '{') depth++;
            if (text[i] == '}' && --depth == 0) return i;
        }
        throw new FormatException("Lua table 大括號未關閉");
    }

    internal static int GetBraceDepth(string text, int start, int end)
    {
        var depth = 0;
        for (var i = start; i < end; i++)
        {
            if (text[i] is '\'' or '"') { i = SkipString(text, i, end); continue; }
            if (text[i] == '-' && i + 1 < end && text[i + 1] == '-') { i = SkipComment(text, i, end); continue; }
            if (text[i] == '{') depth++;
            if (text[i] == '}') depth--;
        }
        return depth;
    }

    private static int SkipString(string text, int start, int end)
    {
        var quote = text[start];
        for (var i = start + 1; i < end; i++)
        {
            if (text[i] == '\\') { i++; continue; }
            if (text[i] == quote) return i;
        }
        throw new FormatException("Lua 字串未關閉");
    }

    private static int SkipComment(string text, int start, int end)
    {
        var newline = text.IndexOf('\n', start, end - start);
        return newline < 0 ? end - 1 : newline;
    }
}
