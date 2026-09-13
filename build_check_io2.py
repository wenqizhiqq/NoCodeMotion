import subprocess, sys, os
log_path = r'D:\wqz\code\NoCodeMotion\build_io.log'
with open(log_path, 'w', encoding='utf-8', errors='ignore') as f:
    p = subprocess.run(
        [r'C:\Program Files\dotnet\dotnet.exe', 'build', r'D:\wqz\code\NoCodeMotion\NoCodeMotion.csproj', '-c', 'Release', '-v', 'minimal'],
        stdout=f, stderr=subprocess.STDOUT, text=True
    )
    f.write(f'\nEXIT_CODE={p.returncode}\n')
print('done', p.returncode)
