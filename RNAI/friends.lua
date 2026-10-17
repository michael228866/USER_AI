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

-- 快速檢查並同步文件狀態（如果文件被清空則重置記憶體）
local function quickSyncWithFile()
    local file = io.open(friendsFilePath, "r")
    if not file then
        -- 文件不存在，清空記憶體
        friends = {}
        friends_lookup = {}
        return true
    end

    local content = file:read("*all")
    file:close()

    -- 檢查多種空文件格式
    -- 1. 完全空白
    -- 2. 只有 friends_data = {}
    -- 3. 有註釋但沒有實際資料（如 "-- 朋友列表為空"）
    if not content or content == "" then
        friends = {}
        friends_lookup = {}
        return true
    end

    -- 檢查是否只有空結構
    local hasStructure = content:match("friends_data%s*=%s*{[^}]*}")
    local hasData = content:match("%[%d+%]%s*=%s*%d+")

    if hasStructure and not hasData then
        friends = {}
        friends_lookup = {}
        TraceAI("檢測到朋友列表被外部清空，已同步清空記憶體")
        return true  -- 返回 true 表示已清空
    end

    return false  -- 返回 false 表示文件有內容
end

function saveFriendsToFile()
    -- 保存時不需要同步，因為我們要寫入新資料
    -- quickSyncWithFile() -- 移除這行，避免清空剛添加的資料

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
        file:write("  步驟2: ALT + 右鍵點擊第二個角點 (移動指令)\n")
        file:write("  說明: 會選取矩形範圍內所有玩家/生命體為朋友\n\n")

        file:write("【全視野範圍添加朋友】\n")
        file:write("  操作: ALT+SHIFT+右鍵兩次點擊地面\n")
        file:write("  說明: 第一次設定起始點，第二次完成添加視野內所有玩家為朋友（無上限）\n\n")

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

function addFriend(id)
    if not id or id <= 0 then
        return false
    end

    -- 檢查 ID 是否在合理範圍內（玩家ID通常在 100000 到 10000000 之間）
    if id < 100000 or id > 10000000 then
        -- TraceAI(string.format("ID:%d 超出合理範圍，跳過", id))
        return false
    end

    -- 不要在每次addFriend時都同步，這會清空剛加入的資料
    -- quickSyncWithFile()

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
    saveFriendsToFile()
    return true
end

function addFriendsInRange(x1, y1, x2, y2, myid, oid)
    local addedCount = 0

    if not myid or not oid then
        return 0
    end

    -- 只在批量操作開始時同步一次
    quickSyncWithFile()

    local Objs = GetActors()
    if not Objs then
        Objs = {}
    end

    -- 正常處理範圍內的單位
    for i, v in ipairs(Objs) do
        if v ~= myid and v ~= oid then
            local x, y = GetV(V_POSITION, v)
            if x and y and x >= x1 and x <= x2 and y >= y1 and y <= y2 then
                local isMonster = false
                local ok, monsterResult = pcall(function() return IsMonster(v) end)
                if ok and monsterResult == 1 then
                    isMonster = true
                end

                if not isMonster then
                    if addFriend(v) then
                        addedCount = addedCount + 1
                    end
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
    local addedCount = 0
    local scannedIds = {}  -- 記錄已掃描的ID，避免重複
    -- 移除上限限制，允許添加所有視野內的玩家
    -- local maxAddCount = 300  -- 原先的上限已移除

    if not myid or not oid then
        return 0
    end

    -- 快速同步文件狀態
    quickSyncWithFile()


    -- 先嘗試使用 GetActors 獲取視野內的所有單位
    local actors = GetActors()
    if actors and #actors > 0 then
        -- TraceAI(string.format("GetActors 返回 %d 個單位", #actors))
        for _, id in ipairs(actors) do
            if id ~= myid and id ~= oid and not scannedIds[id] then
                scannedIds[id] = true

                -- 檢查是否為怪物
                local isMonster = false
                local ok, monsterResult = pcall(function() return IsMonster(id) end)
                if ok and monsterResult == 1 then
                    isMonster = true
                end

                if not isMonster then
                    -- 檢查是否已經是朋友
                    if not isFriend(id) then
                        if addFriend(id) then
                            addedCount = addedCount + 1
                            local px, py = GetV(V_POSITION, id)
                            -- TraceAI(string.format("添加視野內朋友 ID:%d 位置:(%d,%d)", id, px or 0, py or 0))

                            -- 已移除上限限制，可以添加所有視野內的玩家
                        end
                    end
                end
            end
        end
    else
        -- GetActors 失敗時，使用有限範圍掃描
        -- GetActors 失敗時，使用有限範圍掃描

        -- 獲取生命體位置作為中心點
        local myX, myY = GetV(V_POSITION, myid)
        if not myX or not myY then
            -- 無法獲取生命體位置，取消掃描
            return 0
        end

        -- 改為使用更安全的掃描方式
        -- GetActors失敗，無法進行全視野掃描
        -- 請使用矩形範圍選擇或單個添加朋友

        -- 不再進行ID範圍掃描，因為會產生大量無效ID
        -- 只處理已經在 actors 列表中的單位
    end

    saveFriendsToFile()
    -- TraceAI(string.format("全視野模式：總共添加了 %d 個朋友（當前總數：%d）", addedCount, #friends))

    return addedCount
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
