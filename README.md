# 生命體 AI

## 更新

設定器啟動時會在右上角檢查 [GitHub Releases](https://github.com/michael228866/USER_AI/releases/latest)。顯示「有新版可更新」時點擊按鈕，下載 `USER_AI_update.zip`，關閉遊戲與設定器後解壓縮並合併到現有 `USER_AI` 資料夾。更新包不含 `RNAI/custom/` 的生命體設定、名單、Profiles 或 AI Log，不會覆蓋個人資料。首次使用請下載 `USER_AI_full.zip`。

GitHub `main` 分支更新後，Actions 會自動建置 Windows x64 單檔 EXE，執行設定檢查與自測，並發布上述兩種 ZIP。若 GitHub 尚無 Release、網路不可用或更新包缺失，設定器保持可用，按右上角按鈕可重試。

將整個 `USER_AI` 資料夾放在遊戲的 `AI` 資料夾內。執行本目錄的 `生命體AI管理器.exe` 設定生命體；修改後按「寫入 Lua 設定」，並在遊戲中重新載入 AI。

設定器是內含 .NET Runtime 的 Windows x64 單一 EXE，不需要另外安裝 .NET 或複製 DLL。生命體設定與名單位於 `RNAI/custom/`，不要把它們移到 EXE 旁邊。

管理器原始碼、建置方式及測試說明見 [RNAI/custom/manager_source_v2/README.md](RNAI/custom/manager_source_v2/README.md)。
