// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// =====================================================================
// 相机仿真取像：无真实 SDK 时产出合成帧 + 伪检测结果，使"相机取像接入流程"
// 在纯仿真态下也能跑通（取帧→闪光→结果变量→3D 抓拍预览）。接入真实 SDK 后，
// FlowRunnerService 优先用 VisionEngine.CaptureFrame，仅在返回 null 时回退到此。
// =====================================================================
using System;

namespace NoCodeMotion.Services.Vision
{
    public static class VisionSimCapture
    {
        /// <summary>生成一帧合成 BGRA 图像（含一个高亮目标块，位置按相机序号+时间伪随机）。</summary>
        public static byte[] Capture(int index, out int width, out int height)
        {
            width = 320; height = 240;
            var bgra = new byte[width * height * 4];
            var rnd = new Random(index * 7919 + DateTime.Now.Millisecond);
            int bx = rnd.Next(60, width - 60);
            int by = rnd.Next(60, height - 60);
            int br = 38;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int o = (y * width + x) * 4;
                    double d = Math.Sqrt((x - bx) * (x - bx) + (y - by) * (y - by));
                    byte lum = (byte)(40 + (x * 3 % 60));
                    if (d < br)
                    {
                        double k = 1 - d / br;
                        bgra[o] = (byte)(lum + 150 * k);       // B
                        bgra[o + 1] = (byte)(lum + 90 * k);    // G
                        bgra[o + 2] = (byte)(lum + 200 * k);   // R
                    }
                    else
                    {
                        bgra[o] = lum; bgra[o + 1] = lum; bgra[o + 2] = (byte)(lum + 20);
                    }
                    bgra[o + 3] = 255;
                }
            }
            return bgra;
        }

        /// <summary>伪检测结果：目标中心(相对百分比)与匹配分数。</summary>
        public static (double X, double Y, double Score) Detect(int index)
        {
            var rnd = new Random(index * 104729 + DateTime.Now.Millisecond);
            return (rnd.NextDouble() * 100, rnd.NextDouble() * 100, 0.85 + rnd.NextDouble() * 0.14);
        }
    }
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥
