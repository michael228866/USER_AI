using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HomunculusManager.Models;

namespace HomunculusManager.Services;

internal static class ProfileService
{
    internal static bool IsSaved(string path, int slot)
    {
        var root = Parse(path);
        return GetSlot(root, slot)["saved"]?.GetValue<bool>() == true;
    }

    internal static Dictionary<string, int> Load(string path, int slot)
    {
        var profile = GetSlot(Parse(path), slot);
        if (profile["saved"]?.GetValue<bool>() != true)
            throw new InvalidOperationException($"自訂{slot} 尚未儲存");
        var values = new Dictionary<string, int>();
        foreach (var field in SettingsCatalog.All)
        {
            var node = profile[field.ProfileGroup]?[field.Key];
            if (node is null) continue;
            var value = field.Key == "AttackTimeoutMs"
                ? checked((int)Math.Round(node.GetValue<double>() * 1000, MidpointRounding.AwayFromZero))
                : node.GetValue<int>();
            if (value < field.Min || value > field.Max)
                throw new FormatException($"自訂{slot}.{field.Key} 超出範圍 {field.Min}–{field.Max}");
            values.Add(field.Key, value);
        }
        return values;
    }

    internal static void Save(string path, int slot, IReadOnlyDictionary<string, int> values)
    {
        var bytes = File.ReadAllBytes(path);
        var hasBom = bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF });
        var source = new UTF8Encoding(false, true).GetString(bytes, hasBom ? 3 : 0, bytes.Length - (hasBom ? 3 : 0));
        var root = JsonNode.Parse(source) ?? throw new FormatException("配置檔為空");
        var profile = GetSlot(root, slot);
        foreach (var field in SettingsCatalog.All)
        {
            if (!values.TryGetValue(field.Key, out var value) || value < field.Min || value > field.Max)
                throw new ArgumentOutOfRangeException(field.Key, $"允許範圍 {field.Min}–{field.Max}");
            var group = profile[field.ProfileGroup] ?? throw new FormatException($"缺少配置群組：{field.ProfileGroup}");
            if (field.Key == "AttackTimeoutMs") group[field.Key] = value / 1000.0;
            else group[field.Key] = value;
        }
        profile["saved"] = true;
        var newline = source.Contains("\r\n") ? "\r\n" : "\n";
        var updated = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).Replace("\n", newline);
        if (source.EndsWith(newline)) updated += newline;
        var output = (hasBom ? new byte[] { 0xEF, 0xBB, 0xBF } : []).Concat(Encoding.UTF8.GetBytes(updated)).ToArray();
        if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
            throw new IOException("配置檔已由其他程式修改，請重新載入。");
        var temp = path + ".manager." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(output);
                stream.Flush(true);
            }
            File.Replace(temp, path, path + ".manager.bak");
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static JsonNode Parse(string path) => JsonNode.Parse(File.ReadAllText(path, new UTF8Encoding(false, true)))
        ?? throw new FormatException("配置檔為空");

    private static JsonNode GetSlot(JsonNode root, int slot)
    {
        if (slot < 1 || slot > 3) throw new ArgumentOutOfRangeException(nameof(slot));
        return root["profiles"]?[$"自訂{slot}"] ?? throw new FormatException($"找不到自訂{slot}");
    }
}
