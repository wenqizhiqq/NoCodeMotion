import os

out = r"D:\wqz\code\NoCodeMotion\build_verify\bin\NoCodeMotion\debug"
dlls = ["GrayModelNative.dll","OpenCvSharpExtern.dll","opencv_world480.dll","opencv_videoio_ffmpeg4130_64.dll","vcomp140.dll"]
lines = ["OUTDIR=" + out, "EXISTS_DIR=" + str(os.path.isdir(out))]
for d in dlls:
    p = os.path.join(out, d)
    if os.path.exists(p):
        lines.append(f"OK   {d}  {os.path.getsize(p)} bytes")
    else:
        lines.append(f"MISS {d}")
# also list any other dlls present for sanity
if os.path.isdir(out):
    extra = [f for f in os.listdir(out) if f.lower().endswith(".dll") and f not in dlls]
    lines.append("OTHER_DLLS=" + str(len(extra)))
    for f in sorted(extra)[:40]:
        lines.append("  " + f)
with open(r"D:\wqz\code\NoCodeMotion\verify_out.txt", "w", encoding="utf-8") as fh:
    fh.write("\n".join(lines) + "\n")
print("VERIFIED")
