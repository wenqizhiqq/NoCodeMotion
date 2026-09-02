$ErrorActionPreference = 'Continue'
$root = 'D:\wqz\code\NoCodeMotion'
$log = @()
# Try to remove stale obj/bin for a clean restore
foreach ($d in @('obj','bin')) {
    $p = Join-Path $root $d
    if (Test-Path $p) {
        try { Remove-Item $p -Recurse -Force -ErrorAction Stop; $log += "REMOVED $d" }
        catch { $log += "REMOVE_FAIL $d : $_" }
    } else { $log += "MISSING $d" }
}
# Fresh build (no redirection)
& "C:\Program Files\dotnet\dotnet.exe" build (Join-Path $root 'NoCodeMotion.csproj') -c Debug *> (Join-Path $root 'build_log7.txt')
$bl = Get-Content (Join-Path $root 'build_log7.txt') -Encoding UTF8
$errs = $bl | Where-Object { $_ -match 'error CS|error MC|: error ' }
$log += "ERRCOUNT=$($errs.Count)"
if ($errs.Count -gt 0) { $log += 'FIRST:'; $errs | Select-Object -First 5 | ForEach-Object { $log += $_ } } else { $log += 'NO_ERRORS' }
$log += 'TAIL:'; $bl | Select-Object -Last 4 | ForEach-Object { $log += $_ }
$log | Out-File (Join-Path $root 'build_result.txt') -Encoding ascii
