$art = "D:\NCMGC4"
if (Test-Path $art) { Remove-Item -Recurse -Force $art -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Force -Path $art | Out-Null
Write-Output ("ART_DIR=" + $art)
$out = & "C:\Program Files\dotnet\dotnet.exe" build "D:\wqz\code\NoCodeMotion\NoCodeMotion.csproj" -c Debug -p:UseArtifactsOutputPath=true -p:ArtifactsPath="$art" -p:UseSharedCompilation=false 2>&1
Write-Output "=== CS_ERRORS ==="
$out | Select-String -Pattern "CS\d{4}" | ForEach-Object { Write-Output $_ }
Write-Output "=== HEAD_BUILD ==="
$out | Select-String -Pattern "error|Build succeeded|Build FAILED|生成" | Select-Object -First 6 | ForEach-Object { Write-Output $_ }
Write-Output "=== DLL_CHECK ==="
$dll = "$art\bin\NoCodeMotion\debug\NoCodeMotion.dll"
Write-Output ("DLL_PATH=" + $dll)
Write-Output ("DLL_PRESENT=" + (Test-Path $dll))
