-- ============================================
-- 統一生命體配置檔 (Unified Homunculus Config)
-- ============================================
-- 更新時間: 2025-10-13
-- 版本: 8.1.0
-- 說明: 四種一般生命體與五種生命體 S 各自使用獨立配置

AttackAllEnabled = 1
AI_CROWD_ATTACK_PLAYERS = 1
AI_WOE_PLAYER_SCAN_ONLY = true
AI_WOE_PLAYER_SCAN_ENABLED = 1

HomConfig = {
  lif = {
    FollowDis = 3,
    FollowMode = 0,
    MoveDelay = 150,
    RadiusAggr = 11,
    AutoReturnDelay = 800,

    -- 協同作戰設定
    AttackMonstersEnabled = 1,
    MVPMode = 1,
    CooperativeMode = 1,
    AltT_ToggleMode = 1,

    -- 模式設定
    SentryMode = 0,
    SentryRadius = 8,
    AutoCounterAttack = 1,

    -- 黑名單系統設定
    AutoBlacklist = 0,
    BlacklistPriority = 0,
    BlacklistOnly = 0,

    -- 技能控制設定
    SkillMode = 1,
    SkillMinSP = 0,

    -- Dance Attack 設定
    DanceAttackMode = 0,
    PlayerASPD = 188,
    DanceMinSP = 0,
    DanceActivationDelay = 500,
    DanceCombatDelay = 400,

    -- 連續戰鬥模式
    ContinuousAttackMode = 2,

    -- 快捷鍵設定
    HotkeySettings = {
      ALT_T_ModeSwitch = 1,
      ALT_RightClick_Move = 1,
      ALT_RightClick_FirstPoint = 1,
      ALT_SHIFT_SecondPoint = 1,
      ALT_SHIFT_FullView_FirstPoint = 1,
      ALT_SHIFT_FullView_SecondPoint = 1,
      ALT_SHIFT_SingleAdd = 1,
      ALT_SHIFT_CounterAttack = 1,
    },

    -- 排除設定
    AttackAllExclude = {
      homunculus = 0,
      mercenary = 0,
      friends = 1,
      players = 1,
      plants = 2,
      priorityTargets = 1,  -- 啟用副本優先攻擊
      smartAttackOrder = 1,
      emperium = 1,
    },

    -- 狀態卡住偵測與恢復設定
    StuckDetectionEnabled = 1,
    StuckTimeoutMs = 4000,
    StuckEarlyWarningTime = 2000,
    StuckRecoveryAttemptInterval = 700,
    StuckRecoveryMode = 0,

    -- 主動模式定期刷新設定
    ActiveModeRefreshInterval = 400,

    -- 智慧路徑移動優化設定
    SmartMoveEnabled = 1,
    SmartMoveStuckThreshold = 800,
    SmartMoveBypassDistance = 2,
    SmartMoveMaxBypassDistance = 8,
    SmartMoveTimeout = 3000,

    -- 攻擊超時防卡機制
    AttackTimeoutEnabled = 1,
    AttackTimeoutMs = 1000,

    -- 臨時跳過目標配置
    TemporarySkipDuration = 1500,  -- 放棄目標後3秒內不再鎖定

    -- 技能設定

    SkillConfig = {
    Skill_8001 = { enabled = true, selfHpThreshold = 50, ownerHpThreshold = 70, delay = 5000 },  -- 治癒之手
    Skill_8002 = { enabled = true, delay = 35000 },  -- 輕捷移動
    Skill_8203 = { enabled = true },
    Skill_8201 = { enabled = true },
    },

    SmartAttackPriorityOrder = {},

    Skill = {
      {
        id=8001, lv=1, target=1, when=1, times=1, delay=5000,
        sp={0,100}, spAbsMin=13, range=0,
        ownerHealCheck=true,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=0, stemp=0, count=0,
        buffSkill=true,
        priority=150
      },
      {
        id=8002, lv=1, target=2, when=1, times=1, delay=35000,
        sp={0,100}, range=0,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=0, stemp=0, count=0,
        buffSkill=true,
        priority=50
      },
      {
        id=8203, lv=1, target=0, when=1, times=1, delay=2000,
        sp={20,100}, range=9,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0,
        priority=100
      },
      {
        id=8201, lv=1, target=2, when=1, times=1, delay=60000,
        sp={0,100}, range=0,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=0, stemp=0, count=0,
        buffSkill=true,
        priority=50
      },
      {
        id=0, lv=1, target=0, when=1, times=1, delay=0,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0,
        priority=10
      }
    },

    -- 智能攻擊順序
    SmartAttackPriorityOrder = {},
    DanceEnabled = true,
    ASPD = 193,
    DancePhase = 0.4,
    DanceAttack = true,
    UseHopDance = false,
    SkillThrottleMs = 90,
  },

  amistr = {
    FollowDis = 3,
    FollowMode = 0,
    MoveDelay = 150,
    RadiusAggr = 11,
    AutoReturnDelay = 800,

    -- 協同作戰設定
    AttackMonstersEnabled = 1,
    MVPMode = 0,
    CooperativeMode = 1,
    AltT_ToggleMode = 0,

    -- 模式設定
    SentryMode = 0,
    SentryRadius = 8,
    AutoCounterAttack = 1,

    -- 黑名單系統設定
    AutoBlacklist = 0,
    BlacklistPriority = 0,
    BlacklistOnly = 0,

    -- 技能控制設定
    SkillMode = 1,
    SkillMinSP = 0,

    -- Dance Attack 設定
    DanceAttackMode = 0,
    PlayerASPD = 188,
    DancePhase = 0.25,
    UseHopDance = false,
    DanceMinSP = 0,
    DanceActivationDelay = 500,
    DanceCombatDelay = 400,

    -- 連續戰鬥模式
    ContinuousAttackMode = 0,

    -- 快捷鍵設定
    HotkeySettings = {
      ALT_T_ModeSwitch = 1,
      ALT_RightClick_Move = 1,
      ALT_RightClick_FirstPoint = 1,
      ALT_SHIFT_SecondPoint = 1,
      ALT_SHIFT_FullView_FirstPoint = 1,
      ALT_SHIFT_FullView_SecondPoint = 1,
      ALT_SHIFT_SingleAdd = 1,
      ALT_SHIFT_CounterAttack = 0
    },

    -- 排除設定
    AttackAllExclude = {
      homunculus = 1,
      mercenary = 1,
      friends = 1,
      players = 1,
      plants = 2,
      priorityTargets = 0,  -- 啟用副本優先攻擊
      smartAttackOrder = 0,
      emperium = 0
    },

    -- 狀態卡住偵測與恢復設定
    StuckDetectionEnabled = 1,
    StuckTimeoutMs = 5000,
    StuckEarlyWarningTime = 2500,
    StuckRecoveryAttemptInterval = 1000,
    StuckRecoveryMode = 0,

    -- 主動模式定期刷新設定
    ActiveModeRefreshInterval = 500,

    -- 智慧路徑移動優化設定
    SmartMoveEnabled = 1,
    SmartMoveStuckThreshold = 1000,
    SmartMoveBypassDistance = 3,
    SmartMoveMaxBypassDistance = 8,
    SmartMoveTimeout = 5000,

    -- 攻擊超時防卡機制
    AttackTimeoutEnabled = 1,
    AttackTimeoutMs = 1000,

    -- 臨時跳過目標配置
    TemporarySkipDuration = 3000,  -- 放棄目標後3秒內不再鎖定

    -- 技能設定

    SkillConfig = {
    Skill_8006 = { enabled = true, delay = 30000 },  -- 防禦力
    },

    SmartAttackPriorityOrder = {},

    Skill = {
      {
        id=8006, lv=1, target=2, when=1, times=1, delay=30000,
        sp={0,100}, range=0,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=0, stemp=0, count=0,
        buffSkill=true,
        priority=50
      },
      {
        id=0, lv=1, target=0, when=1, times=1, delay=0,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0,
        priority=10
      }
    },

    -- 智能攻擊順序
    SmartAttackPriorityOrder = {}
  },

  filir = {
    FollowDis = 3,
    FollowMode = 0,
    MoveDelay = 150,
    RadiusAggr = 11,
    AutoReturnDelay = 800,

    -- 協同作戰設定
    AttackMonstersEnabled = 1,
    MVPMode = 1,
    CooperativeMode = 1,
    AltT_ToggleMode = 0,

    -- 模式設定
    SentryMode = 0,
    SentryRadius = 8,
    AutoCounterAttack = 1,

    -- 黑名單系統設定
    AutoBlacklist = 0,
    BlacklistPriority = 0,
    BlacklistOnly = 0,

    -- 技能控制設定
    SkillMode = 1,
    SkillMinSP = 100,

    -- Dance Attack 設定
    DanceAttackMode = 1,
    PlayerASPD = 193,
    DancePhase = 0.4,
    UseHopDance = true,
    DanceMinSP = 0,
    DanceActivationDelay = 500,
    DanceCombatDelay = 400,

    -- 連續戰鬥模式
    ContinuousAttackMode = 1,

    -- 快捷鍵設定
    HotkeySettings = {
      ALT_T_ModeSwitch = 1,
      ALT_RightClick_Move = 1,
      ALT_RightClick_FirstPoint = 1,
      ALT_SHIFT_SecondPoint = 1,
      ALT_SHIFT_FullView_FirstPoint = 1,
      ALT_SHIFT_FullView_SecondPoint = 1,
      ALT_SHIFT_SingleAdd = 1,
      ALT_SHIFT_CounterAttack = 0
    },

    -- 排除設定
    AttackAllExclude = {
      homunculus = 1,
      mercenary = 1,
      friends = 1,
      players = 1,
      plants = 2,
      priorityTargets = 0,  -- 啟用副本優先攻擊
      smartAttackOrder = 1,
      emperium = 1
    },

    -- 狀態卡住偵測與恢復設定
    StuckDetectionEnabled = 1,
    StuckTimeoutMs = 5000,
    StuckEarlyWarningTime = 2500,
    StuckRecoveryAttemptInterval = 1000,
    StuckRecoveryMode = 0,

    -- 主動模式定期刷新設定
    ActiveModeRefreshInterval = 500,

    -- 智慧路徑移動優化設定
    SmartMoveEnabled = 1,
    SmartMoveStuckThreshold = 1000,
    SmartMoveBypassDistance = 3,
    SmartMoveMaxBypassDistance = 8,
    SmartMoveTimeout = 5000,

    -- 攻擊超時防卡機制
    AttackTimeoutEnabled = 1,
    AttackTimeoutMs = 1000,

    -- 臨時跳過目標配置
    TemporarySkipDuration = 3000,  -- 放棄目標後3秒內不再鎖定

    -- 技能設定

    SkillConfig = {
    Skill_8009 = { enabled = true, spMinPercent = 20, delay = 2000 },  -- 月光
    Skill_8010 = { enabled = true, delay = 60000 },  -- 橫越速度
    Skill_8011 = { enabled = true, delay = 60000 },  -- 緊急迴避
    },

    SmartAttackPriorityOrder = {},

    Skill = {
      {
        id=8009, lv=5, target=0, when=1, times=1, delay=10000,
        sp={20,100}, spAbsMin=20, range=2,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0,
        priority=80
      },
      {
        id=8010, lv=5, target=2, when=1, times=1, delay=120000,
        sp={0,100}, range=0,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=0, stemp=0, count=0,
        buffSkill=true,
        priority=50
      },
      {
        id=8011, lv=5, target=2, when=1, times=1, delay=120000,
        sp={0,100}, range=0,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=0, stemp=0, count=0,
        buffSkill=true,
        priority=50
      },
      {
        id=0, lv=1, target=0, when=1, times=1, delay=0,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0,
        priority=10
      }
    },

    -- 智能攻擊順序
    SmartAttackPriorityOrder = {}
  },

  Vanilmirth = {
    FollowDis = 1,
    FollowMode = 0,
    MoveDelay = 200,
    RadiusAggr = 20,
    AutoReturnDelay = 1000,

    -- 協同作戰設定
    AttackMonstersEnabled = 1,
    MVPMode = 0,
    CooperativeMode = 0,
    AltT_ToggleMode = 1,

    -- 模式設定
    SentryMode = 0,
    SentryRadius = 13,
    AutoCounterAttack = 0,

    -- 黑名單系統設定
    AutoBlacklist = 1,
    BlacklistPriority = 1,
    BlacklistOnly = 0,

    -- 技能控制設定
    SkillMode = 1,
    SkillMinSP = 0,

    -- Dance Attack 設定
    DanceAttackMode = 1,
    PlayerASPD = 190,
    DanceMinSP = 0,
    DanceActivationDelay = 500,
    DanceCombatDelay = 100,

    -- 連續戰鬥模式
    ContinuousAttackMode = 2,

    -- 快捷鍵設定
    HotkeySettings = {
      ALT_T_ModeSwitch = 1,
      ALT_RightClick_Move = 1,
      ALT_RightClick_FirstPoint = 1,
      ALT_SHIFT_SecondPoint = 1,
      ALT_SHIFT_FullView_FirstPoint = 1,
      ALT_SHIFT_FullView_SecondPoint = 1,
      ALT_SHIFT_SingleAdd = 1,
      ALT_SHIFT_CounterAttack = 0,
    },

    -- 排除設定
    AttackAllExclude = {
      homunculus = 0,
      mercenary = 1,
      friends = 1,
      players = 1,
      plants = 0,
      priorityTargets = 0,  -- 啟用副本優先攻擊
      smartAttackOrder = 1,
      emperium = 1,
    },

    -- 狀態卡住偵測與恢復設定
    StuckDetectionEnabled = 1,
    StuckTimeoutMs = 1500,
    StuckEarlyWarningTime = 500,
    StuckRecoveryAttemptInterval = 1000,
    StuckRecoveryMode = 0,

    -- 主動模式定期刷新設定
    ActiveModeRefreshInterval = 500,

    -- 智慧路徑移動優化設定
    SmartMoveEnabled = 1,
    SmartMoveStuckThreshold = 500,
    SmartMoveBypassDistance = 2,
    SmartMoveMaxBypassDistance = 13,
    SmartMoveTimeout = 2000,  -- 1.5秒超時（2倍速度放棄隔牆怪物）

    -- 攻擊超時防卡機制
    AttackTimeoutEnabled = 1,
    AttackTimeoutMs = 3000,

    -- 臨時跳過目標配置
    TemporarySkipDuration = 2000,  -- 放棄目標後3秒內不再鎖定

    -- 技能設定

    SkillConfig = {
    Skill_8014_lv3 = { enabled = false, ownerHpThreshold = 70, delay = 1000 },  -- 混亂祈福 lv3（檢查主人HP）
    Skill_8014_lv4 = { enabled = false, selfHpThreshold = 50, delay = 1500 },  -- 混亂祈福 lv4（檢查生命體HP）
    Skill_8013 = { enabled = true, spMinPercent = 35, delay = 3050 },  -- 善變
    Skill_8216 = { enabled = false },
    Skill_8217 = { enabled = false },
    },

    SmartAttackPriorityOrder = {},

    Skill = {{
        id=8013, lv=5, target=0, when=1, times=1, delay=3050,
        sp={35,100}, spAbsMin=30, range=9,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0,
        priority=100
      },{
        id=8216, lv=1, target=0, when=1, times=1, delay=1500,
        sp={20,100}, range=9,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0,
        priority=90
      },{
        id=8217, lv=1, target=0, when=1, times=1, delay=5000,
        sp={40,100}, range=9,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0,
        priority=80
      },{
        id=0, lv=1, target=0, when=1, times=1, delay=0,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0,
        priority=10
      }
    },

    -- 智能攻擊順序
    SmartAttackPriorityOrder = {},
    DanceEnabled = true,
    ASPD = 190,
    DancePhase = 0.2,
    DanceAttack = true,
    UseHopDance = false,
    SkillThrottleMs = 90,
  },

  -- 生命體 S：獨立設定；S 技能預設不自動施放。
  eira = {
    FollowDis = 3,
    FollowMode = 0,
    MoveDelay = 150,
    RadiusAggr = 11,
    AutoReturnDelay = 800,

    -- 協同作戰設定
    AttackMonstersEnabled = 1,
    MVPMode = 1,
    CooperativeMode = 1,
    AltT_ToggleMode = 1,

    -- 模式設定
    SentryMode = 0,
    SentryRadius = 8,
    AutoCounterAttack = 1,

    -- 黑名單系統設定
    AutoBlacklist = 0,
    BlacklistPriority = 0,
    BlacklistOnly = 0,

    -- 技能控制設定
    SkillMode = 1,
    SkillMinSP = 0,

    -- Dance Attack 設定
    DanceAttackMode = 0,
    DanceEnabled = false,
    DanceAttack = true,
    UseHopDance = false,
    ASPD = 190,
    AttackThrottleMs = 0,
    DancePhase = 0.4,
    SkillThrottleMs = 110,
    PlayerASPD = 190,
    DanceMinSP = 0,
    DanceActivationDelay = 500,
    DanceCombatDelay = 400,

    -- 連續戰鬥模式
    ContinuousAttackMode = 2,

    -- 快捷鍵設定
    HotkeySettings = {
      ALT_T_ModeSwitch = 1,
      ALT_RightClick_Move = 1,
      ALT_RightClick_FirstPoint = 1,
      ALT_SHIFT_SecondPoint = 1,
      ALT_SHIFT_FullView_FirstPoint = 1,
      ALT_SHIFT_FullView_SecondPoint = 1,
      ALT_SHIFT_SingleAdd = 1,
      ALT_SHIFT_CounterAttack = 1,
    },

    -- 排除設定
    AttackAllExclude = {
      homunculus = 0,
      mercenary = 0,
      friends = 1,
      players = 1,
      plants = 2,
      priorityTargets = 1,  -- 啟用副本優先攻擊
      smartAttackOrder = 1,
      emperium = 1,
    },

    -- 狀態卡住偵測與恢復設定
    StuckDetectionEnabled = 1,
    StuckTimeoutMs = 4000,
    StuckEarlyWarningTime = 2000,
    StuckRecoveryAttemptInterval = 700,
    StuckRecoveryMode = 0,

    -- 主動模式定期刷新設定
    ActiveModeRefreshInterval = 400,

    -- 智慧路徑移動優化設定
    SmartMoveEnabled = 1,
    SmartMoveStuckThreshold = 800,
    SmartMoveBypassDistance = 2,
    SmartMoveMaxBypassDistance = 8,
    SmartMoveTimeout = 3000,

    -- 攻擊超時防卡機制
    AttackTimeoutEnabled = 1,
    AttackTimeoutMs = 1000,

    -- 臨時跳過目標配置
    TemporarySkipDuration = 1500,  -- 放棄目標後3秒內不再鎖定

    -- 技能設定

    SkillConfig = {
      Skill_8024 = { enabled = false },
      Skill_8022 = { enabled = false },
      Skill_8023 = { enabled = false },
      Skill_8025 = { enabled = false },
      Skill_8026 = { enabled = false },
    },
    SmartAttackPriorityOrder = {},

    Skill = {{
        id=8024, lv=1, target=0, when=1, times=1, delay=5000,
        sp={0,100}, range=7,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=80
      },{
        id=8022, lv=1, target=2, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8023, lv=1, target=2, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8025, lv=1, target=0, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8026, lv=1, target=1, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=0, lv=1, target=0, when=1, times=1, delay=0,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=10
      }},
    -- 智能攻擊順序
    SmartAttackPriorityOrder = {},
  },
  bayeri = {
    FollowDis = 3,
    FollowMode = 0,
    MoveDelay = 150,
    RadiusAggr = 11,
    AutoReturnDelay = 800,

    -- 協同作戰設定
    AttackMonstersEnabled = 1,
    MVPMode = 0,
    CooperativeMode = 1,
    AltT_ToggleMode = 0,

    -- 模式設定
    SentryMode = 0,
    SentryRadius = 8,
    AutoCounterAttack = 1,

    -- 黑名單系統設定
    AutoBlacklist = 0,
    BlacklistPriority = 0,
    BlacklistOnly = 0,

    -- 技能控制設定
    SkillMode = 1,
    SkillMinSP = 0,

    -- Dance Attack 設定
    DanceAttackMode = 0,
    DanceEnabled = false,
    DanceAttack = true,
    UseHopDance = false,
    ASPD = 190,
    AttackThrottleMs = 0,
    DancePhase = 0.4,
    SkillThrottleMs = 110,
    PlayerASPD = 190,
    DanceMinSP = 0,
    DanceActivationDelay = 500,
    DanceCombatDelay = 400,

    -- 連續戰鬥模式
    ContinuousAttackMode = 0,

    -- 快捷鍵設定
    HotkeySettings = {
      ALT_T_ModeSwitch = 1,
      ALT_RightClick_Move = 1,
      ALT_RightClick_FirstPoint = 1,
      ALT_SHIFT_SecondPoint = 1,
      ALT_SHIFT_FullView_FirstPoint = 1,
      ALT_SHIFT_FullView_SecondPoint = 1,
      ALT_SHIFT_SingleAdd = 1,
      ALT_SHIFT_CounterAttack = 0
    },

    -- 排除設定
    AttackAllExclude = {
      homunculus = 1,
      mercenary = 1,
      friends = 1,
      players = 1,
      plants = 2,
      priorityTargets = 0,  -- 啟用副本優先攻擊
      smartAttackOrder = 0,
      emperium = 0
    },

    -- 狀態卡住偵測與恢復設定
    StuckDetectionEnabled = 1,
    StuckTimeoutMs = 5000,
    StuckEarlyWarningTime = 2500,
    StuckRecoveryAttemptInterval = 1000,
    StuckRecoveryMode = 0,

    -- 主動模式定期刷新設定
    ActiveModeRefreshInterval = 500,

    -- 智慧路徑移動優化設定
    SmartMoveEnabled = 1,
    SmartMoveStuckThreshold = 1000,
    SmartMoveBypassDistance = 3,
    SmartMoveMaxBypassDistance = 8,
    SmartMoveTimeout = 5000,

    -- 攻擊超時防卡機制
    AttackTimeoutEnabled = 1,
    AttackTimeoutMs = 1000,

    -- 臨時跳過目標配置
    TemporarySkipDuration = 3000,  -- 放棄目標後3秒內不再鎖定

    -- 技能設定

    SkillConfig = {
      Skill_8034 = { enabled = false },
      Skill_8031 = { enabled = false },
      Skill_8032 = { enabled = false },
      Skill_8033 = { enabled = false },
      Skill_8035 = { enabled = false },
    },
    SmartAttackPriorityOrder = {},

    Skill = {{
        id=8034, lv=1, target=0, when=1, times=1, delay=5000,
        sp={0,100}, range=9,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=80
      },{
        id=8031, lv=1, target=0, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8032, lv=1, target=2, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8033, lv=1, target=0, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8035, lv=1, target=2, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=0, lv=1, target=0, when=1, times=1, delay=0,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=10
      }},
    -- 智能攻擊順序
    SmartAttackPriorityOrder = {}
  },
  sera = {
    FollowDis = 1,
    FollowMode = 0,
    MoveDelay = 200,
    RadiusAggr = 20,
    AutoReturnDelay = 1000,

    -- 協同作戰設定
    AttackMonstersEnabled = 1,
    MVPMode = 0,
    CooperativeMode = 0,
    AltT_ToggleMode = 1,

    -- 模式設定
    SentryMode = 0,
    SentryRadius = 13,
    AutoCounterAttack = 0,

    -- 黑名單系統設定
    AutoBlacklist = 1,
    BlacklistPriority = 1,
    BlacklistOnly = 0,

    -- 技能控制設定
    SkillMode = 1,
    SkillMinSP = 0,

    -- Dance Attack 設定
    DanceAttackMode = 0,
    DanceEnabled = false,
    DanceAttack = true,
    UseHopDance = false,
    ASPD = 190,
    AttackThrottleMs = 0,
    DancePhase = 0.4,
    SkillThrottleMs = 110,
    PlayerASPD = 190,
    DanceMinSP = 0,
    DanceActivationDelay = 500,
    DanceCombatDelay = 100,

    -- 連續戰鬥模式
    ContinuousAttackMode = 2,

    -- 快捷鍵設定
    HotkeySettings = {
      ALT_T_ModeSwitch = 1,
      ALT_RightClick_Move = 1,
      ALT_RightClick_FirstPoint = 1,
      ALT_SHIFT_SecondPoint = 1,
      ALT_SHIFT_FullView_FirstPoint = 1,
      ALT_SHIFT_FullView_SecondPoint = 1,
      ALT_SHIFT_SingleAdd = 1,
      ALT_SHIFT_CounterAttack = 0,
    },

    -- 排除設定
    AttackAllExclude = {
      homunculus = 0,
      mercenary = 1,
      friends = 1,
      players = 1,
      plants = 0,
      priorityTargets = 0,  -- 啟用副本優先攻擊
      smartAttackOrder = 1,
      emperium = 1,
    },

    -- 狀態卡住偵測與恢復設定
    StuckDetectionEnabled = 1,
    StuckTimeoutMs = 1500,
    StuckEarlyWarningTime = 500,
    StuckRecoveryAttemptInterval = 1000,
    StuckRecoveryMode = 0,

    -- 主動模式定期刷新設定
    ActiveModeRefreshInterval = 500,

    -- 智慧路徑移動優化設定
    SmartMoveEnabled = 1,
    SmartMoveStuckThreshold = 500,
    SmartMoveBypassDistance = 2,
    SmartMoveMaxBypassDistance = 13,
    SmartMoveTimeout = 2000,  -- 1.5秒超時（2倍速度放棄隔牆怪物）

    -- 攻擊超時防卡機制
    AttackTimeoutEnabled = 1,
    AttackTimeoutMs = 3000,

    -- 臨時跳過目標配置
    TemporarySkipDuration = 2000,  -- 放棄目標後3秒內不再鎖定

    -- 技能設定

    SkillConfig = {
      Skill_8019 = { enabled = false },
      Skill_8018 = { enabled = false },
      Skill_8020 = { enabled = false },
      Skill_8021 = { enabled = false },
    },
    SmartAttackPriorityOrder = {},

    Skill = {{
        id=8019, lv=1, target=0, when=1, times=1, delay=5000,
        sp={0,100}, range=5,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=80
      },{
        id=8018, lv=1, target=2, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8020, lv=1, target=0, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8021, lv=1, target=1, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=0, lv=1, target=0, when=1, times=1, delay=0,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=10
      }},
    -- 智能攻擊順序
    SmartAttackPriorityOrder = {},
  },
  dieter = {
    FollowDis = 3,
    FollowMode = 0,
    MoveDelay = 150,
    RadiusAggr = 11,
    AutoReturnDelay = 800,

    -- 協同作戰設定
    AttackMonstersEnabled = 1,
    MVPMode = 0,
    CooperativeMode = 1,
    AltT_ToggleMode = 0,

    -- 模式設定
    SentryMode = 0,
    SentryRadius = 8,
    AutoCounterAttack = 1,

    -- 黑名單系統設定
    AutoBlacklist = 0,
    BlacklistPriority = 0,
    BlacklistOnly = 0,

    -- 技能控制設定
    SkillMode = 1,
    SkillMinSP = 0,

    -- Dance Attack 設定
    DanceAttackMode = 0,
    DanceEnabled = false,
    DanceAttack = true,
    UseHopDance = false,
    ASPD = 190,
    AttackThrottleMs = 0,
    DancePhase = 0.4,
    SkillThrottleMs = 110,
    PlayerASPD = 190,
    DanceMinSP = 0,
    DanceActivationDelay = 500,
    DanceCombatDelay = 400,

    -- 連續戰鬥模式
    ContinuousAttackMode = 0,

    -- 快捷鍵設定
    HotkeySettings = {
      ALT_T_ModeSwitch = 1,
      ALT_RightClick_Move = 1,
      ALT_RightClick_FirstPoint = 1,
      ALT_SHIFT_SecondPoint = 1,
      ALT_SHIFT_FullView_FirstPoint = 1,
      ALT_SHIFT_FullView_SecondPoint = 1,
      ALT_SHIFT_SingleAdd = 1,
      ALT_SHIFT_CounterAttack = 0
    },

    -- 排除設定
    AttackAllExclude = {
      homunculus = 1,
      mercenary = 1,
      friends = 1,
      players = 1,
      plants = 2,
      priorityTargets = 0,  -- 啟用副本優先攻擊
      smartAttackOrder = 0,
      emperium = 0
    },

    -- 狀態卡住偵測與恢復設定
    StuckDetectionEnabled = 1,
    StuckTimeoutMs = 5000,
    StuckEarlyWarningTime = 2500,
    StuckRecoveryAttemptInterval = 1000,
    StuckRecoveryMode = 0,

    -- 主動模式定期刷新設定
    ActiveModeRefreshInterval = 500,

    -- 智慧路徑移動優化設定
    SmartMoveEnabled = 1,
    SmartMoveStuckThreshold = 1000,
    SmartMoveBypassDistance = 3,
    SmartMoveMaxBypassDistance = 8,
    SmartMoveTimeout = 5000,

    -- 攻擊超時防卡機制
    AttackTimeoutEnabled = 1,
    AttackTimeoutMs = 1000,

    -- 臨時跳過目標配置
    TemporarySkipDuration = 3000,  -- 放棄目標後3秒內不再鎖定

    -- 技能設定

    SkillConfig = {
      Skill_8041 = { enabled = false },
      Skill_8039 = { enabled = false },
      Skill_8040 = { enabled = false },
      Skill_8042 = { enabled = false },
      Skill_8043 = { enabled = false },
    },
    SmartAttackPriorityOrder = {},

    Skill = {{
        id=8041, lv=1, target=0, when=1, times=1, delay=5000,
        sp={0,100}, range=7,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=80
      },{
        id=8039, lv=1, target=2, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8040, lv=1, target=2, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8042, lv=1, target=2, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8043, lv=1, target=0, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=0, lv=1, target=0, when=1, times=1, delay=0,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=10
      }},
    -- 智能攻擊順序
    SmartAttackPriorityOrder = {}
  },
  eleanor = {
    FollowDis = 3,
    FollowMode = 0,
    MoveDelay = 150,
    RadiusAggr = 11,
    AutoReturnDelay = 800,

    -- 協同作戰設定
    AttackMonstersEnabled = 1,
    MVPMode = 1,
    CooperativeMode = 1,
    AltT_ToggleMode = 0,

    -- 模式設定
    SentryMode = 0,
    SentryRadius = 8,
    AutoCounterAttack = 1,

    -- 黑名單系統設定
    AutoBlacklist = 0,
    BlacklistPriority = 0,
    BlacklistOnly = 0,

    -- 技能控制設定
    SkillMode = 1,
    SkillMinSP = 0,

    -- Dance Attack 設定
    DanceAttackMode = 0,
    DanceEnabled = false,
    DanceAttack = true,
    UseHopDance = false,
    ASPD = 190,
    AttackThrottleMs = 0,
    DancePhase = 0.4,
    SkillThrottleMs = 110,
    PlayerASPD = 190,
    DanceMinSP = 0,
    DanceActivationDelay = 500,
    DanceCombatDelay = 400,

    -- 連續戰鬥模式
    ContinuousAttackMode = 1,

    -- 快捷鍵設定
    HotkeySettings = {
      ALT_T_ModeSwitch = 1,
      ALT_RightClick_Move = 1,
      ALT_RightClick_FirstPoint = 1,
      ALT_SHIFT_SecondPoint = 1,
      ALT_SHIFT_FullView_FirstPoint = 1,
      ALT_SHIFT_FullView_SecondPoint = 1,
      ALT_SHIFT_SingleAdd = 1,
      ALT_SHIFT_CounterAttack = 0
    },

    -- 排除設定
    AttackAllExclude = {
      homunculus = 1,
      mercenary = 1,
      friends = 1,
      players = 1,
      plants = 2,
      priorityTargets = 0,  -- 啟用副本優先攻擊
      smartAttackOrder = 1,
      emperium = 1
    },

    -- 狀態卡住偵測與恢復設定
    StuckDetectionEnabled = 1,
    StuckTimeoutMs = 5000,
    StuckEarlyWarningTime = 2500,
    StuckRecoveryAttemptInterval = 1000,
    StuckRecoveryMode = 0,

    -- 主動模式定期刷新設定
    ActiveModeRefreshInterval = 500,

    -- 智慧路徑移動優化設定
    SmartMoveEnabled = 1,
    SmartMoveStuckThreshold = 1000,
    SmartMoveBypassDistance = 3,
    SmartMoveMaxBypassDistance = 8,
    SmartMoveTimeout = 5000,

    -- 攻擊超時防卡機制
    AttackTimeoutEnabled = 1,
    AttackTimeoutMs = 1000,

    -- 臨時跳過目標配置
    TemporarySkipDuration = 3000,  -- 放棄目標後3秒內不再鎖定

    -- 技能設定

    SkillConfig = {
      Skill_8028 = { enabled = false },
      Skill_8027 = { enabled = false },
      Skill_8029 = { enabled = false },
      Skill_8030 = { enabled = false },
      Skill_8036 = { enabled = false },
      Skill_8037 = { enabled = false },
      Skill_8038 = { enabled = false },
    },
    SmartAttackPriorityOrder = {},

    Skill = {{
        id=8028, lv=1, target=0, when=1, times=1, delay=5000,
        sp={0,100}, range=3,
        nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=80
      },{
        id=8027, lv=1, target=2, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8029, lv=1, target=2, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8030, lv=1, target=2, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8036, lv=1, target=0, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8037, lv=1, target=2, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=8038, lv=1, target=2, when=1, times=1, delay=5000,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=70
      },{
        id=0, lv=1, target=0, when=1, times=1, delay=0,
        sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
        chase=1, stemp=0, count=0, priority=10
      }},
    -- 智能攻擊順序
    SmartAttackPriorityOrder = {}
  }
}

-- ============================================
-- 自動載入對應生命體配置
-- ============================================

-- 深度複製 table (處理巢狀 table)
function deepcopy(orig)
  local orig_type = type(orig)
  local copy
  if orig_type == 'table' then
    copy = {}
    for orig_key, orig_value in next, orig, nil do
      copy[deepcopy(orig_key)] = deepcopy(orig_value)
    end
    setmetatable(copy, deepcopy(getmetatable(orig)))
  else
    copy = orig
  end
  return copy
end

-- 載入配置到全域變數（由 Main.lua 呼叫）
function LoadHomConfig(myid)
  local t = GetV(V_HOMUNTYPE, myid)
  TraceAI(string.format("【偵錯】生命體類型 ID: %s", tostring(t)))
  if type(t) == "number" then
    -- 部分客戶端回報 48–52，伺服器資料則使用 6048–6052。
    if t >= 6048 and t <= 6052 then t = t - 6000 end
    if t == 53 then t = 52 end -- 保留舊設定對艾蕾諾亞的 53 別名
    local homType
    if t >= 1 and t < 48 then
      local arr = {"lif","amistr","filir","Vanilmirth"}
      homType = arr[(t - 1) % 4 + 1]
    elseif t >= 48 and t <= 52 then
      local arr = {"eira","bayeri","sera","dieter","eleanor"}
      homType = arr[t - 47]
    end

    TraceAI(string.format("【偵錯】判定生命體為: %s", tostring(homType)))
    local config = homType and HomConfig[homType]
    if config then
      TraceAI(string.format("【偵錯】載入 %s 設定，DanceAttackMode = %s", homType, tostring(config.DanceAttackMode)))
      for k, v in pairs(config) do
        if type(v) == "table" then
          _G[k] = deepcopy(v)
        else
          _G[k] = v
        end
      end
      return true
    else
      TraceAI("【警告】找不到對應的生命體設定檔！")
      return false
    end
  end
  return false
end
