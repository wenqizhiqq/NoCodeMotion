import subprocess, os, shutil, time, re

PROJ = r"D:\wqz\code\NoCodeMotion"
TRASH = r"D:\wqz\code\_projtrash"
DOTNET = r"C:\Program Files\dotnet\dotnet.exe"
RESULT = r"c:\temp\buildresult.txt"

os.makedirs(TRASH, exist_ok=True)
os.makedirs(r"c:\temp", exist_ok=True)

stamp = time.strftime("%Y%m%d_%H%M%S")
moved = []
for name in ("obj", "bin", "_out", "_bld"):
    p = os.path.join(PROJ, name)
    if os.path.exists(p):
        dst = os.path.join(TRASH, f"{name}_{stamp}")
        n = 1
        while os.path.exists(dst):
            dst = os.path.join(TRASH, f"{name}_{stamp}_{n}")
            n += 1
        try:
            shutil.move(p, dst)
            moved.append(f"{name}->{os.path.basename(dst)}")
        except Exception as e:
            moved.append(f"{name}:MOVEFAIL({e})")

cmd = [DOTNET, "build", "-c", "Debug",
       "--disable-build-servers",
       "-p:UseSharedCompilation=false",
       "-p:OutputPath=_out\\"]

env = dict(os.environ)
env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
env["DOTNET_NOLOGO"] = "1"

try:
    proc = subprocess.run(cmd, cwd=PROJ, env=env,
                          capture_output=True, timeout=540)
    out = (proc.stdout or b"").decode("utf-8", "replace") + \
          (proc.stderr or b"").decode("utf-8", "replace")
    code = proc.returncode
except Exception as e:
    out = f"EXCEPTION: {e}"
    code = -1

errs = [l for l in out.splitlines() if re.search(r"error\s+CS\d+", l)]
warns = [l for l in out.splitlines() if re.search(r"warning\s+CS\d+", l)]

lines = [f"EXIT_CODE={code}",
         f"MOVED={len(moved)} " + "; ".join(moved),
         f"ERROR_LINES={len(errs)}"]
for l in errs[:30]:
    lines.append("ERR: " + l.encode("ascii", "replace").decode("ascii"))
lines.append(f"WARN_LINES={len(warns)}")
tail = out.splitlines()[-5:]
lines.append("--- TAIL ---")
for l in tail:
    lines.append(l.encode("ascii", "replace").decode("ascii"))

with open(RESULT, "w", encoding="ascii", errors="replace") as f:
    f.write("\n".join(lines))

print("\n".join(lines))
