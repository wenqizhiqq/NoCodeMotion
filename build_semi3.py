import subprocess
p = subprocess.run(
    ['C:/Program Files/dotnet/dotnet.exe', 'build',
     'D:/wqz/code/NoCodeMotion/NoCodeMotion.csproj', '-c', 'Release', '-v', 'minimal'],
    stdout=subprocess.PIPE, stderr=subprocess.PIPE)
out = (p.stdout or b'').decode('utf-8', 'replace') + '\n' + (p.stderr or b'').decode('utf-8', 'replace')
lines = out.splitlines()
keep = []
keep.append('EXIT_CODE=' + str(p.returncode))
ecs = [l for l in lines if 'error CS' in l]
keep.append('ERROR_CS_COUNT=' + str(len(ecs)))
for l in ecs[:40]:
    keep.append('ERR: ' + ''.join(ch if 32 <= ord(ch) < 127 else '?' for ch in l))
ok = ('已成功生成' in out) or ('Build succeeded' in out) or ('生成成功' in out) or ('0 错误' in out) or ('0 error' in out)
keep.append('BUILD_OK=' + str(ok))
with open(r'D:\wqz\code\NoCodeMotion\build_semi3.txt', 'w', encoding='ascii') as f:
    f.write('\n'.join(keep))
