using System.Text.RegularExpressions;

namespace HomunculusManager.Services;

internal static class ListService
{
    private static readonly Regex Entry = new(@"(?m)^\s*(?:\[\s*\d+\s*\]\s*=\s*)?(?<id>\d+)\s*,?\s*(?:--[^\r\n]*)?$", RegexOptions.Compiled);

    internal static IReadOnlyList<int> Read(string path, string variable)
    {
        var document = LuaConfigService.Open(path);
        var (start, end) = Locate(document.Text, variable);
        var result = new List<int>();
        foreach (var line in document.Text[start..end].Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("--", StringComparison.Ordinal)) continue;
            var match = Entry.Match(line.TrimEnd('\r'));
            if (!match.Success) throw new FormatException($"{variable} 有不支援的名單格式：{trimmed}");
            var id = int.Parse(match.Groups["id"].Value);
            if (id <= 0 || result.Contains(id)) throw new FormatException($"{variable} 有無效或重複 ID：{id}");
            result.Add(id);
        }
        return result;
    }

    internal static void Save(string path, string variable, IReadOnlyCollection<int> ids, IReadOnlyCollection<int>? expectedOriginal = null)
    {
        if (ids.Any(x => x <= 0) || ids.Count != ids.Distinct().Count())
            throw new ArgumentException("ID 必須為正整數且不得重複。", nameof(ids));
        var original = LuaConfigService.Open(path);
        var existing = Read(path, variable);
        if (expectedOriginal is not null && !existing.SequenceEqual(expectedOriginal))
            throw new IOException("名單已由遊戲或其他程式修改，請重新載入後再儲存。");
        if (existing.SequenceEqual(ids)) return;
        var (start, end) = Locate(original.Text, variable);
        var newline = original.Text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var oldLines = original.Text[start..end].Split('\n').Select(x => x.TrimEnd('\r')).ToArray();
        var indent = oldLines.Select(x => Regex.Match(x, @"^[ \t]*(?=\[|\d|--)").Value)
            .FirstOrDefault(x => x.Length > 0) ?? "    ";
        var comments = oldLines.Where(x => x.TrimStart().StartsWith("--", StringComparison.Ordinal) &&
            !x.Contains("名單為空", StringComparison.Ordinal) && !x.Contains("朋友列表為空", StringComparison.Ordinal)).ToList();
        var oldEntryComments = new Dictionary<int, string>();
        foreach (var line in oldLines)
        {
            var match = Entry.Match(line);
            if (match.Success && line.Contains("--", StringComparison.Ordinal))
                oldEntryComments[int.Parse(match.Groups["id"].Value)] = " " + line[line.IndexOf("--", StringComparison.Ordinal)..];
        }
        var lines = new List<string>(comments);
        lines.AddRange(ids.Select((id, i) => $"{indent}[{i + 1}] = {id},{oldEntryComments.GetValueOrDefault(id)}"));
        if (ids.Count == 0 && lines.Count == 0) lines.Add(indent + "-- 名單為空");
        var body = newline + string.Join(newline, lines) + newline;
        var updated = original.Text[..start] + body + original.Text[end..];
        if (updated == original.Text) return;
        AtomicWrite(path, original, updated);
    }

    private static (int Start, int End) Locate(string text, string variable)
    {
        var pattern = @"(?m)^\s*" + Regex.Escape(variable) + @"\s*=\s*\{";
        var matches = Regex.Matches(text, pattern);
        if (matches.Count != 1) throw new FormatException($"找不到唯一名單：{variable}");
        var open = matches[0].Index + matches[0].Length - 1;
        var close = text.IndexOf('}', open + 1);
        if (close < 0 || text[(open + 1)..close].Contains('{')) throw new FormatException("名單巢狀結構不支援");
        return (open + 1, close);
    }

    internal static void AtomicWrite(string path, ConfigDocument original, string updated)
    {
        if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(original.Bytes))
            throw new IOException("檔案已由其他程式修改，請重新載入。 ");
        var bytes = LuaConfigService.SavedPreamble(original).Concat(original.Encoding.GetBytes(updated)).ToArray();
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
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
