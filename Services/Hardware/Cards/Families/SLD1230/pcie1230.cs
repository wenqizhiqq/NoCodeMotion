﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/SLD1230/pcie1230.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
///
///	Description:
///			C# class for PCI-1230
///	Author:
///			PengYi
///	History:
///			2014-7-22  Create

///
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;


namespace Samsun.Domain.MotionCard.Common.SLD1230
{

    public class CPcie1230
    {
        public static int PCI1230Success  = 1024;
        public static int PCI1230ApiFailed = 1025;
        public static int PCI1230InvalidParam = 1026;
        public static int PCI1230DevNotFind = 1027;
        //open/close
        [DllImport("pci1230.dll")] public static extern int Pci1230Open(int pBoardId);         
        [DllImport("pci1230.dll")] public static extern int Pci1230Close(int pBoardId);
		
		
		[DllImport("pci1230.dll")] public static extern int Pci1230Read(int pBoardid, ref uint Data);
		[DllImport("pci1230.dll")] public static extern int Pci1230Write(int pBoardid, uint writedata);
		
        [DllImport("pci1230.dll")] public static extern int Pci1230ReadDiBit(int pBoardid, int bit,ref uint Data);
		[DllImport("pci1230.dll")] public static extern int Pci1230WriteDoBit(int pBoardid, int bit,  uint writedata);

        [DllImport("pci1230.dll")] public static extern int Pci1230ReadDoBit(int pBoardid, int bit, ref uint Data);

    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
