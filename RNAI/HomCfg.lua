-- ===== 基本移動/索敵設定 =====
FollowDis   = 1
MoveDelay   = 500
RadiusAggr  = 12

-- {主人被打, 生命體被打, 其他玩家被打, 主人攻擊, 生命體攻擊, 其他玩家攻擊, 範圍外, 範圍內}
SearchMode     = { {64,128,0,256,0,0,-1,-1}, {500,200,0,500,30,0,-1,1} }
SearchSetting  = SearchMode[2]

-- 若需要弱目標僅普攻，在這裡填入 V_HOMUNTYPE
WeakTargets = {}

-- ===== 優先攻擊目標（數值權重，與 MapInfo.lua 加總）=====
PriorityTargets = {
  -- 高優先（同分）
  [3903] = 10000, -- 花
  [3974] = 10000, -- 蟲卵
  [3897] = 10000, -- 邪骸獸人
  [3898] = 10000, -- 腐屍獸人
  [3972] = 11000, -- 綠色蟑螂
  [1246] = 11000, -- 甜餅人
  -- 次優先
  [3901] = 9000,  -- 冒牌獸人英雄
  [3975] = 9000,  -- 黃金蟲
}

-- ===== MVP 怪物種類（MVPMode=1 時最優先並鎖定）=====
-- 伺服器若有自訂 MVP，把怪物種類 ID 加進來即可
MVPTargets = MVPTargets or {
  [1038]=true, [1039]=true, [1046]=true, [1059]=true, [1086]=true, [1087]=true,
  [1112]=true, [1115]=true, [1147]=true, [1150]=true, [1157]=true, [1159]=true,
  [1190]=true, [1251]=true, [1252]=true, [1272]=true, [1312]=true, [1373]=true,
  [1389]=true, [1418]=true, [1492]=true, [1511]=true, [1583]=true, [1623]=true,
  [1630]=true, [1646]=true, [1647]=true, [1648]=true, [1649]=true, [1650]=true,
  [1651]=true, [1658]=true, [1685]=true, [1688]=true, [1708]=true, [1719]=true,
  [1734]=true, [1751]=true, [1768]=true, [1779]=true, [1785]=true, [1832]=true,
  [1871]=true, [1874]=true, [1885]=true, [1917]=true, [1956]=true, [1957]=true,
  [2022]=true, [2068]=true, [2087]=true, [2165]=true,
}

-- ===== 技能清單（維持你原本 8013 與普攻；如需改 8014 可自行切換）=====
Skill = {}

-- 對單主動技（例：8013）
Skill[#Skill+1] = {
  id=8013, lv=5, target=0, when=1, times=1,
  delay=2500, sp={20,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
  chase=1, stemp=0, count=0
}

-- 普攻（建議永遠置底）
Skill[#Skill+1] = {
  id=0, lv=1, target=0, when=1, times=1,
  delay=100, sp={0,100}, nMyEnemy=0, nOwnerEnemy=0, nRangeEnemy=0,
  chase=1, stemp=0, count=0
}

-- =========================================================
-- ＝ 跳舞 / Hop 模式：開關與參數（新增）＝
-- 你可以只改這區來開關與微調舞步；Main.lua 會自動讀取
-- 來源：ASPD 與跳舞/水平抖動/Hop 參數、技能節流等（整合與改寫）。
-- =========================================================

-- 總開關（關掉就完全不做舞步與 Hop）
DanceEnabled = true     -- ←←← 主要開關

-- ASPD 與節奏
ASPD       = ASPD       or 193   -- 建議測 190~193
DancePhase = DancePhase or 0.40  -- 0.40~0.48，與伺服器節奏微調

-- 水平抖動（貼身左右小步）
DanceAttack            = (DanceAttack ~= false) -- true=啟用抖動
DanceStep              = 1       -- 左右步幅
DanceKeepRange         = 1       -- 與目標維持距離（格）
DanceMaxRadius         = 2       -- ≤ 此距離啟動抖動
DanceMicro             = true    -- 套用 Pattern
DancePattern           = {1}     -- {1}每拍移；或 {1,0}一拍移一拍休
DanceTriggerAttack     = true    -- 節拍點嘗試普攻
DanceTriggerAttackTries= 1

-- Hop 舞步（可選開）
UseHopDance            = true    -- true=嘗試 Hop，一次靠近-普攻-回位
UseDanceAttack         = 1       -- Hop 的模式輔助參數
UseSkillOnly           = 0
DanceMinSP             = 0
SkillMinSP             = SkillMinSP or 0
BerserkMode            = 0
Berserk_Dance          = 0
panicmode              = 0
Panic_UseDanceAttack   = 0
FleeHP                 = 30

-- 技能/移動節流（全域最小施法間隔；Main.lua 會再自動調 ASPD）
SkillThrottleMs        = 90
