function autoUseSkill(myId, ownerId, mobId, excludeWhen)
  -- 確保全域變數已初始化
  nMyEnemy = nMyEnemy or 0
  nOwnerEnemy = nOwnerEnemy or 0
  nRangeEnemy = nRangeEnemy or 0

  local minRadius = 100
  local r = {
    [0] = getObjRectDis(myId, mobId),
    [1] = getObjRectDis(myId, ownerId),
    [2] = 0
  }
  local targets = { [0]=mobId, [1]=ownerId, [2]=myId }
  local t = GetTick()
  
  local maxSP = GetV(V_MAXSP, myId)
  local currentSP = GetV(V_SP, myId)
  local spPct = (currentSP and maxSP and maxSP > 0) and (currentSP / maxSP * 100) or 0
  local curSP = currentSP or 0

  local usedFlag = false
  local bestSkill = nil
  local bestSkillPriority = -1
  local targetIsPlayer = (mobId >= MIN_PLAYER_CODE)
  local canUseSkill = (SkillMode ~= 3) and (not (SkillMode == 2 and not targetIsPlayer))

  for i, sk in ipairs(Skill) do
    local skillKey = "Skill_" .. (sk.id or 0) .. (sk.lv and ("_lv" .. sk.lv) or "")
    local skillEnabled = true
    local effectiveDelay = sk.delay or 0
    local skRange = sk.range or 1
    local skTarget = sk.target or 0
    
    -- 🔧 射程補償：果凍善變射程鎖定為 9
    if sk.id == 8013 then skRange = 9 end

    if SkillConfig and SkillConfig[skillKey] then
      local cfg = SkillConfig[skillKey]
      if cfg.enabled == false then skillEnabled = false end
      effectiveDelay = cfg.delay or effectiveDelay
      if cfg.spMinPercent then
        sk._tempSpMin = cfg.spMinPercent
      else
        sk._tempSpMin = nil
      end
    end

    if skillEnabled then
      local absOK = (sk.spAbsMin == nil) or (curSP >= sk.spAbsMin)
      local healOK = true

      -- 治癒/輔助條件檢查
      if sk.healCheck or sk.ownerHealCheck or sk.selfHealCheck then
        local threshold = 0.5
        local targetToCheck = ownerId
        if sk.selfHealCheck then targetToCheck = myId end
        
        local cfgKey = sk.selfHealCheck and "selfHpThreshold" or "ownerHpThreshold"
        if SkillConfig and SkillConfig[skillKey] and SkillConfig[skillKey][cfgKey] then
          threshold = SkillConfig[skillKey][cfgKey] / 100
        end

        if threshold > 0 then
          local hp = getCachedStat(V_HP, targetToCheck) or GetV(V_HP, targetToCheck)
          local maxHp = getCachedStat(V_MAXHP, targetToCheck) or GetV(V_MAXHP, targetToCheck)
          healOK = (hp and maxHp and maxHp > 0) and (hp / maxHp < threshold) or false
        end
      end

      -- 混亂祈福緊急治療優化
      if sk.id == 8014 and sk.lv == 3 and sk.ownerHealCheck and healOK then
        effectiveDelay = 500
      end

      local skillAllowed = (sk.id == 0) or canUseSkill
      local healExempt = (sk.ownerHealCheck or sk.selfHealCheck) and healOK
      local whenOK = (sk.when ~= excludeWhen) or healExempt
      
      -- 善變在戰鬥中強制開啟 whenOK
      if sk.id == 8013 and mobId > 0 then whenOK = true end

      local rangeOK = (skRange <= 0) or (r[skTarget] and r[skTarget] <= skRange)
      local spMin = sk._tempSpMin or (sk.sp and sk.sp[1]) or 0
      local spMax = (sk.sp and sk.sp[2]) or 100

      if skillAllowed and whenOK and t - (sk.stemp or 0) >= effectiveDelay and
         spMin <= spPct and spPct <= spMax and absOK and healOK and
         (nOwnerEnemy or 0) >= (sk.nOwnerEnemy or 0) and 
         (nMyEnemy or 0) >= (sk.nMyEnemy or 0) and 
         (nRangeEnemy or 0) >= (sk.nRangeEnemy or 0) and 
         rangeOK
      then
        local priority = sk.priority or (100 - i)
        if sk.nOwnerEnemy and sk.nOwnerEnemy > 0 and (nOwnerEnemy or 0) > 0 then priority = priority + 50
        elseif sk.nMyEnemy and sk.nMyEnemy > 0 and (nMyEnemy or 0) > 0 then priority = priority + 30
        elseif sk.id ~= 0 then priority = priority + 20 end

        if priority > bestSkillPriority then
          bestSkill = sk
          bestSkillPriority = priority
        end
      end

      -- 追蹤最小移動半徑
      if mobId > 0 and (sk.id == 0 or (sk.chase or 0) == 1) and r[skTarget] and skRange > 0 then
        if r[skTarget] > skRange and minRadius > skRange then
          minRadius = skRange
        end
      end
    end
  end

  if bestSkill and not usedFlag then
    if bestSkill.id ~= 0 then
      local skillTargetId = targets[bestSkill.target or 0] or 0
      
      -- 補全屬性以防 initialization 失敗
      local castType = bestSkill.castType
      if not castType then
        if SkillData[bestSkill.id] then
          castType = SkillData[bestSkill.id][1]
        else
          castType = 1
        end
      end

      if castType == 0 then
        SkillObject(myId, bestSkill.lv, bestSkill.id, myId)
        usedFlag = true
      elseif castType == 1 and skillTargetId > 0 then
        SkillObject(myId, bestSkill.lv, bestSkill.id, skillTargetId)
        usedFlag = true
      elseif castType == 2 and skillTargetId > 0 then
        local x, y = getCachedPosition(skillTargetId)
        if x and y then
          SkillGround(myId, bestSkill.lv, bestSkill.id, x, y)
          usedFlag = true
        end
      end
      
      if usedFlag then
        bestSkill.count = (bestSkill.count or 0) + 1
        if bestSkill.count >= (bestSkill.times or 1) then
          bestSkill.count = 0
          bestSkill.stemp = t
        end
      end
    end
  end

  return (minRadius >= 100) and false or minRadius, usedFlag
end
