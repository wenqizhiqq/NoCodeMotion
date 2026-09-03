import os

def safe_exists(p):
    try:
        return os.path.exists(p)
    except Exception as ex:
        return "ERR:" + str(ex)[:40]

def w(s):
    with open(r"D:\wqz\code\NoCodeMotion\probe3.txt", "a", encoding="utf-8") as f:
        f.write(s + "\n")

w("E drive root exists: " + str(safe_exists(r"E:\")))
w("E Gitee exists: " + str(safe_exists(r"E:\网络代码\Gitee")))
paths = [
    r"E:\网络代码\Gitee\GrayMatch\GrayModelNative\build-ninja9\GrayModelNative.dll",
    r"E:\网络代码\Gitee\CvMatch\OpenCvSharpExtern.dll",
    r"E:\网络代码\Gitee\CvMatch\opencv_world480.dll",
    r"E:\网络代码\Gitee\CvMatch\opencv_videoio_ffmpeg4130_64.dll",
    r"D:\wqz\code\CvMatch\OpenCvSharpExtern.dll",
    r"D:\wqz\code\GrayMatch\GrayModelNative\build-ninja9\GrayModelNative.dll",
    r"D:\wqz\code\GrayMatch\GrayModelNative\build\GrayModelNative.dll",
    r"D:\wqz\code\NoCodeMotion\bin\Debug\net10.0-windows\OpenCvSharpExtern.dll",
]
for p in paths:
    w("EXISTS " + p + " = " + str(safe_exists(p)))
w("DONE")
