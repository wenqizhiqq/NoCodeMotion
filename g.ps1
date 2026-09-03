$art = "D:\NCMGC5"
if (Test-Path $art) { Remove-Item -Recurse -Force $art -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Force -Path $art | Out-Null
$out = & "C:\Program Files\dotnet\dotnet.exe" build "D:\wqz\code\NoCodeMotion\NoCodeMotion.csproj" -c Debug -p:UseArtifactsOutputPath=true -p:ArtifactsPath="$art" -p:UseSharedCompilation=false 2>&1 | Out-String
$first = $out.Substring(0, [Math]::Min(200, $out.Length))
"HEAD: $first"
$errs = $out | Select-String -Pattern "CS\d{4}"
"ERRCNT=$($errs.Count)"
$dll = "$art\bin\NoCodeMotion\debug\NoCodeMotion.dll"
"DLLPATH=$dll"
"DLLPRESENT=$([bool](Test-Path $dll))"
