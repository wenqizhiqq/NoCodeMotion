from pathlib import Path

log = Path(r"D:\wqz\code\NoCodeMotion\build_manual.log").read_text(encoding="utf-8", errors="replace")
lines = log.splitlines()
errs = [l for l in lines if "error CS" in l or " error " in l.lower()]
warns = [l for l in lines if "warning CS" in l]
build_ok = any("Build succeeded" in l or "生成成功" in l for l in lines)
print("BUILD_OK=", build_ok)
print("CS_ERROR_COUNT=", len(errs))
print("CS_WARN_COUNT=", len(warns))
for e in errs[:40]:
    print("  ERR:", e.strip())
for w in warns[:10]:
    print("  WARN:", w.strip())
print("---- tail ----")
for l in lines[-15:]:
    print(l)
