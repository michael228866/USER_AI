# 跳舞攻擊排程

RO4 的 `SetupAspdTimings`、`GetDanceCell`、`ShouldHopDance`、`DoHopDance`、`AttackDanceTick` 及兩個戰鬥狀態的跳舞呼叫，已依照 `D:\RO3\AI\USER_AI\RNAI\Main.lua` 還原。一般攻擊後才執行舞步；舞步內沿用 RO3 的移動、再嘗試攻擊順序與左右選格。`CanAttackNow` 也沿用 RO3 的上次攻擊時間節流。

RO3 原版舞步只讀取 `ASPD` 與 `DanceEnabled`，不讀取 `PlayerASPD` 或 `DanceAttackMode`。RO4 目前與原版相同；設定器內這兩個舊欄位不再控制舞步，使用時須以實際 `ASPD`、`DanceEnabled` 值為準。

目前飛里樂由 `HomCfg.lua` 的 `ASPD=193` 決定攻擊週期，並使用 `DancePhase=0.4`、`MoveDelay=150`、`UseHopDance=true`。RO3 飛里樂未覆蓋 `UseHopDance`，會繼承 `HomCfg.lua` 的 `true`；RO4 已恢復相同的有效值。RO3 公式為：

- `AttackPeriodMs = max(140, (200 - ASPD) × 20)`
- `DanceIntervalMs = floor(AttackPeriodMs × DancePhase)`
- 若 `MoveDelay` 已設定，保留該值；否則使用 `max(40, floor(DanceIntervalMs × 0.8))`。

| 輸入 ASPD | 理論攻擊週期 | 舞步間隔 | 移動節流 |
| ---: | ---: | ---: | ---: |
| 190 | 200 ms | 80 ms | 150 ms |
| 193 | 140 ms | 56 ms | 150 ms |

`tests/dance_timing_mock.ps1` 比對本機 RO3 舞步函式，並模擬 ASPD 185～193 的計時器。模擬結果是命令排程，不代表伺服器實際命中、傷害或攻速。需要遊戲內 Log 與傷害資料才能驗證體感速度。
