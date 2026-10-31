-- 本檔案包含戰場物件與管理（調整：優先表改由 HomCfg.lua 提供）
Mobs={} -- key: 怪物id; value: {怪物id, 此魔物攻擊對象id, 其他人攻擊此魔物的時間, 是否在視線內(RefreshKey|-1), score}
RefreshKey=0
RefreshTime=0
nOwnerEnemy=0
nMyEnemy=0
nRangeEnemy=0
bestTarget=0

-- 植物（保留原清單）
plantsInitFlag=false
plants={
  [3790]=true,[1078]=true,[1079]=true,[1080]=true,[1081]=true,
  [1082]=true,[1083]=true,[1555]=true,[1084]=true
}

-- 由 HomCfg.lua 提供全域表 PriorityTargets（數值型權重）
PriorityTargets = PriorityTargets or {}

friends={}
others={}

AI_SCAN_INTERVAL_MS = AI_SCAN_INTERVAL_MS or 600
AI_CROWD_ACTOR_COUNT = AI_CROWD_ACTOR_COUNT or 60
AI_CROWD_SCAN_INTERVAL_MS = AI_CROWD_SCAN_INTERVAL_MS or 2000
AI_CROWD_SAFE_MODE = (AI_CROWD_SAFE_MODE ~= false)
AI_CROWD_SAFE_SKIP_MS = AI_CROWD_SAFE_SKIP_MS or 8000
AI_CROWD_SAFE_UNTIL = AI_CROWD_SAFE_UNTIL or 0
AI_CROWD_ATTACK_PLAYERS = AI_CROWD_ATTACK_PLAYERS or 0
AI_WOE_PLAYER_SCAN_ONLY = (AI_WOE_PLAYER_SCAN_ONLY ~= false)
AI_WOE_PLAYER_SCAN_ENABLED = AI_WOE_PLAYER_SCAN_ENABLED or 0
AI_CROWD_PLAYER_LOOP = AI_CROWD_PLAYER_LOOP or 80
AI_CROWD_MAX_PLAYER_TARGETS = AI_CROWD_MAX_PLAYER_TARGETS or 8
AI_PLAYER_TARGET_SCORE = AI_PLAYER_TARGET_SCORE or 50000
AI_TRANSITION_PROTECT_MS = AI_TRANSITION_PROTECT_MS or 8000
AI_LAST_ACTOR_COUNT = AI_LAST_ACTOR_COUNT or 0
AI_MAX_ACTORS_LOOP = AI_MAX_ACTORS_LOOP or 96
AI_MAX_ACTORS_PER_REFRESH = AI_MAX_ACTORS_PER_REFRESH or 32
AI_MAX_MOBS_PER_REFRESH = AI_MAX_MOBS_PER_REFRESH or 18
AI_MAX_OTHERS_PER_REFRESH = AI_MAX_OTHERS_PER_REFRESH or 24
AI_MAX_OTHERS_TARGET_CHECK = AI_MAX_OTHERS_TARGET_CHECK or 20
AI_CROWD_MAX_ACTORS_PER_REFRESH = AI_CROWD_MAX_ACTORS_PER_REFRESH or 12
AI_CROWD_MAX_MOBS_PER_REFRESH = AI_CROWD_MAX_MOBS_PER_REFRESH or 8
AI_CROWD_MAX_OTHERS_PER_REFRESH = AI_CROWD_MAX_OTHERS_PER_REFRESH or 20
AI_CROWD_OTHERS_TARGET_CHECK = AI_CROWD_OTHERS_TARGET_CHECK or 12
AI_PLAYER_ID_MIN = AI_PLAYER_ID_MIN or 100000
AI_SCAN_LOG_INTERVAL_MS = AI_SCAN_LOG_INTERVAL_MS or 5000
AI_SCAN_LAST_LOG_TIME = AI_SCAN_LAST_LOG_TIME or 0

AI_WOE_MAPS = AI_WOE_MAPS or {
  prtg_cas01=true, prtg_cas02=true, prtg_cas03=true, prtg_cas04=true, prtg_cas05=true,
  gefg_cas01=true, gefg_cas02=true, gefg_cas03=true, gefg_cas04=true, gefg_cas05=true,
  payg_cas01=true, payg_cas02=true, payg_cas03=true, payg_cas04=true, payg_cas05=true,
  aldeg_cas01=true, aldeg_cas02=true, aldeg_cas03=true, aldeg_cas04=true, aldeg_cas05=true,
  arug_cas01=true, arug_cas02=true, arug_cas03=true, arug_cas04=true, arug_cas05=true,
  schg_cas01=true, schg_cas02=true, schg_cas03=true, schg_cas04=true, schg_cas05=true
}

local function safe_getv(key, id)
  if not id or id == 0 then return nil end
  local ok, a, b = pcall(GetV, key, id)
  if ok then return a, b end
  return nil
end

local function safe_ismonster(id)
  if type(id)~="number" or id <= 0 then return false end
  if AI_PLAYER_ID_MIN and id >= AI_PLAYER_ID_MIN then return false end
  local ok, value = pcall(IsMonster, id)
  return ok and value == 1
end

local function safe_obj_rect_dis(a, b)
  if not a or not b then return nil end
  local ok, value = pcall(getObjRectDis, a, b)
  if ok then return value end
  return nil
end

local function scan_log(t, message)
  if type(AI_Log) ~= "function" then return end
  if t - (AI_SCAN_LAST_LOG_TIME or 0) < (AI_SCAN_LOG_INTERVAL_MS or 5000) then return end
  AI_SCAN_LAST_LOG_TIME = t
  AI_Log(message)
end

local function safe_current_map_name()
  local fnNames={"GetMap","GetMapName","GetCurrentMap","getmap","getmapname"}
  for i,name in ipairs(fnNames)do
    local fn=_G[name]
    if type(fn)=="function" then
      local ok,value=pcall(fn)
      if ok and type(value)=="string" and value~="" then
        value=string.lower(value)
        value=string.gsub(value,"%.gat$","")
        value=string.gsub(value,"%.rsw$","")
        return value
      end
    end
  end
  return nil
end

local function is_woe_map()
  local map=safe_current_map_name()
  if not map then return false end
  if AI_WOE_MAPS and AI_WOE_MAPS[map] then return true end
  if string.find(map,"_cas",1,true) then return true end
  return false
end

local function clear_battle_scan()
  Mobs={}
  others={}
  nOwnerEnemy=0
  nMyEnemy=0
  nRangeEnemy=0
  bestTarget=0
end

local function is_player_id(id)
  return type(id)=="number" and id >= (AI_PLAYER_ID_MIN or 100000)
end

local function can_attack_players()
  if AttackAllEnabled == 0 then return false end
  if AI_CROWD_ATTACK_PLAYERS ~= 1 then return false end
  if not AttackAllExclude or AttackAllExclude.players ~= 0 then return false end
  if AI_WOE_PLAYER_SCAN_ONLY and not is_woe_map() and AI_WOE_PLAYER_SCAN_ENABLED ~= 1 then return false end
  return true
end

local function is_attackable_player(id, myid, oid)
  if not is_player_id(id) then return false end
  if id==myid or id==oid then return false end
  if AttackAllExclude and AttackAllExclude.friends == 1 then
    if tb_exist(friends,id) then return false end
    if isFriend and isFriend(id) then return false end
  end
  return true
end

-- AutoCounterAttack lets a player who is hitting the owner or the
-- homunculus be fought back even while players are excluded.
local function is_counter_target(id, myid, oid)
  if AutoCounterAttack~=1 then return false end
  local tar=safe_getv(V_TARGET,id)
  return tar==myid or tar==oid
end

local function blacklist_bonus(id)
  if BlacklistPriority==1 and isBlacklisted and isBlacklisted(id) then return 10000 end
  return 0
end

-- The sentry point replaces the owner as the centre of the search while
-- SentryMode holds one (set by an Alt+right-click move in Main.lua).
local function anchor_dis(oid, v)
  if SentryX and SentryY then
    local x,y=safe_getv(V_POSITION,v)
    if not x or not y then return 999 end
    return math.max(math.abs(x-SentryX),math.abs(y-SentryY))
  end
  return safe_obj_rect_dis(oid,v) or 999
end

local function collect_player_targets(A, myid, oid)
  local allowAll=can_attack_players()
  if not allowAll and AutoCounterAttack~=1 then return 0 end

  local added=0
  local maxTargets=AI_CROWD_MAX_PLAYER_TARGETS or 8
  local maxLoop=AI_CROWD_PLAYER_LOOP or 80

  for i,id in ipairs(A)do
    if i>maxLoop then break end
    local counter=is_player_id(id) and id~=myid and id~=oid and is_counter_target(id,myid,oid)
    if (allowAll and is_attackable_player(id,myid,oid)) or counter then
      local score=(AI_PLAYER_TARGET_SCORE or 50000)+blacklist_bonus(id)
      if BlacklistOnly==1 and not counter and not (isBlacklisted and isBlacklisted(id)) then
        score=-1
      end

      if score>=0 then
        Mobs[id]={id,0,0,RefreshKey,score,true}
        if bestTarget==0 or score>((Mobs[bestTarget] and Mobs[bestTarget][5]) or -1)then
          bestTarget=id
        end
        nRangeEnemy=nRangeEnemy+1
        added=added+1
        if added>=maxTargets then break end
      end
    end
  end

  return added
end

-- Homunculus classes are 1-52 on the client and 6001-6016/6048-6052 on the
-- server; mercenaries are 6017-6046. Monster classes start at 1001.
local function is_homunculus_type(t)
  return type(t)=="number" and ((t>=1 and t<=52) or (t>=6001 and t<=6016) or (t>=6048 and t<=6052))
end

local function is_mercenary_type(t)
  return type(t)=="number" and t>=6017 and t<=6046
end

local EMPERIUM=1288

-- CooperativeMode: 0 passive and 1 active use SearchSetting (Main.lua picks
-- it per mode), 2 attacks only the owner's target ignoring every filter, and
-- 3 does that while the owner has a target and searches like 1 otherwise.
-- AutoCounterAttack lets anything hitting the owner or homunculus through.
local function monster_score(v,t,myid,oid,otar,mtar,d1,d2,radius)
  local mobType=safe_getv(V_HOMUNTYPE,v[1])
  local mode=CooperativeMode or 1
  local exclude=AttackAllExclude or {}
  if mode==2 or (mode==3 and otar>0 and Mobs[otar]) then
    if v[1]~=otar then return -1 end
    return 100000000+30-d2
  end
  if mobType==EMPERIUM and exclude.emperium==0 then return -1 end
  local counter=AutoCounterAttack==1 and (v[2]==myid or v[2]==oid)
  if not counter then
    if AttackMonstersEnabled==0 then return -1 end
    if exclude.homunculus==1 and is_homunculus_type(mobType) then return -1 end
    if exclude.mercenary==1 and is_mercenary_type(mobType) then return -1 end
    if BlacklistOnly==1 and not (isBlacklisted and isBlacklisted(v[1])) then return -1 end
    -- FollowMode 1 stays at the owner's feet and only hits what is already in reach
    if (FollowMode or 0)>=1 and d2>(AtkDis or 1) and d1>3 then return -1 end
  end

  local score=(d1<=radius)and SearchSetting[8] or SearchSetting[7]

  -- 攻擊目標關係分
  score=score+((v[2]==0 or tb_exist(friends,v[2]))and 0 or
    ((v[2]==oid)and SearchSetting[1] or((v[2]==myid)and SearchSetting[2] or SearchSetting[3])))

  -- 當前主人/生命體目標加權
  score=score+((v[1]==otar)and SearchSetting[4] or 0)
  score=score+((v[1]==mtar)and SearchSetting[5] or 0)

  -- 其他玩家最近攻擊痕跡
  score=score+((t-v[3]<3000)and SearchSetting[6] or 0)

  -- ★ 由 HomCfg.lua 提供的優先權重（priorityTargets: 0=使用，1=關閉）
  if exclude.priorityTargets~=1 and mobType and PriorityTargets[mobType] then
    score=score+PriorityTargets[mobType]
  end

  -- 智慧攻擊順序：清單越前面越優先
  if exclude.smartAttackOrder==1 and type(SmartAttackPriorityOrder)=="table" then
    for i,id in ipairs(SmartAttackPriorityOrder) do
      if id==mobType then score=score+(#SmartAttackPriorityOrder-i+1)*1000; break end
    end
  end

  score=score+blacklist_bonus(v[1])
  if counter and score<0 then score=0 end
  if score<0 then return -1 end

  -- 最終距離處理；華麗金屬與 MVP 加到不會被其他目標搶走
  score=score*100+30-d1-d2
  if mobType==EMPERIUM and exclude.emperium==1 then score=score+10000000 end
  if MVPMode==1 and mobType and MVPTargets and MVPTargets[mobType] then score=score+5000000 end
  return score
end

function XYInMobs(x,y)
  local a,b
  local ok,Objs=pcall(GetActors)
  if not ok or not Objs then return false end
  for i,v in ipairs(Objs)do
    if i > (AI_MAX_ACTORS_PER_REFRESH or 32) then break end
    a,b=safe_getv(V_POSITION,v)
    if(a==x and b==y)then
      return v
    end
  end
  return false
end

function RemoveTarget()
  if bestTarget~=0 and Mobs[bestTarget]~=nil then
    Mobs[bestTarget] = nil
  end
  bestTarget = 0
end

function RefreshData(myid,oid) --更新戰場情報
  local t=GetTick()
  if (AI_CROWD_SAFE_UNTIL or 0) > t then
    if not can_attack_players() then
      clear_battle_scan()
    end
    return
  end
  local scanInterval=AI_SCAN_INTERVAL_MS or 600
  if (CooperativeMode==1 or CooperativeMode==3) and (ActiveModeRefreshInterval or 0)>0 then
    scanInterval=ActiveModeRefreshInterval
  end
  if (AI_LAST_ACTOR_COUNT or 0) >= (AI_CROWD_ACTOR_COUNT or 60) then
    scanInterval=AI_CROWD_SCAN_INTERVAL_MS or 2000
  end
  if(t-RefreshTime>scanInterval)then
    RefreshTime=t
  else
    return
  end
  nOwnerEnemy=0
  nMyEnemy=0
  nRangeEnemy=0
  RefreshKey=(RefreshKey+1)%30000

  local okActors,A=pcall(GetActors)
  if not okActors or not A then
    scan_log(t, "RefreshData skipped: GetActors failed")
    A={}
  end
  AI_LAST_ACTOR_COUNT=#A
  if AI_CROWD_SAFE_MODE and #A >= (AI_CROWD_ACTOR_COUNT or 60) then
    clear_battle_scan()
    local playerTargets=collect_player_targets(A,myid,oid)
    AI_CROWD_SAFE_UNTIL=t+(AI_CROWD_SAFE_SKIP_MS or 8000)
    scan_log(t, "Crowd safe mode: actors=" .. tostring(#A) .. ", player_targets=" .. tostring(playerTargets) .. ", skip_ms=" .. tostring(AI_CROWD_SAFE_SKIP_MS or 8000))
    return
  end
  local idx,tar,isplants,isfriend,mobTypeId
  others={}
  local otar,mtar=safe_getv(V_TARGET,oid),safe_getv(V_TARGET,myid)
  otar=otar or 0
  mtar=mtar or 0
  local processedActors=0
  local processedMobs=0
  local skippedActors=0
  local maxActors=AI_MAX_ACTORS_PER_REFRESH or 32
  local maxMobs=AI_MAX_MOBS_PER_REFRESH or 18
  local maxOthers=AI_MAX_OTHERS_PER_REFRESH or 24
  local maxOtherTargetChecks=AI_MAX_OTHERS_TARGET_CHECK or 20
  local isCrowded=#A >= (AI_CROWD_ACTOR_COUNT or 60)
  if isCrowded then
    maxActors=AI_CROWD_MAX_ACTORS_PER_REFRESH or 12
    maxMobs=AI_CROWD_MAX_MOBS_PER_REFRESH or 8
    maxOthers=AI_CROWD_MAX_OTHERS_PER_REFRESH or 20
    maxOtherTargetChecks=AI_CROWD_OTHERS_TARGET_CHECK or 12
  end
  local radius=RadiusAggr or 12
  if SentryX then radius=SentryRadius or 13 end
  -- 0 = attack plants, 1 = skip every plant, 2 = skip only the sprout (1555)
  local plantRule=(AttackAllExclude and AttackAllExclude.plants) or 1

  -- 更新 Mobs 的 RefreshKey 與 Target，加入新的 Mob，並把其他物件放 others
  for i,v in ipairs(A)do
    if i > (AI_MAX_ACTORS_LOOP or 96) then
      skippedActors = #A - i + 1
      break
    end

    if v~=oid and v~=myid then
      if type(v)=="number" and AI_PLAYER_ID_MIN and v>=AI_PLAYER_ID_MIN then
        if #others<maxOthers and tb_exist(friends,v)==false then
          others[#others+1]=v
        end
      else
        if processedActors >= maxActors then
          skippedActors = #A - i + 1
          break
        end

        processedActors=processedActors+1

        if(safe_ismonster(v)==true)then
          if processedMobs < maxMobs then
            mobTypeId=safe_getv(V_HOMUNTYPE,v)
            isplants=mobTypeId~=nil and plants[mobTypeId]~=nil and
              (plantRule==1 or (plantRule==2 and mobTypeId==1555))

            if(isplants==false)then
              local motion=safe_getv(V_MOTION,v)
              if(motion~=MOTION_DEAD)then
                if Mobs[v]==nil then
                  Mobs[v]={v,0,0,0}
                end

                local ownerDis=anchor_dis(oid,v)
                Mobs[v][4]=(ownerDis>math.min(radius,14)) and -1 or RefreshKey -- 索敵半徑也是追擊上限，超出就放棄
                tar=safe_getv(V_TARGET,v) or 0
                if(tar>0 and safe_ismonster(tar)==false)then
                  Mobs[v][2]=tar
                  if(tar==myid)then
                    nMyEnemy=nMyEnemy+1
                  elseif(tar==oid)then
                    nOwnerEnemy=nOwnerEnemy+1
                  end
                end
                if(ownerDis<=radius)then
                  nRangeEnemy=nRangeEnemy+1
                end
                processedMobs=processedMobs+1
              end
            end
          end
        elseif(#others<maxOthers and tb_exist(friends,v)==false)then
          others[#others+1]=v
        end
      end
    end
  end
  collect_player_targets(A,myid,oid)
  if skippedActors > 0 then
    scan_log(t, "RefreshData limited: actors=" .. tostring(#A) .. ", crowded=" .. tostring(isCrowded) .. ", processed=" .. tostring(processedActors) .. ", mobs=" .. tostring(processedMobs) .. ", others=" .. tostring(#others))
  end
  for k,mobInfo in pairs(Mobs) do
    if mobInfo[4]~=RefreshKey then
      Mobs[k]=nil
    end
  end

  -- 更新被其他玩家攻擊的時間
  for i,v in ipairs(others)do
    if i > maxOtherTargetChecks then break end
    tar=safe_getv(V_TARGET,v) or 0
    if AutoBlacklist==1 and addEnemyToBlacklist and is_player_id(v) and (tar==myid or tar==oid) then
      addEnemyToBlacklist(v,"攻擊主人或生命體")
    end
    if(tar>0 and safe_ismonster(tar)==true)then
      if(Mobs[tar]~=nil)then
        Mobs[tar][3]=t
      end
    end
  end

  -- 目標挑選與計分
  local max_score,score=-10000,0
  local d1,d2
  bestTarget=0
  for i,v in pairs(Mobs)do
    d1=safe_obj_rect_dis(oid,v[1]) or 999
    d2=safe_obj_rect_dis(myid,v[1]) or 999

    if v[6]==true or is_player_id(v[1]) then
      local counter=is_counter_target(v[1],myid,oid)
      if (can_attack_players() and is_attackable_player(v[1],myid,oid)) or counter then
        score=(AI_PLAYER_TARGET_SCORE or 50000)+blacklist_bonus(v[1])
        if BlacklistOnly==1 and not counter and not (isBlacklisted and isBlacklisted(v[1])) then
          score=-1
        end

        if score>=0 then
          score=score+((v[1]==otar)and SearchSetting[4] or 0)
          score=score+((v[1]==mtar)and SearchSetting[5] or 0)
          score=score+30-d1-d2
        end
      else
        score=-1
      end
    else
      score=monster_score(v,t,myid,oid,otar,mtar,anchor_dis(oid,v[1]),d2,radius)
    end

    -- 暫時跳過打不到的目標（例如隔牆），由 Main.lua 的追擊逾時判定
    if TargetSkipUntil and (TargetSkipUntil[v[1]] or 0)>t then score=-1 end

    Mobs[i][5]=score
    if(score>max_score)then
      max_score=score
      bestTarget=i
    end
  end
end
