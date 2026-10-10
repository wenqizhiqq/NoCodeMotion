# -*- coding: utf-8 -*-
r"""给新建的 .cs 文件补上「同级模板文件」的页眉/页脚水印 + UTF-8 BOM。

为什么需要：本仓库的 .cs 页眉/页脚里混了**逐文件不同**的零宽水印
（U+200B/2063/200D/200C/2060 等），手工敲不出来、工具链也容易吃掉。
所以新文件先用 @@HDR@@ / @@FTR@@ 占位，再用本脚本从**同级已有文件**原样搬运。

用法（仓库根目录）：
    python tools/_seal.py <目标.cs> [模板.cs] [--crlf]

- 目标文件里必须各有一行只写 @@HDR@@ 和一行只写 @@FTR@@（会被整行替换）。
- 模板默认 Services\Hardware\Comm\ICommChannel.cs（3 行页眉 + 2 行页脚）。
- 默认写 LF 无 BOM 之外的一切按模板来：BOM 跟模板一致，行尾默认 LF，--crlf 改 CRLF。
"""
import sys
import os

HDR_MARK = "@@HDR@@"
FTR_MARK = "@@FTR@@"


def split_banner(path):
    """返回 (header_lines, footer_lines)：模板的前 3 行 / 最后 2 行（不含空尾行）。"""
    raw = open(path, "rb").read()
    text = raw.decode("utf-8-sig").replace("\r\n", "\n")
    lines = text.split("\n")
    while lines and lines[-1] == "":
        lines.pop()
    if len(lines) < 6:
        raise SystemExit("模板文件太短，取不到页眉页脚：%s" % path)
    return lines[:3], lines[-2:]


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    crlf = "--crlf" in sys.argv
    if not args:
        raise SystemExit(__doc__)
    target = args[0]
    template = args[1] if len(args) > 1 else os.path.join(
        "Services", "Hardware", "Comm", "ICommChannel.cs")

    hdr, ftr = split_banner(template)
    # ★ BOM 跟**模板**走（本仓库 .cs 一律带 BOM；XAML 也带，但 XAML 无水印，不走本脚本）
    bom = open(template, "rb").read().startswith(b"\xef\xbb\xbf")
    raw = open(target, "rb").read()
    text = raw.decode("utf-8-sig").replace("\r\n", "\n")
    lines = text.split("\n")

    out = []
    seen_h = seen_f = 0
    for ln in lines:
        if ln.strip() == HDR_MARK:
            out.extend(hdr)
            seen_h += 1
        elif ln.strip() == FTR_MARK:
            out.extend(ftr)
            seen_f += 1
        else:
            out.append(ln)
    if seen_h == 0 and seen_f == 0:
        print("提示：没找到占位符，按「已封过」处理，只补 BOM / 统一行尾")
    elif seen_h != 1 or seen_f != 1:
        raise SystemExit("占位符数量不对：HDR=%d FTR=%d（各需 1 个）" % (seen_h, seen_f))

    body = "\n".join(out)
    body = body.replace("\n", "\r\n") if crlf else body
    open(target, "wb").write((b"\xef\xbb\xbf" if bom else b"") + body.encode("utf-8"))

    b = open(target, "rb").read()
    print("%s  bom=%s crlf=%d LF=%d bytes=%d"
          % (target, b.startswith(b"\xef\xbb\xbf"), b.count(b"\r\n"), b.count(b"\n"), len(b)))


if __name__ == "__main__":
    main()
