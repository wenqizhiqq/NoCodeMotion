$ErrorActionPreference = 'Continue'
$root = 'D:\wqz\code\NoCodeMotion'
# Clear read-only attributes on obj/bin (common cause of "access denied" during restore)
foreach ($d in @('obj','bin')) {
    $p = Join-Path $root $d
    if (Test-Path $p) {
        Get-ChildItem $p -Recurse -Force | ForEach-Object { if ($_.IsReadOnly) { $_.IsReadOnly = $false } }
    }
}
& "C:\Program Files\dotnet\dotnet.exe" build (Join-Path $root 'NoCodeMotion.csproj') -c Debug *> (Join-Path $root 'build_log3.txt')
$log = Get-Content (Join-Path $root 'build_log3.txt') -Encoding UTF8
$errs = $log | Where-Object { $_ -match 'error CS|error MC|: error ' }
$warns = $log | Where-Object { $_ -match ': warning ' }
$summary = @()
$summary += "ERRCOUNT=$($errs.Count)"
$summary += "WARNCOUNT=$($warns.Count)"
if ($errs.Count -gt 0) { $summary += 'FIRST_ERRORS:'; $errs | Select-Object -First 10 | ForEach-Object { $summary += $_ } } else { $summary += 'NO_ERRORS' }
$summary += 'TAIL:'
$log | Select-Object -Last 5 | ForEach-Object { $summary += $_ }
$summary | Out-File (Join-Path $root 'build_result.txt') -Encoding ascii
