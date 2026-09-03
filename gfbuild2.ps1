$art = "D:\NCMGrayFix"
if (Test-Path $art) { Remove-Item -Recurse -Force $art -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Force -Path $art | Out-Null
$logpath = "D:\wqz\code\NoCodeMotion\gfbuild.log"
if (Test-Path $logpath) { Remove-Item $logpath -Force -ErrorAction SilentlyContinue }
dotnet build "D:\wqz\code\NoCodeMotion\NoCodeMotion.csproj" -c Debug -p:UseArtifactsOutputPath=true -p:ArtifactsPath="$art" -p:UseSharedCompilation=false > $logpath 2>&1
$dll = "$art\bin\NoCodeMotion\debug\NoCodeMotion.dll"
$DLL_PRESENT = [System.IO.File]::Exists($dll)
"DLL_PRESENT=$DLL_PRESENT" | Out-File "D:\wqz\code\NoCodeMotion\result.txt"
if ($DLL_PRESENT) {
  $len = (Get-Item $dll).Length
  "DLL_SIZE=$len" | Out-File "D:\wqz\code\NoCodeMotion\result.txt" -Append
}
"END" | Out-File "D:\wqz\code\NoCodeMotion\result.txt" -Append
