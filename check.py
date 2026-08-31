"""检查 CylinderPage.xaml 状态"""
import os
P = r"D:\wqz\code\NoCodeMotion\Views\CylinderPage.xaml"
if os.path.exists(P):
    print("EXISTS", os.path.getsize(P), "bytes")
    with open(P, "rb") as f:
        head = f.read(500)
    print("First 500 bytes (raw):")
    print(head)
else:
    print("MISSING")
