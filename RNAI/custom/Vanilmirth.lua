FollowDis  = 3  or 3 --生命體跟隨距離
MoveDelay  = MoveDelay  or 150 --移動後延遲敏感度
RadiusAggr = 10 -- 搜索範圍

AutoReturnDelay = AutoReturnDelay or 800  --ALT+右鍵移動後自動返回主人的延遲時間(毫秒) 已優化至1.5秒
-- 已知問題：設定太短會影響範圍添加白名單功能

-- 協同作戰設定 (預設被動協同模式)
AttackAllEnabled      = 1      --總開關，0=跟隨模式，1=開啟攻擊
AI_CROWD_ATTACK_PLAYERS = 1    -- WoE player scan: 1=enable lightweight player targets in castle mode
AI_WOE_PLAYER_SCAN_ONLY = true  -- only castle maps/manual WoE mode can scan players
AI_WOE_PLAYER_SCAN_ENABLED = 1 -- fallback manual switch when map API is unavailable
AttackMonstersEnabled = 1      --目標是否包含一般怪物，0=不攻擊怪物，1=攻擊怪物
CooperativeMode = CooperativeMode or 0--協同作戰模式 (ALT+T雙擊切換)
-- 模式說明：
-- 0 = 被動協同：主人造成傷害後才攻擊，遵守所有過濾開關
-- 1 = 主動攻擊：自主搜索範圍內敵人，遵守所有過濾開關
-- 2 = 目標鎖定模式：只攻擊主人正在攻擊的目標，無視所有過濾開關
--  • 主人鎖定目標→立即攻擊
--  • 主人切換目標→立即切換攻擊對象
--  • 擊殺後→返回跟隨狀態
--  • 無視玩家設定是否攻擊某物及黑白名單，只要主人攻擊就會攻擊
-- 3 = 內掛鎖定模式：同步主人鎖定 + 無鎖定時自動搜尋 (適合內掛)
--  • 主人鎖定目標→立即攻擊並持續同步
--  • 主人切換目標→戰鬥中也能即時切換
--  • 主人無鎖定→自動搜尋攻擊

-- ALT+T 快捷鍵切換目標模式設定
AltT_ToggleMode = 1  -- ALT+T切換到的模式：0=被動, 1=主動, 2=目標鎖定, 3=內掛鎖定
-- 說明：預設模式由上方 CooperativeMode 決定，此參數設定 ALT+T 要切換到哪個模式


-- 哨兵模式設定 (固定點攻擊周遭敵人)
SentryMode = SentryMode or 0--哨兵模式，0=關閉，1=開啟 (開啟後ALT+右鍵移動時記錄哨兵點)
SentryRadius    = 13                      --哨兵搜索範圍，哨兵點周圍多少格內的敵人會被攻擊

-- 反擊設定
AutoCounterAttack = AutoCounterAttack or 0--自動反擊開關，0=不主動反擊，1=被攻擊時自動反擊

-- 黑名單系統設定
AutoBlacklist = 1-- 自動黑名單：0=關閉，1=開啟 (攻擊您時自動加入黑名單)
BlacklistPriority = 1-- 優先攻擊黑名單：0=關閉，1=開啟 (黑名單玩家有更高攻擊優先級)
BlacklistOnly = 0-- 只攻擊黑名單：0=關閉，1=只攻擊黑名單內的敵人

-- 技能控制設定
SkillMode = 1-- 技能使用模式: 1=使用技能, 2=技能只攻擊玩家, 3=不使用技能
SkillMinSP = 0-- 自動技能最低SP；0=不限制，低於此SP時不施放技能
-- 說明: 控制生命體是否使用技能，支援主動/被動/哨兵/反擊所有模式

-- Dance Attack 設定
DanceAttackMode = 0  -- 跳舞攻擊模式：0=關閉, 1=開啟
PlayerASPD = 190     -- 玩家ASPD值（必填）
DanceMinSP = 0       -- 最小SP需求：僅在SP >= 此值時執行dance
DanceActivationDelay = 500  -- 載入後延遲(ms)：召喚/重登x豪秒後才啟用Dance（防止過圖斷線）
DanceCombatDelay = 100      -- 戰鬥延遲(ms)：鎖定目標x毫秒後才啟用Dance（確保戰鬥穩定）

-- 連續戰鬥模式（三段式整合開關）
ContinuousAttackMode = 2  -- 0=標準模式, 1=連續戰鬥, 2=激進模式
-- 模式說明：
-- 0 = 標準模式：保守型，生命體與主人>9格強制返回，擊殺後返回主人（適合PVP/危險區域）
-- 1 = 連續戰鬥：平衡型，生命體與主人>13格強制返回，擊殺後繼續搜尋（適合一般練功）
-- 2 = 激進模式：效率型，無距離強制返回，擊殺後繼續搜尋（適合密集怪區/主人安全）


--【ASPD 參考表】基於RO公式：每秒攻擊次數 = 50 / (200 - ASPD)
-- ASPD 171: 1.7次/秒,  攻擊間隔580ms, Dance間隔145ms (當前設定)
-- ASPD 175: 2.0次/秒,  攻擊間隔500ms, Dance間隔125ms
-- ASPD 180: 2.5次/秒,  攻擊間隔400ms, Dance間隔100ms
-- ASPD 185: 3.3次/秒,  攻擊間隔300ms, Dance間隔75ms
-- ASPD 190: 5.0次/秒,  攻擊間隔200ms, Dance間隔50ms
-- ASPD 193: 7.1次/秒,  攻擊間隔140ms, Dance間隔35ms
-- ASPD 195: 10次/秒,   攻擊間隔100ms, Dance間隔25ms

-- 快捷鍵設定（符合實際功能）
HotkeySettings = {
  -- 模式切換類
  ALT_T_ModeSwitch = 1,             -- ALT+T雙擊：切換主動/被動協同模式（500ms內雙擊）

  -- 移動與定位類
  ALT_RightClick_Move = 1,          -- ALT+右鍵單擊地面：移動指令或設定哨兵點

  -- 朋友管理類（矩形框選）
  ALT_RightClick_FirstPoint = 1,    -- ALT+右鍵：設定矩形框選第一個角點
  ALT_SHIFT_SecondPoint = 1,        -- ALT+SHIFT+右鍵：設定第二個角點，完成矩形框選添加

  -- 朋友管理類（全視野添加）
  ALT_SHIFT_FullView_FirstPoint = 1,  -- ALT+SHIFT+右鍵第一次點地面：設定全視野添加第一個點
  ALT_SHIFT_FullView_SecondPoint = 1, -- ALT+SHIFT+右鍵第二次點地面：完成全視野範圍添加（無上限）

  -- 朋友管理類（單個操作）
  ALT_SHIFT_SingleAdd = 1,          -- ALT+SHIFT+右鍵點擊玩家：添加單個朋友
  -- ALT_SHIFT_ClearAll = 0,        -- 已移除：ALT+SHIFT+右鍵點擊生命體：清空所有朋友列表

  -- 反擊模式
  ALT_SHIFT_CounterAttack = 1       -- ALT+SHIFT+右鍵點擊主人：切換自動反擊模式開關
}

AttackAllExclude = {
  homunculus = 0,  --是否將敵方生命體排除在外，0=會打敵方生命體，1=不打敵方生命體
  mercenary  = 1,  --是否將敵方傭兵排除在外，0=會打敵方傭兵，1=不打敵方傭兵
  friends    = 1,  --是否將朋友排除在外，0=會打朋友，1=不打朋友
  players    = 1,   --是否將人類玩家排除在外，0=會打人類玩家，1=不打人類玩家
  plants     = 0,  --植物與草，0=會打植物與菇，1=不會打植物與菇，2=不打苗娃但打其他植物菇類
  priorityTargets = 0, --副本特殊怪物，0=優先攻擊，1=關閉
  smartAttackOrder = 0, --主動模式專用智能攻擊順序，0=關閉，1=啟用
  emperium = 1  --華麗金屬(ID:1288)，0=不攻擊華麗金屬，1=最高優先攻擊(鎖定不轉移)
}

-- 狀態卡住偵測與恢復設定
-- 🔧 調整為後備機制：大幅延長超時時間，僅處理極端卡住情況
StuckDetectionEnabled = 1  -- 卡住偵測：0=關閉, 1=開啟
StuckTimeoutMs = 5000  -- 卡住超時時間(毫秒)，15秒後才判定為真正卡住

-- 🚧 早期卡住預警與脫困機制
StuckEarlyWarningTime = 2500  -- 早期預警時間(毫秒)，2秒後開始嘗試脫困移動
StuckRecoveryAttemptInterval = 1000  -- 脫困嘗試間隔(毫秒)，避免頻繁移動
StuckRecoveryMode = 0  --0=隨機移動3格, 1=返回主人旁邊

-- 主動模式定期刷新設定
ActiveModeRefreshInterval = 500  -- 主動模式刷新間隔(毫秒)，預設500ms
-- 說明：主動模式下每隔此時間會強制刷新目標列表，確保臨時出現的怪物能被即時發現

-- 智慧路徑移動優化設定
SmartMoveEnabled = 1             -- 智慧移動：0=關閉, 1=開啟
SmartMoveStuckThreshold = 600    -- 卡住判定時間(毫秒)，600ms內位置變化<2格視為卡住
SmartMoveBypassDistance = 3      -- 初始繞路距離(格)，首次繞路時的偏移距離
SmartMoveMaxBypassDistance = 8   -- 最大繞路距離(格)，多次卡住後最大偏移量
SmartMoveTimeout = 5000          -- 移動超時時間(毫秒)，超時後放棄目標

-- 攻擊超時防卡機制（後備）
-- 🔧 調整為後備機制：大幅延長超時時間，避免干擾正常戰鬥
AttackTimeoutEnabled =1-- 攻擊超時檢測：0=關閉, 1=開啟
AttackTimeoutMs =4000  -- 攻擊超時時間(毫秒)

--========================================
-- 🚧 臨時跳過目標配置（智能隔牆處理）
--========================================
-- 臨時跳過時間：無法接近的目標會被臨時跳過，等待此時間後重試
-- 說明：目標會先經過智慧移動嘗試繞路（5-8秒，6-8次繞路），
--       智慧移動超時後才會被加入臨時跳過列表
TemporarySkipDuration = TemporarySkipDuration or 2000  -- 跳過2秒後重試

--========================================
--【完整快捷鍵操作說明】
--========================================

--【戰鬥模式切換】
-- ALT+T (雙擊) = 切換主動/被動協同模式
--   被動模式(0)：僅在主人攻擊時協助攻擊
--   主動模式(1)：主動搜尋並攻擊範圍內敵人

--【反擊模式開關】
-- ALT+SHIFT+右鍵點擊主人 = 切換自動反擊模式
--   開啟(1)：被攻擊時自動反擊
--   關閉(0)：被攻擊時不主動反擊

--【哨兵模式】
-- ALT+右鍵點擊地面 = 設定哨兵點或移動指令
--   哨兵模式開啟時：設定固定防禦點，自動切換為主動攻擊模式
--   哨兵模式關閉時：普通移動指令，會在延遲後自動返回主人身邊

--【白名單/朋友列表管理】
-- ALT+SHIFT+右鍵點擊玩家 = 添加單個朋友到白名單
-- ALT+SHIFT+右鍵點擊生命體(單擊) = 已移除此功能
-- ALT+SHIFT+右鍵點擊地面兩次 = 添加全視野內所有玩家為朋友（無上限）

--【範圍選擇添加朋友】
-- 方式一：矩形範圍選擇
--   步驟1: ALT+右鍵點擊地面(設定第一個角點)
--   步驟2: ALT+SHIFT+右鍵點擊地面(設定第二個角點)
--   結果：選取矩形範圍內所有玩家加為朋友
--
-- 方式二：全視野範圍添加
--   操作: ALT+SHIFT+右鍵點擊地面兩次
--   結果：自動將全視野內所有玩家加為朋友（無上限）

--========================================

-- 打架用智能攻擊順序說明
-- 會優先鎖定特定職業，不論目前正打誰，只要清單敵人出現會優先鎖定
-- 巫師>舞娘>鐵匠>詩人>其他我自行決定

-- 技能細節設定(若有特殊需求再進行更改，例如:手感問題) --
Skill = {}

-- BUFF技能: 混亂治療 (自身BUFF) - 最高優先級
Skill[#Skill+1] = {
  id=8014, lv=4, target=2, when=1, times=1, delay=300000,  --3秒CD
  sp={0,100}, spAbsMin=40, range=0,
  selfHealCheck=true,  -- HP低於50%時才施放
  nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
  chase=0, stemp=0, count=0,
  buffSkill=true,
  priority=10  -- BUFF最高優先級
}

-- 統一技能設定 - 適用於所有情況(反擊/保護主人/主動攻擊)
Skill[#Skill+1] = {
  id=8013, lv=5, target=0, when=1, times=1, delay=750,
  sp={30,100}, spAbsMin=30, range=9, --SP需高於30%才使用，預留SP空間給混亂治療
  nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
  chase=1, stemp=0, count=0,
  priority=100
}

-- 普通攻擊 (最後選項，無SP限制) - 最低優先權
Skill[#Skill+1] = {
  id=0, lv=1, target=0, when=1, times=1, delay=0,
  sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
  chase=1, stemp=0, count=0,
  priority=10  -- 最低優先級
}

--MH

SmartAttackPriorityOrder = SmartAttackPriorityOrder or {}
