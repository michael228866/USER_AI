using System.Text;
using System.Text.Json.Nodes;
using HomunculusManager.Models;
using HomunculusManager.Services;

namespace HomunculusManager;

internal static class SelfTests
{
    private static int checks;

    internal static void Run(string fixturePath)
    {
        checks = 0;
        var fixture = LuaConfigService.Open(fixturePath);
        var profilesFixture = Path.Combine(Path.GetDirectoryName(fixturePath)!, "manager_profiles.json");
        LuaConfigService.Check(fixturePath);
        Check(true, "四種生命體設定讀取與原樣更新");

        var tempDir = Path.Combine(Path.GetTempPath(), "homunculus-manager-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var config = Path.Combine(tempDir, "homunculus_config.lua");
            var profiles = Path.Combine(tempDir, "manager_profiles.json");
            File.Copy(fixturePath, config);
            File.Copy(profilesFixture, profiles);
            foreach (var type in SettingsCatalog.Types)
            {
                var document = LuaConfigService.Open(config);
                var before = SettingsCatalog.Types.ToDictionary(x => x.Key, x => LuaConfigService.Read(document, x.Key));
                var original = before[type.Key]["AttackMonstersEnabled"];
                var expected = LuaConfigService.Update(document.Text, type.Key, new Dictionary<string, int> { ["AttackMonstersEnabled"] = 1 - original });
                var saved = LuaConfigService.Save(config, document, type.Key, new Dictionary<string, int> { ["AttackMonstersEnabled"] = 1 - original });
                Check(saved.Text == expected, type.Key + " 只改動一個數值");
                Check(File.Exists(config + ".manager.bak"), type.Key + " 備份建立");
                Check(LuaConfigService.Read(saved, type.Key)["AttackMonstersEnabled"] == 1 - original, type.Key + " 寫入讀回");
                foreach (var other in SettingsCatalog.Types.Where(x => x.Key != type.Key))
                    Check(DictionariesEqual(before[other.Key], LuaConfigService.Read(saved, other.Key)), type.Key + " 不影響 " + other.Key);
            }
            var untouched = File.ReadAllBytes(config);
            var same = LuaConfigService.Save(config, LuaConfigService.Open(config), "lif", new Dictionary<string, int>());
            Check(untouched.AsSpan().SequenceEqual(File.ReadAllBytes(config)), "未修改時檔案位元組不變");
            Check(same.Bytes.AsSpan().SequenceEqual(untouched), "未修改時不重寫");
            Check(fixture.Text.Contains("\r\n") && same.Text.Contains("\r\n"), "CRLF 換行保留");
            Check(same.Text.Contains("-- 協同作戰設定"), "Lua 註解保留");
            Check(ProfileService.Load(profiles, 1)["AttackTimeoutMs"] == 3000, "既有配置秒數載入為毫秒");
            Check(SettingsCatalog.Find("priorityTargets").DisplayValue(0) == "原值 0" &&
                  SettingsCatalog.Find("priorityTargets").DisplayValue(1) == "原值 1", "有歧義數值不重新定義");

            foreach (var variant in new[]
            {
                (Name: "utf8-bom.lua", Encoding: (Encoding)new UTF8Encoding(true), Text: fixture.Text),
                (Name: "utf16.lua", Encoding: (Encoding)new UnicodeEncoding(false, true), Text: fixture.Text),
                (Name: "lf.lua", Encoding: (Encoding)new UTF8Encoding(false), Text: fixture.Text.Replace("\r\n", "\n"))
            })
            {
                var variantPath = Path.Combine(tempDir, variant.Name);
                File.WriteAllBytes(variantPath, variant.Encoding.GetPreamble().Concat(variant.Encoding.GetBytes(variant.Text)).ToArray());
                var opened = LuaConfigService.Open(variantPath);
                LuaConfigService.Save(variantPath, opened, "lif", new Dictionary<string, int>
                {
                    ["AttackMonstersEnabled"] = 1 - LuaConfigService.Read(opened, "lif")["AttackMonstersEnabled"]
                });
                var after = LuaConfigService.Open(variantPath);
                Check(after.Preamble.AsSpan().SequenceEqual(opened.Preamble), variant.Name + " 編碼標記保留");
                Check(after.Text.Contains("\r\n") == variant.Text.Contains("\r\n"), variant.Name + " 換行保留");
            }

            var session = new EditorSession(config);
            session.Set("FollowDis", session.Values["FollowDis"] + 1);
            Check(!session.Switch("amistr", () => false) && session.Type == "lif" && session.IsDirty, "切換拒絕時保留修改");
            Check(session.Switch("amistr", () => true) && !session.IsDirty, "切換確認後載入另一生命體");
            session.Set("AttackTimeoutMs", 3000);
            ProfileService.Save(profiles, 3, session.Values);
            Check(ProfileService.IsSaved(profiles, 3), "自訂 3 儲存狀態");
            Check(ProfileService.Load(profiles, 3)["AttackTimeoutMs"] == 3000, "秒與毫秒往返");
            var json = JsonNode.Parse(File.ReadAllText(profiles))!;
            Check(json["profiles"]?["自訂3"]?["parameters"]?["AttackTimeoutMs"]?.GetValue<double>() == 3.0,
                "JSON 以秒儲存");
            Check(File.Exists(profiles + ".manager.bak"), "配置備份建立");

            json["profiles"]?["自訂3"]?["combat"]?.AsObject().Remove("FollowMode");
            File.WriteAllText(profiles, json.ToJsonString());
            Check(!ProfileService.Load(profiles, 3).ContainsKey("FollowMode"), "缺失的配置欄位安全略過");
            CheckThrows<ArgumentOutOfRangeException>(() => session.Set("SkillMinSP", -1), "無效數值拒絕");
            CheckThrows<FormatException>(() => LuaConfigService.Read(LuaConfigService.Open(Write(tempDir, "broken.lua", "HomConfig = { lif = {")), "lif"), "損壞 Lua 拒絕");
            CheckThrows<System.Text.Json.JsonException>(() => ProfileService.Load(Write(tempDir, "broken.json", "{"), 1), "損壞 JSON 拒絕");
            var snapshot = LuaConfigService.Open(config);
            File.AppendAllText(config, "-- external change\r\n");
            CheckThrows<IOException>(() => LuaConfigService.Save(config, snapshot, "lif", new Dictionary<string, int> { ["FollowDis"] = 5 }),
                "外部變更拒絕覆蓋");
            Console.WriteLine($"PASS {checks} checks");
        }
        finally
        {
            if (!Path.GetFullPath(tempDir).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
                throw new IOException("測試暫存路徑超出系統暫存目錄");
            Directory.Delete(tempDir, true);
        }
    }

    private static string Write(string dir, string name, string content)
    {
        var path = Path.Combine(dir, name);
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return path;
    }

    private static bool DictionariesEqual(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b) =>
        a.Count == b.Count && a.All(x => b.TryGetValue(x.Key, out var value) && x.Value == value);

    private static void Check(bool success, string name)
    {
        if (!success) throw new Exception("FAIL: " + name);
        checks++;
    }

    private static void CheckThrows<T>(Action action, string name) where T : Exception
    {
        try { action(); }
        catch (T) { Check(true, name); return; }
        throw new Exception("FAIL: " + name);
    }
}
