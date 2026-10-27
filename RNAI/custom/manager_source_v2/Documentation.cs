using System.Text;
using System.Text.RegularExpressions;
using HomunculusManager.Models;
using HomunculusManager.Services;

namespace HomunculusManager;

internal static class Documentation
{
    internal static void ExportMap(string configPath, string outputPath)
    {
        var document = LuaConfigService.Open(configPath);
        var values = SettingsCatalog.Types.ToDictionary(x => x.Key, x => LuaConfigService.Read(document, x.Key));
        var rnai = Directory.GetParent(Path.GetDirectoryName(configPath)!)!.FullName;
        var source = Directory.EnumerateFiles(rnai, "*.lua", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "custom" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .SelectMany(File.ReadAllLines)
            .Select(line => Regex.Replace(line, @"--.*$", ""))
            .Where(line => !Regex.IsMatch(line, @"^\s*[A-Za-z_][A-Za-z_0-9]*\s*="))
            .ToArray();
        var sb = new StringBuilder();
        sb.AppendLine("# Lua 設定欄位對照表\n");
        sb.AppendLine("從目前 `homunculus_config.lua` 擷取預設值。L/A/F/V 分別代表麗芙、阿密斯特爾、飛里樂、巴尼米樂斯；`—` 表示該生命體沒有此欄位。\n");
        sb.AppendLine("「程式碼引用候選」只表示排除設定檔後，在 Lua 非註解行找到同名文字；動態載入與遊戲內效果仍需實機驗證。`LoadHomConfig` 會把每種生命體配置載入 `_G`。\n");
        sb.AppendLine("| Setting Key | 生命體預設 L/A/F/V | 功能說明 | 資料類型 | UI 元件 | 是否實際引用 |\n|---|---|---|---|---|---|");
        foreach (var field in SettingsCatalog.All)
        {
            var defaults = string.Join("/", SettingsCatalog.Types.Select(type => values[type.Key].TryGetValue(field.Key, out var value)
                ? field.IsBoolean ? value == 1 ? "true" : "false" : (value / (decimal)field.Scale).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : "—"));
            var referenced = source.Any(line => Regex.IsMatch(line, @"\b" + Regex.Escape(field.Key) + @"\b"));
            sb.AppendLine($"| `{field.Key}` | {defaults} | {field.Description.Replace("|", "，")} | {(field.IsBoolean ? "Boolean" : field.Kind == SettingKind.Number ? "Number" : "Enum / 0-1")} | {(field.Kind == SettingKind.Toggle ? "Toggle" : field.Kind == SettingKind.Choice ? "Segmented" : "Numeric Input")} | {(referenced ? "程式碼引用候選" : "僅確認載入，未見直接引用")} |");
        }
        File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false));
    }
}
