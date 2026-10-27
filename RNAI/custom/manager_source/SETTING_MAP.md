# 欄位對照

`LoadHomConfig` 會從 `HomConfig[生命體]` 將欄位載入 Lua 全域值。此表列出目前 GUI 可編輯的 **21 個欄位**；它不表示每個欄位都被 AI 主流程直接引用。`SkillConfig`、`Skill` 和其他巢狀設定目前唯讀。

| GUI 分頁 | Lua Key | 控制項 / 原始值 | 自訂配置群組 |
| --- | --- | --- | --- |
| 戰鬥行為 | `AttackMonstersEnabled` | 開關，0/1 | `exclude` |
| 戰鬥行為 | `CooperativeMode` | 0 被動、1 主動、2 鎖定主人、3 內掛鎖定 | `combat` |
| 戰鬥行為 | `ContinuousAttackMode` | 0 標準、1 連續、2 激進 | `combat` |
| 戰鬥行為 | `AutoCounterAttack` | 開關，0/1 | `combat` |
| 戰鬥行為 | `DanceAttackMode` | 開關，0/1 | `combat` |
| 戰鬥行為 | `FollowMode` | 原始模式 0/1/2，原 Lua 未說明語意 | `combat` |
| 戰鬥行為 | `SentryMode` | 開關，0/1 | `combat` |
| 目標過濾 | `AttackAllExclude.players` | 0 允許、1 排除 | `exclude` |
| 目標過濾 | `AttackAllExclude.friends` | 0 允許、1 排除 | `exclude` |
| 目標過濾 | `AttackAllExclude.homunculus` | 0 允許、1 排除 | `exclude` |
| 目標過濾 | `AttackAllExclude.plants` | 0 攻擊、1 排除、2 排除苗娃 | `exclude` |
| 目標過濾 | `AttackAllExclude.emperium` | 0 不攻擊、1 最高優先 | `exclude` |
| 目標過濾 | `AttackAllExclude.priorityTargets` | 原始值 0/1；原 Lua 註解矛盾 | `exclude` |
| 目標過濾 | `AttackAllExclude.smartAttackOrder` | 0 關閉、1 開啟 | `exclude` |
| 距離與時間 | `RadiusAggr` | 數值 0–100 格 | `parameters` |
| 距離與時間 | `FollowDis` | 數值 0–30 格 | `parameters` |
| 距離與時間 | `SentryRadius` | 數值 0–100 格 | `parameters` |
| 距離與時間 | `PlayerASPD` | 數值 0–200 ASPD | `parameters` |
| 距離與時間 | `AttackTimeoutMs` | Lua 0–60000 毫秒，JSON 秒 | `parameters` |
| 距離與時間 | `AltT_ToggleMode` | 0 被動、1 主動、2 鎖定主人、3 內掛鎖定 | `parameters` |
| 技能管理 | `SkillMinSP` | 數值 0–100000 SP | `parameters` |

以上範圍延續原設定器支援範圍，並依現有 Lua 註解顯示已明確記載的模式名稱。若原註解不明確，GUI 保留原始數值，不自行賦予新語意。每個生命體有獨立區塊；只寫入目前生命體、且只寫入已修改的欄位。
