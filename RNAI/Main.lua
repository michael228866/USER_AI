-- 常數與狀態（沿用原檔）
ST_FOLLOW=0
ST_MOVE_CMD=1
ST_HOLD=2
ST_ATTACK=3
ST_SKILL=4
ST_ATTACK_PRE=5
ST_SKILL_GND=6

AITick=-1
InitStatus=0
MyState=0
DestX=0
DestY=0
Target=0
ManualSkill = { ["id"]=0, ["lv"]=0, ["target"]=0, ["x"]=0, ["y"]=0, ["range"]=0 }
AtkDis=1

Aggr=1
MoveCmdTime=0
FollowCmdTime=0
MyMotion=0
MyMotion_t=0
EnableNormalAttack=true

-- 讀不到時避免 nil
PriorityTargets = PriorityTargets or {}

-- ============（新增）ASPD/舞步/節流 變數 ============
-- 這些會由 HomCfg.lua 覆蓋或給定
DanceEnabled  = (DanceEnabled ~= false)
ASPD          = ASPD or 190
DancePhase    = DancePhase or 0.46
DanceAttack   = (DanceAttack ~= false)
DanceKeepRange= DanceKeepRange or 1
DanceMaxRadius= DanceMaxRadius or 2
DanceStep     = DanceStep or 1
DanceMicro    = (DanceMicro ~= false)
DancePattern  = DancePattern or {1}
DanceTriggerAttack      = (DanceTriggerAttack ~= false)
DanceTriggerAttackTries = DanceTriggerAttackTries or 1

UseHopDance   = UseHopDance or false
UseDanceAttack= UseDanceAttack or 0
UseSkillOnly  = UseSkillOnly or 0
DanceMinSP    = DanceMinSP or 0
SkillMinSP    = SkillMinSP or 0
BerserkMode   = BerserkMode or 0
Berserk_Dance = Berserk_Dance or 0
panicmode     = panicmode or 0
Panic_UseDanceAttack = Panic_UseDanceAttack or 0
FleeHP        = FleeHP or 30

AttackPeriodMs   = AttackPeriodMs or 0
AttackThrottleMs = AttackThrottleMs or 0
DanceIntervalMs  = DanceIntervalMs or 0
SkillThrottleMs  = SkillThrottleMs or 110
LastDanceTick    = LastDanceTick or 0
DancePatternIdx  = DancePatternIdx or 1
DanceDir         = (DanceDir == nil) and 1 or DanceDir
LastAttackTick   = LastAttackTick or 0
LastSkillTick    = LastSkillTick or 0

-- EPS / eEPS（簡化輸出，僅統計數字）
_EPS_WindowStartTick = _EPS_WindowStartTick or 0
_EPS_AttackCount     = _EPS_AttackCount     or 0
_eEPS_AttackCount    = _eEPS_AttackCount    or 0
local EPS_WINDOW_MS  = 2000
local function EPS_Tick(now)
  if _EPS_WindowStartTick == 0 then _EPS_WindowStartTick = now; return end
  local win = now - _EPS_WindowStartTick
  if win >= EPS_WINDOW_MS then
    -- 如需輸出，可改為 TraceAI(string.format(...))；避免噪音先省略
    _EPS_WindowStartTick = now
    _EPS_AttackCount  = 0
    _eEPS_AttackCount = 0
  end
end

-- ASPD → 時序
local function SetupAspdTimings()
  local Tms = ((200 - (ASPD or 190)) / 50) * 1000  -- T = (200-ASPD)/50 * 1000
  if Tms < 140 then Tms = 140 end                  -- 安全下限
  AttackPeriodMs   = math.floor(Tms + 0.5)         -- ASPD=190 → ≈200ms
  if not AttackThrottleMs or AttackThrottleMs==0 then AttackThrottleMs = AttackPeriodMs end
  DanceIntervalMs  = math.floor(AttackPeriodMs * (DancePhase or 0.46))
  if not MoveDelay or MoveDelay==0 then
    MoveDelay      = math.max(40, math.floor(DanceIntervalMs * 0.8))
  end
end

-- 安全節流：普攻/施法
local function CanAttackNow(now)  return (now-(LastAttackTick or 0)) >= (AttackThrottleMs or 200) end
local function CanCastNow(now)    return (now-(LastSkillTick  or 0)) >= (SkillThrottleMs  or 110) end

AI_MAIN_TRANSITION_UNTIL = AI_MAIN_TRANSITION_UNTIL or 0
AI_MAIN_LAST_MY_ID = AI_MAIN_LAST_MY_ID or 0
AI_MAIN_LAST_OWNER_ID = AI_MAIN_LAST_OWNER_ID or 0
AI_MAIN_LAST_RESET_LOG = AI_MAIN_LAST_RESET_LOG or 0

local function SafeGetV(vType,id,p1,p2)
  local ok,a,b
  if p2~=nil then
    ok,a,b=pcall(GetV,vType,id,p1,p2)
  elseif p1~=nil then
    ok,a,b=pcall(GetV,vType,id,p1)
  else
    ok,a,b=pcall(GetV,vType,id)
  end
  if ok then return a,b end
  return nil,nil
end

local function SafeCommand(fn,id)
  if type(fn)~="function" then return {NONE_CMD or 0} end
  local ok,msg=pcall(fn,id)
  if ok and type(msg)=="table" then return msg end
  return {NONE_CMD or 0}
end

-- Every Move is a packet to the server. Callers such as following and hop
-- dancing used to send one or two per AI tick, which servers can treat as a
-- flood and disconnect, so all moves share one rate limit.
local LastMovePacketTick=0
local function SafeMove(id,x,y)
  if not id or not x or not y then return false end
  local now=GetTick()
  if now-LastMovePacketTick<math.max(MoveDelay or 150,100) then return false end
  local ok=pcall(Move,id,x,y)
  if ok then LastMovePacketTick=now end
  return ok==true
end

local function ResetBattleState(reason,now,protectMs)
  Target=0
  bestTarget=0
  Mobs={}
  others={}
  nOwnerEnemy=0
  nMyEnemy=0
  nRangeEnemy=0
  InitStatus=0
  MyState=ST_FOLLOW
  MoveCmdTime=0
  LastAttackTick=0
  LastSkillTick=0
  ManualSkill.target=0
  ManualSkill.x=0
  ManualSkill.y=0
  AI_MAIN_TRANSITION_UNTIL=(now or GetTick())+(protectMs or 0)
  if type(AI_Log)=="function" and (now or GetTick())-(AI_MAIN_LAST_RESET_LOG or 0)>1000 then
    AI_MAIN_LAST_RESET_LOG=now or GetTick()
    AI_Log("AI transition guard: " .. tostring(reason))
  end
end

local function IsTransitionProtected(now)
  return (AI_MAIN_TRANSITION_UNTIL or 0)>now or (AI_TRANSITION_PROTECT_UNTIL or 0)>now
end

local function IsPlayerTarget(target)
  return type(target)=="number" and target >= (AI_PLAYER_ID_MIN or 100000) and AttackAllExclude and AttackAllExclude.players == 0
end

local function IsAttackableTarget(target)
  if not target or target==0 then return false end
  if IsPlayerTarget(target) then return true end
  local ok,isMonster=pcall(IsMonster,target)
  return ok and isMonster==1
end

local function CanUseSkillOnTarget(sk,target)
  if not sk or sk.id==0 then return true end
  local mode=SkillMode or 1
  if mode==3 then return false end
  if sk.buffSkill or sk.target==1 or sk.target==2 then return true end
  if mode==2 then return IsPlayerTarget(target) end
  return true
end

local function CanAutoCastBySP(sk,currentSP)
  if not sk or sk.id==0 then return true end
  local minSP=tonumber(SkillMinSP) or 0
  if minSP<=0 then return true end
  return (currentSP or 0)>=minSP
end

local function IsSkillAutoEnabled(sk)
  if not sk or not sk.id or sk.id==0 then return true end
  local configs=SkillConfig
  if type(configs)~="table" then return true end
  local key="Skill_"..tostring(sk.id)
  local level=sk.lv and configs[key.."_lv"..tostring(sk.lv)]
  local config=level or configs[key]
  return type(config)~="table" or config.enabled~=false
end

local function ReadConfigSource(path)
  local fp=io.open(path,"rb")
  if not fp then return nil end
  local source=fp:read("*a")
  fp:close()
  -- dofile() rejects a UTF-8 BOM, which editors may add when saving config files.
  if string.sub(source,1,3)=="\239\187\191" then source=string.sub(source,4) end
  return source
end

-- The last loaded config, watched so that edits apply without re-summoning.
local ConfigPath,ConfigSource,ConfigCheckTick=nil,nil,0

local function LoadConfigFile(path)
  local source=ReadConfigSource(path)
  if not source then error("cannot open "..path) end
  local chunk,err=(loadstring or load)(source,"@"..path)
  if not chunk then error(err) end
  ConfigPath,ConfigSource=path,source
  return chunk()
end

-- A changed file that does not compile is ignored so a half-finished edit
-- keeps the running settings instead of falling back to another config.
local function ConfigChanged(now)
  if not ConfigPath or now-ConfigCheckTick<(ConfigReloadCheckMs or 2000) then return false end
  ConfigCheckTick=now
  local source=ReadConfigSource(ConfigPath)
  if not source or source==ConfigSource then return false end
  ConfigSource=source
  local chunk,err=(loadstring or load)(source,"@"..ConfigPath)
  if not chunk then
    AI_Log("config changed but has errors, keeping current settings: " .. tostring(err))
    return false
  end
  AI_Log("config changed, reloading " .. ConfigPath)
  return true
end

local function SafeGetMotion(target)
  local ok,motion=pcall(GetV,V_MOTION,target)
  if ok then return motion end
  return nil
end

local function SafeRectDistance(a,b)
  if not a or not b or a==0 or b==0 then return 999 end
  local ok,dis=pcall(getObjRectDis,a,b)
  if ok then return dis end
  return 999
end

local function SafeRectPos(x1,y1,x2,y2,range)
  if not x1 or not y1 or not x2 or not y2 then return nil,nil end
  local ok,x,y=pcall(getRectPos,x1,y1,x2,y2,range)
  if ok then return x,y end
  return nil,nil
end

local function SafeFreeObjRectPos(target,myid,range,oid)
  if not target or target==0 then return nil,nil end
  local ok,x,y=pcall(getFreeObjRectPos,target,myid,range,oid)
  if ok then return x,y end
  return nil,nil
end

-- The client API exposes no walls or pathing, so a monster behind a wall is
-- detected by its effect: the chase stops closing in, the target stays out of
-- range and does not fight back within AttackTimeoutMs. Such a target is
-- skipped for TemporarySkipDuration (checked by the target scoring in
-- MapInfo.lua). Being in attack range counts as fighting because the attack
-- motion is shorter than the attack delay and dancing interrupts it; skill
-- range counts only while skills are actually being cast.
TargetSkipUntil = TargetSkipUntil or {}
local ChaseTarget,ChaseBestDis,ChaseProgressTick,ChaseRange=0,999,0,1

local function ChaseTimedOut(myid,target,now)
  if (AttackTimeoutEnabled or 1)==0 then return false end
  local dis=SafeRectDistance(myid,target)
  if target~=ChaseTarget then
    ChaseTarget,ChaseBestDis,ChaseProgressTick,ChaseRange=target,dis,now,AtkDis or 1
    return false
  end
  local timeout=AttackTimeoutMs or 5000
  local motion=SafeGetMotion(myid)
  if dis<ChaseBestDis or dis<=(AtkDis or 1)
     or (dis<=ChaseRange and now-(LastSkillTick or 0)<=timeout)
     or SafeGetV(V_TARGET,target)==myid
     or motion==MOTION_ATTACK or motion==MOTION_ATTACK2 or motion==MOTION_CAST then
    ChaseBestDis=math.min(ChaseBestDis,dis)
    ChaseProgressTick=now
    return false
  end
  return now-ChaseProgressTick>timeout
end

local function SkipUnreachableTarget(target,now)
  for id,untilTick in pairs(TargetSkipUntil) do
    if untilTick<=now then TargetSkipUntil[id]=nil end
  end
  TargetSkipUntil[target]=now+(TemporarySkipDuration or 10000)
  ChaseTarget=0
  AI_Log("target " .. tostring(target) .. " unreachable, skip " .. tostring(TemporarySkipDuration or 10000) .. "ms")
end

local function SafeAttack(myid,target)
  if not IsAttackableTarget(target) then return false end
  local now=GetTick()
  if IsTransitionProtected(now) then return false end
  if CanAttackNow(now) then
    local ok=pcall(Attack,myid,target)
    if not ok then
      ResetBattleState("attack failed",now,AI_TRANSITION_PROTECT_MS or 8000)
      return false
    end
    LastAttackTick=now
    _EPS_AttackCount  = (_EPS_AttackCount or 0) + 1
    _eEPS_AttackCount = (_eEPS_AttackCount or 0) + 1
    return true
  end
  return false
end

local function SafeSkillObject(myid,lv,id,target)
  local now=GetTick()
  if IsTransitionProtected(now) then return false end
  if CanCastNow(now) then
    local ok=pcall(SkillObject,myid,lv,id,target)
    if not ok then
      ResetBattleState("skill object failed",now,AI_TRANSITION_PROTECT_MS or 8000)
      return false
    end
    LastSkillTick=now
    AI_Log("skill " .. tostring(id) .. " lv" .. tostring(lv) .. " -> " .. tostring(target))
    if id and id>0 then
      _EPS_AttackCount  = (_EPS_AttackCount or 0) + 1
      _eEPS_AttackCount = (_eEPS_AttackCount or 0) + 1
    end
    return true
  end
  return false
end

local function SafeSkillGround(myid,lv,id,x,y)
  local now=GetTick()
  if IsTransitionProtected(now) then return false end
  if CanCastNow(now) then
    if not x or not y then return false end
    local ok=pcall(SkillGround,myid,lv,id,x,y)
    if not ok then
      ResetBattleState("skill ground failed",now,AI_TRANSITION_PROTECT_MS or 8000)
      return false
    end
    LastSkillTick=now
    AI_Log("skill " .. tostring(id) .. " lv" .. tostring(lv) .. " -> (" .. tostring(x) .. "," .. tostring(y) .. ")")
    return true
  end
  return false
end

-- ===== 你原本的主流程（移動、弱目標判定、客製載入、AI 狀態機、技能流程） =====
-- （下段保留原檔內容）
function MoveToDest(id,x,y)
  if IsTransitionProtected(GetTick()) then return false end
  local mx,my=SafeGetV(V_POSITION,id)
  if not mx or not my or not x or not y then return false end
  local vx=x-mx
  local vy=y-my
  local t=GetTick()
  if(vx==0 and vy==0)then return true end
  vx= (vx< -10) and -10 or ((vx>10) and 10 or vx)
  vy= (vy< -10) and -10 or ((vy>10) and 10 or vy)
  if(t-MoveCmdTime>=(MoveDelay or 150))then
    if SafeMove(id,mx+vx,my+vy) then MoveCmdTime=t end
  end
  return false
end

function isWeakTarget(t)
  if(#WeakTargets==0)then return false end
  for i,v in ipairs(WeakTargets)do
    if(SafeGetV(V_HOMUNTYPE,t)==v)then return true end
  end
  return false
end

function log_var(arr)
  local s = ""
  for i,v in ipairs(arr) do
    local t=type(v)
    if(t=="string")then s = s..v.." "
    elseif(t=="boolen")then s = s..(v and "True" or "False").." "
    elseif(v==nil)then s = s.."[nil] "
    elseif(t=="number")then s = s..v.." "
    end
  end
  TraceAI(s)
end

function getSepcFilename(myid)
  local t = SafeGetV(V_HOMUNTYPE, myid)
  local d = "./AI/USER_AI/RNAI/custom/"
  if (type(t)=="number") then
    if t >= 6048 and t <= 6052 then t = t - 6000 end
    if t == 53 then t = 52 end
    if t < 48 then
      local arr = {"lif", "amistr", "filir", "vanilmirth"}
      t = d .. arr[(t - 1) % 4 + 1] .. ".lua"
    elseif t <= 52 then
      local arr = {"eira", "bayeri", "sera", "dieter", "eleanor"}
      t = d .. arr[(t - 48) % 5 + 1] .. ".lua"
    else
      return false
    end
  else
    t = (SafeGetV(V_MERTYPE, myid) or 1) - 1
    if (t ~= 0) then
      t = d .. ((t < 10) and "arc" or (t < 20 and "lan" or "swd")) .. (t % 10 == 9 and "" or "0") .. (t % 10 + 1) .. ".lua"
    else
      local arr = {
        {256,200,"arc01"},{8614,220,"wander_man"},{6157,256,"wicked_nymph"},
        {9815,234,"kasa_tiger"},{9517,260,"salamander"},{14493,243,"teddy_bear"},
        {6120,182,"mimic"},{7543,180,"disguise"},{10000,221,"alice"}
      }
      local mhp = SafeGetV(V_MAXHP, myid)
      local msp = SafeGetV(V_MAXSP, myid)
      if not mhp or not msp then return false end
      local fitName = false
      for i, a in ipairs(arr) do
        for j = 0, 5 do
          local hp = a[1]*(1+j/20)
          if (mhp <= hp and hp < mhp + 1) then
            for k = 0, 5 do
              local sp = a[2]*(1+k/20)
              if (msp <= sp and sp < msp + 1) then
                fitName = a[3]; break
              end
            end
          end
          if (fitName ~= false) then break end
        end
        if (fitName ~= false) then break end
      end
      if (fitName == false) then return false end
      t = d .. fitName .. ".lua"
    end
  end
  return t
end

-- ===== 舞步核心：水平抖動 & Hop（新增）=====
local function ClosestRectToKeepR(id, tx, ty, keepR)
  local mx,my=SafeGetV(V_POSITION,id)
  local rx,ry = SafeRectPos(tx,ty,mx,my,keepR)
  return rx,ry
end

local function GetDanceCell(x, y, enemy)
  local ex, ey = SafeGetV(V_POSITION, enemy)
  if not ex or not ey then return x,y end
  local keepR  = DanceKeepRange or AtkDis or 1
  -- 可選：若環境提供 IsInAttackSight 可先判定視線阻隔
  local sight = _G.IsInAttackSight or _G.isanattacksight
  if type(sight)=="function" then
    local ok,ret=pcall(sight, MyID or 0, enemy)
    if ok and not ret then
      local bx,by = ClosestRectToKeepR(MyID or 0, ex, ey, keepR)
      return bx,by
    end
  end
  DanceDir = (DanceDir==nil) and 1 or DanceDir
  local step   = DanceStep or 1
  local first  = (DanceDir >= 0) and 1 or -1
  local second = -first
  local bx,by = x,y
  local cand = { { bx + first*step, by }, { bx + second*step, by }, }
  local function valid(cx,cy)
    local d = math.max(math.abs(cx-ex), math.abs(cy-ey))
    if d ~= keepR then return false end
    return true
  end
  for i=1,#cand do
    local cx,cy = cand[i][1], cand[i][2]
    if valid(cx,cy) then DanceDir = -DanceDir; return cx,cy end
  end
  return bx,by
end

local function ShouldHopDance(myid, enemy)
  if UseHopDance ~= true then return false end
  if not enemy or enemy<=0 then return false end
  if SafeGetMotion(enemy) == MOTION_DEAD then return false end
  local spok = (UseSkillOnly ~= 1 and UseDanceAttack == 1 and (SafeGetV(V_SP,myid) or 0) >= (DanceMinSP or 0))
  local bers = (BerserkMode == 1 and Berserk_Dance == 1)
  local pnok = (panicmode == 1 and Panic_UseDanceAttack == 1 and (HPPercent and HPPercent(myid) or 100) > (FleeHP or 0))
  return (spok or bers or pnok)
end

local function DoHopDance(myid, enemy)
  if IsTransitionProtected(GetTick()) then return false end
  if SafeRectDistance(myid,enemy) > (DanceMaxRadius or 2) then return false end
  local ex,ey = SafeGetV(V_POSITION, enemy)
  if not ex or not ey then return false end
  local keepR = DanceKeepRange or AtkDis or 1
  local bx,by = ClosestRectToKeepR(myid, ex, ey, keepR)
  if not bx or not by then return false end
  local nx,ny = GetDanceCell(bx,by,enemy)
  if not nx or not ny then return false end
  -- One hop per move interval; the step back happens on the next hop.
  if not SafeMove(myid, nx, ny) then return false end
  return SafeAttack(myid, enemy)
end

function AttackDanceTick(myid,target)
  if not DanceEnabled or not DanceAttack or not target or target<=0 then return end
  if IsTransitionProtected(GetTick()) then return end
  if SafeRectDistance(myid,target) > (DanceMaxRadius or 2) then return end
  local now = GetTick()
  local pat = DancePattern or {1}
  local idx = DancePatternIdx or 1
  local mul = pat[(idx-1) % #pat + 1]
  local doStep = (not DanceMicro) or (mul ~= 0)
  if doStep and now - (LastDanceTick or 0) >= (DanceIntervalMs or 92) then
    local tx,ty = SafeGetV(V_POSITION, target)
    if not tx or not ty then return end
    local keepR = DanceKeepRange or AtkDis or 1
    local bx,by = ClosestRectToKeepR(myid, tx, ty, keepR)
    if not bx or not by then return end
    local dx,dy = GetDanceCell(bx,by,target)
    if now - (MoveCmdTime or 0) >= (MoveDelay or 80) then
      if SafeMove(myid, dx, dy) then MoveCmdTime = now end
    end
    LastDanceTick = now
  end
  DancePatternIdx = ((idx) % #pat) + 1
  if DanceTriggerAttack and (now - (LastAttackTick or 0)) >= ((AttackPeriodMs or 200) - 5) then
    if SafeRectDistance(myid,target) <= (DanceKeepRange or 1) then
      for _=1,(DanceTriggerAttackTries or 1) do
        SafeAttack(myid, target)
      end
      LastAttackTick = now
    end
  end
end

-- 讀取自動技能（原函式）
function GetAutoSkill(myid)
  local min_r=100
  local r=SafeRectDistance(myid,Target)
  local t=GetTick()
  local maxsp=SafeGetV(V_MAXSP,myid) or 1
  if maxsp<=0 then maxsp=1 end
  local currentSP=SafeGetV(V_SP,myid) or 0
  local sp=currentSP/maxsp*100
  local skill_id=0
  for i,sk in ipairs(Skill) do
    if(sk.when~=2 and t-sk.stemp>=sk.delay and sk.sp[1]<=sp and sp<=sk.sp[2] and CanAutoCastBySP(sk,currentSP) and IsSkillAutoEnabled(sk) and nOwnerEnemy>=sk.nOwnerEnemy and nMyEnemy>=sk.nMyEnemy and nRangeEnemy>=sk.nRangeEnemy)then
      if(r<=sk.range)then
        if(skill_id==0)then skill_id=i end
      else
        if(sk.chase==1 and min_r>sk.range)then min_r=sk.range end
      end
    end
  end
  return skill_id,min_r
end

function autoUseSkill(myId, ownerId, mobId, excludeWhen)
  local minRadius = 100
  local r = {
    [0] = SafeRectDistance(myId, mobId),
    [1] = SafeRectDistance(myId, ownerId),
    [2] = 0
  }
  local targets = { [0]=mobId, [1]=ownerId, [2]=myId }
  local t = GetTick()
  local maxsp=SafeGetV(V_MAXSP,myId) or 1
  if maxsp<=0 then maxsp=1 end
  local currentSP=SafeGetV(V_SP,myId) or 0
  local sp = currentSP / maxsp * 100
  local usedFlag = false
  for i, sk in ipairs(Skill) do
    if IsSkillAutoEnabled(sk) and sk.when ~= excludeWhen and t - sk.stemp >= sk.delay and sk.sp[1] <= sp and sp <= sk.sp[2] and
       nOwnerEnemy >= sk.nOwnerEnemy and nMyEnemy >= sk.nMyEnemy and nRangeEnemy >= sk.nRangeEnemy and
       CanUseSkillOnTarget(sk, targets[sk.target]) and CanAutoCastBySP(sk,currentSP) then
      if usedFlag==false and r[sk.target] <= sk.range then
        if sk.id == 0 then
          SafeAttack(myId, targets[sk.target])
        elseif sk.castType == 0 then
          SafeSkillObject(myId, sk.lv, sk.id, myId)
        elseif sk.castType == 1 then
          SafeSkillObject(myId, sk.lv, sk.id, targets[sk.target]) -- 改用安全施法
        elseif sk.castType == 2 then
          local x,y = SafeGetV(V_POSITION, targets[sk.target])
          if x and y then SafeSkillGround(myId, sk.lv, sk.id, x, y) end
        end
        usedFlag = true
        sk.count = sk.count + 1
        if(sk.count >= sk.times)then sk.count = 0; sk.stemp = t end
      end
      if mobId > 0 and sk.chase == 1 and r[sk.target] > sk.range and minRadius > sk.range then
        minRadius = sk.range
      end
    end
  end
  if minRadius >= 100 then return false end
  return minRadius
end

-- 主 AI（保留原流程，掛鉤 ASPD 與舞步）
function AI(myid)
  local currentTick=GetTick()
  if(AITick==currentTick) then return end
  AITick=currentTick
  EPS_Tick(currentTick)

  local oid=SafeGetV(V_OWNER,myid)
  if not oid or oid==0 then
    ResetBattleState("owner unavailable",currentTick,1200)
    return
  end
  -- Every game client writes to the same ai.log.
  AI_LOG_TAG="["..tostring(oid).."] "

  -- The owner or homunculus can disappear from GetV during character exit.
  -- Do not issue game commands while either actor has no valid position.
  local ownerX,ownerY=SafeGetV(V_POSITION,oid)
  local homX,homY=SafeGetV(V_POSITION,myid)
  if type(ownerX)~="number" or type(ownerY)~="number"
      or type(homX)~="number" or type(homY)~="number" then
    ResetBattleState("actor position unavailable",currentTick,1200)
    return
  end

  -- The client reloads the whole AI on summon and on every map change, so the
  -- id/position checks below have nothing to compare against. Stay idle briefly
  -- after a fresh load instead of casting buffs or moving while the map settles.
  if (AI_MAIN_LAST_MY_ID or 0)==0 then
    AI_MAIN_LAST_MY_ID=myid
    AI_MAIN_LAST_OWNER_ID=oid
    ResetBattleState("ai loaded",currentTick,AI_LOAD_PROTECT_MS or 3000)
    return
  end

  if AI_MAIN_LAST_MY_ID~=myid or AI_MAIN_LAST_OWNER_ID~=oid then
    AI_MAIN_LAST_MY_ID=myid
    AI_MAIN_LAST_OWNER_ID=oid
    ResetBattleState("id changed",currentTick,AI_TRANSITION_PROTECT_MS or 8000)
    return
  end

  AI_MAIN_LAST_MY_ID=myid
  AI_MAIN_LAST_OWNER_ID=oid

  if IsTransitionProtected(currentTick) then
    local protectUntil=math.max(AI_MAIN_TRANSITION_UNTIL or 0,AI_TRANSITION_PROTECT_UNTIL or 0)
    ResetBattleState("transition protect active",currentTick,math.max(0,protectUntil-currentTick))
    return
  end

  local msg=SafeCommand(GetMsg,myid)
  local rmsg=SafeCommand(GetResMsg,myid)
  local homType=SafeGetV(V_HOMUNTYPE,myid)
  local isHomunculus=homType~=nil

  if InitStatus==2 and ConfigChanged(currentTick) then InitStatus=0 end
  if InitStatus==0 then
    AI_Log("AI init step 0 start, myid=" .. tostring(myid) .. ", owner=" .. tostring(oid) .. ", isHomunculus=" .. tostring(isHomunculus))
    AtkDis=SafeGetV(V_ATTACKRANGE,myid) or 1
    SetupAspdTimings() -- ← 依 ASPD 設定節奏
    InitStatus=1
    local mytype=homType
    if(tb_property_exist(Skill,"id",0)==false)then EnableNormalAttack=false end
    local unifiedConfig = "./AI/USER_AI/RNAI/custom/homunculus_config.lua"
    local loadedUnified = false
    if isHomunculus and file_exist(unifiedConfig) then
      AI_Log("AI unified config candidate: " .. unifiedConfig)
      local ok, err = pcall(LoadConfigFile, unifiedConfig)
      if ok and type(LoadHomConfig)=="function" then
        local okLoad, loadErr = pcall(LoadHomConfig, myid)
        if okLoad and loadErr ~= false then
          loadedUnified = true
          AI_Log("AI unified config loaded: " .. unifiedConfig)
        else
          AI_Log("AI unified config apply failed: " .. tostring(loadErr))
        end
      else
        AI_Log("AI unified config load failed: " .. tostring(err))
      end
    end

    if not loadedUnified then
      local sepcFilename = getSepcFilename(myid)
      AI_Log("AI config candidate: " .. tostring(sepcFilename))
      if(type(sepcFilename)=="string" and file_exist(sepcFilename))then
        local ok, err = pcall(LoadConfigFile, sepcFilename)
        if ok then
          AI_Log("AI config loaded: " .. sepcFilename)
        else
          AI_Log("AI config load failed: " .. sepcFilename .. ", error=" .. tostring(err))
        end
      else
        AI_Log("AI config skipped: file not found")
      end
    end
    SetupAspdTimings()
    for i,sMode in ipairs(SearchMode) do if(SearchSetting==sMode) then Aggr=i; break end end
    AI_Log("AI init step 0 done, AtkDis=" .. tostring(AtkDis) .. ", EnableNormalAttack=" .. tostring(EnableNormalAttack))
    return
  elseif InitStatus==1 then
    AI_Log("AI init step 1 start, skills=" .. tostring(#Skill))
    InitStatus=2
    for i,sk in ipairs(Skill) do
      local castType,effectArea
      if SkillData[sk.id]~=nil then
        local skData = SkillData[sk.id]
        castType = SkillData[sk.id][1]
        if SkillData[sk.id][2][sk.lv]~=nil then effectArea = SkillData[sk.id][2][sk.lv] else effectArea = 0 end
      else castType = 1; effectArea = 0 end
      if sk["castType"] == nil then sk["castType"]=castType end
      if sk["effectArea"] == nil then sk["effectArea"]=effectArea end
      if sk["range"]==nil then
        if sk.id==0 then
          sk["range"]=SafeGetV(V_ATTACKRANGE,myid) or 1
        elseif sk.target==2 then
          sk["range"]=100
        elseif sk.castType==0 then
          sk["range"] = sk["effectArea"] == 0 and 14 or (sk["effectArea"] - 1) / 2
        else
          sk["range"]=SafeGetV(V_SKILLATTACKRANGE_LEVEL,myid,sk.id,sk.lv) or 1
        end
      end
    end
    AI_Log("AI init step 1 done")
  end

  local mymo=SafeGetV(V_MOTION,myid)
  if not mymo then
    ResetBattleState("motion unavailable",currentTick,1200)
    return
  end
  if(mymo~=MyMotion)then MyMotion=mymo; MyMotion_t=GetTick() end
  -- shortcut: diagnostic to learn when the server regenerates SP, remove once dancing rests for it
  if mymo==MOTION_MOVE then LastWalkTick=currentTick end
  local sp=SafeGetV(V_SP,myid)
  if sp and LastSP and sp>LastSP then
    AI_Log("sp +"..(sp-LastSP).." -> "..sp..", still "..(currentTick-(LastWalkTick or 0)).."ms, last move cmd "..(currentTick-LastMovePacketTick).."ms ago")
  end
  LastSP=sp

  RefreshData(myid,oid)
  if IsTransitionProtected(currentTick) then
    local protectUntil=math.max(AI_MAIN_TRANSITION_UNTIL or 0,AI_TRANSITION_PROTECT_UNTIL or 0)
    ResetBattleState("map transition detected",currentTick,math.max(0,protectUntil-currentTick))
    return
  end

  if msg[1]==MOVE_CMD then
    MyState=ST_MOVE_CMD; DestX=msg[2]; DestY=msg[3]
  elseif msg[1]==FOLLOW_CMD then
    local t=GetTick()
    if(t-FollowCmdTime<500)then Aggr=Aggr%#SearchMode+1; SearchSetting=SearchMode[Aggr] end
    FollowCmdTime=t; MyState=ST_FOLLOW
  elseif msg[1]==ATTACK_OBJECT_CMD then
    if IsAttackableTarget(msg[2]) then
      MyState=ST_ATTACK; Target=msg[2]
    else
      MyState=ST_FOLLOW; Target=0
    end
    if(isHomunculus) then
      local targetType=SafeGetV(V_HOMUNTYPE,msg[2])
      TraceAI("id:"..tostring(msg[2])..",type"..tostring(targetType or "player"))
    end
  elseif msg[1]==SKILL_OBJECT_CMD then
    MyState=ST_SKILL
    ManualSkill.lv = msg[2]; ManualSkill.id = msg[3]; ManualSkill.target = msg[4]
    ManualSkill.range = SafeGetV(V_SKILLATTACKRANGE_LEVEL, myid, ManualSkill.id, ManualSkill.lv) or 1
  elseif msg[1]==SKILL_AREA_CMD then
    MyState = ST_SKILL_GND
    ManualSkill.lv = msg[2]; ManualSkill.id = msg[3]
    ManualSkill.x = msg[4]; ManualSkill.y = msg[5]
    ManualSkill.range = SafeGetV(V_SKILLATTACKRANGE_LEVEL, myid, ManualSkill.id, ManualSkill.lv) or 1
  end

  if(msg[1]==NONE_CMD)then
    if(rmsg[1]==MOVE_CMD)then
      if(MyState==ST_MOVE_CMD or MyState==ST_HOLD)then
        local x1,y1=math.min(DestX,rmsg[2]),math.min(DestY,rmsg[3])
        local x2,y2=math.max(DestX,rmsg[2]),math.max(DestY,rmsg[3])
        local tx,ty
        for i,v in ipairs(others)do
          tx,ty=SafeGetV(V_POSITION,v)
          if(tx and ty and x1<=tx and tx<=x2 and y1<=ty and ty<=y2)then friends[#friends+1]=v end
        end
        MyState=ST_FOLLOW
      else
        rmsg[2]=XYInMobs(rmsg[2],rmsg[3]); rmsg[1]=ATTACK_OBJECT_CMD
      end
    end
    if(rmsg[1]==ATTACK_OBJECT_CMD)then
      if(rmsg[2]==myid)then
        friends={}
      else
        local f_exist=false
        for i,v in ipairs(friends)do if(v==rmsg[2])then f_exist=true; break end end
        if(f_exist==false)then friends[#friends+1]=rmsg[2] end
      end
    end
  end

  if(MyState==ST_MOVE_CMD) then
    if(MoveToDest(myid,DestX,DestY)==true) then MyState=ST_HOLD end

  elseif (MyState==ST_FOLLOW) then
    autoUseSkill(myid, oid, oid, 1)
    if(bestTarget>0 and Mobs[bestTarget] and Mobs[bestTarget][5]>=0)then MyState=ST_ATTACK_PRE end
    if(MyState==ST_FOLLOW and SafeRectDistance(oid,myid)>(FollowDis or 3))then
      local x1,y1=SafeGetV(V_POSITION,oid)
      local x2,y2=SafeGetV(V_POSITION,myid)
      local dx,dy=SafeRectPos(x1,y1,x2,y2,FollowDis or 3)
      SafeMove(myid,dx,dy)
    end

  elseif (MyState==ST_ATTACK_PRE) then
    if(bestTarget<=0 or Mobs[bestTarget]==nil or Mobs[bestTarget][5]<0)then RemoveTarget(); MyState=ST_FOLLOW
    elseif ChaseTimedOut(myid,Mobs[bestTarget][1],currentTick) then
      SkipUnreachableTarget(Mobs[bestTarget][1],currentTick)
      RemoveTarget(); Target=0; MyState=ST_FOLLOW
    else
      Target=Mobs[bestTarget][1]
      if(isWeakTarget(Target))then
        local dis=SafeRectDistance(myid,Target)
        if(dis<=AtkDis)then SafeAttack(myid,Target) end -- 改用安全普攻
        if(Target>0 and SafeRectDistance(oid,Target)<15)then
          local x,y=SafeFreeObjRectPos(Target,myid,AtkDis,oid)
          MoveToDest(myid,x,y)
        end
      else
        local chaseDis = autoUseSkill(myid, oid, Target, 2)
        ChaseRange=tonumber(chaseDis) or AtkDis or 1
        if(chaseDis~=false and Target>0 and SafeRectDistance(oid,Target)<15)then
          local x,y=SafeFreeObjRectPos(Target,myid,chaseDis,oid)
          MoveToDest(myid,x,y)
        end
      end
      -- ======（新增）舞步掛鉤：預攻擊階段也可抖動/試刀 ======
      if DanceEnabled and Target>0 and not IsPlayerTarget(Target) then
        if not UseHopDance or not DoHopDance(myid, Target) then
          AttackDanceTick(myid,Target)
        end
      end
    end

  elseif (MyState==ST_ATTACK) then
    if(Target<=0 or SafeRectDistance(oid, Target)>15 or SafeGetMotion(Target)==MOTION_DEAD)then RemoveTarget(); MyState=ST_FOLLOW
    else
      if(isWeakTarget(Target))then
        local dis=SafeRectDistance(myid,Target)
        if(dis<=AtkDis)then SafeAttack(myid,Target) end
        if(Target>0 and SafeRectDistance(oid,Target)<15)then
          local x,y=SafeFreeObjRectPos(Target,myid,AtkDis,oid)
          MoveToDest(myid,x,y)
        end
      else
        local chaseDis = autoUseSkill(myid, oid, Target, 2)
        if(chaseDis~=false and Target>0 and SafeRectDistance(oid,Target)<15)then
          local x,y=SafeFreeObjRectPos(Target,myid,chaseDis,oid)
          MoveToDest(myid,x,y)
        end
      end
      -- ======（新增）舞步掛鉤：攻擊態持續抖動/Hop ======
      if DanceEnabled and Target>0 and not IsPlayerTarget(Target) and SafeRectDistance(myid,Target) <= (DanceMaxRadius or AtkDis) then
        if not UseHopDance or not DoHopDance(myid, Target) then
          AttackDanceTick(myid,Target)
        end
      end
    end

  elseif (MyState==ST_SKILL) then
    local target = ManualSkill.target
    if(SafeRectDistance(oid, target)>15 or SafeGetMotion(target)==MOTION_DEAD)then
      MyState=ST_FOLLOW
    elseif(SafeRectDistance(myid, target) <= (ManualSkill.range or 1))then
      SafeSkillObject(myid, ManualSkill.lv, ManualSkill.id, target)
      if(IsAttackableTarget(target))then Target = target; MyState = ST_ATTACK else MyState = ST_FOLLOW end
    else
      local x,y=SafeFreeObjRectPos(target, myid, ManualSkill.range, oid)
      MoveToDest(myid, x, y)
    end

  elseif (MyState==ST_SKILL_GND) then
    local x,y = SafeGetV(V_POSITION, myid)
    if not x or not y or not ManualSkill.x or not ManualSkill.y then MyState=ST_FOLLOW; return end
    local okDis,dis = pcall(getRectDis,x,y,ManualSkill.x,ManualSkill.y)
    if not okDis then MyState=ST_FOLLOW; return end
    if(dis <= ManualSkill.range) then
      SafeSkillGround(myid, ManualSkill.lv, ManualSkill.id, ManualSkill.x, ManualSkill.y)
      MyState=ST_FOLLOW
    else
      x,y=SafeRectPos(ManualSkill.x, ManualSkill.y, x, y, ManualSkill.range)
      MoveToDest(myid, x, y)
    end
  end
end
