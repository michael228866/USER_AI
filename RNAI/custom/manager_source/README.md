# 生命體設定器

這是 .NET 9 WPF 桌面版。介面改用深色側欄與分頁，保留原本的 Lua 設定值、自訂配置 JSON 格式及備份流程。AI 戰鬥腳本沒有修改。

## 使用

執行 `RNAI/custom/生命體設定器.exe`。同資料夾須有：

- `生命體設定器.dll`
- `生命體設定器.deps.json`
- `生命體設定器.runtimeconfig.json`
- `homunculus_config.lua`
- `manager_profiles.json`

電腦需安裝 **.NET 9 Windows Desktop Runtime（x64）**。本機離線環境沒有 self-contained 所需的 runtime pack，因此交付為 framework-dependent Windows x64 EXE。

選擇生命體後，在左側切換戰鬥、目標、距離、技能、配置或進階頁。搜尋框可查中文名稱、Lua Key 或用途。布林值使用切換開關，多值模式使用分段按鈕，數值欄位輸入後立即檢查範圍。修改只保留在編輯畫面，按「寫入 Lua 設定」並確認後才寫入檔案。可隨時按「重設編輯」放棄目前修改。

「儲存目前編輯為配置」只更新 `manager_profiles.json`；「套用並寫入 Lua」會在確認後更新目前生命體的 Lua 區塊。部分缺失的配置欄位保留目前 Lua 原值。`AttackTimeoutMs` 在 Lua 使用毫秒，自訂配置 JSON 使用秒。`priorityTargets` 的原始註解互相矛盾，介面只顯示原值 0/1。

技能頁的 `SkillConfig` 與 `Skill` 以及進階頁的其他欄位是**唯讀**。巢狀 Lua table 尚未提供安全的編輯器。完整欄位對照見 [SETTING_MAP.md](SETTING_MAP.md)。

## 資料安全

儲存 Lua 時只替換有修改的數字，其他生命體、註解、編碼、BOM 與換行不會重排。正式檔案使用同資料夾暫存檔及 `File.Replace` 原子替換；更新前會檢查磁碟檔案是否已由其他程式更動。原檔備份在 `homunculus_config.lua.manager.bak`。配置檔也使用 `.manager.bak` 備份。無修改時不寫入檔案。

## 編譯與檢查

在 `D:\RO4\AI\USER_AI` 執行：

```powershell
dotnet restore RNAI\custom\manager_source\HomunculusManager.csproj --source RNAI\custom\manager_source
dotnet build RNAI\custom\manager_source\HomunculusManager.csproj -c Release --no-restore
dotnet RNAI\custom\生命體設定器.dll --check RNAI\custom\homunculus_config.lua
dotnet RNAI\custom\生命體設定器.dll --self-test RNAI\custom\homunculus_config.lua
dotnet RNAI\custom\生命體設定器.dll --ui-smoke
```

`--self-test` 只在系統暫存目錄建立複本，不修改正式 Lua/JSON。`--ui-smoke` 需要設定檔與執行中的 DLL 位於同一資料夾，會開啟離開螢幕範圍的視窗，檢查頁面、生命體切換及搜尋。`--preview <png> [dpi-scale] [width] [height] [page]` 可輸出畫面；例如 `--preview preview.png 1.25 900 600 戰鬥行為`。預覽和 UI smoke 是 Windows 桌面檢查，不等於遊戲內實測。
