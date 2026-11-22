@echo off
rem Publishes the friends list of D:\RO4 as the shared friends list.
cd /d "%~dp0"
powershell -NoProfile -Command "$ids = Select-String -Path 'D:\RO4\AI\USER_AI\RNAI\custom\friends_list.lua' -Pattern '^\s*\[\d+\]\s*=\s*(\d+)' | ForEach-Object { $_.Matches[0].Groups[1].Value }; if (-not $ids) { Write-Host 'friends list is empty, nothing published'; exit 1 }; Set-Content -Path shared_friends.txt -Encoding ascii -Value (@('# Shared friends list: one player ID per line. Managers merge it with the button on the list page.') + $ids); Write-Host ($ids.Count.ToString() + ' IDs')"
if errorlevel 1 goto end
git add shared_friends.txt
git commit -m "Update shared friends list"
git push
:end
pause
