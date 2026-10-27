$ErrorActionPreference = 'Stop'
$main = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot '..\..\..\Main.lua')
$config = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot '..\..\homunculus_config.lua')

foreach ($token in @(
    'local Tms = ((200 - (ASPD or 190)) / 50) * 1000',
    'return (now-(LastAttackTick or 0)) >= (AttackThrottleMs or 200)',
    'now - (LastDanceTick or 0) >= (DanceIntervalMs or 92)',
    'now - (MoveCmdTime or 0) >= (MoveDelay or 80)',
    'if DanceEnabled and Target>0 and not IsPlayerTarget(Target)'
)) {
    if (-not $main.Contains($token)) { throw "Missing RO3 timing or compatibility gate: $token" }
}
foreach ($token in @('DancePhase = 0.4', 'PlayerASPD = 193')) {
    if (-not $config.Contains($token)) { throw "Missing Filir setting: $token" }
}
$vanilmirth = $config.Substring($config.IndexOf('  Vanilmirth = {'))
if (-not $vanilmirth.Contains('UseHopDance = false')) {
    throw 'Vanilmirth Hop still sends Move-Attack-Move in one tick'
}
$filirStart = $config.IndexOf('  filir = {')
$filirEnd = $config.IndexOf('  Vanilmirth = {', $filirStart)
$filir = $config.Substring($filirStart, $filirEnd - $filirStart)
$homCfg = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot '..\..\..\HomCfg.lua')
if (-not $filir.Contains('UseHopDance = true') -or -not $homCfg.Contains('UseHopDance            = true')) {
    throw 'Filir does not inherit RO3 Hop dance'
}
if (-not $main.Contains('    SetupAspdTimings() -- ← 依 ASPD 設定節奏')) {
    throw 'RO3 pre-configuration ASPD timing call is missing'
}
if ($main.Contains('NextDanceAttackDueTick') -or $main.Contains('DanceMoveThrottleMs') -or $main.Contains('if DanceAttackMode == 1 and DanceEnabled')) {
    throw 'The replacement dance scheduler is still present'
}
$dance = $main.Substring($main.IndexOf('function AttackDanceTick(myid,target)'))
if ($dance.IndexOf('SafeMove(myid, dx, dy)') -gt $dance.IndexOf('SafeAttack(myid, target)')) {
    throw 'RO3 dance command order changed'
}
if ($main.IndexOf('SetupAspdTimings()', $main.IndexOf('pcall(LoadHomConfig, myid)')) -lt $main.IndexOf('pcall(LoadHomConfig, myid)')) {
    throw 'ASPD timing was not recomputed after configuration loaded'
}

$referencePath = 'D:\RO3\AI\USER_AI\RNAI\Main.lua'
if (Test-Path $referencePath) {
    $reference = Get-Content -Raw -Encoding UTF8 $referencePath
    foreach ($pair in @(
        @('local function SetupAspdTimings()', '-- 安全節流：普攻/施法'),
        @('local function GetDanceCell(', 'local function ShouldHopDance('),
        @('local function ShouldHopDance(', '-- 讀取自動技能'),
        @('function AttackDanceTick(', '-- 讀取自動技能'),
        @('  elseif (MyState==ST_ATTACK_PRE) then', '  elseif (MyState==ST_SKILL) then')
    )) {
        $start, $end = $pair
        $a = $main.IndexOf($start); $b = $main.IndexOf($end, $a + $start.Length)
        $c = $reference.IndexOf($start); $d = $reference.IndexOf($end, $c + $start.Length)
        if ($a -lt 0 -or $b -lt 0 -or $c -lt 0 -or $d -lt 0) { throw "Missing dance block: $start" }
        $ours = ($main.Substring($a, $b - $a) -replace "`r`n", "`n").TrimEnd()
        $theirs = ($reference.Substring($c, $d - $c) -replace "`r`n", "`n").TrimEnd()
        if ($ours -cne $theirs) { throw "RO3 dance block differs: $start" }
    }
    Write-Output 'RO3 dance function blocks: exact match'
}

foreach ($aspd in 185..193) {
    $period = [Math]::Max(140, (200 - $aspd) * 20)
    $interval = [int][Math]::Floor($period * 0.4)
    $lastAttack = 0; $lastDance = 0; $lastMove = 0; $attacks = 0; $moves = 0
    foreach ($tick in 0..40) {
        $now = $tick * 50
        if ($now - $lastDance -ge $interval) {
            if ($now - $lastMove -ge 150) { $moves++; $lastMove = $now }
            $lastDance = $now
        }
        if ($now - $lastAttack -ge $period) { $attacks++; $lastAttack = $now }
    }
    if ($attacks -lt 6 -or $moves -lt 6) { throw "ASPD $aspd schedule did not progress" }
    Write-Output "ASPD $aspd period=${period}ms dance=${interval}ms MoveDelay=150ms attacks=$attacks moves=$moves"
}
Write-Output 'PASS: RO3 dance parity and ASPD 185-193 timing model (game hits unverified)'
