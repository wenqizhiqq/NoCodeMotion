import struct, sys
p = r"D:\运控\LFA3691 深圳比亚迪BOX整机\深圳比亚迪BOX,IDC整机.dwg"
with open(p, "rb") as f:
    data = f.read()
print("size", len(data))
ver = data[0:6].decode("ascii", "replace")
print("ver", ver)
seek = struct.unpack_from("<I", data, 0x0D)[0]
print("preview seeker @0x0D =", seek, hex(seek))
# inspect bytes at seeker
chunk = data[seek:seek+32]
print("bytes@seeker", chunk[:16].hex(" "))
# search for BMP 'BM' or PNG signature within first 256KB after seeker
sig_bmp = b"BM"
sig_png = b"\x89PNG\r\n\x1a\n"
for name, sig in [("BMP", sig_bmp), ("PNG", sig_png)]:
    idx = data.find(sig, seek, seek+2_000_000)
    print(f"{name} sig found at", idx, hex(idx) if idx>=0 else "")
# also try from 0x80 (after file header) for obfuscated region - dump
print("first 16 @0x80", data[0x80:0x90].hex(" "))
