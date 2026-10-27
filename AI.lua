dofile("./AI/USER_AI/RNAI/Const.lua")
dofile("./AI/USER_AI/RNAI/SkillData.lua")
dofile("./AI/USER_AI/RNAI/HomCfg.lua")
dofile("./AI/USER_AI/RNAI/Geo.lua")
dofile("./AI/USER_AI/RNAI/tablelib.lua")
dofile("./AI/USER_AI/RNAI/MapInfo.lua")
dofile("./AI/USER_AI/RNAI/Main.lua")

local runAI = AI
local idleTest = false
if type(io) == "table" and type(io.open) == "function" then
  local flag = io.open("./AI/USER_AI/RNAI/custom/diagnostic_idle_ai.flag", "r")
  if flag then
    flag:close()
    idleTest = true
    if type(AI_Log) == "function" then pcall(AI_Log, "AI diagnostic idle enabled") end
  end
end
function AI(myid)
  if idleTest then return end
  local ok, err = pcall(runAI, myid)
  if not ok and type(AI_Log) == "function" then
    pcall(AI_Log, "AI callback error: " .. tostring(err))
  end
end
