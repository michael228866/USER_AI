param(
    [Parameter(Mandatory)][string]$ExePath,
    [Parameter(Mandatory)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$exe = (Resolve-Path $ExePath).Path
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$output = (Resolve-Path $OutputDirectory).Path

function New-Package([string]$name, [bool]$includeDefaults) {
    $stage = Join-Path $output ([guid]::NewGuid().ToString('N'))
    $target = Join-Path $stage 'USER_AI'
    try {
        New-Item -ItemType Directory -Force -Path (Join-Path $target 'RNAI/custom') | Out-Null
        Copy-Item -LiteralPath $exe -Destination (Join-Path $target '生命體AI管理器.exe')
        Copy-Item -Path (Join-Path $root '*.lua') -Destination $target
        Copy-Item -Path (Join-Path $root 'RNAI/*.lua') -Destination (Join-Path $target 'RNAI')
        Copy-Item -LiteralPath (Join-Path $root 'RNAI/custom/Vanilmirth.lua') -Destination (Join-Path $target 'RNAI/custom')
        @'
生命體 AI 交付版

安裝：將 USER_AI 資料夾放到遊戲的 AI 資料夾內，執行生命體AI管理器.exe 編輯設定。
更新：在設定器右上角看到新版提示後，下載 USER_AI_update.zip，關閉遊戲與設定器，再合併到現有資料夾。
更新包不包含你的生命體設定、朋友與黑名單、自訂配置及 Log。
AI 運作需要 Lua 檔案，因此 Lua 程式內容可被閱讀；本包不包含 Git 資料或 C# 原始碼。
'@ | Set-Content -LiteralPath (Join-Path $target '使用說明.txt') -Encoding UTF8
        if ($includeDefaults) {
            Copy-Item -LiteralPath (Join-Path $root 'RNAI/custom/homunculus_config.lua') -Destination (Join-Path $target 'RNAI/custom')
            'friends_data = {' + "`n}" | Set-Content -LiteralPath (Join-Path $target 'RNAI/custom/friends_list.lua') -Encoding ASCII
            'enemies_data = {' + "`n}" | Set-Content -LiteralPath (Join-Path $target 'RNAI/custom/enemies_list.lua') -Encoding ASCII
            $profiles = @{ version = '1.0'; profiles = @{} }
            foreach ($slot in 1..3) {
                $profiles.profiles["自訂$slot"] = @{ saved = $false; combat = @{}; exclude = @{}; parameters = @{}; hotkeys = @{} }
            }
            $profiles | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $target 'RNAI/custom/manager_profiles.json') -Encoding UTF8
        }
        $zip = Join-Path $output $name
        if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip }
        Compress-Archive -LiteralPath $target -DestinationPath $zip -CompressionLevel Optimal
        Write-Host $zip
    }
    finally { Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue }
}

New-Package 'USER_AI_full.zip' $true
New-Package 'USER_AI_update.zip' $false
