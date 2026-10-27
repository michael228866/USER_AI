using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using HomunculusManager.Models;
using HomunculusManager.Services;

namespace HomunculusManager;

internal static class SelfTests
{
    private static int checks;

    internal static void Run(string fixturePath)
    {
        checks = 0;
        var releaseTag = "build-" + new string('a', 40);
        Check(UpdateService.HasUpdate(releaseTag, new string('b', 40), true), "不同版本且有更新包時顯示更新");
        Check(!UpdateService.HasUpdate(releaseTag, new string('a', 40), true) &&
              !UpdateService.HasUpdate(releaseTag, "local", false) &&
              !UpdateService.HasUpdate("invalid", "local", true), "相同版本或缺少更新包時不提示");
        CheckUpdateApply();
        var fixture = LuaConfigService.Open(fixturePath);
        var profilesFixture = Path.Combine(Path.GetDirectoryName(fixturePath)!, "manager_profiles.json");
        LuaConfigService.Check(fixturePath);
        foreach (var type in SettingsCatalog.Types)
            foreach (var skill in SkillService.Read(fixture.Text, type.Key).Where(x => x.Id > 0))
                Check(SkillService.ReadAutoEnabled(fixture.Text, type.Key, skill.Id, skill.Level) is not null,
                    type.Key + " 技能 " + skill.Id + " 可逐一開關");
        Check(SettingsCatalog.Types.Length == 9, "四種一般生命體與五種 S 型設定可讀取");
        Check(new[] { 8201, 8203, 8216, 8217 }.All(id => !SkillService.IsHomunculusSkill(id)) &&
              new[] { 0, 8001, 8013, 8028 }.All(SkillService.IsHomunculusSkill),
              "傭兵技能不混入生命體技能編輯清單");
        Check(fixture.Text.Contains("if t >= 6048 and t <= 6052 then t = t - 6000 end", StringComparison.Ordinal) &&
              fixture.Text.Contains("local arr = {\"eira\",\"bayeri\",\"sera\",\"dieter\",\"eleanor\"}", StringComparison.Ordinal),
              "五種 S 型 ID 映射存在");
        foreach (var (type, skillId, skillIds) in new[] {
                     ("eira", 8024, new[] { 8022, 8023, 8024, 8025, 8026 }),
                     ("bayeri", 8034, new[] { 8031, 8032, 8033, 8034, 8035 }),
                     ("sera", 8019, new[] { 8018, 8019, 8020, 8021 }),
                     ("dieter", 8041, new[] { 8039, 8040, 8041, 8042, 8043 }),
                     ("eleanor", 8028, new[] { 8027, 8028, 8029, 8030, 8036, 8037, 8038 }) })
        {
            var skills = SkillService.Read(fixture.Text, type);
            Check(skills.Count == skillIds.Length + 1 && skills[^1].Id == 0 &&
                  skills.Where(x => x.Id > 0).Select(x => x.Id).Order().SequenceEqual(skillIds),
                type + " 具備所有本機支援的 S 技能與普通攻擊");
            Check(skillIds.All(id => SkillService.ReadAutoEnabled(fixture.Text, type, id, 1) == false),
                type + " 新增技能全部預設不自動施放");
            Check(SkillService.ReadAutoEnabled(fixture.Text, type, skillId, 1) == false,
                type + " 的 S 技能預設不自動施放");
            Check(LuaConfigService.Read(fixture, type)["DanceEnabled"] == 0,
                type + " 未校準舞步預設關閉");
            Check(LuaConfigService.ReadValue(fixture.Text, type, SettingsCatalog.Find("DanceAttack")) == 1,
                type + " 開啟舞步時無第二開關阻擋");
            Check(LuaConfigService.ExtractBlock(fixture.Text, type, "SkillConfig").Contains($"Skill_{skillId}", StringComparison.Ordinal) &&
                  LuaConfigService.GetUnmanagedFields(fixture.Text, type).Contains("AttackThrottleMs = 0", StringComparison.Ordinal),
                  type + " 有獨立技能開關與 ASPD 節流重算設定");
        }
        var workAreas = new[] { new Rect(0, 0, 1920, 1040), new Rect(1920, 0, 1600, 900) };
        var remembered = WindowPlacementService.Resolve(new WindowPlacementState(2050, 80, 1000, 700), workAreas);
        Check(remembered.Left == 2050 && remembered.Top == 80, "保留第二螢幕視窗位置");
        var disconnected = WindowPlacementService.Resolve(new WindowPlacementState(5000, 5000, 1000, 700), workAreas);
        Check(disconnected.Left >= 0 && disconnected.Right <= 1920 && disconnected.Top >= 0, "螢幕移除後移回可見範圍");
        var invalidWindow = WindowPlacementService.Resolve(new WindowPlacementState(double.NaN, 0, 500, 300), workAreas);
        Check(invalidWindow.Width == 1000 && invalidWindow.Height == 700, "無效視窗紀錄使用預設尺寸");
        var oversized = WindowPlacementService.Resolve(new WindowPlacementState(100, 100, 2500, 1500), workAreas);
        Check(oversized.Width == 1920 && oversized.Height == 1040, "過大視窗縮至螢幕工作範圍");

        var tempDir = Path.Combine(Path.GetTempPath(), "homunculus-manager-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var placementPath = Path.Combine(tempDir, "window.json");
            WindowPlacementService.Write(placementPath, new WindowPlacementState(100, 80, 1000, 700));
            Check(WindowPlacementService.Load(placementPath) == new WindowPlacementState(100, 80, 1000, 700),
                "視窗大小與位置讀寫");
            File.WriteAllText(placementPath, "{");
            Check(WindowPlacementService.Load(placementPath) is null, "損壞視窗紀錄安全忽略");
            var config = Path.Combine(tempDir, "homunculus_config.lua");
            var profiles = Path.Combine(tempDir, "manager_profiles.json");
            File.Copy(fixturePath, config);
            File.Copy(profilesFixture, profiles);
            if (!ProfileService.IsSaved(profiles, 1))
                ProfileService.Save(profiles, 1, new Dictionary<string, int> { ["AttackTimeoutMs"] = 3000 });
            var sSkillPath = Path.Combine(tempDir, "s-skill.lua");
            File.Copy(fixturePath, sSkillPath);
            SkillService.SaveAutoEnabled(sSkillPath, "eira", 8022, 1, false, true);
            var sSkillText = LuaConfigService.Open(sSkillPath).Text;
            Check(SkillService.ReadAutoEnabled(sSkillText, "eira", 8022, 1) == true &&
                  SkillService.ReadAutoEnabled(sSkillText, "eira", 8024, 1) == false &&
                  SkillService.ReadAutoEnabled(sSkillText, "sera", 8019, 1) == false,
                  "S 技能開關只影響選取的生命體");
            CheckThrows<ArgumentOutOfRangeException>(() =>
                SkillService.SaveLevel(sSkillPath, "eleanor", 1, 8027, 1, 2),
                "艾蕾諾亞變換姿態僅可設一級");
            CheckThrows<ArgumentOutOfRangeException>(() =>
                SkillService.SaveLevel(sSkillPath, "eleanor", 0, 8028, 1, 6),
                "艾蕾諾亞音速爪拒絕超過五級");
            foreach (var name in new[] { "friends_list.lua", "enemies_list.lua" })
                File.Copy(Path.Combine(Path.GetDirectoryName(fixturePath)!, name), Path.Combine(tempDir, name));
            File.AppendAllText(Path.Combine(tempDir, "friends_list.lua"), "\n-- 詳細操作說明\n");
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
            var spBefore = LuaConfigService.Open(config);
            var amistrSp = LuaConfigService.Read(spBefore, "amistr")["SkillMinSP"];
            var spSaved = LuaConfigService.Save(config, spBefore, "filir",
                new Dictionary<string, int> { ["SkillMinSP"] = 100 });
            Check(LuaConfigService.Read(spSaved, "filir")["SkillMinSP"] == 100,
                "飛里樂技能最低 SP 寫入讀回");
            Check(LuaConfigService.Read(spSaved, "amistr")["SkillMinSP"] == amistrSp,
                "飛里樂技能最低 SP 不影響其他生命體");
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
                Check(after.Preamble.AsSpan().SequenceEqual(LuaConfigService.SavedPreamble(opened)), variant.Name + " 編碼標記正確（UTF-8 不寫 BOM）");
                Check(after.Text.Contains("\r\n") == variant.Text.Contains("\r\n"), variant.Name + " 換行保留");
            }

            var session = new EditorSession(config);
            session.Set("FollowDis", session.Values["FollowDis"] + 1);
            Check(!session.Switch("amistr", () => false) && session.Type == "lif" && session.IsDirty, "切換拒絕時保留修改");
            Check(session.Switch("amistr", () => true) && !session.IsDirty, "切換確認後載入另一生命體");
            session.Set("AttackTimeoutMs", 3000);
            ProfileService.Save(profiles, 3, session.Values, "測試配置");
            Check(ProfileService.IsSaved(profiles, 3), "自訂 3 儲存狀態");
            Check(ProfileService.Name(profiles, 3) == "測試配置", "自訂配置名稱讀寫");
            Check(ProfileService.Load(profiles, 3)["AttackTimeoutMs"] == 3000, "秒與毫秒往返");
            Check(ProfileService.Load(profiles, 3).ContainsKey("StuckDetectionEnabled"), "擴充欄位存入自訂配置");
            var json = JsonNode.Parse(File.ReadAllText(profiles))!;
            Check(json["profiles"]?["自訂3"]?["parameters"]?["AttackTimeoutMs"]?.GetValue<double>() == 3.0,
                "JSON 以秒儲存");
            Check(File.Exists(profiles + ".manager.bak"), "配置備份建立");
            ProfileService.Delete(profiles, 2);
            Check(!ProfileService.IsSaved(profiles, 2) && ProfileService.IsSaved(profiles, 1), "刪除單一配置不影響其他配置");

            foreach (var type in SettingsCatalog.Types)
            {
                var before = LuaConfigService.Open(config);
                var skills = SkillService.Read(before.Text, type.Key);
                Check(skills.Count > 0, type.Key + " 技能清單讀取");
                var skill = skills[0];
                SkillService.SaveDelay(config, type.Key, skill.Index, skill.Id, skill.Level, skill.Delay + 123);
                var after = LuaConfigService.Open(config);
                Check(SkillService.Read(after.Text, type.Key)[0].Delay == skill.Delay + 123, type.Key + " 技能延遲寫入");
                foreach (var other in SettingsCatalog.Types.Where(x => x.Key != type.Key))
                    Check(SkillService.Read(after.Text, other.Key)[0].Delay == SkillService.Read(before.Text, other.Key)[0].Delay,
                        type.Key + " 技能寫入不影響 " + other.Key);
                CheckThrows<ArgumentOutOfRangeException>(() => SkillService.SaveDelay(config, type.Key, skill.Index, skill.Id, skill.Level, -1),
                    type.Key + " 無效技能延遲拒絕");
                if (skill.Id > 0)
                {
                    var levelBefore = LuaConfigService.Open(config);
                    SkillService.SaveLevel(config, type.Key, skill.Index, skill.Id, skill.Level, skill.Level + 1);
                    var levelAfter = LuaConfigService.Open(config);
                    Check(SkillService.Read(levelAfter.Text, type.Key)[skill.Index].Level == skill.Level + 1,
                        type.Key + " 技能等級寫入讀回");
                    Check(levelAfter.Text.Contains("-- 協同作戰設定"), type.Key + " 技能等級寫入保留註解");
                    foreach (var other in SettingsCatalog.Types.Where(x => x.Key != type.Key))
                        Check(SkillService.Read(levelAfter.Text, other.Key)[0].Level == SkillService.Read(levelBefore.Text, other.Key)[0].Level,
                            type.Key + " 技能等級不影響 " + other.Key);
                    CheckThrows<ArgumentOutOfRangeException>(() => SkillService.SaveLevel(config, type.Key, skill.Index, skill.Id, skill.Level + 1, 0),
                        type.Key + " 無效技能等級拒絕");
                    CheckThrows<IOException>(() => SkillService.SaveLevel(config, type.Key, skill.Index, skill.Id, skill.Level, skill.Level + 2),
                        type.Key + " 舊技能狀態拒絕覆蓋");
                }
            }
            var levelBound = Write(tempDir, "skill-level-bound.lua",
                fixture.Text.Replace("Skill_8001 =", "Skill_8001_lv1 =", StringComparison.Ordinal));
            CheckThrows<InvalidOperationException>(() => SkillService.SaveLevel(levelBound, "lif", 0, 8001, 1, 2),
                "等級專用 SkillConfig 拒絕無聲失效");
            var togglePath = Write(tempDir, "skill-toggle.lua", fixture.Text);
            var toggleBefore = LuaConfigService.Open(togglePath);
            var initialToggle = SkillService.ReadAutoEnabled(toggleBefore.Text, "filir", 8010, 5);
            var otherToggle = SkillService.ReadAutoEnabled(toggleBefore.Text, "filir", 8009, 5);
            Check(initialToggle is not null && otherToggle is not null,
                "飛里樂技能自動施放開關讀取");
            SkillService.SaveAutoEnabled(togglePath, "filir", 8010, 5, initialToggle!.Value, !initialToggle.Value);
            var toggleAfter = LuaConfigService.Open(togglePath);
            Check(SkillService.ReadAutoEnabled(toggleAfter.Text, "filir", 8010, 5) == !initialToggle.Value &&
                  SkillService.ReadAutoEnabled(toggleAfter.Text, "filir", 8009, 5) == otherToggle,
                "只關閉指定技能");
            Check(LuaConfigService.Read(toggleAfter, "amistr")["SkillMinSP"] ==
                  LuaConfigService.Read(toggleBefore, "amistr")["SkillMinSP"] &&
                  toggleAfter.Text.Split('\n').Zip(toggleBefore.Text.Split('\n')).Count(x => x.First != x.Second) == 1,
                "技能開關只修改一行且不影響其他生命體");
            CheckThrows<IOException>(() => SkillService.SaveAutoEnabled(togglePath, "filir", 8010, 5,
                initialToggle.Value, initialToggle.Value),
                "技能開關拒絕過期狀態覆蓋");
            var friendsPath = Path.Combine(tempDir, "friends_list.lua");
            var enemiesPath = Path.Combine(tempDir, "enemies_list.lua");
            ListService.Save(friendsPath, "friends_data", [1001, 1002]);
            ListService.Save(enemiesPath, "enemies_data", [2001]);
            Check(ListService.Read(friendsPath, "friends_data").SequenceEqual([1001, 1002]), "朋友列表寫入讀回");
            Check(ListService.Read(enemiesPath, "enemies_data").SequenceEqual([2001]), "黑名單寫入讀回");
            var friendBytes = File.ReadAllBytes(friendsPath);
            ListService.Save(friendsPath, "friends_data", [1001, 1002]);
            Check(friendBytes.AsSpan().SequenceEqual(File.ReadAllBytes(friendsPath)), "名單未變更時保持位元組不變");
            Check(File.ReadAllText(friendsPath).Contains("詳細操作說明"), "名單外註解保留");
            Check(File.Exists(friendsPath + ".manager.bak") && File.Exists(enemiesPath + ".manager.bak"), "名單備份建立");
            CheckThrows<ArgumentException>(() => ListService.Save(friendsPath, "friends_data", [1, 1]), "重複 ID 拒絕");
            CheckThrows<IOException>(() => ListService.Save(friendsPath, "friends_data", [1001, 1003], [1001]),
                "名單外部變更拒絕覆蓋");
            CheckThrows<FormatException>(() => ListService.Read(Write(tempDir, "bad-list.lua", "friends_data = { bad() }"), "friends_data"),
                "不安全名單格式拒絕");
            var danceDoc = LuaConfigService.Open(config);
            var danceValues = LuaConfigService.Read(danceDoc, "lif");
            var changedDance = LuaConfigService.Save(config, danceDoc, "lif", new Dictionary<string, int>
            {
                ["DanceEnabled"] = 1 - danceValues["DanceEnabled"], ["DancePhase"] = danceValues["DancePhase"] + 1
            });
            Check(LuaConfigService.Read(changedDance, "lif")["DancePhase"] == danceValues["DancePhase"] + 1,
                "小數舞步節拍寫入讀回");
            Check(LuaConfigService.Read(changedDance, "lif")["DanceEnabled"] == 1 - danceValues["DanceEnabled"],
                "Lua Boolean 寫入讀回");
            var rollback = LuaConfigService.Open(config + ".manager.bak");
            LuaConfigService.Check(config + ".manager.bak");
            ListService.AtomicWrite(config, LuaConfigService.Open(config), rollback.Text);
            Check(LuaConfigService.Read(LuaConfigService.Open(config), "lif")["DanceEnabled"] == danceValues["DanceEnabled"],
                "上一次 Lua 備份還原");

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

    private static void CheckUpdateApply()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hm_update_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var package = Path.Combine(dir, "package");
            var install = Path.Combine(dir, "install");
            Directory.CreateDirectory(Path.Combine(package, "RNAI", "custom"));
            Directory.CreateDirectory(Path.Combine(install, "RNAI", "custom"));
            Write(package, "AI.lua", "new ai");
            Write(Path.Combine(package, "RNAI"), "Main.lua", "new main");
            Write(package, "manager.exe", "new exe");
            Write(install, "AI.lua", "old ai");
            Write(install, "manager.exe", "old exe");
            Write(Path.Combine(install, "RNAI", "custom"), "homunculus_config.lua", "user config");

            var exe = UpdateService.Apply(package, install, Path.Combine(install, "manager.exe"));
            Check(File.ReadAllText(Path.Combine(install, "AI.lua")) == "new ai" &&
                  File.ReadAllText(Path.Combine(install, "RNAI", "Main.lua")) == "new main", "自動更新覆蓋 AI 檔案");
            Check(exe == Path.Combine(install, "manager.exe") && File.ReadAllText(exe) == "new exe" &&
                  File.ReadAllText(exe + ".old") == "old exe", "自動更新替換管理器並保留舊版");
            Check(File.ReadAllText(Path.Combine(install, "RNAI", "custom", "homunculus_config.lua")) == "user config",
                "自動更新不動個人設定");
        }
        finally { Directory.Delete(dir, true); }
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
