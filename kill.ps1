$names = @('MSBuild', 'VBCSCompiler', 'dotnet')
foreach ($n in $names) {
    Get-Process -Name $n -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}
"killed" | Set-Content -Encoding utf8 "D:\wqz\code\NoCodeMotion\_killed.txt"
