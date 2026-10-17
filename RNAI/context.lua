-- AI/USER_AI/RNAI/context.lua
-- ???? Tick ?????????? C API ???

local C_GetV = GetV
local C_GetActors = GetActors

local NIL_SENTINEL = {}

local Context = {}
Context.__index = Context

function Context.new()
  local self = {
    tick = -1,
    scalars = {},
    positions = {},
    actorsList = nil,
    skillRange = {},
  }
  return setmetatable(self, Context)
end

local function cacheScalar(container, key, value)
  if value == nil then
    container[key] = NIL_SENTINEL
  else
    container[key] = value
  end
end

local function readScalar(container, key)
  local cached = container[key]
  if cached == nil then
    return nil, false
  end
  if cached == NIL_SENTINEL then
    return nil, true
  end
  return cached, true
end

function Context:beginTick(tick)
  if self.tick ~= tick then
    self.tick = tick
    self.scalars = {}
    self.positions = {}
    self.actorsList = nil
    self.skillRange = {}
  end
end

function Context:getStat(vType, id)
  if not id or id <= 0 then
    return nil
  end

  local byType = self.scalars[vType]
  if not byType then
    byType = {}
    self.scalars[vType] = byType
  end

  local value, hasValue = readScalar(byType, id)
  if hasValue then
    return value
  end

  local ok, result = pcall(C_GetV, vType, id)
  if not ok then
    cacheScalar(byType, id, nil)
    return nil
  end

  cacheScalar(byType, id, result)
  return result
end

function Context:getOwner(id)
  return self:getStat(V_OWNER, id)
end

function Context:getMotion(id)
  return self:getStat(V_MOTION, id)
end

function Context:getAttackRange(id)
  return self:getStat(V_ATTACKRANGE, id)
end

function Context:getHP(id)
  return self:getStat(V_HP, id)
end

function Context:getMaxHP(id)
  return self:getStat(V_MAXHP, id)
end

function Context:getSP(id)
  return self:getStat(V_SP, id)
end

function Context:getMaxSP(id)
  return self:getStat(V_MAXSP, id)
end

function Context:getHomunType(id)
  return self:getStat(V_HOMUNTYPE, id)
end

function Context:getPosition(id)
  if not id or id <= 0 then
    return nil, nil
  end

  local cached = self.positions[id]
  if cached then
    return cached[1], cached[2]
  end

  local ok, x, y = pcall(C_GetV, V_POSITION, id)
  if not ok then
    self.positions[id] = { nil, nil }
    return nil, nil
  end

  self.positions[id] = { x, y }
  return x, y
end

function Context:getSkillRange(id, skillId, level)
  if not id or id <= 0 or not skillId or not level then
    return nil
  end

  local byActor = self.skillRange[id]
  if not byActor then
    byActor = {}
    self.skillRange[id] = byActor
  end

  local bySkill = byActor[skillId]
  if not bySkill then
    bySkill = {}
    byActor[skillId] = bySkill
  end

  local value, hasValue = readScalar(bySkill, level)
  if hasValue then
    return value
  end

  local ok, result = pcall(C_GetV, V_SKILLATTACKRANGE_LEVEL, id, skillId, level)
  if not ok then
    cacheScalar(bySkill, level, nil)
    return nil
  end

  cacheScalar(bySkill, level, result)
  return result
end

function Context:getActors()
  if self.actorsList ~= nil then
    return self.actorsList
  end

  local ok, actors = pcall(C_GetActors)
  if not ok or actors == nil then
    actors = {}
  end

  self.actorsList = actors
  return actors
end

return Context
