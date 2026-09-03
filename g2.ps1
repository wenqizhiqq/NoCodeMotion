$art = "D:\NCMGC6"
if (Test-Path $art) { Remove-Item -Recurse -Force $art -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Force -Path $art | Out-Null
Start-Transcript "D:\wqz\code\NoCodeMotion\g.log"
dotnet build "D:\wqz\code\NoCodeMotion\NoCodeMotion.csproj" -c Debug -p:UseArtifactsOutputPath=true -p:ArtifactsPath="$art" -p:UseSharedCompilation=false 2>&1 | Out-Null
$dll = "$art\bin\NoCodeMotion\debug\NoCodeMotion.dll"
"RESULT: $((Test-Path $dll).ToString())"
Stop-Transcript
