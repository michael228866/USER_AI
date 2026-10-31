local function LoadModules()
  dofile("./AI/USER_AI/RNAI/Const.lua")
  dofile("./AI/USER_AI/RNAI/SkillData.lua")
  dofile("./AI/USER_AI/RNAI/HomCfg.lua")
  dofile("./AI/USER_AI/RNAI/Geo.lua")
  dofile("./AI/USER_AI/RNAI/tablelib.lua")
  dofile("./AI/USER_AI/RNAI/MapInfo.lua")
  dofile("./AI/USER_AI/RNAI/friends.lua")
  dofile("./AI/USER_AI/RNAI/enemies.lua")
  dofile("./AI/USER_AI/RNAI/Main.lua")
  return AI
end

-- The manager writes this file after installing an update, so a change
-- means new AI files are in place and can be loaded without re-summoning.
local REVISION_FILE = "./AI/USER_AI/installed_revision.txt"
local function ReadRevision()
  if type(io) ~= "table" or type(io.open) ~= "function" then return nil end
  local fp = io.open(REVISION_FILE, "r")
  if not fp then return nil end
  local text = fp:read("*a")
  fp:close()
  return text
end

local runAI = LoadModules()
local revision, revisionCheckTick = ReadRevision(), 0
local idleTest = false
if type(io) == "table" and type(io.open) == "function" then
  local flag = io.open("./AI/USER_AI/RNAI/custom/diagnostic_idle_ai.flag", "r")
  if flag then
    flag:close()
    idleTest = true
    if type(AI_Log) == "function" then pcall(AI_Log, "AI diagnostic idle enabled") end
  end
end

local function CheckForUpdatedAI()
  local now = GetTick()
  if now - revisionCheckTick < 2000 then return end
  revisionCheckTick = now
  local current = ReadRevision()
  if not current or current == revision then return end
  revision = current
  local ok, result = pcall(LoadModules)
  if ok then runAI = result end
  if type(AI_Log) == "function" then
    pcall(AI_Log, ok and "AI files updated, reloaded" or ("AI reload failed: " .. tostring(result)))
  end
end

local wrapper
wrapper = function(myid)
  if idleTest then return end
  CheckForUpdatedAI()
  AI = wrapper -- Main.lua redefines AI when it is reloaded
  local ok, err = pcall(runAI, myid)
  if not ok and type(AI_Log) == "function" then
    pcall(AI_Log, "AI callback error: " .. tostring(err))
  end
end
AI = wrapper
