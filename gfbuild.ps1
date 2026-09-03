$art = "D:\NCMGrayFix"
if (Test-Path $art) { Remove-Item -Recurse -Force $art -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Force -Path $art | Out-Null
dotnet build "D:\wqz\code\NoCodeMotion\NoCodeMotion.csproj" -c Debug -p:UseArtifactsOutputPath=true -p:ArtifactsPath="$art" -p:UseSharedCompilation=false 2>&1 | Out-Null
$dll = "$art\bin\NoCodeMotion\debug\NoCodeMotion.dll"
"DLL_PATH=$dll"
"DLL_PRESENT=$([System.IO.File]::Exists($dll))"
