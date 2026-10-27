# 原版功能盤點與復刻狀態

## 分析依據

`生命體設定管理器_NoAuth_新版.exe` 為約 50 MB 的 Windows PE；檔案資訊顯示 `生命體設定管理器_NoAuth.dll`、版本 8.5.2。內嵌資訊含 `net9.0`、`Microsoft.WindowsDesktop.App`、`System.Windows.Forms.dll` 與 win-x64 9.0.8 runtime pack。因此可確認為 .NET 9 Windows Forms 自包含封裝。此處沒有對原版組件進行完整 IL 還原，不能宣稱掌握其全部內部實作。

把原版 EXE、Lua 設定與 JSON 配置複製到隔離資料夾後，可啟動其視窗；以 Windows UI Automation 讀取控制項。原版六個分頁為「常用、戰鬥、安全、進階、名單、Profiles」。以下 `Confirmed` 只表示畫面上存在該操作，不代表已驗證原版實際寫入效果。

| 功能 | 原版證據等級 | v2 狀態 | 說明 |
|---|---|---|---|
| 生命體選擇 | Confirmed | 完成 | 原本四種與五種 S 型各自讀寫、切換前確認；S 型為 v2 擴充。 |
| 設定檔資料夾選擇／重新載入 | Confirmed | 部分完成 | v2 可選擇另一份 `homunculus_config.lua`；目前沒有原版下拉式最近資料夾清單。 |
| 常用距離與時間參數 | Confirmed | 完成 | 跟隨、移動、搜尋、哨戒、ASPD、SP、Dance 延遲。 |
| 戰鬥模式與反擊 | Confirmed | 完成 | 原版可見的模式與開關均納入可編輯欄位。 |
| 目標排除與優先權 | Confirmed | 完成 | `priorityTargets` 註解矛盾，v2 保留原始 0/1 語意。 |
| 黑名單開關 | Confirmed | 完成 | 與 Lua 欄位逐項對應；未直接引用的欄位不保證遊戲效果。 |
| 卡住偵測、移動恢復 | Confirmed | 完成 | 原版進階頁所見標量值納入。 |
| 舞步設定 | Confirmed | 完成 | 限有欄位的麗芙與巴尼米樂斯；小數節拍按 0.01 精度保存。 |
| 快捷鍵開關 | Confirmed | 完成 | `HotkeySettings` 巢狀表內 8 個欄位。 |
| 朋友與黑名單 ID 增刪 | Confirmed | 完成 | 兩個 Lua 名單檔；讀取、增刪、清空、儲存、重新載入。 |
| 自訂配置 1～3 | Confirmed | 完成 | 載入、命名、儲存、套用、刪除與重新載入均可操作；原版內部 JSON 寫入細節未還原。 |
| 儲存前備份 | Confirmed | 完成 | Lua、JSON、名單有實際變更時使用 `.manager.bak`。 |
| 還原上一次 Lua 備份 | Unsupported | 完成 | v2 新增；原版畫面沒有找到此按鈕。 |
| 自動技能設定 | Unverified | 完成目前技能表的開關、等級與延遲編輯 | v2 可逐一設定九種生命體的 `SkillConfig.enabled`；五種 S 型提供本專案 SkillData 支援的 26 項技能，均預設關閉自動施放；遊戲內效果仍待驗證。 |
| 設定匯入與匯出 | Unsupported | 未完成 | 原版可見控制項中未找到獨立匯入／匯出；不要以此宣稱原版絕對沒有隱藏功能。 |
| 名單或技能的遊戲內生效 | Unverified | 無法驗證 | 自動測試只驗證檔案讀寫，未在 RO 遊戲內測試。 |

## Lua 有效性邊界

`Main.lua` 呼叫 `LoadHomConfig`，後者依生命體型別把配置放入全域變數。實際執行的 `Main.lua` 現在會先查 `_lvN` 專用鍵，再查不帶等級的技能鍵，並在自動施法時遵守 `SkillConfig.enabled`；`autoUseSkill_final.lua` 沒有由 AI 入口載入。完整欄位、目前預設值和靜態引用候選見 [SETTING_MAP.md](SETTING_MAP.md)。
