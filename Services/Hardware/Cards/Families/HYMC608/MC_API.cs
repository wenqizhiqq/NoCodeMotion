﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/HYMC608/MC_API.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace WenQiZhi.Domain.MotionCard.Common.HYMC608
{
    //
    // 连接类型, MC系列可以用串口与网口链接
    enum MC_CONNECTION_TYPE
    {
        MC_CONNECTION_COM = 1,
        MC_CONNECTION_ETH = 2,
        MC_CONNECTION_USB = 3,
        MC_CONNECTION_PCI = 4,
        MC_CONNECTION_CAN = 5,
    };

    enum ERR_CODE_MC
    {
        ERRCODE_UNKNOWN = 1,
        ERRCODE_PARAERR = 2,
        ERRCODE_TIMEOUT = 3,
        ERRCODE_CONTROLLERBUSY = 4,
        ERRCODE_CONNECT_TOOMANY = 5,

        ERRCODE_OS_ERR = 6,
        ERRCODE_CANNOT_OPEN_COM = 7,
        ERRCODE_CANNOT_CONNECTETH = 8,
        ERRCODE_HANDLEERR = 9, //链接错误
        ERRCODE_SENDERR = 10, //链接错误
        ERRCODE_GFILE_ERR = 11, //G文件语法错误
        ERRCODE_FIRMWAREERR = 12, //固件文件错误

        ERRCODE_FILENAME_TOOLONG = 13, //文件名太长
        ERRCODE_FIRMWAR_MISMATCH = 14, //固件文件不匹配

        ERRCODE_CARD_NOTSUPPORT = 15, //对应的卡不支持这个功能


        ERRCODE_BUFFER_TOO_SMALL = 15, //输入的缓冲太小
        ERRCODE_NEED_PASSWORD = 16,    //密码保护
        ERRCODE_PASSWORD_ENTER_TOOFAST = 17,    //密码输入太快



        ERRCODE_GET_LENGTH_ERR = 100, //收到的数据包的长度错误， 这个测试完成后不会出现, 字符串接口时可能超过缓冲长度

        ERRCODE_COMPILE_OFFSET = 1000, //文件编译错误


        ERRCODE_CONTROLLERERR_OFFSET = 100000, //控制器上面传来的错误，加上这个偏移

    };



    //[System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
    public class MCShell
    {
        /*********************************************************
        函数声明
        **********************************************************/

        /*************************************************************
        说明：与控制器建立链接
        输入：无
        输出：卡链接phandle
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_Open")]//, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 MC_Open(int type, ref string pconnectstring, ref IntPtr phandle);
       

        /*************************************************************
        说明：与控制器建立串口链接
        输入：无
        输出：卡链接phandle
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_OpenCom")]
        public static extern Int32 MC_OpenCom(uint comid, uint baudrate, ref IntPtr phandle);

       /*************************************************************
        说明：与控制器建立链接
        输入：无
        输出：卡链接phandle
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_OpenCan")]
        public static extern Int32 MC_OpenCan(UInt32 canid, UInt32 baud, ref IntPtr phandle);



        /*************************************************************
        说明：用网口与控制器建立链接
        输入：IP地址，字符串的方式输入
        输出：卡链接phandle
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_OpenEth")]
        public static extern Int32 MC_OpenEth(string ipaddr, ref IntPtr phandle);

        /*************************************************************
        说明：与控制器建立链接
        输入：IP地址，32位数的IP地址输入, 注意字节顺序
        输出：卡链接phandle
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_OpenEthEx")]
        public static extern Int32 MC_OpenEthEx(Int32 straddr, ref IntPtr phandle);

        /*************************************************************
        说明：关闭控制器链接
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_Close")]
        public static extern Int32 MC_Close(IntPtr phandle);


        /*************************************************************
        说明：命令的延时等待时间
        输入：卡链接phandle 毫秒时间
        输出：
        返回值：错误码
        *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_SetTimeOut")]
        public static extern Int32 MC_SetTimeOut(IntPtr phandle, Int32 timems);

        /*************************************************************
        说明：命令的延时等待时间
        输入：卡链接phandle 
        输出：毫秒时间
        返回值：错误码
        *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_GetTimeOut")]
         public static extern Int32 MC_GetTimeOut(IntPtr phandle, ref Int32 ptimems);

        /*************************************************************
        说明：读取长时间命令的进度
        输入：卡链接phandle 
        输出：
        返回值：进度， 浮点， 
        *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_GetProgress")]
         public static extern float MC_GetProgress(IntPtr phandle);


        /**********************************************
            command 函数列表
        *******************************************/


        /*************************************************************
        说明：//读取系统状态
        输入：卡链接phandle
        输出：状态
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetState")]
         public static extern int MC_GetState(IntPtr phandle, out Byte pstate);
        //[DllImport("MCX08.dll", EntryPoint = "")] Int32 __stdcall SMCGetState(IntPtr phandle,ref Byte  pstate);

        /*************************************************************
        说明：//读取链接控制器的轴数
        输入：卡链接phandle
        输出：
        返回值：轴数，出错0
        *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_GetAxises")]
        public static extern Byte MC_GetAxises(IntPtr phandle);

        /*************************************************************
        说明：下载程序文件 下载前会编译一次
        输入：卡链接phandle 文件名
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_DownProgram")]
         public static extern Int32 MC_DownProgram(IntPtr phandle, string pfilename, string pfilenameinControl);

        /*************************************************************
        说明：下载程序文件 
        输入：卡链接phandle buff 控制器上文件的名字
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_DownMemProgram")]
        public static extern Int32 MC_DownMemProgram(IntPtr phandle, byte[] pbuffer, UInt32 buffsize, string pfilenameinControl);
        [DllImport("MCX08.dll", EntryPoint = "MC_DownSafeProgram")]
        public static extern Int32 MC_DownSafeProgram(IntPtr phandle, byte[] pbuffer, UInt32 buffsize, string pfilenameinControl);
        /*************************************************************
        说明：下载程序文件 到临时文件中
        输入：卡链接phandle buff 控制器上文件的名字
        输出：
        返回值：错误码
        *************************************************************/
        //[DllImport("MCX08.dll", EntryPoint = "SMCDownProgramToTemp")]
         //public static extern Int32 SMCDownProgramToTemp(IntPtr phandle, string pfilename);
        //[DllImport("MCX08.dll", EntryPoint = "")] int32 __stdcall SMCDownProgramToTemp(SMCHANDLE handle, string pfilename);

        /*************************************************************
        说明：下载程序文件 到临时文件中
        输入：卡链接phandle buff 控制器上文件的名字
        输出：
        返回值：错误码
        *************************************************************/
        //[DllImport("MCX08.dll", EntryPoint = "SMCDownMemProgramToTemp")]
       //  public static extern Int32 SMCDownMemProgramToTemp(IntPtr phandle, string pbuffer, Int32 buffsize);


        /*************************************************************
        说明：运行
        输入：卡链接phandle 文件名， 当为NULL的时候运行缺省文件
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_RunProgramFile")]
        public static extern Int32 MC_RunProgramFile(IntPtr phandle, string pfilenameinControl);



        /*************************************************************
        说明：下载到ram中运行
        输入：卡链接phandle 文件名
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_DownProgramToRamAndRun")]
        public static extern Int32 MC_DownProgramToRamAndRun(IntPtr phandle, string pfilename);        

        /*************************************************************
        说明：下载到ram中运行
        输入：卡链接phandle 内存buff
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_DownMemProgramToRamAndRun")]
        public static extern Int32 MC_DownMemProgramToRamAndRun(IntPtr phandle, byte[] pbuffer, Int32 buffsize);

        /*************************************************************
        说明：上传程序文件
        输入：卡链接phandle 内存buff
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_UpProgram")]
        public static extern Int32 MC_UpProgram(IntPtr phandle, string pfilename, string pfilenameinControl);

        /*************************************************************
        说明：上传程序文件
        输入：卡链接phandle 内存buff
        输出：
        返回值：错误码
        *************************************************************/
        //[DllImport("MCX08.dll", EntryPoint = "SMCUpProgramToMem")]
        //public static extern Int32 SMCUpProgramToMem(IntPtr phandle, ref Byte pbuffer, Int32 buffsize, string pfilenameinControl, ref Int32 puifilesize);
        //[DllImport("MCX08.dll", EntryPoint = "")] int32 __stdcall SMCUpProgramToMem(SMCHANDLE handle, char* pbuffer, uint32 buffsize, char* pfilenameinControl, uint32* puifilesize);

        /*************************************************************
        说明：运行控制器内部程序
        输入：卡链接handle 文件名， 当为NULL的时候运行缺省文件
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_Run")]
        public static extern Int32 MC_Run(IntPtr phandle);

        /*************************************************************
        说明：暂停
        输入：卡链接phandle 文件名， 当为NULL的时候运行缺省文件
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_Pause")]
         public static extern Int32 MC_Pause(IntPtr phandle);

        /*************************************************************
        说明：停止
        输入：卡链接phandle 文件名， 当为NULL的时候运行缺省文件
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_Stop")]
        public static extern Int32 MC_Stop(IntPtr phandle);

        /*************************************************************
        说明：运行临时文件
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
       // [DllImport("MCX08.dll", EntryPoint = "SMCRunTempFile")]
       // public static extern  Int32 SMCRunTempFile(IntPtr phandle);
        //[DllImport("MCX08.dll", EntryPoint = "")] int32 __stdcall SMCRunTempFile(SMCHANDLE handle);

        /*************************************************************
        说明：读取剩余空间
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_EnterPassword")]
        public static extern Int32 MC_EnterPassword(IntPtr phandle, string pass);

        [DllImport("MCX08.dll", EntryPoint = "MC_ModifyPassword")]
        public static extern Int32 MC_ModifyPassword(IntPtr phandle, string pass,string passnew);

        [DllImport("MCX08.dll", EntryPoint = "MC_CheckRemainProgramSpace")]
        public static extern Int32 MC_CheckRemainProgramSpace(IntPtr phandle, ref Int32 pRemainSpaceInKB);

       // [DllImport("MCX08.dll", EntryPoint = "MC_CheckRemainProgramSpace")]
       // public static extern Int32 MC_CheckRemainProgramSpace(IntPtr phandle, ref Int32 pRemainSpaceInKB);

        /*************************************************************
        说明：读取程序停止原因
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        //[DllImport("MCX08.dll", EntryPoint = "SMCCheckProgramStopReason")]
        // public static extern Int32 SMCCheckProgramStopReason(IntPtr phandle, ref Int32 pStopReason);
        //[DllImport("MCX08.dll", EntryPoint = "")] int32 __stdcall SMCCheckProgramStopReason(SMCHANDLE handle, uint32 * pStopReason);

        /*************************************************************
        说明：读取程序当前行
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadCurRunLine")]
        public static extern Int32 MC_ReadCurRunLine(IntPtr phandle, out Int32 pLineNum);
        


        /*************************************************************
        说明：设置单步运行，这个实时修改状态，重启后丢失
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_SetStepRun")]
        public static extern Int32 MC_SetStepRun(IntPtr phandle, Byte bifStep);


    

        /*************************************************************
        说明：继续运行
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ContinueRun")]
        public static extern Int32 MC_ContinueRun(IntPtr phandle);
      


        /*************************************************************
        说明：检查文件是否存在
        输入：卡链接phandle 控制器上文件名，不带扩展
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_CheckProgramFile")]
        public static extern Int32 MC_CheckProgramFile(IntPtr phandle, string pfilenameinControl, ref Byte pbIfExist, ref Int32 pFileSize);


        /*************************************************************
        说明：查找控制器上的文件， 文件名为空表示文件不不存在
        输入：卡链接phandle 控制器上文件名，不带扩展
        输出： 是否存在 文件大小
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_FindFirstProgramFile")]
        public static extern Int32 MC_FindFirstProgramFile(IntPtr phandle, ref Byte pfilenameinControl, ref Int32 pFileSize);
        


        /*************************************************************
        说明：查找控制器上的文件， 文件名为空表示文件不不存在
        输入：卡链接phandle 控制器上文件名，不带扩展
        输出： 是否存在 文件大小
        返回值：错误码
        *************************************************************/
       [DllImport("MCX08.dll", EntryPoint = "MC_FindNextProgramFile")]
        public static extern  Int32 MC_FindNextProgramFile(IntPtr phandle, ref Byte pfilenameinControl, ref Int32 pFileSize);

        /*************************************************************
        说明：查找控制器上的当前文件
        输入：卡链接phandle 控制器上文件名，不带扩展
        输出： 是否存在 文件大小(暂时不支持)
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetCurProgramFile")]
        public static extern Int32 MC_GetCurProgramFile(IntPtr phandle, ref Byte pfilenameinControl, ref Int32 pFileSize);


        /*************************************************************
        说明：删除控制器上的文件
        输入：卡链接phandle 控制器上文件名，不带扩展
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_DeleteProgramFile")]
        public static extern Int32 MC_DeleteProgramFile(IntPtr phandle, string pfilenameinControl);

        /*************************************************************
        说明：删除控制器上的文件
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_RemoveAllProgramFiles")]
        public static extern Int32 MC_RemoveAllProgramFiles(IntPtr phandle);
   

        /***********************  设置部分  ************************/


        /*************************************************************
        说明：通用的字符串接口
        输入：卡链接phandle 发送字符串，接收字符串， 接收字符串长度, 当不想要应答时，把uiResponseLength = 0
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_Command")]
        public static extern Int32 MC_Command(IntPtr phandle, string pszCommand, out byte psResponse, UInt32 uiResponseLength);

        /*************************************************************
        说明：读取脚本输出的信息
        uimax必须足够大才好
        输入：卡链接handle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadMessage", CharSet = CharSet.Auto)]
        public static extern Int32 MC_ReadMessage(IntPtr phandle, out byte retByte, UInt32 uimax, out UInt32 puiread);

   

        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetCurIpAddr")]
        public static extern Int32 MC_GetCurIpAddr(IntPtr phandle, ref Byte sIpAddr, ref Byte sGateAddr, ref Byte sMask, ref Byte pbifdhcp);

 

 

        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_SetMotionAxisSpeed")]
        public static extern Int32 MC_SetMotionAxisSpeed(IntPtr phandle, Byte iaxis, Int32 uiSpeed);


        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：速度
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetMotionAxisSpeed")]
        public static extern Int32 MC_GetMotionAxisSpeed(IntPtr phandle, Byte iaxis);

        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_SetStartSpeed")]
        public static extern Int32 MC_SetStartSpeed(IntPtr phandle, Byte iaxis, Int32 uiSpeed);

        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetStartSpeed")]
        public static extern Int32 MC_GetStartSpeed(IntPtr phandle, Byte iaxis);


        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_SetAcceleration")]
        public static extern Int32 MC_SetAcceleration(IntPtr phandle, Byte iaxis, Int32 uiValue);

        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetAcceleration")]
        public static extern Int32 MC_GetAcceleration(IntPtr phandle, Byte iaxis);

        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_SetDeceleration")]
        public static extern Int32 MC_SetDeceleration(IntPtr phandle, Byte iaxis, Int32 uiValue);
        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetDeceleration")]
        public static extern Int32 MC_GetDeceleration(IntPtr phandle, Byte iaxis);

      
     
       
        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_SetLinerSpeed")]
        public static extern Int32 MC_SetLinerSpeed(IntPtr phandle,Byte iliner,Int32 uiValue);

        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetLinerSpeed")]
        public static extern Int32 MC_GetLinerSpeed(IntPtr phandle,Byte iliner);

        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_SetLinerAcceleration")]
        public static extern Int32 MC_SetLinerAcceleration(IntPtr phandle, Byte iliner, Int32 uiValue);

        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetLinerAcceleration")]
        public static extern Int32 MC_GetLinerAcceleration(IntPtr phandle, Byte iliner);


        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_SetLinerDeceleration")]
        public static extern Int32 MC_SetLinerDeceleration(IntPtr phandle, Byte iliner, Int32 uiValue);

        /*************************************************************
        说明：参数函数
        输入：卡链接phandle
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetLinerDeceleration")]
        public static extern Int32 MC_GetLinerDeceleration(IntPtr phandle, Byte iliner);

        ////////////////////////////////////软限位相关函数////////////////////////////////////////////////////////
        //是否使能软限位
        [DllImport("MCX08.dll", EntryPoint = "MC_SetIfSoftLimition")]
        public static extern Boolean MC_SetIfSoftLimition(IntPtr phandle, Byte iaxis, Boolean value);

        //读取是否使能软限位
        [DllImport("MCX08.dll", EntryPoint = "MC_GetIfSoftLimition")]
        public static extern Boolean MC_GetIfSoftLimition(IntPtr phandle, Byte iaxis);

        //设定指定轴的正向软限位
        [DllImport("MCX08.dll", EntryPoint = "MC_SetMotionSoftLimitionPlus")]
        public static extern UInt32 MC_SetMotionSoftLimitionPlus(IntPtr phandle, Byte iaxis, Int32 value);

        //读取指定轴的正向软限位
        [DllImport("MCX08.dll", EntryPoint = "MC_GetMotionSoftLimitionPlus")]
        public static extern Int32 MC_GetMotionSoftLimitionPlus(IntPtr phandle, Byte iaxis);

        //设定指定轴的负向软限位
        [DllImport("MCX08.dll", EntryPoint = "MC_SetMotionSoftLimitionDec")]
        public static extern UInt32 MC_SetMotionSoftLimitionDec(IntPtr phandle, Byte iaxis, Int32 value);

        //读取指定轴的负向软限位
        [DllImport("MCX08.dll", EntryPoint = "MC_GetMotionSoftLimitionDec")]
        public static extern Int32 MC_GetMotionSoftLimitionDec(IntPtr phandle, Byte iaxis);
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////


        /***********************  运动部分  ************************/
 
        /*************************************************************
        说明：
        输入：卡链接phandle 轴号， 长度， 是否绝对移动
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_Pmove_Enter")]
        public static extern Int32 MC_Pmove_Enter(IntPtr phandle, Byte iaxis);

        [DllImport("MCX08.dll", EntryPoint = "MC_Pmove_SetRelative")]
        public static extern Int32 MC_Pmove_SetRelative(IntPtr phandle, Byte iaxis ,Int32 ilength);

        [DllImport("MCX08.dll", EntryPoint = "MC_Pmove_SetAbsolute")]
        public static extern Int32 MC_Pmove_SetAbsolute(IntPtr phandle, Byte iaxis, Int32 ilength);

        [DllImport("MCX08.dll", EntryPoint = "MC_Pmove_GetAbsolute")]
        public static extern Int32 MC_Pmove_GetAbsolute(IntPtr phandle, Byte iaxis);

        [DllImport("MCX08.dll", EntryPoint = "MC_Pmove_GetRelative")]
        public static extern Int32 MC_Pmove_GetRelative(IntPtr phandle, Byte iaxis);

        [DllImport("MCX08.dll", EntryPoint = "MC_Pmove_Start")]
        public static extern Int32 MC_Pmove_Start(IntPtr phandle, Byte iaxis);


        /*************************************************************
        说明：速度运动
        输入：卡链接phandle 轴号， 
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_Vmove_Enter")]
        public static extern Int32 MC_Vmove_Enter(IntPtr phandle, Byte iaxis);

        //
        [DllImport("MCX08.dll", EntryPoint = "MC_Vmove_GetDir")]
        public static extern Int32 MC_Vmove_GetDir(IntPtr phandle, Byte iaxis);

        [DllImport("MCX08.dll", EntryPoint = "MC_Vmove_SetDir")]
        public static extern Int32 MC_Vmove_SetDir(IntPtr phandle, Byte iaxis, Byte idir);


        [DllImport("MCX08.dll", EntryPoint = "MC_Vmove_SetSpeed")]
        public static extern Int32 MC_Vmove_SetSpeed(IntPtr phandle, Byte iaxis, Int32 speed);


        [DllImport("MCX08.dll", EntryPoint = "MC_Vmove_GetSpeed")]
        public static extern Int32 MC_Vmove_GetSpeed(IntPtr phandle, Byte iaxis);

        [DllImport("MCX08.dll", EntryPoint = "MC_Vmove_Start")]
        public static extern Int32 MC_Vmove_Start(IntPtr phandle, Byte iaxis);




     


        /*************************************************************
        说明：
        输入：卡链接phandle 轴号， 方向
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_CheckDown")]
        public static extern Byte MC_CheckDown(IntPtr phandle, Byte iaxis);
        [DllImport("MCX08.dll", EntryPoint = "MC_CheckAllDown")]
        public static extern Byte MC_CheckAllDown(IntPtr phandle);
        /*************************************************************
        说明：回零，回零模式通过参数指定
        输入：卡链接phandle 轴号， 方向
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetZeroDir")]
        public static extern Byte MC_GetZeroDir(IntPtr phandle, Byte iaxis);

        [DllImport("MCX08.dll", EntryPoint = "MC_GetZeroMode")]
        public static extern Byte MC_GetZeroMode(IntPtr phandle, Byte iaxis);

        [DllImport("MCX08.dll", EntryPoint = "MC_GetZeroSpeed")]
        public static extern Int32 MC_GetZeroSpeed(IntPtr phandle, Byte iaxis);

        [DllImport("MCX08.dll", EntryPoint = "MC_SetZeroDir")]
        public static extern Int32 MC_SetZeroDir(IntPtr phandle, Byte iaxis, Byte idir);

        [DllImport("MCX08.dll", EntryPoint = "MC_SetZeroMode")]
        public static extern Int32 MC_SetZeroMode(IntPtr phandle, Byte iaxis, Byte idir);

        [DllImport("MCX08.dll", EntryPoint = "MC_SetZeroSpeed")]
        public static extern Int32 MC_SetZeroSpeed(IntPtr phandle, Byte iaxis, Int32 speed);

        [DllImport("MCX08.dll", EntryPoint = "MC_HomeMove")]
        public static extern Int32 MC_HomeMove(IntPtr phandle, Byte iaxis);


        /*************************************************************
        说明：
        输入：卡链接phandle 轴号
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_Home_IfHoming")]
        public static extern Byte MC_Home_IfHoming(IntPtr phandle, Byte iaxis);

        /*************************************************************
        说明：
        输入：卡链接phandle 轴号
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_DeclStop")]
        public static extern Int32 MC_DeclStop(IntPtr phandle, Byte iaxis);

        [DllImport("MCX08.dll", EntryPoint = "MC_DeclStopAll")]
        public static extern Int32 MC_DeclStopAll(IntPtr phandle);

        /*************************************************************
        说明：
        输入：卡链接phandle 轴号
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ImdStop")]
        public static extern Int32 MC_ImdStop(IntPtr phandle, Byte iaxis);

        [DllImport("MCX08.dll", EntryPoint = "MC_ImdStopAll")]
        public static extern Int32 MC_ImdStopAll(IntPtr phandle);


   


        /*************************************************************
        说明：读取位置 脉冲产生器的坐标
        输入：
        输出：脉冲位置
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetAllPulsePositon")]
        public static extern Int32 MC_GetAllPulsePositon(IntPtr phandle, out int pos);

       
        /*************************************************************
        说明：
        输入：卡链接phandle 轴号 坐标
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_SetPulsePositon")]
        public static extern Int32 MC_SetPulsePositon(IntPtr phandle, Byte iaxis, Int32 iposition);
        /*************************************************************
         说明：
         输入：卡链接phandle 轴号 坐标
         输出：
         返回值：错误码
         *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetPulsePositon")]
        public static extern Int32 MC_GetPulsePositon(IntPtr phandle, Byte iaxis);




        /*************************************************************
        说明：
        输入：卡链接phandle 轴号 坐标
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetCurSpeed")]
        public static extern Int32 MC_GetCurSpeed(IntPtr phandle, Byte iaxis);

        /*************************************************************
        说明：
        输入：卡链接phandle 轴号 坐标
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetAimPositon")]
        public static extern Int32 MC_GetAimPositon(IntPtr phandle, Byte iaxis);



        /*************************************************************
        说明：手轮运动
        输入：卡链接phandle 轴号
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_HandWheel_Enter")]
        public static extern Int32 MC_HandWheel_Enter(IntPtr phandle, Byte iaxis);
        [DllImport("MCX08.dll", EntryPoint = "MC_HandWheel_GetConfig")]
        public static extern Int32 MC_HandWheel_GetConfig(IntPtr phandle, Byte iaxis, out Int16 pimulti, out Byte pbifDirReverse);
        [DllImport("MCX08.dll", EntryPoint = "MC_HandWheel_Set")]
        public static extern Int32 MC_HandWheel_Set(IntPtr phandle, Byte iaxis, Int16 pimulti,  Byte pbifDirReverse);


        /*************************************************************
        说明：插补运动
        输入：卡链接phandle 轴号
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_SetLinerCornerSpeed")]
        public static extern Int32 MC_SetLinerCornerSpeed(IntPtr phandle, Byte iliner, Int32 value);

        [DllImport("MCX08.dll", EntryPoint = "MC_GetLinerCornerSpeed")]
        public static extern Int32 MC_GetLinerCornerSpeed(IntPtr phandle, Byte iliner);

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_Enter")]
        public static extern Int32 MC_Liner_Enter(IntPtr phandle, Byte iliner,Byte iaxises, Byte[] iaxisList);
        //添加需要参加插补的轴号
        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_AddAxis")]
        public static extern Int32 MC_Liner_AddAxis(IntPtr phandle, Byte iliner, Byte iaxis);
        //删除参加插补的轴号
        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_DelAxis")]
        public static extern Int32 MC_Liner_DelAxis(IntPtr phandle, Byte iliner, Byte iaxis);
        //圆弧运动
        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_Arc")]
        public static extern Int32 MC_Liner_Arc(IntPtr phandle, Byte iliner,Int32 imark,Byte bIfRelative, Byte iaxis1, Byte iaxis2, Int32 Distance1, Int32 Distance2, 
                                                Int32 Center1, Int32 Center2, Byte bIfAnticlockwise, Byte bIfDynaSpeed);

        //螺旋插补
        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_Screw")]
        public static extern Int32 MC_Liner_Screw(IntPtr phandle, Byte iliner, Int32 imark, Byte bIfRelative, Byte iaxis1, Byte iaxis2, Int32 Distance1, Int32 Distance2,
                                                Int32 Center1, Int32 Center2, Byte bIfAnticlockwise, Byte iaxis3, Int32 Distance3,Byte bifGlassScrew,Byte bIfDynaSpeed);
        //三点圆弧插补
        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_Arc_3P")]
        public static extern UInt32 MC_Liner_Arc_3P(IntPtr phandle, Byte iliner, Int32 imark, Byte bIfRelative, Byte iaxis1, Byte iaxis2, Int32 MidPoint1, Int32 MidPoint2,
                                                Int32 EndPoint1, Int32 EndPoint2, Byte bIfDynaSpeed);
        //空间圆弧插补
        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_SpaceArc")]
        public static extern UInt32 MC_Liner_SpaceArc(IntPtr phandle, Byte iliner, Int32 imark, Byte bIfRelative, Byte[] iaxisList, Int32[] MidDistance, Int32[] EndDistance, Byte bIfDynaSpeed);

        //直线插补
        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_LineN")]
        public static extern Int32 MC_Liner_LineN(IntPtr phandle, Byte iliner, Int32 imark, Byte bIfRelative, Byte itotalaxises, Byte[] iaxisList, Int32[] Distance, Byte bIfDynaSpeed);
        //限速点
        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_AddSpeedLimition")]
        public static extern Int32 MC_Liner_AddSpeedLimition(IntPtr phandle, Byte iliner, Int32 speedLimit);

        //线段添加结束
        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_AddEnd")]
        public static extern Int32 MC_Liner_AddEnd(IntPtr phandle, Byte iliner);
        //启动插补运动
        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_Start")]
        public static extern Int32 MC_Liner_Start(IntPtr phandle, Byte iliner);

        //插补暂停
        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_Pause")]
        public static extern Int32 MC_Liner_Pause(IntPtr phandle, Byte iliner);



        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_GetRemainBufferSpace")]
        public static extern Int16 MC_Liner_GetRemainBufferSpace(IntPtr phandle, Byte iliner);

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_GetCurSpeed")]
        public static extern Int32 MC_Liner_GetCurSpeed(IntPtr phandle, Byte iliner);

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_GetTotalVectLength")]
        public static extern Int32 MC_Liner_GetTotalVectLength(IntPtr phandle, Byte iliner);

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_GetCurMark")]
        public static extern Int32 MC_Liner_GetCurMark(IntPtr phandle, Byte iliner);   

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_GetState")]
        public static extern Byte MC_Liner_GetState(IntPtr phandle, Byte iliner);


        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_CheckDown")]
        public static extern Byte MC_Liner_CheckDown(IntPtr phandle, Byte iliner);

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_DeclStop")]
        public static extern Byte MC_Liner_DeclStop(IntPtr phandle, Byte iliner);

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_ImdStop")]
        public static extern Byte MC_Liner_ImdStop(IntPtr phandle, Byte iliner);

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_Get_Outbit_Space")]
        public static extern Byte MC_Liner_Get_Outbit_Space(IntPtr phandle);


	/*********************** 连续插补IO部分 ************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_Clear_All_Outbit")]
        public static extern Int32 MC_Liner_Clear_All_Outbit(IntPtr phandle);

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_Delay")]
        public static extern Int32 MC_Liner_Clear_All_Outbit(IntPtr phandle, Byte iliner, Byte imark, Byte delay);

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_IO")]
        public static extern Int32 MC_Liner_IO(IntPtr phandle, Byte iliner, Byte ioNum, Byte IoState, Byte ifrev, Byte iotype, Byte delay);

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_Add_Outbit")]
        public static extern Int32 MC_Liner_Add_Outbit(IntPtr phandle, Byte iliner, Byte ioNum, Byte IoState);

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_Add_Outbit_Extern")]
        public static extern Int32 MC_Liner_Add_Outbit_Extern(IntPtr phandle,Byte iliner,Byte ioNum,Byte IoState);

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_Add_Iopulse")]
        public static extern Int32 MC_Liner_Add_Iopulse(IntPtr phandle,Byte iliner,Byte ioNum,Int32 count,Int32 periodus);

        [DllImport("MCX08.dll", EntryPoint = "MC_Liner_Add_Delaytime")]
        public static extern Int32 MC_Liner_Add_Delaytime(IntPtr phandle,Byte iliner,Int32 timems);
        /***********************  IO等接口部分  ************************/

        /*************************************************************
        说明：点亮LED，或者灭掉
        输入：卡链接phandle led编号，从1开始
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_WriteLed")]
        public static extern Int32 MC_WriteLed(IntPtr phandle, Int32 iLedNum, Byte bifLighten);


        /*************************************************************
        说明：写输出口
        输入：卡链接phandle io编号，从1开始 0-低电平， 1- 高电平
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_WriteOutBit")] 
        public static extern Int32 MC_WriteOutBit(IntPtr phandle, UInt16 ioNum, Byte IoState);
        //SMC6200API int32 __stdcall SMCWriteOutBit(SMCHANDLE handle, uint16 ioNum, uint8 IoState)

        /*************************************************************
        说明：读输入口
        输入：卡链接phandle io编号，从1开始
        输出：0-低电平， 1- 高电平
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadInBit")] 
        public static extern Int32 MC_ReadInBit(IntPtr phandle, Int32 ioNum, ref Byte pIoState);

        /*************************************************************
        说明：读输出口
        输入：卡链接phandle io编号，从1开始
        输出：0-低电平， 1- 高电平
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadOutBit")] 
        public static extern Int32 MC_ReadOutBit(IntPtr phandle, UInt16 ioNum, out byte pIoState);
        //SMC6200API int32 __stdcall SMCReadOutBit(SMCHANDLE handle, uint16 ioNum, uint8* pIoState)

        /*************************************************************
        说明：写全部输出口
        输入：卡链接phandle 
              IoMask: 1的位要修改，可以通过这个参数修改指定几个IO
              IoState:  0-低电平， 1- 高电平;  0-31位 代表 1-32IO
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_WriteOutPort")] 
        public static extern Int32 MC_WriteOutPort(IntPtr phandle, Int32 IoMask, Int32 IoState);

        /*************************************************************
        说明：读全部输入口
        输入：卡链接phandle 
        输出：0-低电平， 1- 高电平
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadInPort")]
        public static extern Int32 MC_ReadInPort(IntPtr phandle, out UInt32 pIoState);

        /*************************************************************
        说明：读全部输出口
        输入：卡链接phandle 
        输出：0-低电平， 1- 高电平
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadOutPort")] 
        public static extern Int32 MC_ReadOutPort(IntPtr phandle, out UInt32 pIoState);


        /*************************************************************
        说明：读伺服告警输入状态
        输入：卡链接phandle io编号，从1开始
        输出：0-有效， 1- 无效
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadAlarmState")]
        public static extern Int32 MC_ReadAlarmState(IntPtr phandle, Byte iaxis, ref Byte pIoState);

        /*************************************************************
        说明：读原点输入状态
        输入：卡链接phandle io编号，从1开始
        输出：0-有效， 1- 无效
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadHomeState")]
        public static extern Int32 MC_ReadHomeState(IntPtr phandle, Byte iaxis, ref Byte pIoState);


        /*************************************************************
        说明：读急停输入状态
        输入：卡链接phandle
        输出：0-有效， 1- 无效
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadEMGState")]
        public static extern Int32 MC_ReadEMGState(IntPtr phandle, out Byte pIoState);
        /*************************************************************
        说明：读手轮AB输入状态,
        输入：卡链接phandle
        输出：0-有效， 1- 无效
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadHandWheelStates")]
        public static extern Int32 MC_ReadHandWheelStates(IntPtr phandle, Byte iaxis, ref Byte pIoAState, ref Byte pIoBState);

        /*************************************************************
        说明：读限位状态
        输入：卡链接phandle
        输出：0-有效， 1- 无效
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadElStates")]
        public static extern Int32 MC_ReadElStates(IntPtr phandle, Byte iaxis, ref Byte pElDecState, ref Byte pElPlusState);


        /*************************************************************
        说明：读减速信号输入状态
        输入：卡链接phandle io编号，从1开始
        输出：0-有效， 1- 无效
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadSdStates")]
        public static extern Int32 MC_ReadSdStates(IntPtr phandle, Byte iaxis, ref Byte pIoState);

        /*************************************************************
        说明：读到位信号输入状态
        输入：卡链接phandle io编号，从1开始
        输出：0-有效， 1- 无效
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadInpStates")]
        public static extern Int32 MC_ReadInpStates(IntPtr phandle, Byte iaxis, ref Byte pIoState);

        /*************************************************************
        说明：写PWM占空比
        输入：卡链接phandle 通道:1/2
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_WritePwmDuty")]
        public static extern Int32 MC_WritePwmDuty(IntPtr phandle, Byte ichannel, float fDuty);
        /*************************************************************
        说明：写PWM频率
        输入：卡链接phandle 通道:1/2
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_WritePwmFreqency")]
        public static extern Int32 MC_WritePwmFreqency(IntPtr phandle, Byte ichannel, float fFre);

        /*************************************************************
        说明：写DA输出电压
        输入：卡链接phandle 通道:1/2
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_WriteDaOut")]
        public static extern Int32 MC_WriteDaOut(IntPtr phandle, Byte ichannel, float fLevel);

        /*************************************************************
        说明：PWM占空比
        输入：卡链接phandle 通道:1/2
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadPwmDuty")]
        public static extern Int32 MC_ReadPwmDuty(IntPtr phandle, Byte ichannel, ref float fDuty);

        /*************************************************************
        说明：PWM频率
        输入：卡链接phandle 通道:1/2
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadPwmFreqency")]
        public static extern Int32 MC_ReadPwmFreqency(IntPtr phandle, Byte ichannel, ref float fFre);

        /*************************************************************
        说明：DA输出电压
        输入：卡链接phandle 通道:1/2
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ReadDaOut")]
        public static extern Int32 MC_ReadDaOut(IntPtr phandle, Byte ichannel, ref float fLevel);


        /*************************************************************
        说明：客户编号, 这个函数只对部分客户开放
        输入：卡链接phandle
        输出：状态
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetClientId")]
       public static extern Int32 MC_GetClientId(IntPtr phandle,ref Int32 pId);

        /*************************************************************
        说明：软件产品类型
        输入：卡链接phandle
        输出：状态
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetSoftwareId")]
        public static extern Int32 MC_GetSoftwareId(IntPtr phandle,ref Int32 pId);


        /*************************************************************
        说明：硬件编号
        输入：卡链接phandle
        输出：状态
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetHardwareId")]
        public static extern Int32 MC_GetHardwareId(IntPtr phandle,out Int32 pId);


        /*************************************************************
        说明：软件版本号，用日期标识
        输入：卡链接phandle
        输出：状态
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetSoftwareVersion")]
        public static extern Int32 MC_GetSoftwareVersion(IntPtr phandle, out Int32 pVersion);

       
       

        /*************************************************************
        说明：modbus寄存器操作
        输入：卡链接phandle 寄存器地址
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_ModbusSet0x")]
        public static extern Int32 MC_ModbusSet0x(IntPtr phandle, Int32 start, Int32 inum, Byte[] pdata);

        [DllImport("MCX08.dll", EntryPoint = "MC_ModbusGet0x")]
        public static extern Int32 MC_ModbusGet0x(IntPtr phandle, Int32 start, Int32 inum, out Byte pdata);

        [DllImport("MCX08.dll", EntryPoint = "MC_ModbusGet4x")]
        public static extern Int32 MC_ModbusGet4x(IntPtr phandle, Int32 start, Int32 inum, out Int32 pdata);

        [DllImport("MCX08.dll", EntryPoint = "MC_ModbusSet4x")]
        public static extern Int32 MC_ModbusSet4x(IntPtr phandle, Int32 start, Int32 inum, Int32[] pdata);


        [DllImport("MCX08.dll", EntryPoint = "MC_ModbusSetIEEE")]
        public static extern Int32 MC_ModbusSetIEEE(IntPtr phandle, Int32 start, float[] pdata);

        [DllImport("MCX08.dll", EntryPoint = "MC_ModbusGetIEEE")]
        public static extern Int32 MC_ModbusGetIEEE(IntPtr phandle, Int32 start, out float pdata);
  
    

        /*************************************************************
        说明：读取编码器的位置
        输入：连接句柄：handle；轴号：0-3或0xFF
        输出：编码器的位置
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetEncoderPositon")]
        public static extern Int32 MC_GetEncoderPositon(IntPtr phandle, Byte iaxis);

        /*************************************************************
        说明：设置编码器的位置
        输入：连接句柄：handle；轴号：0-3
        输出：编码器的位置
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_SetEncoderPositon")]
        public static extern Int32 MC_SetEncoderPositon(IntPtr phandle, Byte iaxis, Int32 pposition);

     
    

        /*************************************************************
        功能：设置编码器输入口的计数方式。
        参数：  axis：轴号
		        mode：编码器反馈输入模式
			        0 非A/B 相, 为脉冲+方向
			        1 1 倍 A/B 相脉冲信号
			        2 2 倍A/B 相脉冲信号
			        3 4 倍A/B 相脉冲信号
        返回值：无
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_SetEncoderSet")]
        public static extern Int32 MC_SetEncoderSet(IntPtr phandle, Byte axis, Byte mode);

        /*************************************************************
        功能：设置编码器输入口的计数方式。
        参数：  axis：轴号
        输出：	mode：编码器反馈输入模式
			        0 非A/B 相, 为脉冲+方向
			        1 1 倍 A/B 相脉冲信号
			        2 2 倍A/B 相脉冲信号
			        3 4 倍A/B 相脉冲信号
        返回值：无
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetEncoderSet")]
        public static extern Int32 MC_GetEncoderSet(IntPtr phandle, Byte axis, ref Byte mode);

        /*************************************************************
        说明：回零，回零模式通过参数指定
        输入：卡链接handle 轴号， 方向
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_SetIfCounterEnable")]
        public static extern Int32 MC_SetIfCounterEnable(IntPtr phandle, Byte iaxis, Byte EzTimes);

        /*************************************************************
        说明：回零，回零模式通过参数指定
        输入：卡链接handle 轴号， 方向
        输出：
        返回值：错误码
        *************************************************************/
        [DllImport("MCX08.dll", EntryPoint = "MC_GetIfCounterEnable")]
        public static extern Byte MC_GetIfCounterEnable(IntPtr phandle, Byte iaxis);



      
        /*************************************************************
        说明：回零，回零模式通过参数指定
        输入：卡链接handle 轴号， 方向
        输出：
        返回值：错误码
        *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_SetHomeEzTimes")]
        public static extern Int32 MC_SetHomeEzTimes(IntPtr phandle, Byte iaxis, Byte EzTimes);

        /*************************************************************
        说明：回零，回零模式通过参数指定
        输入：卡链接handle 轴号， 方向
        输出：
        返回值：错误码
        *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_GetHomeEzTimes")]
         public static extern Byte MC_GetHomeEzTimes(IntPtr phandle, Byte iaxis);


         //断点和调试功能

         /*************************************************************
         说明：设置断点信息
         输入：
         输出：
         返回值：错误码
         *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_SetBreakPointInfo")]
         public static extern UInt32 MC_SetBreakPointInfo(IntPtr phandle, int[] lineList, int linenum, char taskID);
         //SMC6200API uint32 __stdcall SMC_SetBreakPointInfo(SMCHANDLE Card, int32* line,int32 linenum)
         /*************************************************************
         说明：设置单步运行
         输入：
         输出：
         返回值：错误码
         *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_SetStepRun")]
         public static extern UInt32 MC_SetStepRun(IntPtr phandle);
         //SMC6200API uint32 __stdcall SMCSetStepRun(SMCHANDLE Card)


   
         /*************************************************************
         说明：跳过当前断点
         输入：
         输出：
         返回值：错误码
         *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_SetStepOver")]
         public static extern UInt32 MC_SetStepOver(IntPtr phandle);
         //SMC6200API uint32 __stdcall SMCSetStepOver(SMCHANDLE Card)




         /*************************************************************
         说明：读取变量至,如有需要读取变量 dim temp ，temp1 ；则生成字符串如下 (temp ,temp1),作为输入参数
               如果是数组则生成(temp(1),temp1(3))
         输入：最多读取和设置32个变量
         输出：
         返回值：错误码
         *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_ReadVar")]
         public static extern UInt32 MC_ReadVar(IntPtr phandle, byte[] strVar, out Int64 varValue, out int varNum);
         //SMC6200API uint32 __stdcall SMCReadVar(SMCHANDLE Card, char*varstring, int64* var, int32 *num)

         /*************************************************************
         说明：修改数组值，支持大数据下载
         输入：数组
         输出：
         返回值：错误码
         *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_ModifyStructEx")]
         public static extern UInt32 MC_ModifyStructEx(IntPtr phandle, byte[] strArry, Int64[] varValue, out int totalNum);
         //SMC6200API uint32 __stdcall SMCModifyVar(SMCHANDLE Card,char*varstring, int64* var,int32 varnum)


         /*************************************************************
         说明：修改变量,如有变量Abc ,bbb;则生成变量字符串如下(Abc,bbb)
         输入：最多读取和设置32个变量
         输出：
         返回值：错误码
         *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_ModifyVar")]
         public static extern UInt32 MC_ModifyVar(IntPtr phandle, byte[] strVar, Int64[] varValue, out int varNum);
         //SMC6200API uint32 __stdcall SMCModifyVar(SMCHANDLE Card,char*varstring, int64* var,int32 varnum)


         /*************************************************************
         说明：输入的字符串数组字符串,如要读取A,B,C数组则输入 "A,B,C"
         输入：
         输出：
         返回值：错误码
         *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_ReadStructSet")]
         public static extern UInt32 MC_ReadStructSet(IntPtr phandle, byte[] strVar);
         //SMC6200API uint32 __stdcall SMCReadStructSet(SMCHANDLE Card,char*varstring)

         /*************************************************************
         说明：读取数组和上面的设置对应数组数组
         输入：
         输出：
         返回值：错误码
         *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_ModifyStruct")]
         public static extern UInt32 MC_ModifyStruct(IntPtr phandle, uint index, out Int64 varType, out int SartIndex, out int varNum);
         //uint32 __stdcall SMCModifyStruct(SMCHANDLE Card ,uint32 index, int64* var, int32 num)

         /*************************************************************
             说明：读取数组和上面的设置对应数组数组
             输入：
             输出：
             返回值：错误码
             *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_ReadStruct")]
         public static extern UInt32 MC_ReadStruct(IntPtr phandle, uint index, out Int64 varValue, out int varNum);
         //uint32 __stdcall SMCReadStruct(SMCHANDLE Card ,uint32 index,int64* var,int32 *num)

         /*************************************************************
         说明：查询输入的字符串类型,如果是数组则返回数组的总位数
         输入：
         输出：
         返回值：错误码
         *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_GetStringType")]
         public static extern UInt32 MC_GetStringType(IntPtr phandle, byte[] strVar, out int varType, out int varNum);
         //SMC6200API uint32 __stdcall SMCGetStringType(SMCHANDLE Card,char*varstring,int32* m_Type,int32* num)

         /*************************************************************
        说明：查询输入的字符串类型,如果是数组则返回数组的总位数
        输入：
        输出：
        返回值：错误码
        *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_EncodeBasicFile")]
         public static extern int MC_EncodeBasicFile(byte[] pInData, IntPtr pOutData, int len);
         //public static extern int SMCEncodeBasicFile(byte[] pInData, out byte[] pOutData, int len);
         //public static extern int SMCEncodeBasicFile(byte[] pInData, ref string outText , int len);
         //SMC6200API char* __stdcall SMCEncodeBasicFile(const char* pData)

         [DllImport("MCX08.dll", EntryPoint = "MC_DownEncodedFile")]
         public static extern int MC_DownEncodedFile(IntPtr phandle, string fPath);
         //int32 __stdcall SMCDownEncodedFile(SMCHANDLE handle, LPCTSTR fpath)

         [DllImport("MCX08.dll", EntryPoint = "BasicFileDecode")]
         public static extern int BasicFileDecode(string fPath, IntPtr pOutData, int len);
         //int32 __stdcall BasicFileDecode(const char* pInData, char* pOutData, int len)
         //BasicFileDecode(LPCTSTR fpath, char* pOutData, int len)

         [DllImport("MCX08.dll", EntryPoint = "MC_GetTaskState")]
         public static extern int MC_GetTaskState(IntPtr phandle, out UInt16 pstate);
         //int32 __stdcall SMCGetTaskState(SMCHANDLE handle,uint16 *pstate)

         /*************************************************************
         说明：读取各轴停止原因
         输入：
         输出：无
         返回值：停止原因
                          0--正常
                         1--硬限位
                         2--急停
                         4--ALARM
                         8--软限位
                         16--手动停止
         *************************************************************/
         [DllImport("MCX08.dll", EntryPoint = "MC_GetStopReason")]
         public static extern int MC_GetStopReason(IntPtr phandle, byte iaxis);




        /******************************************************************特殊信号参数配置********************************************************************/
        //设置指定轴的报警信号有效电平
        [DllImport("MCX08.dll", EntryPoint = "MC_SetIfAlarmHighValid")]
        public static extern int MC_SetIfAlarmHighValid(IntPtr phandle,Byte iaxis, bool value);


        //读取指定轴的报警信号有效电平
        [DllImport("MCX08.dll", EntryPoint = "MC_GetIfAlarmHighValid")]
        public static extern int MC_GetIfAlarmHighValid(IntPtr phandle, Byte iaxis);


        //设置指定轴是否使能报警信号
        [DllImport("MCX08.dll", EntryPoint = "MC_SetIfAlarmValid")]
        public static extern int MC_SetIfAlarmValid(IntPtr phandle, Byte iaxis,bool value);

        //读取指定轴是否报警
        [DllImport("MCX08.dll", EntryPoint = "MC_GetIfAlarmValid")]
        public static extern int MC_GetIfAlarmValid(IntPtr phandle, Byte iaxis);

       
        //设置指定轴的原点信号有效电平
        [DllImport("MCX08.dll", EntryPoint = "MC_SetIfHOMEHighValid")]
        public static extern int MC_SetIfHOMEHighValid(IntPtr phandle, Byte iaxis,bool value);

        //读取指定轴的原点信号有效电平
        [DllImport("MCX08.dll", EntryPoint = "MC_GetIfHOMEHighValid")]
        public static extern int MC_GetIfHOMEHighValid(IntPtr phandle, Byte iaxis);


        //设置指定轴的到位信号有效电平
        [DllImport("MCX08.dll", EntryPoint = "MC_SetIfINPHighValid")]
        public static extern int MC_SetIfINPHighValid(IntPtr phandle, Byte iaxis,bool value);


        //读取指定轴的到位信号有效电平
        [DllImport("MCX08.dll", EntryPoint = "MC_GetIfINPHighValid")]
        public static extern int MC_GetIfINPHighValid(IntPtr phandle, Byte iaxis);


        //设置指定轴的是否使能到位信号
        [DllImport("MCX08.dll", EntryPoint = "MC_SetIfINPValid")]
        public static extern int MC_SetIfINPValid(IntPtr phandle, Byte iaxis,bool value);


        //读取指定轴的是否使能到位信号
        [DllImport("MCX08.dll", EntryPoint = "MC_GetIfINPValid")]
        public static extern int MC_GetIfINPValid(IntPtr phandle, Byte iaxis);


        //设置指定轴的是否使能限位信号
        [DllImport("MCX08.dll", EntryPoint = "MC_SetIfELValid")]
        public static extern int MC_SetIfELValid(IntPtr phandle, Byte iaxis, Byte value);


        //读取是指定轴的否使能限位信号
        [DllImport("MCX08.dll", EntryPoint = "MC_GetIfELValid")]
        public static extern int MC_GetIfELValid(IntPtr phandle, Byte iaxis);


        //设置指定轴的限位信号有效电平
        [DllImport("MCX08.dll", EntryPoint = "MC_SetIfELHighValid")]
        public static extern int MC_SetIfELHighValid(IntPtr phandle, Byte iaxis, bool value);


        //读取指定轴的限位信号有效电平
        [DllImport("MCX08.dll", EntryPoint = "MC_GetIfELHighValid")]
        public static extern int MC_GetIfELHighValid(IntPtr phandle, Byte iaxis);


        //设置急停信号的有效电平
        [DllImport("MCX08.dll", EntryPoint = "MC_SetIfEMGHighValid")]
        public static extern int MC_SetIfEMGHighValid(IntPtr phandle,bool value);


        //读取急停信号的有效电平
        [DllImport("MCX08.dll", EntryPoint = "MC_GetIfEMGHighValid")]
        public static extern int MC_GetIfEMGHighValid(IntPtr phandle);

        //设置指定轴的脉冲模式
        [DllImport("MCX08.dll", EntryPoint = "MC_SetPulseOutSet ")]
        public static extern int MC_SetPulseOutSet(IntPtr phandle, Byte iaxis, Byte value);

        //读取指定轴的脉冲模式
        [DllImport("MCX08.dll", EntryPoint = "MC_GetPulseOutSet ")]
        public static extern Byte MC_GetPulseOutSet(IntPtr phandle, Byte iaxis);


         /*************************************************************
         说明：读轴所有输入状态
         输入：卡链接handle 轴号，当0XFF的时候，表示读取所有的轴，
         输出：0-有效， 1- 无效
         返回值：错误码
        **************************************************************/
         public struct AxisStates
         {
             byte m_axisnum;

             public byte m_HomeState;
             public byte m_AlarmState;

             public byte m_SDState;
             public byte m_INPState;

             public byte m_ElDecState;
             public byte m_ElPlusState;

             public byte m_HandWheelAState;
             public byte m_HandWheelBState;
             public byte m_EncodeAState; //6200没有这个信号
             public byte m_EncodeBState; //6200没有这个信号
             //uint8 m_EMGState; //每个轴都一样

             public byte m_ClearState; //6200没有这个信号

             //增加软限位信号
             public byte m_SoftElDecState; //0- 有效
             public byte m_SoftElPlusState;
         }

         [DllImport("MCX08.dll", EntryPoint = "MC_ReadAxisStates")]
         public static extern int MC_ReadAxisStates(IntPtr phandle, byte iaxis, out AxisStates pAxisState);
        // int32 __stdcall SMCReadAxisStates(SMCHANDLE handle, uint8 iaxis, struct_AxisStates* pAxisState)


       }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
