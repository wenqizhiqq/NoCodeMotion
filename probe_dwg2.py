import struct
p = r"D:\运控\LFA3691 深圳比亚迪BOX整机\深圳比亚迪BOX,IDC整机.dwg"
with open(p, "rb") as f:
    data = f.read()
seek = struct.unpack_from("<I", data, 0x0D)[0]
print("prefix(119 bytes):", data[seek:seek+119].hex(" "))
png_start = data.find(b"\x89PNG\r\n\x1a\n", seek)
print("png_start", hex(png_start), "delta", png_start-seek)
# parse PNG IHDR
off = png_start
assert data[off:off+8] == b"\x89PNG\r\n\x1a\n"
off += 8
width=height=None; bitd=None; colort=None
end = png_start
while True:
    if off+8 > len(data): break
    ln = struct.unpack_from(">I", data, off)[0]
    typ = data[off+4:off+8]
    if typ == b"IHDR":
        width, height, bitd, colort = struct.unpack_from(">IIBB", data, off+8)
    if typ == b"IEND":
        end = off+8
        break
    off += 12 + ln
print("PNG width,height,bitdepth,colortype:", width, height, bitd, colort)
print("png bytes len:", end - png_start)
with open(r"D:\wqz\code\NoCodeMotion\dwg_preview.png","wb") as o:
    o.write(data[png_start:end])
print("saved dwg_preview.png")
