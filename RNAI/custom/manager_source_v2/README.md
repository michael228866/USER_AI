# 生命體 AI 管理器 v2

Dance Attack 的手動 ASPD 輸入、節奏公式、舊設定相容性及 Mock 測試見 [DANCE_TIMING.md](DANCE_TIMING.md)。

## GitHub 更新提示

視窗右上角啟動時非同步檢查 GitHub 最新 Release；只有 Release 包含 `USER_AI_update.zip`，且版本 SHA 與目前 EXE 不同時才顯示更新提示。點擊會開啟官方 Release 頁。離線或 GitHub 尚無 Release 時不影響設定編輯。

`.github/workflows/release.yml` 在 `main` 更新時建置、驗證並發布 `USER_AI_full.zip` 與 `USER_AI_update.zip`。後者排除個人設定、名單、Profiles 與 Log。手動建置若要與 Release 正確比對，需在 `dotnet publish` 加上 `-p:UpdateRevision=<40 位 Git SHA>`；未指定時版本為 `local`。

「技能管理」可逐一開關九種生命體的自動技能，並修改 `Skill` 表的技能等級（1–10）與延遲；`id=0` 普通攻擊不受自動技能開關影響。技能開關寫入對應生命體的 `SkillConfig.enabled`，手動施法仍可使用。請填入遊戲中已習得的等級。若有綁定舊等級的 `SkillConfig` 項目，會拒絕直接變更以免設定失效。

舊設定中的 `8201`、`8203`、`8216`、`8217` 是傭兵技能 ID；設定器會提示並從生命體技能編輯區排除，保留原始 Lua 內容供檢查。

五種生命體 S（艾拉、巴耶里、莎拉、迪特爾、艾蕾諾亞）各有獨立的 Lua 設定與設定器頁面。AI 接受 `V_HOMUNTYPE` 的 48–52 或 6048–6052，並保留舊設定的 53→艾蕾諾亞別名；實際遊戲回報值仍須以 RO4 Log 確認。五種 S 型已列出本專案 SkillData 支援的全部 26 項 S 技能（艾拉 5、巴耶里 5、莎拉 4、迪特爾 5、艾蕾諾亞 7）；全部預設關閉自動施放，普通攻擊可用。請先在遊戲中確認已習得技能及等級，再逐項開啟；較新、尚未收錄在本專案 SkillData 的技能沒有擅自加入自動施放。五種 S 型在載入各自 ASPD 後會重算攻擊節流。五種 S 型的「跳舞攻擊」可各自開關，預設關閉；開啟後直接使用舞步攻擊，不需要再調整第二個開關。尚未以遊戲實測校準 S 型攻距；原本四種生命體的設定保留。

這是以原版 NoAuth EXE 的可見功能為基準重建的 WPF / .NET 9 Windows 桌面程式。原版 EXE 保持原樣；新原始碼位於本資料夾。

## 執行

執行專案根目錄的 `生命體AI管理器.exe`。這是 Windows x64 自含 .NET Runtime 的單一 EXE，不需另外安裝 .NET；設定檔 `homunculus_config.lua`、`manager_profiles.json`、`friends_list.lua`、`enemies_list.lua` 保持在 `RNAI/custom/`。啟動時會讀取設定，只有按下明確的儲存按鈕才會寫入。

介面左側保留原版「常用、戰鬥、安全、進階、名單、Profiles」六頁，另外提供總覽與技能管理；頂部生命體卡片、搜尋框及頁尾儲存狀態固定。Lua 設定、自訂配置、名單、技能延遲分別儲存；每項動作的按鈕清楚標示寫入位置。

視窗預設 1000×700，最小 800×600，啟動置中；關閉後的大小與位置記錄在 `%LOCALAPPDATA%\HomunculusAiManager\window.json`。若上次螢幕已移除，視窗會移回可見工作區。側欄在窄視窗縮為 160 px；設定卡片與名單卡片依寬度重排，頂部生命體選擇與頁尾儲存列固定。程式宣告 Per Monitor V2 DPI 支援。尺寸與縮放截圖及驗收結果見 [WINDOWED_UI.md](WINDOWED_UI.md)。

## 建置與驗證

在 Windows PowerShell、專案根目錄執行：

```powershell
dotnet build RNAI/custom/manager_source_v2/HomunculusManager.csproj -c Release
dotnet RNAI/custom/manager_source_v2/bin/Release/net9.0-windows/生命體AI管理器.dll --check RNAI/custom/homunculus_config.lua
dotnet RNAI/custom/manager_source_v2/bin/Release/net9.0-windows/生命體AI管理器.dll --self-test RNAI/custom/homunculus_config.lua
dotnet RNAI/custom/manager_source_v2/bin/Release/net9.0-windows/生命體AI管理器.dll --ui-smoke RNAI/custom/homunculus_config.lua
```

`--self-test` 會複製設定至系統暫存資料夾後寫入測試，不修改正式 Lua/JSON/名單。`--ui-smoke` 只在 Windows 上檢查視窗頁面和尺寸，不取代人工操作或遊戲內驗證。

正式交付使用自含 Runtime 的單一檔案建置；在 Windows x64 上執行下列命令，再將 `publish_single/生命體AI管理器.exe` 複製到 `USER_AI/` 根目錄：

```powershell
dotnet publish RNAI/custom/manager_source_v2/HomunculusManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -p:NuGetAudit=false -o RNAI/custom/manager_source_v2/publish_single
```

如需重建欄位表：

```powershell
dotnet RNAI/custom/manager_source_v2/bin/Release/net9.0-windows/生命體AI管理器.dll --export-map RNAI/custom/homunculus_config.lua RNAI/custom/manager_source_v2/SETTING_MAP.md
```

## 資料安全與限制

Lua 欄位只替換被編輯的字面值；跨生命體定位、巢狀表、外部修改偵測、編碼及換行保留均由 `Services/` 實作。寫入使用同目錄暫存檔後再取代原檔。朋友與黑名單 ID 為四種生命體共用檔案。

已支援的技能編輯包含 `SkillConfig.enabled`、`Skill` 項目的等級與延遲。`SkillConfig` 其他輔助欄位與 `Skill` 的其餘屬性唯讀。自訂配置保存可編輯的標量值，不保存技能表與 ID 名單。沒有執行 RO 遊戲內驗證，不能宣稱 AI 行為與原版完全等價。詳見 [FEATURE_PARITY.md](FEATURE_PARITY.md)。
