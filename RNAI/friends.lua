-- ./AI/USER_AI/RNAI/friends.lua
-- 朋友管理系統（優化版）

-- 朋友列表（強制重置，避免記憶體殘留）
friends = {}
-- 新增：朋友查詢表（用於加速查詢）
friends_lookup = {}

local friendsFilePath = "./AI/USER_AI/RNAI/custom/friends_list.lua"

-- 同步查詢表與陣列
local function syncFriendsLookup()
    friends_lookup = {}
    for i, id in ipairs(friends) do
        friends_lookup[id] = true
    end
end

function saveFriendsToFile()
    -- 保存時不需要同步，因為我們要寫入新資料

    local file = io.open(friendsFilePath, "w")
    if not file then
        -- TraceAI("錯誤: 無法創建朋友列表文件 - " .. friendsFilePath)
        return false
    end

    local success, err = pcall(function()
        file:write("-- ============================================\n")
        file:write("-- 朋友列表 (Friends List)\n")
        file:write("-- ============================================\n\n")

        file:write("-- 更新時間: " .. os.date("%Y-%m-%d %H:%M:%S") .. "\n")
        file:write("-- 朋友總數: " .. #friends .. "\n")
        file:write("-- 文件位置: " .. friendsFilePath .. "\n\n")

        file:write("friends_data = {\n")
        if #friends == 0 then
            file:write("    -- 朋友列表為空\n")
        else
            for i, id in ipairs(friends) do
                file:write(string.format("    [%d] = %d,  -- 朋友 %d\n", i, id, i))
            end
        end
        file:write("}\n\n")

        file:write("--[[\n")
        file:write("=== 詳細操作說明 ===\n\n")
        file:write("【添加單個朋友】\n")
        file:write("  操作: ALT + SHIFT + 右鍵點擊目標\n")
        file:write("  說明: 對任何玩家或生命體使用攻擊指令即可添加為朋友\n\n")

        file:write("【清空朋友列表】\n")
        file:write("  操作: ALT + SHIFT + 右鍵點擊自己的生命體\n")
        file:write("  說明: 對自己使用攻擊指令會清空所有朋友\n\n")

        file:write("【批量添加朋友（矩形範圍）】\n")
        file:write("  步驟1: ALT + 右鍵點擊第一個角點 (移動指令)\n")
        file:write("  步驟2: ALT + SHIFT + 右鍵點擊第二個角點（生命體還在走向第一點時）\n")
        file:write("  說明: 會選取矩形範圍內所有玩家/生命體為朋友\n\n")

        file:write("【全視野範圍添加朋友】\n")
        file:write("  操作: 生命體跟隨時，ALT+SHIFT+右鍵點一下空地\n")
        file:write("  說明: 畫面內所有玩家立即加為朋友（無上限）；不要點到自己的生命體，那會清空名單\n\n")

        file:write("--]]\n")
    end)

    file:close()

    if success then
        syncFriendsLookup()  -- 同步查詢表
        return true
    else
        return false
    end
end

local friendsSource, friendsSyncTick

-- The manager and other game windows write this file too, so pick up their
-- changes; a half-written file (no closing brace yet) waits for the next try.
local function syncFriendsFromFile()
    local file = io.open(friendsFilePath, "r")
    if not file then return end
    local content = file:read("*all")
    file:close()
    if content and content ~= friendsSource and content:find("friends_data%s*=%s*{.-}") then
        loadFriendsFromFile()
    end
end

function SyncFriendsTick(now)
    if friendsSyncTick and now - friendsSyncTick < 2000 then return end
    friendsSyncTick = now
    syncFriendsFromFile()
end

function loadFriendsFromFile()
    local file = io.open(friendsFilePath, "r")
    if not file then
        return false
    end

    local content = file:read("*all")
    file:close()

    if not content then
        return false
    end

    friendsSource = content
    friends = {}  -- 重置朋友列表
    friends_lookup = {}  -- 重置查詢表
    local loadedCount = 0
    local cleanedCount = 0

    for id in content:gmatch("%[%d+%] = (%d+)") do
        local friendId = tonumber(id)
        -- 過濾掉無效的 ID（只保留 100000 到 10000000 之間的 ID）
        if friendId and friendId >= 100000 and friendId <= 10000000 then
            friends[#friends + 1] = friendId
            friends_lookup[friendId] = true  -- 同時更新查詢表
            loadedCount = loadedCount + 1
        elseif friendId then
            cleanedCount = cleanedCount + 1
        end
    end

    if cleanedCount > 0 then
        TraceAI(string.format("清理了 %d 個無效的朋友ID", cleanedCount))
    end

    if loadedCount > 0 then
        -- TraceAI(string.format("成功載入 %d 個朋友從文件", loadedCount))
        return true
    else
        -- TraceAI("文件中沒有找到有效的朋友ID")
        return false
    end
end

function clearFriends()
    local count = #friends
    friends = {}
    friends_lookup = {}  -- 清空查詢表
    -- TraceAI(string.format("已清空所有好友清單 (共移除 %d 個好友)", count))
    saveFriendsToFile()
end

function addFriend(id, batch)
    if not id or id <= 0 then
        return false
    end

    -- 檢查 ID 是否在合理範圍內（玩家ID通常在 100000 到 10000000 之間）
    if id < 100000 or id > 10000000 then
        -- TraceAI(string.format("ID:%d 超出合理範圍，跳過", id))
        return false
    end

    -- Merge first: saving writes the whole list and would drop another window's additions.
    -- A batch (whole screen or a box) syncs once before and saves once after instead.
    if not batch then syncFriendsFromFile() end

    -- 檢查是否為黑名單敵人（互斥邏輯）
    if isBlacklisted and isBlacklisted(id) then
        TraceAI(string.format("【朋友】玩家 ID:%d 是黑名單敵人，無法加入朋友列表", id))
        return false
    end

    -- 使用查詢表快速檢查（O(1) 複雜度）
    if friends_lookup[id] then
        -- TraceAI(string.format("ID:%d 已在好友清單中", id))
        return false
    end

    friends[#friends + 1] = id
    friends_lookup[id] = true  -- 更新查詢表
    -- TraceAI(string.format("已添加好友 ID:%d (好友總數: %d)", id, #friends))
    if not batch then saveFriendsToFile() end
    return true
end

function addFriendsInRange(x1, y1, x2, y2, myid, oid)
    local addedCount = 0

    if not myid or not oid then
        return 0
    end

    -- 只在批量操作開始時同步一次
    syncFriendsFromFile()

    local Objs = GetActors()
    if not Objs then
        Objs = {}
    end

    -- 正常處理範圍內的單位
    for i, v in ipairs(Objs) do
        if v ~= myid and v ~= oid then
            local x, y = GetV(V_POSITION, v)
            if x and y and x >= x1 and x <= x2 and y >= y1 and y <= y2 then
                -- addFriend keeps only player IDs; IsMonster is not asked because it
                -- reports other guilds' players as monsters on siege maps.
                if addFriend(v, true) then
                    addedCount = addedCount + 1
                end
            end
        end
    end

    saveFriendsToFile()
    TraceAI(string.format("範圍框選：在(%d,%d)-(%d,%d)範圍內添加了 %d 個朋友", x1, y1, x2, y2, addedCount))
    return addedCount
end

-- 新增全視野添加朋友函數（專用於穿透模式）
function addAllVisibleFriends(myid, oid)
    if not myid or not oid then
        return 0
    end
    syncFriendsFromFile()

    -- Counts are logged so a short result can be explained from ai.log.
    -- Player IDs are judged by range like the attack code, not by IsMonster,
    -- which reports other guilds' players as monsters on siege maps.
    local actors = GetActors() or {}
    local added, already, blocked, monsters, others = 0, 0, 0, 0, 0
    for _, id in ipairs(actors) do
        if id ~= myid and id ~= oid then
            local ok, monster = pcall(IsMonster, id)
            if id < 100000 or id > 10000000 then
                if ok and monster == 1 then monsters = monsters + 1 else others = others + 1 end  -- monsters; homunculi, pets, NPCs
            elseif isFriend(id) then
                already = already + 1
            elseif addFriend(id, true) then
                added = added + 1
            else
                blocked = blocked + 1  -- blacklisted
            end
        end
    end

    saveFriendsToFile()
    if AI_Log then
        AI_Log(string.format("friends scan: actors=%d added=%d already=%d blacklisted=%d monsters=%d others=%d total=%d",
            #actors, added, already, blocked, monsters, others, #friends))
    end
    return added
end

function showFriends()
    if #friends == 0 then
        return
    end

    for i, id in ipairs(friends) do
    end
end

-- 優化版：使用查詢表的 isFriend 函數（O(1) 複雜度）
function isFriend(id)
    return friends_lookup[id] == true
end

function removeFriend(id)
    for i, v in ipairs(friends) do
        if v == id then
            table.remove(friends, i)
            friends_lookup[id] = nil  -- 從查詢表移除
            saveFriendsToFile()
            return true
        end
    end
    return false
end

-- 初始化朋友管理系統
local loaded = loadFriendsFromFile()
if not loaded then
    -- 如果無法載入，創建空的朋友列表文件
    saveFriendsToFile()
    -- TraceAI("初始化空的朋友管理系統")
else
    -- 載入成功，不需要立即重新寫入文件
    -- 只在實際修改朋友列表時才寫入
    -- TraceAI("朋友管理系統已從文件恢復")
end
