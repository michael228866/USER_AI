-- ============================================================
-- FollowZero 模式：站在主人腳下，攻擊範圍內敵人，不追擊
-- Version: 3.0.0 - 靜態防禦
-- Updated: 2025-10-21
-- ============================================================

-- 檢查是否啟用 FollowZero 模式
function IsFollowZeroMode()
	return FollowZeroMode and FollowZeroMode == 1
end

-- FollowZero 模式的移動處理：每個 tick 都直接移動到主人位置（大步前進）
function FollowZeroMove(myid)
	local oid = GetV(V_OWNER, myid)
	if not oid then return false end

	local mx, my = GetV(V_POSITION, myid)
	local ox, oy = GetV(V_POSITION, oid)

	if not mx or not my or not ox or not oy then
		return false
	end

	-- 計算方向和距離
	local dx = ox - mx
	local dy = oy - my
	local dist = math.max(math.abs(dx), math.abs(dy))

	-- 每個 tick 都直接移動到主人位置（大步前進）
	if dist > 0 then
		Move(myid, ox, oy)
	end

	return true
end

-- FollowZero 模式下是否允許普通攻擊
-- 允許攻擊，但只攻擊範圍內的目標
function FollowZeroCanAttack()
	return true  -- 允許攻擊範圍內的敵人
end

-- FollowZero 模式下是否允許追擊
-- 絕對不追擊，永遠站在主人腳下
function FollowZeroCanChase()
	if not IsFollowZeroMode() then
		return true  -- 未啟用模式，允許追擊
	end
	return false  -- FollowZero 模式下禁止追擊
end

-- FollowZero 模式下是否允許釋放技能
function FollowZeroCanCastSkill()
	return true  -- 總是允許釋放技能
end

-- FollowZero 模式下是否允許進入攻擊狀態
-- 允許，但只攻擊範圍內的敵人
function FollowZeroCanEnterAttackState()
	return true  -- 允許攻擊範圍內的敵人
end

-- FollowZero 模式下檢查目標是否在允許的攻擊範圍內
-- 只攻擊：技能範圍內 或 主人普攻範圍內的敵人
function FollowZeroCanAttackTarget(myid, targetId)
	if not IsFollowZeroMode() then
		return true  -- 未啟用模式，允許任何目標
	end

	local oid = GetV(V_OWNER, myid)
	if not oid or not targetId then
		return false
	end

	-- 獲取生命體技能範圍（假設最大技能範圍為 9）
	local skillRange = 9

	-- 檢查目標是否在生命體技能範圍內
	local myDist = getObjRectDis(myid, targetId)
	if myDist and myDist <= skillRange then
		return true  -- 在技能範圍內，允許攻擊
	end

	-- 檢查目標是否在主人普攻範圍內（範圍約 2-3）
	local ownerDist = getObjRectDis(oid, targetId)
	if ownerDist and ownerDist <= 3 then
		return true  -- 在主人普攻範圍內，允許攻擊
	end

	return false  -- 不在允許範圍內，不攻擊
end

-- FollowZero 模式下是否應該立即返回主人身邊
-- 攻擊完畢或目標超出範圍時返回
function FollowZeroShouldReturnToOwner(myid, targetId)
	if not IsFollowZeroMode() then
		return false  -- 未啟用模式，不強制返回
	end

	if not targetId or targetId == 0 then
		return true  -- 沒有目標，返回主人身邊
	end

	-- 如果目標超出允許範圍，返回主人身邊
	if not FollowZeroCanAttackTarget(myid, targetId) then
		return true
	end

	return false
end
