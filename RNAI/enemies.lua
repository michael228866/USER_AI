-- ./AI/USER_AI/RNAI/enemies.lua
-- 黑名單管理系統

-- 黑名單列表（擴展結構：包含ID、類型、原因、時間）
enemies_blacklist = enemies_blacklist or {}
-- 黑名單查詢表（用於加速查詢）
enemies_lookup = enemies_lookup or {}
-- 黑名單詳細信息表（ID -> {type, reason, time}）
enemies_details = enemies_details or {}
-- 攻擊歷史記錄（ID -> 最後攻擊時間）
local attack_history = attack_history or {}
local ATTACK_MEMORY_DURATION = 3000  -- 3秒記憶時間

local enemiesFilePath = "./AI/USER_AI/RNAI/custom/enemies_list.lua"
local MAGIC_BASE = 7  -- 基礎值
local MAGIC_FACTOR = 3  -- 乘數因子
local MAX_ENEMIES = MAGIC_BASE + MAGIC_FACTOR  -- 運算結果 = 10

-- 記錄攻擊事件
function recordAttackEvent(id, reason)
    local currentTime = GetTick and GetTick() or 0
    attack_history[id] = {
        time = currentTime,
        reason = reason
    }
end

-- 檢查是否有未處理的攻擊記錄
function checkPendingAttacks()
    local currentTime = GetTick and GetTick() or 0
    local processed = {}

    for id, record in pairs(attack_history) do
        if currentTime - record.time <= ATTACK_MEMORY_DURATION then
            -- 在記憶時間內，嘗試加入黑名單
            if AutoBlacklist and AutoBlacklist == 1 and addEnemyToBlacklist then
                addEnemyToBlacklist(id, record.reason)
            end
            processed[id] = true
        else
            -- 超過記憶時間，清理記錄
            processed[id] = true
        end
    end

    -- 清理已處理的記錄
    for id in pairs(processed) do
        attack_history[id] = nil
    end
end

-- 同步查詢表與陣列
local function syncEnemiesLookup()
    enemies_lookup = {}
    for i, id in ipairs(enemies_blacklist) do
        enemies_lookup[id] = true
    end
end

-- 判斷ID類型
local function getIdType(id)
    if id >= 100000 then
        return "玩家"
    else
        return "怪物"
    end
end

function saveEnemiesToFile()
    local file = io.open(enemiesFilePath, "w")
    if not file then
        return false
    end

    local success, err = pcall(function()
        file:write("-- 黑名單 (Enemies List)\n")
        file:write("-- 更新時間: " .. os.date("%Y-%m-%d %H:%M:%S") .. "\n")
        file:write("-- 黑名單總數: " .. #enemies_blacklist .. "\n\n")

        file:write("enemies_data = {\n")
        if #enemies_blacklist == 0 then
            file:write("    -- 格式: [索引] = ID, -- 敵人類型\n")
        else
            for i, id in ipairs(enemies_blacklist) do
                local details = enemies_details[id] or {}
                local idType = details.type or getIdType(id)

                if idType == "玩家" then
                    file:write(string.format("    [%d] = %d, -- 玩家\n", i, id))
                else
                    file:write(string.format("    [%d] = %d, -- 怪物\n", i, id))
                end
            end
        end
        file:write("}\n")

    end)

    file:close()

    if success then
        syncEnemiesLookup()  -- 同步查詢表
        return true
    else
        return false
    end
end

function loadEnemiesFromFile()
    local file = io.open(enemiesFilePath, "r")
    if not file then
        return false
    end

    local content = file:read("*all")
    file:close()

    if not content then
        return false
    end

    enemies_blacklist = {}  -- 重置黑名單
    enemies_lookup = {}  -- 重置查詢表
    enemies_details = {}  -- 重置詳細信息
    local loadedCount = 0
    local cleanedCount = 0

    -- 解析黑名單格式
    for line in content:gmatch("[^\r\n]+") do
        -- 優先匹配簡單格式：[數字] = ID,  -- 註解
        local enemyId = line:match("%[%d+%] = (%d+)")

        if enemyId then
            enemyId = tonumber(enemyId)
            if enemyId and enemyId >= 1 and enemyId <= 10000000 then
                enemies_blacklist[#enemies_blacklist + 1] = enemyId
                enemies_lookup[enemyId] = true  -- 同時更新查詢表
                enemies_details[enemyId] = {
                    type = getIdType(enemyId),
                    reason = "載入時恢復",
                    time = os.date("%m-%d %H:%M")
                }
                loadedCount = loadedCount + 1
            else
                cleanedCount = cleanedCount + 1
            end
        end
    end

    if cleanedCount > 0 then
        TraceAI(string.format("清理了 %d 個無效的敵人ID", cleanedCount))
    end

    if loadedCount > 0 then
        TraceAI(string.format("【黑名單】成功載入 %d 個敵人", loadedCount))
        return true
    else
        -- TraceAI("文件中沒有找到有效的敵人ID")
        return false
    end
end

function clearEnemies()
    local count = #enemies_blacklist
    enemies_blacklist = {}
    enemies_lookup = {}  -- 清空查詢表
    enemies_details = {}  -- 清空詳細信息
    TraceAI(string.format("已清空所有黑名單 (共移除 %d 個敵人)", count))
    saveEnemiesToFile()
end

function addEnemyToBlacklist(id, reason)
    if not id or id <= 0 then
        return false
    end

    local actualId = id
    local idType = getIdType(id)

    -- 如果是怪物，使用怪物類型ID（職業代碼）便於手動管理
    if idType == "怪物" then
        local ok, monsterType = pcall(function() return GetV(V_HOMUNTYPE, id) end)
        if ok and monsterType and monsterType > 0 then
            actualId = monsterType  -- 使用怪物類型ID
        end
    end

    -- 擴展ID範圍檢查：支援怪物ID (1-99999) 和玩家ID (100000-10000000)
    if actualId > 10000000 then
        -- TraceAI(string.format("ID:%d 超出合理範圍，跳過", actualId))
        return false
    end

    -- 檢查是否為朋友（互斥邏輯）
    if isFriend and isFriend(actualId) then
        return false
    end

    if _k2 and _k2(actualId)then
        return false
    end

    -- 使用查詢表快速檢查（O(1) 複雜度）- 避免重複添加相同ID
    if enemies_lookup[actualId] then
        -- TraceAI(string.format("ID:%d 已存在於黑名單中", actualId))
        return false  -- 已存在，跳過
    end

    -- 檢查黑名單人數上限
    if #enemies_blacklist >= MAX_ENEMIES then
        -- 移除最舊的記錄（FIFO）
        local oldestId = enemies_blacklist[1]
        local oldestDetails = enemies_details[oldestId] or {}
        local oldestType = oldestDetails.type or getIdType(oldestId)

        table.remove(enemies_blacklist, 1)
        enemies_lookup[oldestId] = nil
        enemies_details[oldestId] = nil
    end

    local reasonText = reason or "攻擊行為"
    local timeStr = os.date("%m-%d %H:%M")

    enemies_blacklist[#enemies_blacklist + 1] = actualId
    enemies_lookup[actualId] = true  -- 更新查詢表
    enemies_details[actualId] = {
        type = idType,
        reason = reasonText,
        time = timeStr
    }

    saveEnemiesToFile()
    return true
end

-- 優化版：使用查詢表的 isBlacklisted 函數（O(1) 複雜度）
function isBlacklisted(id)
    -- 直接檢查ID
    if enemies_lookup[id] == true then
        return true
    end

    -- 如果是怪物實例ID，檢查其類型ID是否在黑名單中
    if getIdType(id) == "怪物" then
        local ok, monsterType = pcall(function() return GetV(V_HOMUNTYPE, id) end)
        if ok and monsterType and monsterType > 0 then
            return enemies_lookup[monsterType] == true
        end
    end

    return false
end

function removeEnemyFromBlacklist(id)
    for i, v in ipairs(enemies_blacklist) do
        if v == id then
            local details = enemies_details[id] or {}
            local idType = details.type or getIdType(id)

            table.remove(enemies_blacklist, i)
            enemies_lookup[id] = nil  -- 從查詢表移除
            enemies_details[id] = nil  -- 從詳細信息移除
            TraceAI(string.format("【黑名單】已移除%s ID:%d", idType, id))
            saveEnemiesToFile()
            return true
        end
    end
    return false
end

function showEnemies()
    if #enemies_blacklist == 0 then
        TraceAI("【黑名單】目前無敵人記錄")
        return
    end

    local playerCount = 0
    local monsterCount = 0

    TraceAI("================================================")
    TraceAI("             當前黑名單")
    TraceAI("================================================")
    for i, id in ipairs(enemies_blacklist) do
        local details = enemies_details[id] or {}
        local idType = details.type or getIdType(id)
        local reason = details.reason or "未知原因"
        local timeStr = details.time or "未知時間"
        if idType == "玩家" then
            playerCount = playerCount + 1
        else
            monsterCount = monsterCount + 1
        end

        local displayText = idType
        if idType == "怪物" then
            displayText = string.format("怪物類型ID %d", id)
        else
            displayText = string.format("%s ID %d", idType, id)
        end

        TraceAI(string.format("%d: %s (%s, %s)", i, displayText, reason, timeStr))
    end
    TraceAI("------------------------------------------------")
    TraceAI(string.format("統計: 玩家 %d 個, 怪物 %d 個, 總計 %d 個", playerCount, monsterCount, #enemies_blacklist))
    TraceAI("================================================")
end

-- 獲取黑名單數量
function getBlacklistCount()
    return #enemies_blacklist
end

-- 去除重複ID的清理函數
function cleanupDuplicateEnemies()
    local uniqueIds = {}
    local cleanList = {}
    local cleanLookup = {}
    local cleanDetails = {}
    local removedCount = 0

    for _, id in ipairs(enemies_blacklist) do
        if not uniqueIds[id] then
            uniqueIds[id] = true
            table.insert(cleanList, id)
            cleanLookup[id] = true
            cleanDetails[id] = enemies_details[id]
        else
            removedCount = removedCount + 1
        end
    end

    enemies_blacklist = cleanList
    enemies_lookup = cleanLookup
    enemies_details = cleanDetails

    if removedCount > 0 then
        TraceAI(string.format("【黑名單清理】移除了 %d 個重複ID，剩餘 %d 個唯一敵人", removedCount, #enemies_blacklist))
        saveEnemiesToFile()
        return removedCount
    end
    return 0
end

-- 初始化黑名單管理系統
local loaded = loadEnemiesFromFile()
if not loaded then
    -- 如果無法載入，創建空的黑名單文件
    saveEnemiesToFile()
    TraceAI("【黑名單】初始化空的黑名單系統")
else
    -- 載入成功，執行去重清理
    cleanupDuplicateEnemies()
end

-- 手動添加黑名單函數（用戶自行添加）
function addToBlacklist(id, reason)
    if not id or type(id) ~= "number" then
        TraceAI("錯誤：請提供有效的ID數字")
        return false
    end

    if id <= 0 or id > 10000000 then
        TraceAI("錯誤：ID範圍必須在 1-10000000 之間")
        return false
    end

    -- 檢查是否已存在
    if enemies_lookup[id] then
        TraceAI(string.format("ID %d 已存在於黑名單中", id))
        return false
    end

    -- 添加到黑名單
    enemies_blacklist[#enemies_blacklist + 1] = id
    enemies_lookup[id] = true
    enemies_details[id] = {
        type = getIdType(id),
        reason = reason or "手動添加",
        time = os.date("%m-%d %H:%M")
    }

    saveEnemiesToFile()
    TraceAI(string.format("成功添加 %s ID:%d 到黑名單", getIdType(id), id))
    return true
end

-- 手動移除黑名單函數
function removeFromBlacklist(id)
    if not id or type(id) ~= "number" then
        TraceAI("錯誤：請提供有效的ID數字")
        return false
    end

    for i, v in ipairs(enemies_blacklist) do
        if v == id then
            table.remove(enemies_blacklist, i)
            enemies_lookup[id] = nil
            enemies_details[id] = nil
            saveEnemiesToFile()
            TraceAI(string.format("成功移除 %s ID:%d 從黑名單", getIdType(id), id))
            return true
        end
    end

    TraceAI(string.format("ID %d 不在黑名單中", id))
    return false
end
