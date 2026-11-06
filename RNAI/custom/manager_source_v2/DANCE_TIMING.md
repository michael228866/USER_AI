# 跳舞攻擊排程

RO4 的 `SetupAspdTimings`、`GetDanceCell`、`ShouldHopDance`、`DoHopDance`、`AttackDanceTick` 及兩個戰鬥狀態的跳舞呼叫，已依照 `D:\RO3\AI\USER_AI\RNAI\Main.lua` 還原。一般攻擊後才執行舞步；舞步內沿用 RO3 的移動、再嘗試攻擊順序與左右選格。`CanAttackNow` 也沿用 RO3 的上次攻擊時間節流。

RO3 原版舞步只讀取 `ASPD` 與 `DanceEnabled`，不讀取 `PlayerASPD`。RO4 目前與原版相同；設定器內的 `PlayerASPD` 不控制舞步，使用時須以實際 `ASPD`、`DanceEnabled` 值為準。舊的 `DanceAttackMode` 已從設定檔與設定器移除。

目前飛里樂由 `HomCfg.lua` 的 `ASPD=193` 決定攻擊週期，並使用 `DancePhase=0.4`、`MoveDelay=150`。`DanceEnabled` 是唯一的舞步開關，開啟時一律使用 Hop 舞步（跳出去、普攻、跳回來）。RO3 公式為：

- `AttackPeriodMs = max(140, (200 - ASPD) × 20)`
- `DanceIntervalMs = floor(AttackPeriodMs × DancePhase)`
- 若 `MoveDelay` 已設定，保留該值；否則使用 `max(40, floor(DanceIntervalMs × 0.8))`。

| 輸入 ASPD | 理論攻擊週期 | 舞步間隔 | 移動節流 |
| ---: | ---: | ---: | ---: |
| 190 | 200 ms | 80 ms | 150 ms |
| 193 | 140 ms | 56 ms | 150 ms |

