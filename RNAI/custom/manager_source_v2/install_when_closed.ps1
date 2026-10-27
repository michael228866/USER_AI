$ErrorActionPreference = 'Stop'
$destination = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$source = (Resolve-Path (Join-Path $PSScriptRoot 'publish_single')).Path
$names = @('生命體AI管理器.exe')
$status = Join-Path $PSScriptRoot 'install_status.txt'
$deadline = [DateTime]::UtcNow.AddHours(2)
while ([DateTime]::UtcNow -lt $deadline) {
    try {
        foreach ($name in $names) {
            $path = Join-Path $destination $name
            $stream = [System.IO.File]::Open($path, 'Open', 'ReadWrite', 'None')
            $stream.Dispose()
        }
        foreach ($name in $names) {
            [System.IO.File]::Copy((Join-Path $source $name), (Join-Path $destination $name), $true)
        }
        [System.IO.File]::WriteAllText($status, 'Installed ' + [DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss'))
        exit 0
    }
    catch [System.IO.IOException] { Start-Sleep -Seconds 2 }
    catch [System.UnauthorizedAccessException] { Start-Sleep -Seconds 2 }
}
[System.IO.File]::WriteAllText($status, 'Timed out waiting for manager to close')
exit 1
