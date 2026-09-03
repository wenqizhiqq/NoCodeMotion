import os, shutil

proj = r"D:\wqz\code\NoCodeMotion"
native = os.path.join(proj, "Native")
os.makedirs(native, exist_ok=True)

# (src, dst_name) for DLLs that should exist in this sandbox
copies = [
    (r"D:\wqz\code\CvMatch\opencv_world480.dll", "opencv_world480.dll"),
    (r"D:\wqz\code\CvMatch\opencv_videoio_ffmpeg4130_64.dll", "opencv_videoio_ffmpeg4130_64.dll"),
    (r"C:\Windows\System32\vcomp140.dll", "vcomp140.dll"),
]

# DLLs that are required but NOT present in this sandbox (must be supplied by user from their Gitee build outputs)
missing = [
    (r"D:\wqz\code\GrayMatch\GrayModelNative\build-ninja9\GrayModelNative.dll", "GrayModelNative.dll"),
    (r"D:\wqz\code\CvMatch\OpenCvSharpExtern.dll", "OpenCvSharpExtern.dll"),
]

lines = []
for src, name in copies:
    dst = os.path.join(native, name)
    if os.path.exists(src):
        shutil.copy2(src, dst)
        lines.append(f"COPIED {name} <- {src} ({os.path.getsize(dst)} bytes)")
    else:
        lines.append(f"MISSING SRC {name} : {src}")

for src, name in missing:
    dst = os.path.join(native, name)
    if os.path.exists(src):
        shutil.copy2(src, dst)
        lines.append(f"COPIED {name} <- {src}")
    else:
        lines.append(f"NOT IN SANDBOX (user must supply) {name} : expected at {src}")

# list what ended up in Native\
lines.append("--- Native\\ contents ---")
for f in sorted(os.listdir(native)):
    fp = os.path.join(native, f)
    lines.append(f"  {f}  {os.path.getsize(fp)} bytes")

with open(os.path.join(proj, "copy_manifest.txt"), "w", encoding="utf-8") as fh:
    fh.write("\n".join(lines) + "\n")
print("DONE_COPY")
