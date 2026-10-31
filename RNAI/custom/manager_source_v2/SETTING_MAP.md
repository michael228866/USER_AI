# Lua 設定欄位對照表

從目前 `homunculus_config.lua` 擷取預設值。L/A/F/V 分別代表麗芙、阿密斯特爾、飛里樂、巴尼米樂斯；`—` 表示該生命體沒有此欄位。

「程式碼引用候選」只表示排除設定檔後，在 Lua 非註解行找到同名文字；動態載入與遊戲內效果仍需實機驗證。`LoadHomConfig` 會把每種生命體配置載入 `_G`。

| Setting Key | 生命體預設 L/A/F/V | 功能說明 | 資料類型 | UI 元件 | 是否實際引用 |
|---|---|---|---|---|---|
| `AttackMonstersEnabled` | 1/1/1/1 | 是否把一般怪物列入攻擊目標。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `CooperativeMode` | 1/1/1/0 | 決定生命體如何配合主人選擇攻擊目標。 | Enum / 0-1 | Segmented | 僅確認載入，未見直接引用 |
| `ContinuousAttackMode` | 2/0/0/2 | 控制擊殺後是否繼續搜尋，以及返回主人的距離。 | Enum / 0-1 | Segmented | 僅確認載入，未見直接引用 |
| `AutoCounterAttack` | 1/1/1/0 | 受到攻擊時是否反擊。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `FollowMode` | 0/0/0/0 | 原 Lua 未說明各數值的行為；保留原始數值。 | Enum / 0-1 | Segmented | 僅確認載入，未見直接引用 |
| `SentryMode` | 0/0/0/0 | 是否使用哨戒點周圍的搜尋行為。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `players` | 1/1/1/1 | 1 代表不攻擊玩家；0 代表允許。 | Enum / 0-1 | Toggle | 程式碼引用候選 |
| `friends` | 1/1/1/1 | 避免把好友清單中的對象當作目標。 | Enum / 0-1 | Toggle | 程式碼引用候選 |
| `homunculus` | 0/1/1/0 | 是否排除敵方生命體。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `plants` | 2/2/2/0 | 植物和菇類的攻擊規則。 | Enum / 0-1 | Segmented | 程式碼引用候選 |
| `emperium` | 1/0/0/1 | 設定檔中的華麗金屬目標規則。 | Enum / 0-1 | Segmented | 僅確認載入，未見直接引用 |
| `priorityTargets` | 1/0/0/0 | 原 Lua 對 0/1 的註解互相矛盾，僅顯示原始值。 | Enum / 0-1 | Segmented | 僅確認載入，未見直接引用 |
| `smartAttackOrder` | 1/0/0/0 | 主動模式的目標排序設定。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `RadiusAggr` | 11/11/11/20 | 搜尋目標的範圍。 | Number | Numeric Input | 程式碼引用候選 |
| `FollowDis` | 3/3/3/1 | 生命體與主人保持的距離。 | Number | Numeric Input | 程式碼引用候選 |
| `SentryRadius` | 8/8/8/13 | 哨戒點周圍的搜尋範圍。 | Number | Numeric Input | 僅確認載入，未見直接引用 |
| `PlayerASPD` | 188/188/188/190 | 手動輸入生命體攻速；唯一主要節奏來源。舊 ASPD 欄位只作為缺少此值時的相容備援。 | Number | Numeric Input | 程式碼引用候選 |
| `AttackTimeoutMs` | 1000/1000/1000/3000 | 攻擊卡住後放棄目標的時間；Lua 使用毫秒。 | Number | Numeric Input | 僅確認載入，未見直接引用 |
| `AltT_ToggleMode` | 1/0/0/1 | 按快捷鍵後切換到的協同作戰模式。 | Enum / 0-1 | Segmented | 僅確認載入，未見直接引用 |
| `SkillMinSP` | 0/0/0/0 | 低於此 SP 時不自動施放技能；0 表示不限制。 | Number | Numeric Input | 程式碼引用候選 |
| `MoveDelay` | 150/150/150/200 | 兩次移動指令間的間隔。 | Number | Numeric Input | 程式碼引用候選 |
| `AutoReturnDelay` | 800/800/800/1000 | 指定移動後返回主人旁邊的等待時間。 | Number | Numeric Input | 僅確認載入，未見直接引用 |
| `DanceMinSP` | 0/0/0/0 | 低於此 SP 時停止走位攻擊。 | Number | Numeric Input | 程式碼引用候選 |
| `DanceActivationDelay` | 500/500/500/500 | 載入後等待走位攻擊啟動。 | Number | Numeric Input | 程式碼引用候選 |
| `DanceCombatDelay` | 400/400/400/100 | 鎖定目標後等待走位攻擊。 | Number | Numeric Input | 程式碼引用候選 |
| `MVPMode` | 1/0/0/0 | 原版提供此開關；目前未確認消費程式，請以 Lua 原值為準。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `SkillMode` | 1/1/1/1 | 1 正常；2 僅玩家；3 不用技能。 | Enum / 0-1 | Segmented | 程式碼引用候選 |
| `AutoBlacklist` | 0/0/0/1 | 受攻擊時加入黑名單。 | Enum / 0-1 | Toggle | 程式碼引用候選 |
| `BlacklistPriority` | 0/0/0/1 | 原版提供此開關；實際目標排序依 Lua 邏輯。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `BlacklistOnly` | 0/0/0/0 | 原版提供此開關；實際目標過濾依 Lua 邏輯。 | Enum / 0-1 | Toggle | 程式碼引用候選 |
| `mercenary` | 0/1/1/1 | 目標排除表中的傭兵值。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `ALT_RightClick_Move` | 1/1/1/1 | 控制 Alt 右鍵移動快捷鍵。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `ALT_RightClick_FirstPoint` | 1/1/1/1 | 控制第一點快捷鍵。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `ALT_SHIFT_FullView_FirstPoint` | 1/1/1/1 | 控制全視野加入第一點。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `ALT_SHIFT_SingleAdd` | 1/1/1/1 | 控制加入單一好友快捷鍵。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `ALT_SHIFT_CounterAttack` | 1/0/0/0 | 控制切換反擊快捷鍵。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `StuckDetectionEnabled` | 1/1/1/1 | 移動卡住時嘗試恢復。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `StuckTimeoutMs` | 4000/5000/5000/1500 | 判定卡住的等待時間。 | Number | Numeric Input | 僅確認載入，未見直接引用 |
| `StuckEarlyWarningTime` | 2000/2500/2500/500 | 提前嘗試修正移動。 | Number | Numeric Input | 僅確認載入，未見直接引用 |
| `StuckRecoveryAttemptInterval` | 700/1000/1000/1000 | 兩次脫困嘗試間的時間。 | Number | Numeric Input | 僅確認載入，未見直接引用 |
| `StuckRecoveryMode` | 0/0/0/0 | 0 隨機移動；1 返回主人。 | Enum / 0-1 | Segmented | 僅確認載入，未見直接引用 |
| `ActiveModeRefreshInterval` | 400/500/500/500 | 重新搜尋目標的週期。 | Number | Numeric Input | 僅確認載入，未見直接引用 |
| `SmartMoveEnabled` | 1/1/1/1 | 啟用繞路與卡點處理。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `SmartMoveStuckThreshold` | 800/1000/1000/500 | 移動位置未變的判定時間。 | Number | Numeric Input | 僅確認載入，未見直接引用 |
| `SmartMoveBypassDistance` | 2/3/3/2 | 首次繞路偏移格數。 | Number | Numeric Input | 僅確認載入，未見直接引用 |
| `SmartMoveMaxBypassDistance` | 8/8/8/13 | 多次卡住後的最大偏移。 | Number | Numeric Input | 僅確認載入，未見直接引用 |
| `SmartMoveTimeout` | 3000/5000/5000/2000 | 繞路失敗後放棄目標的時間。 | Number | Numeric Input | 僅確認載入，未見直接引用 |
| `AttackTimeoutEnabled` | 1/1/1/1 | 攻擊過久時放棄目標。 | Enum / 0-1 | Toggle | 僅確認載入，未見直接引用 |
| `TemporarySkipDuration` | 1500/3000/3000/2000 | 放棄目標後重新鎖定前的等待時間。 | Number | Numeric Input | 僅確認載入，未見直接引用 |
| `DanceEnabled` | true/—/—/true | 舞步系統總開關；Lua true/false。 | Boolean | Toggle | 程式碼引用候選 |
| `ASPD` | —/—/—/— | 舞步節奏使用的攻速。 | Number | Numeric Input | 程式碼引用候選 |
| `DancePhase` | 0.4/0.25/0.25/0.2 | 輸入 0.25 代表 25%；無設定時預設 0.25。這不是已驗證的最佳值。 | Number | Numeric Input | 程式碼引用候選 |
| `DanceAttack` | true/—/—/true | 水平左右移動攻擊；Lua true/false。 | Boolean | Toggle | 程式碼引用候選 |
| `UseHopDance` | false/—/—/true | 靠近、普攻、回位；Lua true/false。 | Boolean | Toggle | 程式碼引用候選 |
| `SkillThrottleMs` | 90/—/—/90 | 最短技能施放間隔。 | Number | Numeric Input | 程式碼引用候選 |
