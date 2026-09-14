import subprocess, sys
p = subprocess.run(
    ['C:/Program Files/dotnet/dotnet.exe', 'build',
     'D:/wqz/code/NoCodeMotion/NoCodeMotion.csproj', '-c', 'Release', '-v', 'minimal'],
    stdout=subprocess.PIPE, stderr=subprocess.PIPE)
out = (p.stdout or b'').decode('utf-8', 'replace') + '\n' + (p.stderr or b'').decode('utf-8', 'replace')
print('EXIT_CODE=', p.returncode)
ecs = [l for l in out.splitlines() if 'error CS' in l]
print('ERROR_CS_COUNT=', len(ecs))
for l in ecs[:40]:
    print(l)
ok = ('已成功生成' in out) or ('Build succeeded' in out) or ('生成成功' in out) or ('0 错误' in out) or ('0 error' in out)
print('BUILD_OK=', ok)
sys.exit(0)
