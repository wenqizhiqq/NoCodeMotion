import subprocess
p = subprocess.run(
    ['C:/Program Files/dotnet/dotnet.exe', 'build', 'D:/wqz/code/NoCodeMotion/NoCodeMotion.csproj', '-c', 'Release', '-v', 'minimal'],
    capture_output=True, text=True, encoding='utf-8', errors='ignore'
)
print('EXIT:', p.returncode)
print('--- STDOUT ---')
print(p.stdout[-4000:] if p.stdout else '')
print('--- STDERR ---')
print(p.stderr[-2000:] if p.stderr else '')
