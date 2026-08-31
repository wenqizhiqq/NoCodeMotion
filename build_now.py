#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Fresh-obj build for NoCodeMotion WPF.
Bypasses EDR file lock on cached compile artifacts and bin\Debug\...exe.
Routes build to fresh `obj_fresh` and redirects OutputPath to `_out`.
"""
import os, shutil, subprocess, sys, time, re

ROOT = r"D:\wqz\code\NoCodeMotion"
TRASH = r"D:\wqz\code\_projtrash"
DOTNET = r"C:\Program Files\dotnet\dotnet.exe"
LOG_FILE = os.path.join(ROOT, "buildlog_now.txt")

def move_to_trash(name):
    src = os.path.join(ROOT, name)
    if not os.path.exists(src):
        return None
    os.makedirs(TRASH, exist_ok=True)
    dst = os.path.join(TRASH, name)
    # If a copy with the same name already exists in trash from prior runs,
    # suffix this one with timestamp so we never collide (and never delete).
    if os.path.exists(dst):
        stamp = time.strftime("_%Y%m%d_%H%M%S")
        dst = f"{dst}{stamp}"
    shutil.move(src, dst)
    return dst

def main():
    os.chdir(ROOT)
    moved = []
    # Move fresh-obj candidates to trash (never delete = safe under EDR handles).
    for name in os.listdir(ROOT):
        if name.startswith("obj") or name in ("_out", "_bld"):
            dst = move_to_trash(name)
            if dst:
                moved.append(f"{name} -> {os.path.basename(os.path.dirname(dst))}/{os.path.basename(dst)}")

    # Run dotnet build through PowerShell with redirected OutputPath to fresh `_out\`
    ps = (
        f"Set-Location '{ROOT}'; "
        f"& '{DOTNET}' build -c Debug --disable-build-servers -p:UseSharedCompilation=false -p:OutputPath=_out\\ "
        f"2>&1 | Tee-Object -FilePath '{LOG_FILE}'; "
        f"exit $LASTEXITCODE"
    )
    result = subprocess.run(
        ["powershell", "-NoProfile", "-NonInteractive", "-Command", ps],
        capture_output=True, text=True, timeout=600
    )
    print("POWERSHELL EXIT:", result.returncode)
    print("STDOUT (last 80 lines):")
    out = (result.stdout or "") + (result.stderr or "")
    lines = out.splitlines()
    for ln in lines[-80:]:
        print(ln)
    print(f"\n--- moved ({len(moved)}) ---")
    for m in moved:
        print(m)

if __name__ == "__main__":
    main()
