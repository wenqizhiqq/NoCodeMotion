﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/YKMCC400P/YKMCC400PSDK.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
#define reversal

using csMCC;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Samsun.Domain.MotionCard.Common.YKMCC400P
{
    public class YKMCC400PSDK
    {
        public bool IsInit { get; set; }

        private static YKMCC400PSDK _instance;
        public static YKMCC400PSDK Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new YKMCC400PSDK();
                return YKMCC400PSDK._instance;
            }
            set { YKMCC400PSDK._instance = value; }
        }


        /// <summary>
        /// 取板卡函数的返回值的释义
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetErrorInfo(int ErrNum)
        {
            if (ErrNum > 0)
            {
                ErrNum += 10000;
            }
            var msg = string.Empty;
            Dictionary<int, string> dic = new Dictionary<int, string>()
            {
                {0,"" },  //ERR_NOERR
                {10001,"PCI通讯错误" },
                {10002,"固件不支持此命令ID" },
                {10003,"固件不支持此参数, 参数错误" },
                {10004,"固件不支持此回零模式" },
                {10005,"一维比较器序号超有效范围，范围[0,7]" },
                {10006,"比较通用输出IO超有效范围，范围[0,15]" },
                {10007,"IO映射类型范围超限，有效范围[0,6]" },
                {10008,"通用输入IO序号有效范围超限，有效范围[0,15]" },
                {10009,"输出IO序号有效范围超限，有效范围[0,31]" },
                {10010,"高速锁存器有效范围超限，有效范围[0,7]" },
                {10011,"比较缓冲区已满" },
                {10012,"正软限位报警" },
                {10013,"负软位报警" },
                {10014,"轴正在运动中" },
                 {10015,"轴序号有效范围超限，有效范围[0,7]" },
                 {10016,"点位运动目标位置有效范围超限，有效范围[2147483640, -2147483640]" },
                  {10017,"减速阶段不支持在线变速" },
                   //{10014,"轴正在运动中" },
                    {10018,"立即停止时不支持在线变速" },
                     {10019,"减速阶段不支持在线变位置" },
                      {10020,"立即停止时不支持在线变速" },
                       {10021,"坐标系序号有效范围超限，有效范围[[0.1]" },
                        {10022,"圆弧半径圆弧，半径大于起点和终点距离的一般" },
                         {10023,"终点半径模式或三点模式圆弧，起点和终点坐标重合" },
                          {10024,"三点模式圆弧，三点在同一直线上" },
                           {10025,"减速停止加速度小于等于零" },
                            {10026,"轴最大速度为零" },
                             {10027,"轴EMG信号有效" },
                              {10028,"轴报警信号有效" },
                               {10029,"高速比较器序号有效范围超限，有效范围[0,3]" },
                                {10030,"高速比较计数错误" },
                                 {10031,"轴还没处于空闲状态" },
                                 {10032,"空间圆弧插补不能用半径圆弧模式" },
                                 {10033,"轴正硬限位有效" },
                                 {10034,"轴负硬限位有效" },
                                 {10035,"圆心终点圆弧起点和终点重合" },
                            {10036,"轴当前处于原点位置" },
                      {10037,"读取文件缓冲区太小" },
                      {10038,"文件名太长" },
                     {10039,"文件名错误" },
                    {10040,"文件太长" },
                     {10041,"文件ID改变" },
                      {10042,"下载文件块号错误" },
                       {10043,"文件大小不匹配" },
                        {10044,"CRC不匹配" },
                         {10045,"轴当前处于EZ点位置" },
                          {10046,"PWM已使能" },
                           {10047,"插补轴号相同了" },
                                       {10048,"插补参数无效" },
                     {10049,"自定义编号无效" },
                      {10050,"IO被设置成其它用途，不能做普通IO使用" },
                       {10051,"回零方向错误" },
                        {10052,"回零模式超" },
                         {10053,"文电子齿轮主轴没有运行" },
                          {10054,"外部减速停止信号有效" },
                           {10065,"反向间隙参数设置错误" },
                                       {10056,"预览未完成" },
                     {10057,"联动插补正在运行" },
                      {10058,"联动插补未使能" },
                       {10059,"延时插补未使能" },
                        {10060,"延时插补参数错误" },
                         {10061,"延时插补未完成" },
                          {10062,"比较通道错误" },
                           {10100,"IO被设置成锁存IO，不能做普通IO使用" },
                                                {10120,"IO被设置成一维比较，不能做普通IO使用" },
                      {10140,"IO被设置成二维比较，不能做普通IO使用" },
                       {10200,"fpga升级失败" },
                        {10202,"数据写入错误" },
                         {10203,"数据校验错误" },
                          {10205,"数据读写超时" },
                           {10206,"读出数据错误" },
                                                {10301,"插补缓冲满了" },
                      {10302,"插补缓冲区空了" },
                       {10400,"回零轴序号有效范围超限，有效范围[0,7]" },
                        {10401,"回零模块插补周期设置错误" },
                         {10402,"回零模块最大回零速度小于等于零" },
                          {10403,"回零模块回零加速度小于等于零" },
                           {10404,"回零模块回零减速度小于等于零" },
                                                {10500,"手轮模块手轮倍率为零" },
                      {10600,"插补模块插补周期设置错误" },
                       {10601,"插补模块插补速度小于等于零" },
                        {10602,"插补模块直线段长度为零" },
                         {10603,"插补模块起跳速度大于最大速度" },
                          {10604,"插补模块终点速度大于最大速度" },
                           {10605,"插补模块加速时间小于1ms" },
                                                {10606,"插补模块减速时间小于1ms" },
                      {10607,"插补平滑时间为负值" },
                       {10608,"直线插补预览中间点不在直线范围" },
                        {10700,"点位模块周期设置错误" },
                         {10701,"点位模块最大速度小于等于零" },
                          {10702,"点位模块加速度小于等于零" },
                           {10703,"点位模块减速度小于等于零" },


                                                 {10704,"点位模块在线变速失败" },
                       {10705,"点位模块当前轴状态不允许在线变速" },
                        {10706,"点位模块运动距离为零" },
                         {10707,"点位模块起跳速度大于最大速度" },
                          {10708,"点位模块终点速度大于最大速度" },
                           {10709,"点位模块停止加速度无效" },
                                                 {10710,"点位模块平滑时间无效" },
                       {10800,"JOG模块周期设置错误" },
                        {10801,"JOG模块最大速度小于等于零" },
                         {10802,"JOG模块加速时间小于1ms" },
                          {10803,"JOG模块减速时间小于1ms" },
                           {10804,"JOG模块在线变速失败" },
                                                 {10805,"JOG模块轴当前状态不允许在线变速" },
                       {10806,"JOG模块起跳速度大于最大速度" },
                        {10807,"JOG模块终点速度大于最大速度" },
                         {10808,"JOG模块终点速停止加速无效" },
                          {10900,"系统缺少初始化操作指令或初始化失败" },
                           {10901,"PWM允许的最大频率只有500KHZ" },

                            {10902,"PWM频率设置值必须大于等于零" },
                        {10903,"PWM允许的占空比不能大于1" },
                         {10904,"PWM占空比设置值必须大于等于零" },
                          {10905,"pwm设置错误" },
                           {11000,"当前坐标系状态不允许进行此操作" },
                            {11001,"IO输出模式无效" },
                        {11002,"限位模式无效" },
                         {11003,"IO输入模式无效" },
                          {11004,"输入IO序号无效" },
                           {11005,"最大速度设置错误" },



                             {11006,"圆弧参数错误" },
                         {11007,"直线插补参数错误（下发轴数大于坐标系维数）" },
                          {11200,"插补正在进行中" },
                           {11201,"插补系就绪状态" },
                             {11202,"插补器释放状态" },
                         {11203,"插补器停止状态" },
                          {11204,"插补器缓冲区数据预处理状态" },
                           {11205,"插补暂停状态" },
                             {11206,"插补器错误状态" },
                         {11207,"减速停止过程中，清除缓冲区" },
                          {11208,"暂停减速停止过程中" },
                           {11209,"插补系运行出错减速停止过程中" },


                            {11210,"减速停止过程中清除轴列表与位置列表" },
                          {11300,"前瞻缓冲区满了" },
                           {11301,"前瞻缓冲区空了" },
                            {11302,"过渡计算错误" },
                          {11312,"Pmotion s加速度设置错误" },
                           {11313,"Pmotion 参数设置错误" },

                             {11314,"Pmotion 时间参数设置错误" },
                          {11401,"区域限制ID超范围" },
                           {11402,"区域限制区域数量超范围" },
                            {11403,"区域限制IO超范围" },
                          {11404,"区域限制轴超范围" },
                           {11406,"采集数据个数超范围" },
                             {11500,"连续插补 插补系设置错误" },
                          {11506,"连续插补未完成" },
                           {11600,"PVT数据点超出给定范围1000；有效范围[0-1000]" },
                            {11601,"电子凸轮表数据点超出给定范围1000；有效范围[0-1000];" }

            };
            if (dic.ContainsKey(ErrNum))
            {
                return dic[ErrNum];
            }
            return "unkown errNum";

        }






        //DMC3000卡 所用函数

        /// <summary>
        /// 设置主板上输入IO的端口的电平（虚拟运动卡专用）
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="bitno"></param>
        /// <param name="on_off">输入电平，0：低电平，1：高电平</param>
        /// <returns></returns>
        public int SetCardWriteInBit(int CardNo, int bitno, int on_off)
        {
            return 0;
        }



        #region 板卡设置函数 √
        /// <summary>
        /// 控制卡初始化函数，分配系统资源
        /// </summary>
        /// <returns><para>0：    没有找到控制卡，或者控制卡异常</para><para>1~8：  控制卡数 </para><para>负值： 表明有 2 张或 2 张以上控制卡的硬件设置卡号相同；返回值取绝对值后减 1即为该卡号</para></returns>
        public int InitBoardCard()
        {
            return MCC.YK_board_init(); // YK_board_init();
        }

        /// <summary>
        /// 控制卡硬件复位函数
        /// <para>注 意：</para><para>执行复位操作后，必须等待 5 秒方可执行初始化控制卡，否则会出错。出错后必须重新执行复位操作，再等待 5 秒后执行初始化控制卡</para>
        /// </summary>
        /// <returns>错误代码</returns>
        public int CardBoardReset()
        {
            return MCC.YK_board_reset(); // YK_board_reset();
        }

        /// <summary>
        /// 控制卡软复位函数
        /// </summary>
        /// <returns>错误代码</returns>
        public int CardSoftReset(ushort CardNo)
        {
            return 0; 
        }

        /// <summary>
        /// 控制卡关闭函数，释放系统资源
        /// </summary>
        /// <returns>错误代码</returns>
        public int CloseBoardCard()
        {
            return MCC.YK_board_close(); // YK_board_close();
        }

        /// <summary>
        /// 获取控制卡硬件 ID 号
        /// </summary>
        /// <param name="CardNum">返回初始化成功的卡数</param>
        /// <param name="CardTypeList">返回控制卡固件类型数组  : 参数CardTypeList 类型为十六进制  </param>
        /// <param name="CardIdList">返回控制卡硬件 ID 号数组，卡号按从小到大顺序排列 </param>
        /// <returns>错误代码 </returns>
        public int GetCardInfList(ref UInt16 CardNum, ref UInt32[] CardTypeList, ref UInt16[] CardIdList)
        {
            uint v1=0, v2=0;
            MCC.YK_get_card_soft_version(CardNum, ref v1, ref v2);
            var res= MCC.YK_get_CardInfList(ref CardNum, CardTypeList, CardIdList); // YK_get_CardInfList(ref CardNum, CardTypeList, CardIdList);
            //CardTypeList[0] = v2;
            return res;
        }

        /// <summary>
        /// 设置插补速度
        /// </summary>
        /// <param name="cardNo"></param>
        /// <param name="crd"></param>
        /// <param name="minVel"></param>
        /// <param name="maxVel"></param>
        /// <param name="tacc"></param>
        /// <param name="tdec"></param>
        /// <param name="stopVel"></param>
        /// <returns></returns>
        public short SetMultiVectorSpeed(ushort cardNo, ushort crd, double minVel, double maxVel, double tacc)
        {
            double stopvel = 0;
            return MCC.YK_get_vector_profile_multicoor(cardNo, crd, ref minVel,ref maxVel, ref tacc, ref tacc, ref stopvel); // YK_set_vector_profile_multicoor(cardNo, crd, 0, maxVel, tacc, 0, 0);
        }

        /// <summary>
        /// /// 获取控制卡硬件版本号 
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="CardVersion">返回控制卡硬件版本号 </param>
        /// <returns>错误代码</returns>
        public int GetCardVersion(int CardNo, ref UInt32 CardVersion)
        {
            return MCC.YK_get_card_version((ushort)CardNo, ref CardVersion); // YK_get_card_version((ushort)CardNo, ref CardVersion);
        }

        /// <summary>
        /// 获取控制卡固件版本号 
        /// </summary>
        /// <param name="CardNo">卡号 </param>
        /// <param name="FirmID">返回控制卡固件类型</param>
        /// <param name="SubFirmID"> 返回控制卡固件版本号 </param>
        /// <returns>错误代码</returns>
        public int GetCardSoftVersion(int CardNo, ref UInt32 FirmID, ref UInt32 SubFirmID)
        {
            return MCC.YK_get_card_soft_version((ushort)CardNo, ref FirmID, ref SubFirmID); //  YK_get_card_soft_version((ushort)CardNo, ref FirmID, ref SubFirmID);
        }

        /// <summary>
        /// 获取控制卡动态库文件版本号
        /// </summary>
        /// <param name="LibVer">返回库版本号</param>
        /// <returns>错误代码</returns>
        public int GetCardLibVersion(ref UInt32 LibVer)
        {
            return MCC.YK_get_card_lib_version(ref LibVer); // YK_get_card_lib_version(ref LibVer);
        }

        /// <summary>
        /// 获取当前卡的轴数 
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="TotalAxis">返回当前卡的轴数</param>
        /// <returns>错误代码</returns>
        public int GetCardTotalAxisNum(int CardNo, ref UInt32 TotalAxis)
        {
            return MCC.YK_get_total_axes((ushort)CardNo, ref TotalAxis); //YK_get_total_axes((ushort)CardNo, ref TotalAxis);
        }

        /// <summary>
        /// 下载参数文件
        /// <para> 注  意：</para>
        /// <para>1）当使用相对路径时，参数文件与程序必须在同一目录下</para>
        /// <para>2）可以在 Motion3000 软件中“参数设置”界面下，将各轴参数设置好，然后点击“参数文件操作”-“读取”将参数文件保存。最后在编写程序时，使用函数YK_download_configfile 将参数文件下载 </para>
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="FileName">文件路径：| 相对路径 ：参数文件名+后缀：|  绝对路径：完整描述参数文件的路径+文件名后缀</param>
        /// <returns>错误代码</returns>
        public int GetCardDownloadConfigfile(int CardNo, string FileName = "AxisPara.ini")
        {
            int result = MCC.YK_download_configfile((ushort)CardNo, FileName);// YK_download_configfile((ushort)CardNo, FileName);
#if reversal     //Flip usage instructions based on Rexay's own dynamic link library
            //result = MCC.YK_download_configfile_ex((ushort)CardNo, FileName);
#endif
            return result;
        }

        /// <summary>
        /// 下载固件文件
        ///  <para>注  意：</para>
        ///   <para>1）当使用相对路径时，固件文件与程序必须在同一目录下</para>
        ///    <para>2）可以在控制卡 Motion 软件中右击主卡->“固件升级”菜单下直接升级固件</para>
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="FileName">文件路径：</param>
        /// <returns>错误代码</returns>
        public int GetCardDownloadFirmware(int CardNo, string FileName)
        {
            return MCC.YK_download_firmware((ushort)CardNo, FileName); // YK_download_firmware((ushort)CardNo, FileName);
        }

        #endregion

        #region 脉冲模式设置函数 √
        /// <summary>
        /// 设置指定轴的脉冲输出模式 
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5  DMC3400A：0~3 </param>
        /// <param name="outmode">脉冲输出方式选择: 
        /// <para>0：正方向脉冲: PULSE输出端：低电平 ；DIR 输出端：高电平 | 负方向脉冲: PULSE输出端: 低电平  ；DIR 输出端:低电平</para>
        /// <para>1：正方向脉冲: PULSE输出端：高电平 ；DIR 输出端：高电平 | 负方向脉冲: PULSE输出端: 高电平  ；DIR 输出端:低电平</para>
        /// <para>2：正方向脉冲: PULSE输出端：低电平 ；DIR 输出端：低电平 | 负方向脉冲: PULSE输出端: 低电平  ；DIR 输出端:高电平</para>
        /// <para>3：正方向脉冲: PULSE输出端：高电平 ；DIR 输出端：低电平 | 负方向脉冲: PULSE输出端: 高电平  ；DIR 输出端:高电平</para>
        /// <para>4：正方向脉冲: PULSE输出端：低电平 ；DIR 输出端：高电平 | 负方向脉冲: PULSE输出端: 高电平  ；DIR 输出端:低电平</para>
        /// <para>5：正方向脉冲: PULSE输出端：高电平 ；DIR 输出端：低电平 | 负方向脉冲: PULSE输出端: 低电平  ；DIR 输出端:高电平</para>
        /// </param>
        /// <returns>错误代码</returns>
        public int SetCardAxisPulseOutMode(int CardNo, int axis, int outmode)
        {
            return MCC.YK_set_pulse_outmode((ushort)CardNo, (ushort)axis, (ushort)outmode); // YK_set_pulse_outmode((ushort)CardNo, (ushort)axis, (ushort)outmode);
        }

        /// <summary>
        /// 读取指定轴的脉冲输出模式设置 
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5  DMC3400A：0~3 </param>
        /// <param name="outmode">返回脉冲输出方式
        /// <para>0：正方向脉冲: PULSE输出端：低电平 ；DIR 输出端：高电平 | 负方向脉冲: PULSE输出端: 低电平  ；DIR 输出端:低电平</para>
        /// <para>1：正方向脉冲: PULSE输出端：高电平 ；DIR 输出端：高电平 | 负方向脉冲: PULSE输出端: 高电平  ；DIR 输出端:低电平</para>
        /// <para>2：正方向脉冲: PULSE输出端：低电平 ；DIR 输出端：低电平 | 负方向脉冲: PULSE输出端: 低电平  ；DIR 输出端:高电平</para>
        /// <para>3：正方向脉冲: PULSE输出端：高电平 ；DIR 输出端：低电平 | 负方向脉冲: PULSE输出端: 高电平  ；DIR 输出端:高电平</para>
        /// <para>4：正方向脉冲: PULSE输出端：低电平 ；DIR 输出端：高电平 | 负方向脉冲: PULSE输出端: 高电平  ；DIR 输出端:低电平</para>
        /// <para>5：正方向脉冲: PULSE输出端：高电平 ；DIR 输出端：低电平 | 负方向脉冲: PULSE输出端: 低电平  ；DIR 输出端:高电平</para>
        /// </param>
        /// <returns>错误代码</returns>
        public int GetCardAxisPulseOutMode(int CardNo, int axis, ref UInt16 outmode)
        {
            return MCC.YK_get_pulse_outmode((ushort)CardNo, (ushort)axis, ref outmode); // YK_get_pulse_outmode((ushort)CardNo, (ushort)axis, ref outmode);
        }
        #endregion

        #region 回原点运动函数 √

        /// <summary>
        /// 设置回零遇限位是否反找
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        public int SetCardAxisHomeElReturn(int CardNo, int axis, ushort enable)
        {
            /*
             short YK_set_home_el_return(WORD CardNo,WORD axis,WORD enable)
            功 能：设置回零遇限位是否反找
            参 数：CardNo 控制卡卡号
            axis 指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5
             DMC3400A：0~3
            Enable 使能是否遇限位反找
            返回值：错误代码
            说 明：设置回零过程当中遇限位是否反找，需要在配置回零函数时配置，一次即可，以后均
            不用配置。DMC3C00，3400A 默认使能，DMC3800，3600 默认不使能。限位反找功能只针对非限
            位回零方式起作用。
             */
            return 0;
            //return MCC.YK_set_home_el_return((ushort)CardNo, (ushort)axis, enable);
        }

        /// <summary>
        /// 设置ORG 原点信号 （适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="org_logic">ORG 信号有效电平，0：低有效，1：高有效 </param>
        /// <param name="filter">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisHomePinLogic(int CardNo, int axis, int org_logic, double filter)
        {
            return MCC.YK_set_home_pin_logic((ushort)CardNo, (ushort)axis, (ushort)org_logic, filter); // YK_set_home_pin_logic((ushort)CardNo, (ushort)axis, (ushort)org_logic, filter);
        }

        /// <summary>
        /// 读取ORG 原点信号设置 （适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="org_logic">ORG 信号有效电平，0：低有效，1：高有效 </param>
        /// <param name="filter">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisHomePinLogic(int CardNo, int axis, ref UInt16 org_logic, ref double filter)
        {
            return MCC.YK_get_home_pin_logic((ushort)CardNo, (ushort)axis, ref org_logic, ref filter); // YK_get_home_pin_logic((ushort)CardNo, (ushort)axis, ref org_logic, ref filter);
        }

        /// <summary>
        /// 设置指定轴的回原点模式（适用于所有脉冲卡）
        /// <para>注意：</para>
        /// <para>1）当回零模式mode=4 时，回零速度模式将固定为低速回零</para>
        /// <para>2) DMC3C00后四轴只支持 0、1、2、10、11、12 六种回零模式</para>
        /// <para>3) 后三种回零模式最新固件（3XX201611 及以后固件）才支持。正向回零时进行正限位回零，负向回零时进行负限位回零； 若开始回零时处于限位信号中，会先向设置的 回零方向的反向运动，移出限位信号范围后，再变向，找相应的限位信号；一次限位回 零遇到限位信号后急停；一次限位回零加反找在反找阶段遇到限位信号后急停；二次限位回零在第二次遇到限位信号后急停； </para>
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="home_dir">回零方向，0：负向，1：正向 </param>
        /// <param name="vel"> 回零速度模式：0：低速回零，即以本指令前面的 YK_set_profile 函数设置的起始速度运行 1：高速回零，即以本指令前面的 YK_set_profile 函数设置的最大速度运行</param>
        /// <param name="mode">回零模式：0：一次回零  | 1：一次回零加回找  | 2：二次回零  | 3：一次回零后再记一个同向 EZ 脉冲进行回零  | 4：记一个 EZ 脉冲进行回零  | 5：原点加反向 EZ  | 6：原点锁存  | 7：原点锁存加同向 EZ 锁存  | 8：单独记一个 EZ 锁存  | 9：原点锁存加反向 EZ 锁存 | 10.一次限位回零 | 11.	一次限位回零加反找 | 12.	二次限位回零</param>
        /// <param name="EZ_count">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisHomeMode(int CardNo, int axis, int home_dir, double vel, int mode, int EZ_count)
        {
            return MCC.YK_set_homemode((ushort)CardNo, (ushort)axis, (ushort)home_dir, vel, (ushort)mode, (ushort)EZ_count); // YK_set_homemode((ushort)CardNo, (ushort)axis, (ushort)home_dir, vel, (ushort)mode, (ushort)EZ_count);
        }

        /// <summary>
        /// 读取指定轴的回原点模式（适用于所有脉冲卡）
        /// 说  明：
        /// <para>说  明：</para>
        /// <para>设置回零过程当中遇限位是否反找，需要在配置回零函数时配置，一次即可，以后均不用配置。DMC3C00，3400A 默认使能，DMC3800，3600 默认不使能。限位反找功能只针对非限位回零方式起作用。 </para>
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="home_dir">回零方向，0：负向，1：正向 </param>
        /// <param name="vel"> 回零速度模式：0：低速回零，即以本指令前面的 YK_set_profile 函数设置的起始速度运行 1：高速回零，即以本指令前面的 YK_set_profile 函数设置的最大速度运行</param>
        /// <param name="mode">回零模式：0：一次回零  | 1：一次回零加回找  | 2：二次回零  | 3：一次回零后再记一个同向 EZ 脉冲进行回零  | 4：记一个 EZ 脉冲进行回零  | 5：原点加反向 EZ  | 6：原点锁存  | 7：原点锁存加同向 EZ 锁存  | 8：单独记一个 EZ 锁存  | 9：原点锁存加反向 EZ 锁存 | 10.一次限位回零 | 11.	一次限位回零加反找 | 12.	二次限位回零</param>
        /// <param name="EZ_count">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisHomeMode(int CardNo, int axis, ref UInt16 home_dir, ref double vel, ref UInt16 mode, ref UInt16 EZ_count)
        {
            return MCC.YK_get_homemode((ushort)CardNo, (ushort)axis, ref home_dir, ref vel, ref mode, ref EZ_count); // YK_get_homemode((ushort)CardNo, (ushort)axis, ref home_dir, ref vel, ref mode, ref EZ_count);
        }

        /// <summary>
        /// 启动轴回零
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <returns>错误代码</returns>
        public int CardAxisHomeMove(int CardNo, int axis)
        {
            return MCC.YK_home_move((ushort)CardNo, (ushort)axis); // YK_home_move((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 读取回零执行状态
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <param name="status">0：停止中；1：回零中；2：回零成功；3：回零失败；</param>
        /// <returns></returns>
        public int CardAxisGetHomeResult(int CardNo, int axis, ref ushort status)
        {
            //if (MCC.YK_check_done((ushort)CardNo, (ushort)axis) == 0)
            //{
            //    status = 1;
            //}
            //else
            //{
            //    status = 0;
            //}
            //return 0;
            int res1= MCC.YK_get_home_status((ushort)CardNo, (ushort)axis, ref status);
            //雷塞API的state为： 1：回零完成，0：回零未完成
            //因此，这里需要做一个转换
            if (status==2)
            {
                status = 1;
            }
            else
            {
                status = 0;
            }
            return res1;
        }

        #endregion

        #region 原点锁存函数 √

        /// <summary>
        /// 设置原点锁存模式
        /// <para>注意:DMC3C00后四轴不支持原点锁存功能</para>
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="enable">原点锁存使能，0：禁止，1：允许 </param>
        /// <param name="logic">触发方式，0：下降沿，1：上升沿 </param>
        /// <param name="source">位置源选择，0：指令位置计数器，1：编码器计数器 </param>
        /// <returns>错误代码</returns>
        public int SetCardAxisHomelatchMode(int CardNo, int axis, UInt16 enable, UInt16 logic, UInt16 source)
        {
            return MCC.YK_set_homelatch_mode((ushort)CardNo, (ushort)axis, enable, logic, source); // YK_set_homelatch_mode((ushort)CardNo, (ushort)axis, enable, logic, source);
        }

        /// <summary>
        /// 获取原点锁存模式
        /// <para>注意:DMC3C00后四轴不支持原点锁存功能</para>
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="enable">返回原点锁存使能状态 ，0：禁止，1：允许 </param>
        /// <param name="logic"> 返回触发方式 ，0：下降沿，1：上升沿 </param>
        /// <param name="source">返回位置源选择，0：指令位置计数器，1：编码器计数器 </param>
        /// <returns>错误代码</returns>
        public int GetCardAxisHomelatchMode(int CardNo, int axis, UInt16 enable, UInt16 logic, UInt16 source)
        {
            return MCC.YK_get_homelatch_mode((ushort)CardNo, (ushort)axis, ref enable, ref logic, ref source); // YK_get_homelatch_mode((ushort)CardNo, (ushort)axis, ref enable, ref logic, ref source);
        }

        /// <summary>
        /// 清除原点锁存标志
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <returns>错误代码</returns>
        public int ClearCardAxisHomelatchFlag(int CardNo, int axis)
        {
            return MCC.YK_reset_homelatch_flag((ushort)CardNo, (ushort)axis); // YK_reset_homelatch_flag((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 读取原点锁存标志
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <returns>原点锁存标志，0：未锁存，1：锁存 </returns>
        public int GetCardAxisHomelatchFlag(int CardNo, int axis)
        {
            return MCC.YK_get_homelatch_value((ushort)CardNo, (ushort)axis); // YK_get_homelatch_flag((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 读取原点锁存值 
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <returns>锁存值，单位：pulse </returns>
        public int GetCardAxisHomelatchValue(int CardNo, int axis)
        {
            return MCC.YK_get_homelatch_value((ushort)CardNo, (ushort)axis); // YK_get_homelatch_value((ushort)CardNo, (ushort)axis);
        }

        #endregion

        #region 限位开关设置函数 √

        /// <summary>
        /// 设置EL 限位信号  
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="el_enable">EL 信号的使能状态：0：正负限位禁止 1：正负限位允许 2：正限位禁止、负限位允许 3：正限位允许、负限位禁止</param>
        /// <param name="el_logic">EL 信号的有效电平：0：正负限位低电平有效 1：正负限位高电平有效 2：正限位低有效，负限位高有效 3：正限位高有效，负限位低有效</param>
        /// <param name="el_mode">EL 制动方式：0：正负限位立即停止  1：正负限位减速停止  2：正限位立即停止，负限位减速停止</param>
        /// <returns>错误代码 </returns>
        public int SetCardAxisELMode(int CardNo, int axis, UInt16 el_enable, UInt16 el_logic, UInt16 el_mode)
        {
            return MCC.YK_set_el_mode((ushort)CardNo, (ushort)axis, el_enable, el_logic, el_mode); // YK_set_el_mode((ushort)CardNo, (ushort)axis, el_enable, el_logic, el_mode);
        }

        /// <summary>
        /// 读取 EL 限位信号设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="el_enable">返回设置的 EL 信号使能状态 </param>
        /// <param name="el_logic"> 返回设置的 EL 信号有效电平 </param>
        /// <param name="el_mode">返回 EL 制动方式</param>
        /// <returns>错误代码 </returns>
        public int GetCardAxisELMode(int CardNo, int axis, ref UInt16 el_enable, ref UInt16 el_logic, ref UInt16 el_mode)
        {
            return MCC.YK_get_el_mode((ushort)CardNo, (ushort)axis, ref el_enable, ref el_logic, ref el_mode); // YK_get_el_mode((ushort)CardNo, (ushort)axis, ref el_enable, ref el_logic, ref el_mode);
        }

        /// <summary>
        /// 设置软限位 
        ///  <para>注  意：</para>
        /// <para>正、负限位位置可为正数也可为负数，但正限位位置应大于负限位位置 </para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="enable">使能状态，0：禁止，1：允许</param>
        /// <param name="source_sel">计数器选择，0：指令位置计数器，1：编码器计数器</param>
        /// <param name="SL_action">限位停止方式，0：减速停止，1：立即停止 </param>
        /// <param name="N_limit">负限位位置，单位：pulse </param>
        /// <param name="P_limit">正限位位置，单位：pulse </param>
        /// <returns>错误代码 </returns>
        public int SetCardAxisSoftLimit(int CardNo, int axis, UInt16 enable, UInt16 source_sel, UInt16 SL_action, Int32 N_limit, Int32 P_limit)
        {
            return MCC.YK_set_softlimit((ushort)CardNo, (ushort)axis, enable, source_sel, SL_action, N_limit, P_limit);
        }

        /// <summary>
        /// 读取软限位设置 
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="enable">返回使能状态 </param>
        /// <param name="source_sel">返回计数器选择 </param>
        /// <param name="SL_action">返回限位停止方式 </param>
        /// <param name="N_limit">返回负限位脉冲数</param>
        /// <param name="P_limit">返回正限位脉冲数</param>
        /// <returns>错误代码 </returns>
        public int GetCardAxisSoftLimit(int CardNo, int axis, ref UInt16 enable, ref UInt16 source_sel, ref UInt16 SL_action, ref Int32 N_limit, ref Int32 P_limit)
        {
            return MCC.YK_get_softlimit((ushort)CardNo, (ushort)axis, ref enable, ref source_sel, ref SL_action, ref N_limit, ref P_limit);
        }

        #endregion

        #region 位置计数器控制函数 √

        /// <summary>
        ///  设置指令脉冲位置
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="current_position">指令脉冲位置，单位：pulse </param>
        /// <returns>错误代码</returns>
        public int SetCardAxisPosition(int CardNO, int axis, Int32 current_position)
        {
            return MCC.YK_set_position((ushort)CardNO, (ushort)axis, current_position);
        }

        /// <summary>
        ///  获取指令脉冲位置
        ///  <para>注意：</para>
        ///  <para>如果开启螺距补偿功能，则该函数读取到的数值为补偿后轴的位置；</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <returns>指令脉冲位置，单位：pulse </returns>
        public int GetCardAxisPosition(int CardNo, int axis)
        {
            return MCC.YK_get_position((ushort)CardNo, (ushort)axis);
        }

        #endregion

        #region 运动状态检测及控制函数 √

        /// <summary>
        /// 读取当前速度值 
        /// <para>注意：</para>
        /// <para>当执行直线插补运动时，该函数读取的速度为矢量速度；当执行圆弧插补运动时，该函数读取的速度为各轴分量速度</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <returns>指定轴的速度，单位：pulse/s</returns>
        public double GetCardAxisCurrentSpeed(int CardNo, int axis)
        {
            double pspeed = 0;
            pspeed = MCC.YK_read_current_speed((ushort)CardNo, (ushort)axis);
            if (pspeed < 0)
            {
                pspeed = 0 - pspeed;
            }
            return pspeed;
        }


        /// <summary>
        /// 获取主卡与接线盒的通讯连接状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="State">连接状态，0：连接，1：断开</param>
        /// <returns>返回值：错误代码</returns>
        public int GetCardLinkState(int CardNo, ref UInt16 State)
        {
            return MCC.YK_LinkState((ushort)CardNo, ref State);
        }

        /// <summary>
        /// 获取指定轴的运动状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <returns>0：指定轴正在运行，1：指定轴已停止</returns>
        public int GetCardAxisCheckDone(int CardNo, int axis)
        {
            return MCC.YK_check_done((ushort)CardNo, (ushort)axis);
        }


        /// <summary>
        /// 获取坐标系的运动状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">指定控制卡上的坐标系号（取值范围：0~1）</param>
        /// <returns>：坐标系状态，0：正在使用中，1：正常停止</returns>
        public int GetCardCheckDoneMulticoor(int CardNo, int crd)
        {
            return MCC.YK_check_done_multicoor((ushort)CardNo, (ushort)crd);
        }

        /// <summary>
        /// 获取指定轴有关运动信号的状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <returns>
        /// <para> 0  、ALM 1：表示伺服报警信号 ALM 为 ON； 0：OFF</para>
        /// <para> 1  、EL+ 1：表示正硬限位信号 +EL 为 ON； 0：OFF</para>
        /// <para> 2  、EL- 1：表示负硬限位信号–EL 为 ON； 0：OFF</para>
        /// <para> 3  、EMG 1：表示急停信号 EMG 为 ON； 0：OFF</para>
        /// <para> 4  、ORG 1：表示原点信号 ORG 为 ON； 0：OFF</para>
        /// <para> 6  、SL+ 1：表示正软限位信号+SL 为 ON； 0：OFF</para>
        /// <para> 7  、SL- 1：表示负软件限位信号-SL 为 ON； 0：OFF</para>
        /// <para> 8  、INP 1：表示伺服到位信号 INP 为 ON； 0：OFF</para>
        /// <para> 9  、EZ 1：表示 EZ 信号为 ON； 0：OFF</para>
        /// <para> 10 、 RDY 1：表示伺服准备信号 RDY 为 ON（DMC3800 卡专用）；0：OFF</para>
        /// <para> 11 、 DSTP1：表示减速停止信号 DSTP 为 ON（DMC3800 卡专用）；0：OFF</para>
        /// </returns>
        public int GetCardAxisIOStatus(int CardNo, int axis)
        {
            return (int)MCC.YK_axis_io_status((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 指定轴停止运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="stop_mode">制动方式，0：减速停止，1：紧急停止</param>
        /// <returns>错误代码</returns>
        public int CardAxisStop(int CardNo, int axis, int stop_mode)
        {
            return (int)MCC.YK_stop((ushort)CardNo, (ushort)axis, (ushort)stop_mode);
        }

        /// <summary>
        /// 停止坐标系内所有轴的运动
        /// <para>此函数适用于插补运动</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">指定控制卡上的坐标系号（取值范围：0~1）</param>
        /// <param name="stop_mode">制动方式，0：减速停止，1：立即停止</param>
        /// <returns>错误代码</returns>
        public int CardAxisStopMulticoor(int CardNo, int crd, int stop_mode)
        {
            return (int)MCC.YK_stop_multicoor((ushort)CardNo, (ushort)crd, (ushort)stop_mode);
        }

        /// <summary>
        /// 紧急停止所有轴
        /// <para>注意：</para>
        /// <para>此函数适用于所有运动模式</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns>错误代码</returns>
        public int CardAxisEmgStop(int CardNo)
        {
            return (int)MCC.YK_emg_stop((ushort)CardNo);
        }

        #endregion

        #region 单轴运动速度曲线设置 √

        /// <summary>
        /// 设置单轴运动速度曲线  
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="Min_Vel">起始速度，单位：pulse/s (最大值为 2M)</param>
        /// <param name="Max_Vel">最大速度，单位：pulse/s (最大值为 2M)</param>
        /// <param name="Tacc">加速时间，单位：s (最小值为 0.001s)</param>
        /// <param name="Tdec">减速时间，单位：s (最小值为 0.001s)</param>
        /// <param name="stop_vel">停止速度，单位：pulse/s (最大值为 2M)</param>
        /// <returns>：错误代码</returns>
        public int SetCardAxisProfile(int CardNo, int axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double stop_vel)
        {
            return MCC.YK_set_profile((ushort)CardNo, (ushort)axis, Min_Vel, Max_Vel, Tacc, Tdec, stop_vel);
        }

        /// <summary>
        /// 获取单轴运动速度曲线  
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="Min_Vel">返回起始速度，单位：pulse/s (最大值为 2M)</param>
        /// <param name="Max_Vel">返回最大速度，单位：pulse/s (最大值为 2M)</param>
        /// <param name="Tacc">返回加速时间，单位：s (最小值为 0.001s)</param>
        /// <param name="Tdec">返回减速时间，单位：s (最小值为 0.001s)</param>
        /// <param name="stop_vel">返回停止速度，单位：pulse/s (最大值为 2M)</param>
        /// <returns>：错误代码</returns>
        public int GetCardAxisProfile(int CardNo, int axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double stop_vel)
        {
            return MCC.YK_get_profile((ushort)CardNo, (ushort)axis, ref Min_Vel, ref Max_Vel, ref Tacc, ref Tdec, ref stop_vel);
        }

        /// <summary>
        /// 设置单轴速度曲线 S 段参数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="s_mode">保留参数，固定值为 0</param>
        /// <param name="s_para">S 段时间，单位：s；范围：0~0.5 s</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisSProfile(int CardNo, int axis, int s_mode, double s_para)
        {
            return MCC.YK_set_s_profile((ushort)CardNo, (ushort)axis, (ushort)s_mode, s_para);
        }



        /// <summary>
        /// 设置单轴速度曲线 S 段参数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="s_mode">保留参数，固定值为 0</param>
        /// <param name="s_para">返回设置的 S 段时间，单位：s；范围：0~0.5 s</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisSProfile(int CardNo, int axis, int s_mode, ref double s_para)
        {
            return MCC.YK_get_s_profile((ushort)CardNo, (ushort)axis, (ushort)s_mode, ref s_para);
        }
        #endregion

        #region 单轴运动函数 √

        /// <summary>
        /// 指定轴点位运动
        /// <para>注 意：</para>
        /// <para>当运动模式为相对坐标模式时，目标位置大于 0 时正向运动，小于 0 时反向运动。</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="Dist">目标位置，单位：pulse</param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns>错误代码</returns>
        public int CardAxisPMove(int CardNo, int axis, int Dist, int posi_mode)
        {
            return MCC.YK_pmove((ushort)CardNo, (ushort)axis, Dist, (ushort)posi_mode);
        }


        /// <summary>
        /// 指定轴连续运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="dir">运动方向，0：负方向，1：正方向</param>
        /// <returns>错误代码</returns>
        public int CardAxisVMove(int CardNo, int axis, int dir)
        {
            return MCC.YK_vmove((ushort)CardNo, (ushort)axis, (ushort)dir);
        }

        /// <summary>
        /// 在线改变指定轴的当前运动速度
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="Curr_Vel">改变后的运动速度，单位：pulse/s</param>
        /// <param name="Taccdec">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int CardAxisChangeSpeed(int CardNo, int axis, double Curr_Vel, double Taccdec)
        {
            return MCC.YK_change_speed((ushort)CardNo, (ushort)axis, Curr_Vel, Taccdec);
        }

        /// <summary>
        /// 在线改变指定轴的当前目标位置
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="dist">目标位置，单位：pulse</param>
        /// <param name="posi_mode">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int CardAxisResetTargetPosition(int CardNo, int axis, int dist, int posi_mode)
        {
            return MCC.YK_reset_target_position((ushort)CardNo, (ushort)axis, dist, (ushort)posi_mode);
        }



        /// <summary>
        /// 强行改变指定轴的当前目标位置（在线/非在线）
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="dist">目标位置，单位：pulse</param>
        /// <param name="posi_mode">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int CardAxisUpdateTargetPosition(int CardNo, int axis, int dist, int posi_mode)
        {
            return MCC.YK_update_target_position((ushort)CardNo, (ushort)axis, dist, (ushort)posi_mode);
        }
        #endregion

        #region 插补速度曲线设置函数 √


        /// <summary>
        /// 设置插补速度
        /// <para> 说 明：</para>
        /// <para>DMC3000 系列卡支持两个插补系（参数 Crd）。两个插补系的速度可独立设置，执行插补运动时两个插补系可独立进行插补运动（即可同时进行两组插补运动）</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">指定控制卡上的坐标系号（取值范围：0~1）</param>
        /// <param name="Min_Vel">保留参数，固定值为 0</param>
        /// <param name="Max_Vel">合成最大速度，单位：pulse/s</param>
        /// <param name="Tacc">加减速时间，单位：s（最小值为 0.001s）</param>
        /// <param name="Tdec">保留参数，固定值为 0</param>
        /// <param name="Stop_Vel">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int SetCardVectorProfileMulticoor(int CardNo, int Crd, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel)
        {
            return MCC.YK_set_vector_profile_multicoor((ushort)CardNo, (ushort)Crd, Min_Vel, Max_Vel, Tacc, Tdec, Stop_Vel);
        }

        /// <summary>
        /// 读取插补速度设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">指定控制卡上的坐标系号；取值范围：0~1</param>
        /// <param name="Min_Vel">保留参数</param>
        /// <param name="Max_Vel">返回合成最大速度，单位：pulse/s</param>
        /// <param name="Tacc">返回加减速时间，单位：s</param>
        /// <param name="Tdec">保留参数</param>
        /// <param name="Stop_Vel">保留参数</param>
        /// <returns>错误代码</returns>
        public int GetCardVectorProfileMulticoor(int CardNo, int Crd, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double Stop_Vel)
        {
            return MCC.YK_get_vector_profile_multicoor((ushort)CardNo, (ushort)Crd, ref Min_Vel, ref Max_Vel, ref Tacc, ref Tdec, ref Stop_Vel);
        }

        #endregion

        #region 插补运动函数 √

        /// <summary>
        /// 直线插补运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">指定控制卡上的坐标系号；取值范围：0~1</param>
        /// <param name="axisNum">插补轴数，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5DMC3400A：0~3</param>
        /// <param name="axisList">插补轴列表</param>
        /// <param name="DistList">插补轴目标位置列表，单位：pulse</param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns></returns>
        public int CardAxisLineMulticoor(int CardNo, int Crd, int axisNum, UInt16[] axisList, int[] DistList, int posi_mode)
        {
            var list2 = new List<double>();
            foreach (var m in DistList) list2.Add(m);
            return MCC.YK_line_multicoor((ushort)CardNo, (ushort)Crd, (ushort)axisNum, axisList, list2.ToArray(), (ushort)posi_mode);
        }

        /// <summary>
        /// 两轴圆弧插补运动，圆心位置+终点位置
        /// <para>注 意：</para>
        /// <para>1）检测圆弧插补状态应使用坐标系状态检测函数 YK_check_done_multicoor；停止正在执行的圆弧插补运动应使用坐标系停止函数 YK_stop_multicoor</para>
        /// <para>2）圆弧插补设置的终点位置与理论终点位置的允许误差在+/-100 个脉冲以内。以相对坐标模式为例，当圆心位置为（0,1000），终点理论位置为（0，2000）时，而终点位置被设置为（0,2100），该圆弧插补仍可正常运行</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">指定控制卡上的坐标系号；取值范围：0~1</param>
        /// <param name="AxisList">轴列表数组</param>
        /// <param name="Target_Pos">终点坐标，单位：pulse</param>
        /// <param name="Cen_Pos">圆心坐标，单位：pulse</param>
        /// <param name="Arc_Dir">圆弧方向，0：顺时针，1：逆时针</param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns>错误代码</returns>
        public int CardAxisArcMoveMulticoor(int CardNo, int Crd, UInt16[] AxisList, int[] Target_Pos, int[] Cen_Pos, int Arc_Dir, int posi_mode)
        {
            var TargetPos = new List<double>();
            foreach (var m in Target_Pos) TargetPos.Add(m);
            var CenPos = new List<double>();
            foreach (var m in Cen_Pos) CenPos.Add(m);
            return MCC.YK_arc_move_multicoor((ushort)CardNo, (ushort)Crd, AxisList, TargetPos.ToArray(), CenPos.ToArray(), (ushort)Arc_Dir, (ushort)posi_mode);
        }

        #endregion

        #region PVT 运动函数 √

        /// <summary>
        /// 向指定数据表传送数据，采用 PTT 模式
        /// <para>注 意：</para>
        /// <para>1）下载的第一组（即起始点）数据中位置、时间必须为 0；数组中的数据都是以起始点的数据为参考点</para>
        /// <para>2）调用该指令向数据表中传递数据时，会删除数据表中原先的数据，因此所有数据应当一次传送完毕。如果使用数据表的轴正在运动，禁止更新数据表</para>
        /// <para>3）DMC3C00 后四轴不支持 PVT 功能</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="count">数据点个数，每个数据表具有 1000 个存储空间，每个数据点占用 1 个存储空间</param>
        /// <param name="pTime">数据点时间数组，单位：s（精度：ms）；数组长度：count</param>
        /// <param name="pPos">数据点位置数组，单位：pulse；数组长度：count</param>
        /// <returns>错误代码</returns>
        public int SendCardAxisPttTable(int CardNo, int axis, UInt32 count, double[] pTime, int[] pPos)
        {
            return MCC.YK_PttTable((ushort)CardNo, (ushort)axis, count, pTime, pPos);
        }

        /// <summary>
        /// 向指定数据表传送数据，采用 PTS 模式
        /// <para>注 意：</para>
        /// <para>1）下载的第一组（即起始点）数据中位置、时间必须为 0；数组中的数据都是以起始点的数据为参考点</para>
        /// <para> 2）调用该指令向数据表中传递数据时，会删除数据表中原有数据，因此所有数据应当一次传送完毕。如果使用数据表的轴正在运动，禁止更新数据表</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="count">数据点个数，每个数据表具有 1000 个存储空间，每个数据点占用 1 个存储空间</param>
        /// <param name="pTime">数据点时间数组，单位：s（精度：ms）；数组长度：count</param>
        /// <param name="pPos">数据点位置数组，单位：pulse；数组长度：count</param>
        /// <param name="pPercent">数据点百分比数组，百分比的取值范围：[0,100]；数组长度：count</param>
        /// <returns>错误代码</returns>
        public int SendCardAxisPtsTable(int CardNo, int axis, UInt32 count, double[] pTime, int[] pPos, double[] pPercent)
        {
            return MCC.YK_PtsTable((ushort)CardNo, (ushort)axis, count, pTime, pPos, pPercent);
        }

        /// <summary>
        /// 向指定数据表传送数据，采用 PVT 模式
        /// <para>注 意：</para>
        /// <para>1）下载的第一组（即起始点）数据中位置、时间、速度必须为 0；数组中的数据都是以起始点的数据为参考点</para>
        /// <para>2）调用该指令向数据表中传递数据时，会删除数据表中原有数据，因此所有数据应当一次传送完毕。如果使用数据表的轴正在运动，禁止更新数据表</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="count">数据点个数，每个数据表具有 5000 个存储空间，每个数据点占用 1个存储空间</param>
        /// <param name="pTime">数据点时间数组，单位：s（精度：ms）；数组长度：count</param>
        /// <param name="pPos">数据点位置数组，单位：pulse；数组长度：count</param>
        /// <param name="pVel">数据点速度数组，单位：pulse/s；数组长度：count</param>
        /// <returns>错误代码</returns>
        public int SendCardAxisPvtTable(int CardNo, int axis, UInt32 count, double[] pTime, int[] pPos, double[] pVel)
        {
            return MCC.YK_PvtTable((ushort)CardNo, (ushort)axis, count, pTime, pPos, pVel);
        }

        /// <summary>
        /// 向指定数据表传送数据，采用 PVTS 模式
        /// <para>注 意：</para>
        /// <para>1）下载的第一组（即起始点）数据中位置、时间、速度必须为 0；数组中的数据都是以起始点的数据为参考点</para>
        /// <para>2）调用该指令向数据表中传递数据时，会删除数据表中原有数据，因此所有数据应当一次传送完毕。如果使用数据表的轴正在运动，禁止更新数据表</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="count">数据点个数，每个数据表具有 5000 个存储空间，每个数据点占用 1个存储空间</param>
        /// <param name="pTime">数据点时间数组，单位：s（精度：ms）；数组长度：count</param>
        /// <param name="pPos">数据点位置数组，单位：pulse；数组长度：count</param>
        /// <param name="velBegin">设置的第一点的速度，单位：pulse/s</param>
        /// <param name="velEnd">设置的最后一点的速度，单位：pulse/s</param>
        /// <returns>错误代码</returns>
        public int SendCardAxisPvtsTable(int CardNo, int axis, UInt32 count, double[] pTime, int[] pPos, double velBegin, double velEnd)
        {
            return MCC.YK_PvtsTable((ushort)CardNo, (ushort)axis, count, pTime, pPos, velBegin, velEnd);
        }

        /// <summary>
        /// 启动 PVT 运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axisnum">轴数</param>
        /// <param name="AxisList">轴列表</param>
        /// <returns>错误代码</returns>
        public int CardAxisPvtMoveStart(int CardNo, int axisnum, UInt16[] AxisList)
        {
            return MCC.YK_PvtMove((ushort)CardNo, (ushort)axisnum, AxisList);
        }

        #endregion

        #region 运动伺服驱动专用接口函数 √


        /// <summary>
        /// 控制指定轴的伺服使能端口的输出
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="on_off">设置伺服使能端口电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int CardAxisWriteSevonPin(int CardNo, int axis, int on_off)
        {
            return MCC.YK_write_sevon_pin((ushort)CardNo, (ushort)axis, (ushort)on_off);
        }

        /// <summary>
        /// 读取指定轴的伺服使能端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <returns>伺服使能端口电平，0：低电平，1：高电平</returns>
        public int GetCardAxisSevonPin(int CardNo, int axis)
        {
            return MCC.YK_read_sevon_pin((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 读取指定轴的 RDY 端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <returns>RDY 端口电平，0：低电平，1：高电平</returns>
        public int GetCardAxisRdyPin(int CardNo, int axis)
        {
            return MCC.YK_read_rdy_pin((ushort)CardNo, (ushort)axis);
        }


        /// <summary>
        /// 设置指定轴的 INP 信号
        /// <para>注意：</para>
        /// <para>当使能 INP 信号功能后，只有在 INP 信号为有效状态时，对应的轴才能进行运动，否则此时检测轴的状态是正在运行（即对轴运动作限制）</para> 
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="enable">INP 信号使能，0：禁止，1：允许</param>
        /// <param name="inp_logic">INP 信号的有效电平，0：低有效，1：高有效</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisInpMode(int CardNo, int axis, int enable, int inp_logic)
        {
            return MCC.YK_set_inp_mode((ushort)CardNo, (ushort)axis, (ushort)enable, (ushort)inp_logic);
        }


        /// <summary>
        /// 读取指定轴的 INP 信号设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="enable">返回 INP 信号使能状态</param>
        /// <param name="inp_logic">返回设置的 INP 信号有效电平</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisInpMode(int CardNo, int axis, ref UInt16 enable, ref UInt16 inp_logic)
        {
            return MCC.YK_get_inp_mode((ushort)CardNo, (ushort)axis, ref enable, ref inp_logic);
        }


        /// <summary>
        /// 设置指定轴的 ALM 信号
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="enable">ALM 信号使能，0：禁止，1：允许</param>
        /// <param name="alm_logic">ALM 信号的有效电平，0：低有效，1：高有效</param>
        /// <param name="alm_action">ALM 信号的制动方式，0：立即停止（只支持该方式）</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisAlmMode(int CardNo, int axis, int enable, int alm_logic, int alm_action)
        {
            return MCC.YK_set_alm_mode((ushort)CardNo, (ushort)axis, (ushort)enable, (ushort)alm_logic, (ushort)alm_action);
        }


        /// <summary>
        /// 读取指定轴的 ALM 信号设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="enable">返回 ALM 信号使能状态</param>
        /// <param name="alm_logic">返回设置的 ALM 信号有效电平</param>
        /// <param name="alm_action">返回 ALM 信号的制动方式</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisAlmMode(int CardNo, int axis, UInt16 enable, UInt16 alm_logic, UInt16 alm_action)
        {
            return MCC.YK_get_alm_mode((ushort)CardNo, (ushort)axis, ref enable, ref alm_logic, ref alm_action);
        }



        /// <summary>
        /// 控制指定轴的 ERC 信号输出
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="sel">输出电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisWriteErcPin(int CardNo, int axis, int sel)
        {
            return MCC.YK_write_erc_pin((ushort)CardNo, (ushort)axis, (ushort)sel);
        }




        /// <summary>
        /// 读取指定轴的 ERC 端口电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <returns>ERC 端口电平，0：低电平，1：高电平</returns>
        public int GetCardAxisWriteErcPin(int CardNo, int axis)
        {
            return MCC.YK_read_erc_pin((ushort)CardNo, (ushort)axis);
        }



        #endregion

        #region 通用输入输出 IO 函数 √

        /// <summary>
        /// 获取指定控制卡的某个输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围：0~15</param>
        /// <returns>指定的输入端口电平：0：低电平，1：高电平</returns>
        public int GetCardInBit(int CardNo, int bitno)
        {
            return MCC.YK_read_inbit((ushort)CardNo, (ushort)bitno);
        }

        /// <summary>
        /// 设置指定控制卡的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围：0~15</param>
        /// <param name="on_off">输出电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int SetCardWriteOutBit(int CardNo, int bitno, int on_off)
        {
            return MCC.YK_write_outbit((ushort)CardNo, (ushort)bitno, (ushort)on_off);
        }

        /// <summary>
        /// 获取指定控制卡的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围：0~15</param>
        /// <returns>指定输出端口的电平，0：低电平，1：高电平</returns>
        public int GetCardOutBit(int CardNo, int bitno)
        {
            return MCC.YK_read_outbit((ushort)CardNo, (ushort)bitno);
        }


        /// <summary>
        /// 获取指定控制卡的全部输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="portno">IO 组号，取值范围：0、1</param>
        /// <returns>：bit0~bit31</returns>
        public uint GetCardInPort(int CardNo, int portno)
        {
            return MCC.YK_read_inport((ushort)CardNo, (ushort)portno);
        }

        /// <summary>
        /// 获取指定控制卡的全部输出口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="portno">保留参数，固定值为 0</param>
        /// <returns>bit0~bit31</returns>
        public uint GetCardOutPort(int CardNo, int portno)
        {
            return MCC.YK_read_outport((ushort)CardNo, (ushort)portno);
        }

        /// <summary>
        /// 设置指定控制卡的全部输出口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="portno">保留参数，固定值为 0</param>
        /// <param name="outport_val"> bit0 ~bit31</param>
        /// <returns>错误代码</returns>
        public int SetCardOutPort(int CardNo, int portno, uint outport_val)
        {
            return MCC.YK_write_outport((ushort)CardNo, (ushort)portno, outport_val);
        }


        /// <summary>
        /// IO 输出延时翻转
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输出端口号,取值范围：0~15</param>
        /// <param name="reverse_time">延时翻转时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int SetCardReverseOutBit(int CardNo, int bitno, int reverse_time)
        {
            return MCC.YK_reverse_outbit((ushort)CardNo, (ushort)bitno, reverse_time);
        }

        /// <summary>
        /// 设置 IO 计数模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输出端口号,取值范围：0~15</param>
        /// <param name="mode">IO 计数模式，0：禁用，1：上升沿计数，2：下降沿计数</param>
        /// <param name="filter_time">滤波时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int SetCardIoCountMode(int CardNo, int bitno, int mode, double filter_time)
        {
            return MCC.YK_set_io_count_mode((ushort)CardNo, (ushort)bitno, (ushort)mode, filter_time);
        }

        /// <summary>
        /// 获取IO 计数模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输出端口号,取值范围：0~15</param>
        /// <param name="mode">返回 IO 计数模式</param>
        /// <param name="filter_time">返回滤波时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int GetCardIoCountMode(int CardNo, int bitno, ref UInt16 mode, ref double filter_time)
        {
            return MCC.YK_get_io_count_mode((ushort)CardNo, (ushort)bitno, ref mode, ref filter_time);
        }
        /// <summary>
        /// 设置 IO 计数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输出端口号,取值范围：0~15</param>
        /// <param name="CountValue">IO 计数值</param>
        /// <returns>错误代码</returns>
        public int SetCardIoCountValue(int CardNo, int bitno, int CountValue)
        {
            return MCC.YK_set_io_count_value((ushort)CardNo, (ushort)bitno, (uint)CountValue);
        }

        /// <summary>
        /// 设置 IO 计数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输出端口号,取值范围：0~15</param>
        /// <param name="CountValue">返回 IO 计数值</param>
        /// <returns>错误代码</returns>
        public int SetCardIoCountValue(int CardNo, int bitno, uint CountValue)
        {
            return MCC.YK_set_io_count_value((ushort)CardNo, (ushort)bitno, CountValue);
        }

        /// <summary>
        /// 读取 IO 计数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输出端口号,取值范围：0~15</param>
        /// <param name="CountValue">返回 IO 计数值</param>
        /// <returns>错误代码</returns>
        public int GetCardIoCountValue(int CardNo, int bitno, ref uint CountValue)
        {
            return MCC.YK_get_io_count_value((ushort)CardNo, (ushort)bitno, ref CountValue);
        }


        #endregion

        #region 手轮功能函数 √


        /// <summary>
        /// 设置单轴手轮运动控制输入方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5DMC3400A：0~3</param>
        /// <param name="inmode">手轮输入方式，0：A、B 相位正交信号；1：脉冲+方向信号</param>
        /// <param name="multi">手轮倍率，正数表示默认方向，负数表示与默认方向反向</param>
        /// <param name="vh">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisHandWhellInMode(int CardNo, int axis, int inmode, int multi, double vh)
        {
            return MCC.YK_set_handwheel_inmode((ushort)CardNo, (ushort)axis, (ushort)inmode, multi, vh);
        }



        /// <summary>
        /// 获取单轴手轮运动控制输入方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5DMC3400A：0~3</param>
        /// <param name="inmode">返回手轮输入方式</param>
        /// <param name="multi">返回手轮倍率</param>
        /// <param name="vh">保留参数</param>
        /// <returns></returns>
        public int GetCardAxisHandWhellInMode(int CardNo, int axis, UInt16 inmode, int multi, double vh)
        {
            return MCC.YK_get_handwheel_inmode((ushort)CardNo, (ushort)axis, ref inmode, ref multi, ref vh);
        }



        /// <summary>
        /// 启动手轮运动
        /// <para>注 意：</para>
        /// <para>当启动手轮运动后，只有发送 YK_stop 或 YK_emg_stop 命令后才会退出手轮模式</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5DMC3400A：0~3</param>
        /// <returns>错误代码</returns>
        public int CardAxisHandWhellMove(int CardNo, int axis)
        {
            return MCC.YK_handwheel_move((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 读取手轮通道选择设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="index">0：高速通道  1：低速通道</param>
        /// <returns>错误代码</returns>
        public int SetCardHandWhellChannel(int CardNo, int index)
        {
            return MCC.YK_set_handwheel_channel((ushort)CardNo, (ushort)index);
        }

        /// <summary>
        /// 读取手轮通道选择设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="index">返回设置的手轮通道</param>
        /// <returns>错误代码</returns>
        public int GetCardHandWhellChannel(int CardNo, UInt16 index)
        {
            return MCC.YK_get_handwheel_channel((ushort)CardNo, ref index);
        }



        /// <summary>
        /// 设置多轴手轮运动控制输入方式
        /// <para>注 意：</para>
        /// <para>通过该函数设置可以使一个手轮通道控制多个轴同时运动</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="inmode">手轮输入方式：0：A、B 相位正交信号；1：脉冲+方向信号</param>
        /// <param name="AxisNum">参与手轮运动的轴数 </param>
        /// <param name="AxisList">参与手轮运动的轴号数组</param>
        /// <param name="multi">手轮倍率数组:正数表示默认方向，负数表示与默认方向反向</param>
        /// <returns>错误代码</returns>
        public int SetCardHandWhellInModeExtern(int CardNo, UInt16 inmode, UInt16 AxisNum, UInt16[] AxisList, int[] multi)
        {
            return MCC.YK_set_handwheel_inmode_extern((ushort)CardNo, inmode, AxisNum, AxisList, multi);
        }

        /// <summary>
        /// 读取多轴手轮运动控制输入方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="inmode">返回手轮输入方式</param>
        /// <param name="AxisNum">返回参与手轮运动的轴数</param>
        /// <param name="AxisList">返回参与手轮运动的轴号数组</param>
        /// <param name="multi">返回手轮倍率数组</param>
        /// <returns>错误代码</returns>
        public int GetCardHandWhellInModeExtern(int CardNo, ref UInt16 inmode, ref UInt16 AxisNum, ref UInt16[] AxisList, ref int[] multi)
        {
            return MCC.YK_get_handwheel_inmode_extern((ushort)CardNo, ref inmode, ref AxisNum, AxisList, multi);
        }

        #endregion

        #region 编码器函数 √

        /// <summary>
        /// 设置编码器的计数方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~7，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="mode">编码器的计数方式：  |  0：非 A/B 相(脉冲/方向)  |   1：1×A/B  |   2：2×A/B  |   3：4×A/B</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisCounterInMode(int CardNo, int axis, int mode)
        {
            return MCC.YK_set_counter_inmode((ushort)CardNo, (ushort)axis, (ushort)mode);
        }



        /// <summary>
        /// 获取编码器的计数方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~7，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="mode">返回编码器的计数方式</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisCounterInMode(int CardNo, int axis, ref UInt16 mode)
        {
            return MCC.YK_get_counter_inmode((ushort)CardNo, (ushort)axis, ref mode);
        }


        /// <summary>
        /// 设置指定轴编码器脉冲计数值
        /// <para>说 明：</para>
        /// <para>此函数 axis 参数为 8 时可以设置手轮编码器计数值，DMC3400A 无高速手轮通道，不支持设置手轮编码器计数值；</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~7，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="encoder_value">编码器计数值，单位：pulse</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisEncoder(int CardNo, int axis, int encoder_value)
        {
            return MCC.YK_set_encoder((ushort)CardNo, (ushort)axis, encoder_value);
        }
        /// <summary>
        /// 获取指定轴编码器脉冲计数值
        /// <para>说 明：</para>
        /// <para>此函数 axis 参数为 8 时可以读手轮编码器计数值，DMC3400A 无高速手轮通道，不支持读取手轮编码器计数值；如果开启螺距补偿功能，则该函数读取到的数值为补偿后轴的编码器位置；</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~7，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <returns>编码器计数值，单位：pulse</returns>
        public int GetCardAxisEncoder(int CardNo, int axis)
        {
            return MCC.YK_get_encoder((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 设置指定轴的 EZ 信号
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~7，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="ez_logic">返回设置的 EZ 信号有效电平，0：低有效，1：高有效</param>
        /// <param name="ez_mode">保留参数，固定值为 0</param>
        /// <param name="filter">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisEZMode(int CardNo, int axis, int ez_logic, int ez_mode, double filter)
        {
            return MCC.YK_set_ez_mode((ushort)CardNo, (ushort)axis, (ushort)ez_logic, (ushort)ez_mode, filter);
        }


        /// <summary>
        /// 获取指定轴的 EZ 信号设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~7，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="ez_logic">返回设置的 EZ 信号有效电平</param>
        /// <param name="ez_mode">保留参数</param>
        /// <param name="filter">保留参数</param>
        /// <returns></returns>
        public int GetCardAxisEZMode(int CardNo, int axis, UInt16 ez_logic, UInt16 ez_mode, double filter)
        {
            return MCC.YK_get_ez_mode((ushort)CardNo, (ushort)axis, ref ez_logic, ref ez_mode, ref filter);
        }
        #endregion

        #region 高速位置锁存函数 √

        /// <summary>
        /// 设置指定轴的 LTC 信号
        /// <para>注 意：</para><para>DMC3C00 后四轴不支持高速位置锁存功能</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~7，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="ltc_logic"> LTC 信号的触发方式，0：下降沿锁存，1：上升沿锁存，2：双边沿锁存</param>
        /// <param name="ltc_mode">保留参数，固定值为 0</param>
        /// <param name="filter">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisLtcMode(int CardNo, int axis, UInt16 ltc_logic, UInt16 ltc_mode, double filter)
        {
            return MCC.YK_set_ltc_mode((ushort)CardNo, (ushort)axis, ltc_logic, ltc_mode, filter);
        }

        public int SetCardAxisLatchMode(int CardNo, int axis, int all_enable, int latch_source, int triger_chunnel)
        {
            return MCC.YK_set_latch_mode((ushort)CardNo, (ushort)axis, (ushort)all_enable, (ushort)latch_source, (ushort)triger_chunnel);
        }
        /// <summary>
        /// 获取指定轴的 LTC 信号设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~7，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="ltc_logic">返回设置的 LTC 信号的触发方式</param>
        /// <param name="ltc_mode">保留参数</param>
        /// <param name="filter">保留参数</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisLtcMode(int CardNo, int axis, ref UInt16 ltc_logic, ref UInt16 ltc_mode, ref double filter)
        {
            return MCC.YK_get_ltc_mode((ushort)CardNo, (ushort)axis, ref ltc_logic, ref ltc_mode, ref filter);
        }

        /// <summary>
        /// 设置锁存方式
        /// <para> 注 意：</para>
        /// <para>1）DMC3400A 只有一个高速锁存通道 LTC0，DMC3C00/3800/3600,有两个高速锁存通道LTC0，LTC1；LTC0 锁存 0~3 号轴，LTC1 锁存 4~7 号轴</para>
        /// <para>2）DMC3400A 中触发延时急停模式只对 0 号轴起作用，DMC3C00/3800/3600 中，触发延时急停模式只对 0 号轴及 4 号轴起作用；LTC0 对应为 0 号轴，LTC1 对应为 4号轴</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~7，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="all_enable">锁存方式：  |   0：单次锁存   |   1：保留  |   2：连续锁存  |   3：触发延时急停  |   </param>
        /// <param name="latch_source">锁存源，0：指令位置计数器，1：编码器计数器</param>
        /// <param name="triger_chunnel">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisLatchMode(int CardNo, int axis, UInt16 all_enable, UInt16 latch_source, UInt16 triger_chunnel)
        {
            return MCC.YK_set_latch_mode((ushort)CardNo, (ushort)axis, all_enable, latch_source, triger_chunnel);
        }


        /// <summary>
        /// 读取锁存方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~7，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="all_enable">返回锁存方式设置</param>
        /// <param name="latch_source">返回锁存源设置</param>
        /// <param name="triger_chunnel">保留参数</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisLatchMode(int CardNo, int axis, ref UInt16 all_enable, ref UInt16 latch_source, ref UInt16 triger_chunnel)
        {
            return MCC.YK_get_latch_mode((ushort)CardNo, (ushort)axis, ref all_enable, ref latch_source, ref triger_chunnel);
        }

        /// <summary>
        /// 设置 LTC 端口触发延时急停时间
        /// <para>注 意：</para>
        /// <para>：触发延时急停模式只对 0 号轴及 4 号轴起作用；LTC0 对应为 0 号轴，LTC1 对应为 4号轴</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3400A：0，DMC3C00/3800/3600：0、4</param>
        /// <param name="time">触发延时停止时间，单位：us，取值范围：1us~50ms</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisLatchStopTime(int CardNo, int axis, int time)
        {
            return MCC.YK_set_latch_stop_time((ushort)CardNo, (ushort)axis, time);
        }

        /// <summary>
        /// 读取 LTC 端口触发延时急停时间
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3400A：0，DMC3C00/3800/3600：0、4</param>
        /// <param name="time">返回触发延时停止时间设置，单位：us</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisLatchStopTime(int CardNo, int axis, int time)
        {
            return MCC.YK_get_latch_stop_time((ushort)CardNo, (ushort)axis, ref time);
        }

        /// <summary>
        /// LTC 反相输出设置
        /// <para>注 意：</para><para>当某输出端口作为 LTC 反相输出后，该端口将不能通过通用 IO 函数设置输出值；</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <param name="enable">使能状态：0：禁止，1：使能</param>
        /// <param name="bitno">通用输出 IO 口号，取值范围：0~15</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisOutMode(int CardNo, int axis, int enable, int bitno)
        {
            return MCC.YK_SetLtcOutMode((ushort)CardNo, (ushort)axis, (ushort)enable, (ushort)bitno);
        }

        /// <summary>
        ///  读取 LTC 反相输出设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <param name="enable">返回使能状态：0：禁止，1：使能</param>
        /// <param name="bitno">返回设置的通用输出 IO 口号</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisOutMode(int CardNo, int axis, ref ushort enable, ref ushort bitno)
        {
            return MCC.YK_GetLtcOutMode((ushort)CardNo, (ushort)axis, ref enable, ref bitno);
        }


        /// <summary>
        /// 从控制卡内读取锁存器的值
        /// <para>注 意：</para><para>：当选择锁存方式为单次锁存时，用此函数读取锁存值</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <returns>锁存值</returns>
        public int GetCardAxisLatchValue(int CardNo, int axis)
        {
            return MCC.YK_get_latch_value((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 从控制卡内读取指定卡内锁存器的标志位
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <returns>0：未触发锁存，1：已触发锁存</returns>
        public int GetCardAxisLatchFlag(int CardNo, int axis)
        {
            return MCC.YK_get_latch_flag((ushort)CardNo, (ushort)axis);
        }

        #endregion

        #region 从控制卡内读取指定卡内锁存器的标志位 √

        /// <summary>
        /// 复位指定卡的锁存器的标志位
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <returns></returns>
        public int GetCardAxisReserLatchFlag(int CardNo, int axis)
        {
            return MCC.YK_reset_latch_flag((ushort)CardNo, (ushort)axis);
        }


        /// <summary>
        /// 从 PC 缓存中读取锁存器已锁存个数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <returns></returns>
        public int GetCardAxisLatchFlagExtern(int CardNo, int axis)
        {
            return MCC.YK_get_latch_flag_extern((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 按索引号读取 PC 缓冲区中已保存的锁存值
        /// <para>注 意：</para><para>当选择锁存方式为连续锁存时，用此函数读取锁存值。索引号按锁存顺序从 0 开始排列（即第一次锁存的位置值存在索引号为 0 处，第二次锁存的位置值存在索引号为 1处，以此类推）</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <param name="Index">索引号</param>
        /// <returns>锁存值</returns>
        public int GetCardAxisLatchValueExtern(int CardNo, int axis, int Index)
        {
            return MCC.YK_get_latch_value_extern((ushort)CardNo, (ushort)axis, (ushort)Index);
        }


        #endregion

        #region 位置比较输出 按索引号读取 PC 缓冲区中已保存的锁存值位置比较函数 √

        /// <summary>
        /// 二维高速比较：该函数用于强制二维比较输出，输出按照配置好的脉冲模式或者 pwm 模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">保留，参数值为 0</param>
        /// <param name="enable">强制二维比较输出，设置为 1 使能</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisHcmp2dForceOutput(int CardNo, ushort hcmp, ushort enable)
        {
            //return LTDMC.dmc_hcmp_2d_force_output((ushort)CardNo, hcmp, enable);
            return 0;
        }

        /// <summary>
        /// 二维高速比较：读取高速比较参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">保留，参数值为 0</param>
        /// <param name="remained_points">返回可添加比较点数</param>
        /// <param name="x_current_point">返回当前 x 比较点位置</param>
        /// <param name="y_current_point">返回当前 y 比较点位置</param>
        /// <param name="runned_points">返回已比较点数</param>
        /// <param name="current_state">比较器状态 1 正在输出 0 输出完成</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisHcmp2dCurrentState(int CardNo, ushort hcmp, ref int remained_points,
ref int x_current_point, ref int y_current_point, ref int runned_points, ref ushort current_state)
        {
            //return LTDMC.dmc_hcmp_2d_get_current_state((ushort)CardNo, hcmp, ref remained_points,
            //    ref x_current_point, ref y_current_point, ref runned_points,
            //    ref current_state);
            return 0;
        }


        /// <summary>
        /// 二维高速比较：添加/更新高速比较位置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">保留，参数值为 0</param>
        /// <param name="x_cmp_pos">队列模式下：添加 x 比较位置，单位：pulse</param>
        /// <param name="y_cmp_pos">队列模式下：添加 x 比较位置，单位：pulse</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisHcmp2dAddPoint(int CardNo, ushort hcmp, int x_cmp_pos, int y_cmp_pos)
        {
            //return LTDMC.dmc_hcmp_2d_add_point((ushort)CardNo, hcmp, x_cmp_pos, y_cmp_pos);
            return 0;
        }

        /// <summary>
        /// 二维高速比较：清除所有缓冲区高速位置缓冲比较值，并退出当前比较状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">保留，参数值为 0</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisHcmp2dClearPoints(int CardNo, ushort hcmp)
        {
            //return LTDMC.dmc_hcmp_2d_clear_points((ushort)CardNo, hcmp);
            return 0;
        }

        /// <summary>
        /// 二维高速比较：读取高速比较器
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">保留，参数值为 0</param>
        /// <param name="cmp_mode">返回 x 轴关联轴号</param>
        /// <param name="x_axis">返回 x 轴关联轴号</param>
        /// <param name="cmp_source">返回 x 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="y_axis">返回 y 轴关联轴号</param>
        /// <param name="y_cmp_source">返回 y 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="error">返回 x/y 轴误差带设置，单位：pulse</param>
        /// <param name="cmp_logic">返回有效电平：0：低电平，1：高电平</param>
        /// <param name="time">返回脉冲宽度，单位：us，取值范围：1us~20s</param>
        /// <param name="pwm_enable">pwm 模式使能</param>
        /// <param name="duty">占空比</param>
        /// <param name="freq">频率</param>
        /// <param name="port_sel">输出口选择</param>
        /// <param name="pwm_number">输出的 pwm 脉冲数</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisHcmp2dsetConfig(int CardNo, ushort hcmp, ref ushort cmp_mode,
               ref ushort x_axis, ref ushort cmp_source, ref ushort y_axis, ref ushort y_cmp_source,
               ref int error, ref ushort cmp_logic, ref int time, ref ushort pwm_enable, ref double duty,
               ref int freq, ref ushort port_sel, ref ushort pwm_number)
        {

            //return LTDMC.dmc_hcmp_2d_get_config((ushort)CardNo, hcmp, ref cmp_mode,
            //    ref x_axis, ref cmp_source, ref y_axis, ref y_cmp_source, ref error,
            //    ref cmp_logic, ref time, ref pwm_enable, ref duty,
            //    ref freq, ref port_sel, ref pwm_number);
            return 0;
        }


        /// <summary>
        /// 二维高速比较：配置高速比较器
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">保留，参数值为 0</param>
        /// <param name="cmp_mode">比较模式： 0：进入误差带后触发 1：进入误差带单轴等于后再触发</param>
        /// <param name="x_axis">x 轴关联轴号 DMC3400A：0~3，DMC3600：0~6，DMC3800/DMC3C00：0~8</param>
        /// <param name="cmp_source">x 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="y_axis">y 轴关联轴号 轴号范围：DMC3400A：0~3，DMC3600：0~6，DMC3800/DMC3C00：0~8</param>
        /// <param name="y_cmp_source">y 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="error">x/y 轴误差带设置，单位：pulse</param>
        /// <param name="cmp_logic">有效电平：0：低电平，1：高电平</param>
        /// <param name="time">脉冲宽度，单位：us，取值范围：1us~20s</param>
        /// <param name="pwm_enable">pwm 模式使能</param>
        /// <param name="duty">占空比</param>
        /// <param name="freq">频率</param>
        /// <param name="port_sel">输出口选择</param>
        /// <param name="pwm_number">输出的 pwm 脉冲数</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisHcmp2dsetConfig(int CardNo, ushort hcmp, ushort cmp_mode,
            ushort x_axis, ushort cmp_source, ushort y_axis, ushort y_cmp_source,
            int error, ushort cmp_logic, int time, ushort pwm_enable, double duty,
            int freq, ushort port_sel, ushort pwm_number)
        {

            //return LTDMC.dmc_hcmp_2d_set_config((ushort)CardNo, hcmp, cmp_mode,
            //    x_axis, cmp_source, y_axis, y_cmp_source, error, cmp_logic, time, pwm_enable, duty,
            //    freq, port_sel, pwm_number);
            return 0;
        }

        /// <summary>
        /// 二维高速比较：读取高速比较使能
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="hcmp"></param>
        /// <param name="cmpEnable"></param>
        /// <returns>错误代码</returns>
        public int GetCardAxisHcmp2dsetEnable(int CardNo, int hcmp, ref ushort cmpEnable)
        {
            //return LTDMC.dmc_hcmp_2d_get_enable((ushort)CardNo, (ushort)hcmp, ref cmpEnable);
            return 0;
        }


        /// <summary>
        /// 二维高速比较：设置高速比较使能
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="hcmp"></param>
        /// <param name="cmpEnable"></param>
        /// <returns></returns>
        public int SetCardAxisHcmp2dsetEnable(int CardNo, int hcmp, int cmpEnable)
        {
            //YK_hcmp_2d_set_enable
             //return MCC.YK_hcmp_2d_set_enable((ushort)CardNo, (ushort)hcmp, (ushort)cmpEnable);
            return 0;
        }

        /// <summary>
        /// 设置一维位置比较器 (单轴低速)
        /// <para>注 意：</para><para>DMC3C00 后四轴不支持位置比较功能</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">   比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisCompareConfig(int CardNo, int axis, int enable, int cmp_source)
        {
            return MCC.YK_compare_set_config((ushort)CardNo, (ushort)axis, (ushort)enable, (ushort)cmp_source);
        }


        /// <summary>
        ///  一维低速位置比较: 读取一维位置比较器设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <param name="enable">返回比较功能状态</param>
        /// <param name="cmp_source">返回比较源</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisCompareConfig(int CardNo, int axis, ref UInt16 enable, ref UInt16 cmp_source)
        {
            return MCC.YK_compare_get_config((ushort)CardNo, (ushort)axis, ref enable, ref cmp_source);
        }





        /// <summary>
        /// 清除已添加的所有一维位置比较点
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <returns>错误代码</returns>
        public int ClearCardAxisComparePoints(int CardNo, int axis)
        {
            return MCC.YK_compare_clear_points((ushort)CardNo, (ushort)axis);
        }



        /// <summary>
        /// 添加一维位置比较点
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <param name="pos">比较位置，单位：pulse</param>
        /// <param name="dir">比较模式，0：小于等于，1：大于等于</param>
        /// <param name="action">比较点触发功能编号</param>
        /// <param name="actpara">比较点触发功能参数</param>
        /// <returns>错误代码</returns>
        public int AddCardAxisComparePoint(int CardNo, int axis, int pos, int dir, UInt16 action, UInt32 actpara)
        {
            return MCC.YK_compare_add_point((ushort)CardNo, (ushort)axis, pos, (ushort)dir, action, actpara);
        }

        /// <summary>
        /// 读取当前一维比较点位置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <param name="pos">返回当前比较点位置，单位：pulse</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisCompareCurrentPoint(int CardNo, int axis, ref int pos)
        {
            return MCC.YK_compare_get_current_point((ushort)CardNo, (ushort)axis, ref pos);
        }

        /// <summary>
        /// 查询已经比较过的一维比较点个数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <param name="pointNum">返回已经比较过的点数</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisComparePointsRunned(int CardNo, int axis, ref int pointNum)
        {
            return MCC.YK_compare_get_points_runned((ushort)CardNo, (ushort)axis, ref pointNum);
        }


        /// <summary>
        /// 查询可以加入的一维比较点个数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <param name="pointNum">返回可以加入的比较点数</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisComparePointsRemained(int CardNo, int axis, ref int pointNum)
        {
            return MCC.YK_compare_get_points_remained((ushort)CardNo, (ushort)axis, ref pointNum);
        }


        /// <summary>
        /// 设置二维位置比较器
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="enable">二维位置比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">二维位置比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns>错误代码</returns>
        public int SetCardCompareConfigExtern(int CardNo, int enable, int cmp_source)
        {
            return MCC.YK_compare_set_config_extern((ushort)CardNo, (ushort)enable, (ushort)cmp_source);
        }

        /// <summary>
        /// 读取二维位置比较器设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="enable">返回比较功能状态</param>
        /// <param name="cmp_source">返回比较源</param>
        /// <returns>：错误代码</returns>
        public int GetCardCompareConfigExtern(int CardNo, UInt16 enable, UInt16 cmp_source)
        {
            return MCC.YK_compare_get_config_extern((ushort)CardNo, ref enable, ref cmp_source);
        }



        /// <summary>
        /// 清除已添加的所有二维位置比较点
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns>错误代码</returns>
        public int ClearCardComparePointsExtern(int CardNo)
        {
            return MCC.YK_compare_clear_points_extern((ushort)CardNo);
        }



        /// <summary>
        /// 添加二维位置比较点
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定卡上的即将进行位置比较的轴列表(两个轴)</param>
        /// <param name="pos">二维位置比较位置列表，单位：pulse</param>
        /// <param name="dir">比较模式列表，0：小于等于，1：大于等于</param>
        /// <param name="action">二维位置比较点触发功能编号</param>
        /// <param name="actpara">二维位置比较点触发功能参数</param>
        /// <returns>错误代码</returns>
        public int AddCardComparePointExtern(int CardNo, UInt16[] axis, Int32[] pos, UInt16[] dir, UInt16 action, UInt32 actpara)
        {
            return MCC.YK_compare_add_point_extern((ushort)CardNo, axis, pos, dir, action, actpara);
        }


        /// <summary>
        /// 读取当前二维位置比较点位置
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="pos">返回当前二维位置比较点位置</param>
        /// <returns>错误代码</returns>
        public int GetCardCompareCurrentPointExtern(int CardNo, ref int[] pos)
        {
            //int[] pos1 = new int[2] { 0, 0 };
            var res= MCC.YK_compare_get_current_point_extern((ushort)CardNo,pos);
            return 0;
        }



        /// <summary>
        /// 读取当前二维位置比较点位置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="pointNum">返回已经比较过的二维位置比较点数</param>
        /// <returns>错误代码</returns>
        public int GetCardComparePointsRunnedExtern(int CardNo, ref int pointNum)
        {
            return MCC.YK_compare_get_points_runned_extern((ushort)CardNo, ref pointNum);
        }


        /// <summary>
        /// 查询可以加入的二维比较点个数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="pointNum">返回可以加入的二维位置比较点数</param>
        /// <returns>错误代码</returns>
        public int GetCardComparePointsRemainedExtern(int CardNo, ref int pointNum)
        {
            return MCC.YK_compare_get_points_remained_extern((ushort)CardNo, ref pointNum);
        }



        #endregion

        #region 高速位置比较函数 √

        /// <summary>
        /// 设置高速比较模式
        ///<para>注  意：</para>
        ///<para>1）当选择模式 1 时，只有当前位置等于比较位置时，CMP 端口才输出有效电平</para>
        ///<para>2）当选择模式 2 时，只要当前位置小于比较位置时，CMP 端口就一直保持有效电平</para>
        ///<para>3）当选择模式 3 时，只要当前位置大于比较位置时，CMP 端口就一直保持有效电平</para>
        ///<para>4）当选择模式 4 或 5 时，CMP 端口输出有效电平的时间通过 YK_hcmp_set_config函数的 time 参数（脉冲宽度）设置</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <param name="cmp_enable">比较模式：0：禁止（默认值）| 1：等于 | 2：小于 | 3：大于 | 4：队列 | ;提供 127 个点比较空间，采用先添加先比较，比较完可追加比较点，也可一次性添加多个比较点 | 5：线性，提供起始比较点，位置增量，比较次数  </param>
        /// <returns>错误代码</returns>
        public int SetCardAxisHcmpMode(int CardNo, int hcmp, int cmp_enable)
        {
            return MCC.YK_hcmp_set_mode((ushort)CardNo, (ushort)hcmp, (ushort)cmp_enable);
        }
        /// <summary>
        /// 清除已添加的所有高速位置比较点
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <returns>错误代码</returns>
        public int ClearCardHcmpPoints(int CardNo, int hcmp)
        {
            return MCC.YK_hcmp_clear_points((ushort)CardNo, (ushort)hcmp);
        }
        /// <summary>
        ///读取高速比较模式设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <param name="cmp_enable">返回比较模式设置</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisHcmpMode(int CardNo, int hcmp, ref UInt16 cmp_enable)
        {
            return MCC.YK_hcmp_get_mode((ushort)CardNo, (ushort)hcmp, ref cmp_enable);
        }

        /// <summary>
        /// 配置高速比较器
        /// <para>注 意：</para> <para>该函数的 time 参数（脉冲宽度）只对队列和线性比较模式起作用</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <param name="axis">关联轴号，取值范围：DMC3C00：0~7，DMC3800：0~7，DMC3600：0~5DMC3400A：0~3</param>
        /// <param name="cmp_source">比较位置源：0：指令位置计数器，1：编码器计数器</param>
        /// <param name="cmp_logic">有效电平：0：低电平，1：高电平</param>
        /// <param name="time">脉冲宽度，单位：us，取值范围：1us~20s</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisHcmpConfig(int CardNo, int hcmp, int axis, int cmp_source, int cmp_logic, int time)
        {
            return MCC.YK_hcmp_set_config((ushort)CardNo, (ushort)hcmp, (ushort)axis, (ushort)cmp_source, (ushort)cmp_logic, time);
        }

        /// <summary>
        /// 读取高速比较器配置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <param name="axis">返回关联轴号设置</param>
        /// <param name="cmp_source">返回比较位置源设置</param>
        /// <param name="cmp_logic">返回有效电平设置</param>
        /// <param name="time">返回脉冲宽度设置</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisHcmpConfig(int CardNo, int hcmp, ref UInt16 axis, UInt16 cmp_source, UInt16 cmp_logic, int time)
        {
            return MCC.YK_hcmp_get_config((ushort)CardNo, (ushort)hcmp, ref axis, ref cmp_source, ref cmp_logic, ref time);
        }

        /// <summary>
        /// 添加/更新高速比较位置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <param name="cmp_pos"><para>队列模式下：添加比较位置，单位：pulse</para><para>  线性模式下：更新起始比较位置，单位：pulse</para><para>其他模式下：更新比较位置，单位：pulse</para></param>
        /// <returns>错误代码</returns>
        public int AddCardHcmpPoint(int CardNo, int hcmp, int cmp_pos)
        {
            return MCC.YK_hcmp_add_point((ushort)CardNo, (ushort)hcmp, cmp_pos);
        }

        /// <summary>
        /// 设置高速比较线性模式参数    
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <param name="Increment">位置增量值，单位：pulse（正值表示位置递增，负值表示位置递减）</param>
        /// <param name="Count">比较次数，取值范围：1~32767</param>
        /// <returns>错误代码</returns>
        public int SetCardHcmpLiner(int CardNo, int hcmp, int Increment, int Count)
        {
            return MCC.YK_hcmp_set_liner((ushort)CardNo, (ushort)hcmp, Increment, Count);
        }

        /// <summary>
        /// 读取高速比较线性模式参数设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <param name="Increment">返回位置增量值设置</param>
        /// <param name="Count">返回比较次数设置</param>
        /// <returns>错误代码</returns>
        public int GetCardHcmpLiner(int CardNo, int hcmp, ref int Increment, ref int Count)
        {
            return MCC.YK_hcmp_get_liner((ushort)CardNo, (ushort)hcmp, ref Increment, ref Count);
        }


        /// <summary>
        /// 读取高速比较参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <param name="remained_points">返回可添加比较点数</param>
        /// <param name="current_point">返回当前比较点位置，单位：pulse</param>
        /// <param name="runned_points">返回已比较点数</param>
        /// <returns>错误代码</returns>
        public int GetCardHcmpCurrentState(int CardNo, int hcmp, ref int remained_points, ref int current_point, ref int runned_points)
        {
            return MCC.YK_hcmp_get_current_state((ushort)CardNo, (ushort)hcmp, ref remained_points, ref current_point, ref runned_points);
        }

        /// <summary>
        /// 控制指定 CMP 端口的输出
        /// <para>注 意：</para> <para>该函数只在 CMP 禁止功能的状态下起作用</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <param name="on_off">设置 CMP 端口电平，0：低电平，1：高电平</param>
        /// <returns>：错误代码</returns>
        public int SetCardHcmpCmpPin(int CardNo, int hcmp, int on_off)
        {
            return MCC.YK_write_cmp_pin((ushort)CardNo, (ushort)hcmp, (ushort)on_off);
        }

        /// <summary>
        /// 读取指定 CMP 端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <returns>CMP 端口电平</returns>
        public int GetCardHcmpCmpPin(int CardNo, int hcmp)
        {
            return MCC.YK_read_cmp_pin((ushort)CardNo, (ushort)hcmp);
        }
        #endregion

        #region 异常信号接口函数 √

        /// <summary>
        /// 设置 EMG 急停信号
        /// <para>注 意：</para>
        /// <para>DMC3C00 后四轴不支持异常停止功能</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">DMC3C00/3800 卡：指定轴号，取值范围：0~7; DMC3600 卡：指定轴号，取值范围：0~5 ;DMC3400A:指定轴号，取值范围：0~3</param>
        /// <param name="enable">允许/禁止信号功能，0：禁止，1：允许</param>
        /// <param name="emg_logic">EMG 信号有效电平，0：低有效，1：高有效  </param>
        /// <returns>错误代码</returns>
        public int SetCardAxisEmgMode(int CardNo, int axis, int enable, int emg_logic)
        {
            return MCC.YK_set_emg_mode((ushort)CardNo, (ushort)axis, (ushort)enable, (ushort)emg_logic);
        }

        /// <summary>
        /// ：读取 EMG 急停信号设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">DMC3C00/3800 卡：指定轴号，取值范围：0~7; DMC3600 卡：指定轴号，取值范围：0~5 ;DMC3400A:指定轴号，取值范围：0~3</param>
        /// <param name="enable">返回 EMG 信号功能状态</param>
        /// <param name="emg_logic">错误代码</param>
        /// <returns></returns>
        public int GetCardAxisEmgMode(int CardNo, int axis, ref UInt16 enable, UInt16 emg_logic)
        {
            return MCC.YK_get_emg_mode((ushort)CardNo, (ushort)axis, ref enable, ref emg_logic);
        }

        /// <summary>
        /// 设置减速停止信号
        /// <para>注 意：</para>
        ///  <para>减速停止信号（DSTP）的减速时间由函数 YK_set_dec_stop_time 设置</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">DMC3C00/3800 卡：指定轴号，取值范围：0~7; DMC3600 卡：指定轴号，取值范围：0~5 ;DMC3400A:指定轴号，取值范围：0~3</param>
        /// <param name="enable">允许/禁止硬件信号功能，0：禁止，1：允许</param>
        /// <param name="logic">外部减速停止信号有效电平，0：低有效，1：高有效</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisIODstpMode(int CardNo, int axis, int enable, int logic)
        {
            return MCC.YK_set_io_dstp_mode((ushort)CardNo, (ushort)axis, (ushort)enable, (ushort)logic);
        }

        /// <summary>
        /// 读取减速停止信号设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A： 0~3</param>
        /// <param name="enable">返回 DSTP 硬件信号功能状态</param>
        /// <param name="logic">返回设置的外部减速停止信号有效电平</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisIODstpMode(int CardNo, int axis, ref UInt16 enable, ref UInt16 logic)
        {
            return MCC.YK_get_io_dstp_mode((ushort)CardNo, (ushort)axis, ref enable, ref logic);
        }

        /// <summary>
        /// 设置减速停止时间
        /// <para>注 意：</para>
        /// <para>当发生异常停止时，如：调用 YK_stop 函数、限位信号（软硬件）被触发、减速停止信号(DSTP)被触发等进行减速停止时，减速停止时间都为 YK_set_dec_stop_time函数里设置的减速时间</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A： 0~3</param>
        /// <param name="stop_time">减速时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisDecStopTime(int CardNo, int axis, double stop_time)
        {
            return MCC.YK_set_dec_stop_time((ushort)CardNo, (ushort)axis, stop_time);
        }

        /// <summary>
        /// 读取减速停止时间设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A： 0~3</param>
        /// <param name="stop_time">返回设置的减速时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisDecStopTime(int CardNo, int axis, ref double stop_time)
        {
            return MCC.YK_get_dec_stop_time((ushort)CardNo, (ushort)axis, ref stop_time);
        }

        #endregion

        #region  轴 IO 映射函数 √
        /// <summary>
        /// 设置轴 IO 映射关系
        /// <para>注 意：</para>
        /// <para>1）该函数可以实现对专用 IO 信号的硬件输入接口进行任意配置 </para>
        /// <para>2) DMC3C00 后四轴不支持 IO 映射功能</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A： 0~3</param>
        /// <param name="IoType">指定轴的 IO 信号类型：
        /// <para>0：正限位信号，AxisIoInMsg_PEL</para>
        /// <para>1：负限位信号，AxisIoInMsg_NEL</para>
        /// <para>2：原点信号，AxisIoInMsg_ORG</para>
        /// <para>3：急停信号，AxisIoInMsg_EMG</para>
        /// <para>4：减速停止信号，AxisIoInMsg_DSTP</para>
        /// <para>5：伺服报警信号，AxisIoInMsg_ALM</para>
        /// <para>6：伺服准备信号，AxisIoInMsg_RDY（保留）</para>
        /// <para>7：伺服到位信号，AxisIoInMsg_INP</para>
        /// </param>
        /// <param name="MapIoType">轴 IO 映射类型：
        /// <para>0：正限位输入端口，AxisIoInPort_PEL</para>
        /// <para>1：负限位输入端口，AxisIoInPort_NEL</para>
        /// <para>2：原点输入端口，AxisIoInPort_ORG</para>
        /// <para>3：伺服报警输入端口，AxisIoInPort_ALM</para>
        /// <para>4：伺服准备输入端口，AxisIoInPort_RDY</para> 
        /// <para>5：伺服到位输入端口，AxisIoInPort_INP</para>
        /// <para>6：通用输入端口，AxisIoInPort_IO</para>
        /// </param>
        /// <param name="MapIoIndex">轴 IO 映射索引号：<para>1）当轴 IO 映射类型设置为 6 时，此参数可设置为 0~15 整数，表示该映射对应的具体通用输入端口号</para><para>2）当轴 IO 映射类型设置为 0~5 时，此参数可设置 0~7 整数，表示该映射所对应的具体轴号</para></param>
        /// <param name="Filter">轴 IO 信号滤波时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisIOMap(int CardNo, int axis, int IoType, int MapIoType, int MapIoIndex, double Filter)
        {
            return MCC.YK_set_axis_io_map((ushort)CardNo, (ushort)axis, (ushort)IoType, (ushort)MapIoType, (ushort)MapIoIndex, Filter);
        }

        /// <summary>
        /// 读取轴 IO 映射关系设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A： 0~3</param>
        /// <param name="IoType">轴 IO 信号类型</param>
        /// <param name="MapIoType">返回轴 IO 映射类型</param>
        /// <param name="MapIoIndex">返回轴 IO 映射索引号</param>
        /// <param name="Filter">返回轴 IO 信号滤波时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisIOMap(int CardNo, int axis, int IoType, ref UInt16 MapIoType, ref UInt16 MapIoIndex, double Filter)
        {
            return MCC.YK_get_axis_io_map((ushort)CardNo, (ushort)axis, (ushort)IoType, ref MapIoType, ref MapIoIndex, ref Filter);
        }

        /// <summary>
        /// 统一设置所有专用 IO 的滤波时间
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Filter">轴 IO 信号滤波时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int SetCardSpecialInPutFilter(int CardNo, double Filter)
        {
            return MCC.YK_set_special_input_filter((ushort)CardNo, Filter);
        }

        #endregion

        #region 虚拟 IO 映射函数 √

        /// <summary>
        /// 设置虚拟 IO 映射关系
        /// <para>注 意：</para>
        /// <para>1）该函数可以实现专用通用 IO 输入接口的滤波功能 </para>
        /// <para>2：YK_set_io_map_virtual </para>
        /// <para>3)DMC3C00 后四轴不支虚拟 IO 映射功能</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">虚拟 IO 口号,取值范围：0~15</param>
        /// <param name="MapIoType">轴 IO 映射类型：
        /// <para>0：正限位输入端口，AxisIoInPort_PEL</para>
        /// <para>1：负限位输入端口，AxisIoInPort_NEL</para>
        /// <para>2：原点输入端口，AxisIoInPort_ORG</para>
        /// <para>3：伺服报警输入端口，AxisIoInPort_ALM</para>
        /// <para>4：伺服准备输入端口，AxisIoInPort_RDY</para> 
        /// <para>5：伺服到位输入端口，AxisIoInPort_INP</para>
        /// <para>6：通用输入端口，AxisIoInPort_IO</para>
        /// </param>
        /// <param name="MapIoIndex">虚拟 IO 映射索引号：
        /// <para>1）当虚拟 IO 映射类型设置为 6 时，此参数可设置为 0~15 整数， 表示该映射对应的具体通用输入端口号</para>
        /// <para>2）当虚拟 IO 映射类型设置为 0~5 时，此参数可设置 0~7 整数，表示该映射所对应的具体轴号</para>
        /// </param>
        /// <param name="Filter">虚拟 IO 信号滤波时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int SetCardIOMapVirtual(int CardNo, int bitno, int MapIoType, int MapIoIndex, double Filter)
        {
            return MCC.YK_set_io_map_virtual((ushort)CardNo, (ushort)bitno, (ushort)MapIoType, (ushort)MapIoIndex, Filter);
        }

        /// <summary>
        /// 读取虚拟 IO 映射关系设置
        /// <para>注 意：</para>
        /// <para>1）此函数需要配合虚拟 IO 映射功能使用</para> 
        /// <para>2）YK_read_inbit_virtual 与 YK_read_inbit 函数的区别是：YK_read_inbit 函数是不经过滤波直接读取硬件端口的电平状态，YK_read_inbit_virtual 函数通过虚拟 IO 映射后读取相应端口滤波后的电平状态</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">虚拟 IO 口号,取值范围：0~15</param>
        /// <param name="MapIoType">返回虚拟 IO 映射类型</param>
        /// <param name="MapIoIndex">返回虚拟 IO 映射索引号</param>
        /// <param name="filter_time">返回虚拟 IO 信号滤波时间，单位：s</param>
        /// <returns></returns>
        public int GetCardIOMapVirtual(int CardNo, int bitno, ref UInt16 MapIoType, ref UInt16 MapIoIndex, ref double filter_time)
        {
            return MCC.YK_get_io_map_virtual((ushort)CardNo, (ushort)bitno, ref MapIoType, ref MapIoIndex, ref filter_time);
        }

        /// <summary>
        /// 读取滤波后的虚拟 IO 口电平状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">虚拟 IO 口号,取值范围：0~15</param>
        /// <returns>指定的虚拟 IO 口电平：0：低电平，1：高电平</returns>
        public int GetCardInBitVirtual(int CardNo, int bitno)
        {
            return MCC.YK_read_inbit_virtual((ushort)CardNo, (ushort)bitno);
        }
        #endregion

        #region 检测轴到位状态函数 √
        /// <summary>
        /// 设置位置误差带
        /// <para>编码器系数的说明：</para> 
        /// <para>当使用 YK_check_success_encoder 函数检测编码器是否到位时，其用于判断的编码器位置为：编码器计数值乘以编码器系数的值。</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A： 0~3</param>
        /// <param name="factor">编码器系数</param>
        /// <param name="error">位置误差带，单位：pulse</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisFactorError(int CardNo, int axis, double factor, int error)
        {
            return MCC.YK_set_factor_error((ushort)CardNo, (ushort)axis, factor, error);
        }

        /// <summary>
        /// 读取位置误差带设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A： 0~3</param>
        /// <param name="factor">返回编码器系数设置</param>
        /// <param name="error">返回位置误差带设置</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisFactorError(int CardNo, int axis, ref double factor, int error)
        {
            return MCC.YK_get_factor_error((ushort)CardNo, (ushort)axis, ref factor, ref error);
        }

        /// <summary>
        /// 检测指令到位
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A： 0~3</param>
        /// <returns>0：表示指令位置在设定的目标位置的误差带之外 ；1：表示指令位置在设定的目标位置的误差带之内</returns>
        public int SetCardAxisCheckSuccessPulse(int CardNo, int axis)
        {
            return MCC.YK_check_success_pulse((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 检测编码器到位
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A： 0~3</param>
        /// <returns>0：表示编码器位置在设定的目标位置的误差带之外；1：表示编码器位置在设定的目标位置的误差带之内</returns>
        public int SetCardAxisCheckSuccessEncoder(int CardNo, int axis)
        {
            return MCC.YK_check_success_encoder((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 设置 CAN 通讯状态
        /// <para>注 意：</para>
        /// <para>1）当关闭运动控制卡时，CAN 通讯不会被自动断开；当再次初始化运动控制卡时，  CAN-IO 通讯依然保持之前的状态；</para>
        /// <para>2）当连接 CAN 通讯时，必须使用 nmc_get_can_state 函数读取 CAN-IO 的通讯状态， 确认 CAN 通讯已正常连接。当连接出现异常时，可再次调用 nmc_set_can_state函数进行连接；</para>
        /// <para>3）设置波特率时需保证控制卡波特率与模块波特率相对应；</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeNum">CAN 节点数，取值范围：1~8</param>
        /// <param name="state">设置通讯状态，0：断开，1：连接</param>
        /// <param name="baud">设置控制卡波特率<para>波特率参数：0,1,2,3,4,5</para><para>对应波特率：1000Kbps，800Kbps,500Kbps,250Kbps,125Kbps,100Kbps</para></param>
        /// <returns></returns>
        //public int SetCard_CANIO_ConnectState(int CardNo, int NodeNum, int state, int baud)
        //{
        //    return MCC.nmc_set_connect_state((ushort)CardNo, (ushort)NodeNum, (ushort)state, (ushort)baud);
        //}


        /// <summary>
        /// 读取 CAN 通讯状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeNum">返回 CAN 节点数</param>
        /// <param name="state">返回 CAN-IO 通讯状态，0：断开，1：连接，2：异常</param>
        /// <returns>错误代码</returns>
        public int GetCard_CANIO_ConnectState(int CardNo, ref UInt16 NodeNum, ref UInt16 state)
        {
            return MCC.YK_get_can_state((ushort)CardNo, ref NodeNum, ref state); // nmc_get_connect_state((ushort)CardNo, ref NodeNum, ref state);
        }
        /// <summary>
        /// 设置指定 CAN-IO 扩展模块的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="IoBit">输出口号</param>
        /// <param name="IoValue">输出电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int SetCard_CANIO_OutBit(int CardNo, int NodeID, int IoBit, int IoValue)
        {
            return MCC.YK_write_can_outbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit, (ushort)IoValue); // nmc_write_outbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit, (ushort)IoValue);
        }

        /// <summary>
        /// 读取指定 CAN-IO 扩展模块的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="IoBit">输出口号</param>
        /// <param name="IoValue">指定输出端口的电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int GetCard_CANIO_OutBit(int CardNo, int NodeID, int IoBit, ref ushort IoValue)
        {
            var res= MCC.YK_read_can_outbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit); // nmc_read_outbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit, ref IoValue);
            IoValue = (ushort)res;
            return res;
        }

        /// <summary>
        /// 读取指定 CAN-IO 扩展模块的某个输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="IoBit">输入端口号</param>
        /// <param name="IoValue">指定输入端口的电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int GetCard_CANIO_InBit(int CardNo, int NodeID, int IoBit, ref ushort IoValue)
        {
            var res= MCC.YK_read_can_inbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit ); // nmc_read_inbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit, ref IoValue);
            IoValue = (ushort)res;
            return res;
        }


        /// <summary>
        /// 设置指定 CAN-IO 扩展模块的输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出值，bit0~bit32 的值分别代表第 0~32 号输出口的电平</param>
        /// <returns>错误代码</returns>
        public int SetCard_CANIO_OutPort(int CardNo, int NodeID, int PortNo, int IoValue)
        {
            return MCC.YK_write_can_outport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, (uint)IoValue); // nmc_write_outport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, (uint)IoValue);
        }



        /// <summary>
        /// 读取指定 CAN-IO 扩展模块的输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出端口的值，bit0~bit31 的值分别代表第 0~31 号输出口的电平 </param>
        /// <returns></returns>
        public int GetCard_CANIO_OutPort(int CardNo, int NodeID, int PortNo, ref UInt32 IoValue)
        {
            var res= MCC.YK_read_can_outport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo); // nmc_read_outport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref IoValue);
            IoValue = res;
            return (int)res;
        }

        /// <summary>
        /// 读取指定 CAN-IO 扩展模块的输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出端口的值，bit0~bit31 的值分别代表第 0~31 号输出口的电平 </param>
        /// <returns>错误代码</returns>
        public int GetCard_CANIO_InPort(int CardNo, int NodeID, int PortNo, ref UInt32 IoValue)
        {
            var res = MCC.YK_read_can_inport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo ); // nmc_read_outport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref IoValue);
            IoValue = res;
            return (int)res;
            return 0;
            //return MCC.nmc_read_inport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref IoValue);
        }


        #endregion

        #region AD/DA 函数 √

        /// <summary>
        /// 设置 DA 输出使能
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="enable">DA 使能状态，0：禁止，1：使能</param>
        /// <returns>错误代码</returns>
        public int SetCardDAEnable(int CardNo, int enable)
        {
            return 0;
            //return MCC.YK_set_da_enable((ushort)CardNo, (ushort)enable);
        }
        /// <summary>
        /// 读取 DA 输出使能设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="enable">DA 使能状态，0：禁止，1：使能</param>
        /// <returns>错误代码</returns>
        public int GetCardDAEnable(int CardNo, ref UInt16 enable)
        {
            return 0;
            //return MCC.YK_get_da_enable((ushort)CardNo, ref enable);
        }

        /// <summary>
        /// 设置 DA 输出
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">DA 输出口号，取值范围 0、1</param>
        /// <param name="Vout">DA 输出电压，输出电压范围 -10V ~ 10V</param>
        /// <returns>错误代码</returns>
        public int SetCardDAOutPut(int CardNo, int channel, double Vout)
        {
            return 0;
            //return MCC.YK_set_da_output((ushort)CardNo, (ushort)channel, Vout);
        }

        /// <summary>
        /// 读取 DA 输出
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">DA 输出口号，取值范围 0、1</param>
        /// <param name="Vout">DA 输出电压，输出电压范围 -10V ~ 10V</param>
        /// <returns>错误代码</returns>
        public int GetCardDAOutPut(int CardNo, int channel, ref double Vout)
        {
            return 0;
            //return MCC.YK_get_da_output((ushort)CardNo, (ushort)channel, ref Vout);
        }

        /// <summary>
        /// 读取 AD 输入
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">AD 输入口号，取值范围 0 ~ 7</param>
        /// <param name="Vout">AD 输入电压，输入电压范围 -10V ~ 10V</param>
        /// <returns>错误代码</returns>
        public int GetCardADInPut(int CardNo, int channel, ref double Vout)
        {
            return MCC.YK_get_ad_input((ushort)CardNo, (ushort)channel, ref Vout);
        }

        /// <summary>
        /// 读取 AD 输入
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">AD 输入口号，取值范围 0 ~ 7</param>
        /// <param name="Vout">AD 输入电压，输入电压范围 -10V ~ 10V</param>
        /// <returns>错误代码</returns>
        public int GetCardADInPutEx(int CardNo, int channel, ref double Vout)
        {
            return 0;
            //return MCC.YK_get_ad_inputEx((ushort)CardNo, (ushort)channel, ref Vout);
        }


        #endregion

        #region 密码管理函数 √

        /// <summary>
        /// 修改密码
        /// <para>注 意：</para>
        ///  <para>除了调用此函数外，用户也可以在 Motion3000 软件中“帮助”->“密码管理”菜单下直接设置新密码</para>
        /// </summary>
        /// <param name="CardNo"> 控制卡卡号</param>
        /// <param name="new_sn">新密码，密码长度不大于 255 个字符</param>
        /// <returns>错误代码</returns>
        public int SetCardNewPassWord(int CardNo, string new_sn)
        {
            return MCC.YK_write_sn((ushort)CardNo, new_sn);
        }

        /// <summary>
        /// 密码校验
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="check_sn">旧密码</param>
        /// <returns>校验状态，0：失败，1：成功</returns>
        public int CardCheckPassWord(int CardNo, string check_sn)
        {
            return MCC.YK_check_sn((ushort)CardNo, check_sn);
        }

        /// <summary>
        /// 函数调用打印输出设置
        /// <para>注 意：</para>
        /// <para>使能打印输出后，可监控运动函数库的调用情况。在用户调用函数时，将输出相关信息，并保存在指定文件路径中；函数设置模式 2 全部不打印需配合最新动态库（20181030 及以后动态库）使用。</para>
        /// </summary>
        /// <param name="CardNo">打印输出模式，0：只打印报错函数，1：全部打印，2：全部不打印</param>
        /// <param name="FileName">文件保存路径：参数文件名+后缀：相对路径 ；完整描述参数文件的路径+文件名后缀：绝对路径</param>
        /// <returns>错误代码</returns>
        public int SetCardDebugMode(int CardNo, string FileName)
        {
            return MCC.YK_set_debug_mode((ushort)CardNo, FileName);
        }

        /// <summary>
        /// 读取函数调用打印输出设置
        /// </summary>
        /// <param name="mode">返回打印输出使能状态</param>
        /// <param name="FileName">返回文件保存路径</param>
        /// <returns>错误代码</returns>
        public int GetCardDebugMode(ref UInt16 mode, IntPtr FileName)
        {
            return MCC.YK_get_debug_mode(ref mode, FileName);
        }


        #endregion

        #region 扩展IO √

        /// <summary>
        /// YK_3000_设置 扩展IO 通讯状态
        /// <para>注 意：</para>
        /// <para>1）当关闭运动控制卡时，CAN 通讯不会被自动断开；当再次初始化运动控制卡时， CAN-IO 通讯依然保持之前的状态；</para>
        /// <para>2）当连接 CAN 通讯时，必须使用 nmc_get_can_state 函数读取 CAN-IO 的通讯状态，确认 CAN 通讯已正常连接。当连接出现异常时，可再次调用 nmc_set_can_state函数进行连接；</para>
        /// <para>3）设置波特率时需保证控制卡波特率与模块波特率相对应；</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeNum">CAN 节点数，取值范围：1~8</param>
        /// <param name="state">设置通讯状态，0：断开，1：连接</param>
        /// <param name="baud">设置控制卡波特率：波特率参数：0,1,2,3,4,5；对特率：1000Kbps，800Kbps,500Kbps,250Kbps,125Kbps,100Kbps</param>
        /// <returns>返回值：错误代码</returns>
        public int SetExpandIOConnectState(int CardNo, int NodeNum, int state, int baud)
        {
            return MCC.YK_set_can_state((ushort)CardNo, (ushort)NodeNum, (ushort)state, (ushort)baud); // nmc_set_connect_state((ushort)CardNo, (ushort)NodeNum, (ushort)state, (ushort)baud);
        }

        /// <summary>
        /// YK_3000_读取 扩展IO 通讯状态
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="NodeNum"></param>
        /// <param name="state"></param>
        /// <param name="baud"></param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandIOConnectState(int CardNo, ref ushort NodeNum, ref ushort state)
        {
            return MCC.YK_get_can_state((ushort)CardNo, ref NodeNum, ref state); // nmc_get_connect_state((ushort)CardNo, ref NodeNum, ref state);
        }

        //*********************************************************************************************************************





        /// <summary>
        /// YK_3000_设置指定 CAN-IO 扩展模块的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="IoBit">输出口号</param>
        /// <param name="IoValue">输出电平，0：低电平，1：高电平</param>
        /// <returns>返回值：错误代码</returns>
        public int SetExpandIOOutBit(int CardNo, int NodeID, int IoBit, int IoValue)
        {
            return MCC.YK_write_can_outbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit, (ushort)IoValue); // nmc_write_outbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit, (ushort)IoValue);
        }

        /// <summary>
        /// YK_3000_读取指定 CAN-IO 扩展模块的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="IoBit">输出口号</param>
        /// <param name="IoValue">输出电平，0：低电平，1：高电平</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandIOOutBit(int CardNo, int NodeID, int IoBit, ref ushort IoValue)
        {
          
            var res= MCC.YK_read_can_outbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit); // nmc_read_outbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit, ref IoValue);
            IoValue = (ushort)res;
            return res;
        }

        /// <summary>
        /// YK_3000_设置指定 CAN-IO 扩展模块的输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出值，bit0~bit32 的值分别代表第 0~32 号输出口的电平</param>
        /// <returns>返回值：错误代码</returns>
        public int SetExpandIOPortNoOutBit(int CardNo, int NodeID, int PortNo, int IoValue)
        {
            return MCC.YK_write_can_outport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, (ushort)IoValue); // nmc_write_outport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, (ushort)IoValue);
        }

        /// <summary>
        /// YK_3000_读取指定 CAN-IO 扩展模块的输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出值，bit0~bit31 的值分别代表第 0~31 号输出口的电平</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandIOPortNoOutBit(int CardNo, int NodeID, int PortNo, ref uint IoValue)
        {
            var res= MCC.YK_read_can_outport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo); // nmc_read_outport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref IoValue);
            IoValue = res;
            return (int)res;
        }


        /// <summary>
        /// YK_3000_设置指定 CAN-ADDA 扩展模块的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输出端口号</param>
        /// <param name="IoValue">输出电平，单位：mV/mA</param>
        /// <returns>返回值：错误代码</returns>
        public int SetExpandADDAIOPortNoOutBit(int CardNo, int NodeID, int PortNo, double IoValue)
        {
            return 0;
            //return MCC.nmc_set_da_output((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, IoValue);
        }

        /// <summary>
        /// YK_3000_读取指定 CAN-ADDA 扩展模块的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输出端口号</param>
        /// <param name="IoValue">输出电平，单位：mV/mA</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandADDAIOPortNoOutBit(int CardNo, int NodeID, int PortNo, ref double IoValue)
        {
            return 0;
            //return MCC.nmc_get_da_output((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref IoValue);
        }

        /// <summary>
        ///  YK_3000_设置指定 CAN-ADDA 扩展模块的某个输出端口的模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输入端口号</param>
        /// <param name="mode">输入模式，0：电压模式，1：电流模式</param>
        /// <param name="buffer_nums">保留参数</param>
        /// <returns>返回值：错误代码</returns>
        public int SetExpandADDAIOOutPortNoMode(int CardNo, int NodeID, int PortNo, int mode, uint buffer_nums)
        {
            return 0; 
            //return MCC.nmc_set_da_mode((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, (ushort)mode, buffer_nums);
        }



        /// <summary>
        ///  YK_3000_读取指定 CAN-ADDA 扩展模块的某个输出端口的模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输入端口号</param>
        /// <param name="mode">输入模式，0：电压模式，1：电流模式</param>
        /// <param name="buffer_nums">保留参数</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandADDAIOOutPortNoMode(int CardNo, int NodeID, int PortNo, ref ushort mode, uint buffer_nums)
        {
            return 0; 
            //return MCC.nmc_get_da_mode((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref mode, buffer_nums);
        }



        //*********************************************************************************************************************

        /// <summary>
        /// YK_3000_读取指定 CAN-IO 扩展模块的某个输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="IoBit">输出口号</param>
        /// <param name="IoValue">输出电平，0：低电平，1：高电平</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandIOInBit(int CardNo, int NodeID, int IoBit, ref ushort IoValue)
        {
            return 0; //
            //return MCC.nmc_read_inbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit, ref IoValue);
        }



        /// <summary>
        /// YK_3000_读取指定 CAN-IO 扩展模块的输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出值，bit0~bit31 的值分别代表第 0~31 号输出口的电平</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandIOPortNoInBit(int CardNo, int NodeID, int PortNo, ref uint IoValue)
        {
            return 0; 
            //return MCC.nmc_read_inport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref IoValue);
        }

        /// <summary>
        /// YK_3000_读取指定 CAN-ADDA 扩展模块的某个输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输出端口号</param>
        /// <param name="IoValue">输出电平，单位：mV/mA</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandADDAIOPortNoInBit(int CardNo, int NodeID, int PortNo, ref double IoValue)
        {
            return 0;
            //return MCC.nmc_get_ad_input((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref IoValue);
        }

        /// <summary>
        ///  YK_3000_设置指定 CAN-ADDA 扩展模块的某个输入端口的模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输入端口号</param>
        /// <param name="mode">输入模式，0：电压模式，1：电流模式</param>
        /// <param name="buffer_nums">保留参数</param>
        /// <returns>返回值：错误代码</returns>
        public int SetExpandADDAIOInPortNoMode(int CardNo, int NodeID, int PortNo, int mode, uint buffer_nums = 999)
        {
            return 0;
            //return MCC.nmc_set_ad_mode((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, (ushort)mode, buffer_nums);
        }

        /// <summary>
        ///  YK_3000_读取指定 CAN-ADDA 扩展模块的某个输入端口的模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输入端口号</param>
        /// <param name="mode">输入模式，0：电压模式，1：电流模式</param>
        /// <param name="buffer_nums">保留参数</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandADDAIOInPortNoMode(int CardNo, int NodeID, int PortNo, ref ushort mode, uint buffer_nums = 999)
        {
            return 0;
            //return MCC.nmc_get_ad_mode((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref mode, buffer_nums);

        }



        /// <summary>
        /// YK_3000_保存模式设置到模块 FLASH
        /// <para>注 意：</para>
        /// <para>1）保存后模块会断开连接，需要重新连接才能进行正常控制。</para>
        /// <para>2）设置的模式会断电保存，上电后电压或电流模式为最后一次断电前设置的模式。</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">保留参数，设为 0</param>
        /// <param name="NodeNum">节点号，1-8</param>
        /// <returns>返回值：错误代码</returns>
        public int SaveExpandIoToFlash(int CardNo, int PortNum, int NodeNum)
        {
            return 0;
            //return MCC.nmc_write_to_flash((ushort)CardNo, (ushort)PortNum, (ushort)NodeNum);
        }

        #endregion

        #region 螺距补偿函数

        /// <summary>
        /// 使能螺距补偿功能
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号</param>
        /// <param name="enable">使能螺距补偿功能，1：使能，0：不使能</param>
        /// <returns></returns>
        public int SetCardAxisLeadscrewCompEnable(int CardNo, int axis, ushort enable)
        {
            return MCC.YK_set_leadscrew_comp_enable((ushort)CardNo, (ushort)axis, enable);
        }

        /// <summary>
        /// 设置螺距补偿配置参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号</param>
        /// <param name="n">补偿点数</param>
        /// <param name="startpos">补偿起始点的规划位置（单位：pulse）</param>
        /// <param name="lenpos">测量段的总长（单位：pulse）；</param>
        /// <param name="pCompPos">对应为正方向运动时，各点位置需要补偿的脉冲数；</param>
        /// <param name="pCompNeg">保留</param>
        /// <returns></returns>
        public int SetCardAxisLeadscrewCompConfig(int CardNo, int axis, int n, int startpos,
            int lenpos, int[] pCompPos, int[] pCompNeg)
        {
            return MCC.YK_set_leadscrew_comp_para((ushort)CardNo, (ushort)axis, (ushort)n,
               startpos, lenpos, pCompPos, pCompNeg);
        }

        #endregion

        #region 反向间隙 (MCC1200/MCC1600)

        /// <summary>
        /// 设置指定轴的反向间隙值(MCC1200/MCC1600)
        /// </summary>
        /// <param name="CardNo">控制卡号</param>
        /// <param name="axis">指定轴号，取值范围: 取值范围:MCC400:0~3, MCC800:0~7, MCC1200:0~11, MCC1600:0~15;</param>
        /// <param name="backlash_value">反向间隙值，单位：unit；</param>
        /// <param name="backlash_interval">反向间隔,脉冲/ms 单位;</param>
        /// <param name="backlash_dir">反向间隙补偿值方向；0 反向，1正向;</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisSetBacklash(int CardNo, int axis, int backlash_value, int backlash_interval,
            int backlash_dir)
        {
            //MCC.YK_set_backlash(CardNo,axis,backlash_value,backlash_interval,backlash_dir);
            return 0;
        }

        /// <summary>
        /// 读取指定轴的反向间隙值设置(MCC1200/MCC1600)
        /// </summary>
        /// <param name="CardNo">控制卡号</param>
        /// <param name="axis">指定轴号，取值范围: 取值范围:MCC400:0~3, MCC800:0~7, MCC1200:0~11, MCC1600:0~15;</param>
        /// <param name="backlash_value">返回反向间隙值，单位：unit；</param>
        /// <param name="backlash_interval">返回反向间隔,脉冲/ms 单位;</param>
        /// <param name="backlash_dir">返回反向间隙补偿值方向，0 反向，1正向;</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisSetBacklash(int CardNo, int axis, ref int backlash_value,
            ref int backlash_interval, ref int backlash_dir)
        {
            //MCC.YK_get_backlash(CardNo,axis,ref backlash_value,ref backlash_interval,ref backlash_dir);
            backlash_value = 0; backlash_interval = 0; backlash_dir = 0;
            return 0;
        }


        #endregion

        #region 轴停止原因

        /// <summary>
        /// 读取指定轴的停止原因
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号, 取值范围：MCC400:0~3，MCC800:0~7，MCC1200:0~11，MCC1600:0~15;</param>
        /// <param name="StopReason">停止原因: 0:正常停止 1:ALM 立即停止 2:ALM 减速停止 3:LTC 外部触发 4:EMG 立即停止 5:正硬限位 6:负硬限位 7:正硬限位减速停止 8:负硬限位减速停止 9:正软限位立即停止 10:负软限位立即停止 11:正软限位减速停止 12:负软限位减速停止 13:命令立即停止 14:命令减速停止 15:其它原因立即停止 16:其它原因减速停止 17:未知原因立即停止 18:未知原因减速停止 19:外部IO 减速停止</param>
        /// <returns></returns>
        public int GetCardAxisStopReason(int CardNo, int axis, ref int StopReason)
        {
            return MCC.YK_get_stop_reason((ushort)CardNo,(ushort)axis,ref StopReason);
        }

        #endregion


        #region 跟随运动(MCC1600)

        /// <summary>
        /// 轴跟随参数设置.(MCC1600)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="slave_axis">跟随轴号</param>
        /// <param name="master_axis">主轴</param>
        /// <param name="followradio">跟随比例</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisFollowParameter(int CardNo, int slave_axis, int master_axis,
            double followradio)
        {
            //MCC.YK_set_follow_parameter(CardNo,slave_axis,master_axis,followradio);
            return 0;
        }

        /// <summary>
        /// 读取轴跟随参数.(MCC1600)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="slave_axis">跟随轴号</param>
        /// <param name="master_axis">主轴</param>
        /// <param name="followradio">跟随比例</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisFollowParameter(int CardNo, int slave_axis, ref int master_axis,
            ref double followradio)
        {
            //MCC.YK_get_fellow_parameter(CardNo,slave_axis,ref master_axis,ref followradio);
            return 0;
        }

        /// <summary>
        /// 轴跟随使能.(MCC1600)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="slave_axis">跟随轴号</param>
        /// <param name="enable">使能状态</param>
        /// <returns></returns>
        public int SetCardAxisFollowEnable(int CardNo, int slave_axis, int enable)
        {
            //MCC.YK_set_follow_enable(CardNo,slave_axis,enable);
            return 0;
        }

        /// <summary>
        /// 读取轴跟随状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="slave_axis">跟随轴号</param>
        /// <param name="enable">使能状态</param>
        /// <returns></returns>
        public int GetCardAxisFollowEnable(int CardNo, int slave_axis, ref int enable)
        {
            //MCC.YK_get_follow_enable(CardNo,slave_axis,ref enable);
            return 0;
        }

        #endregion


        #region 同步运动

        /// <summary>
        /// 同步运行单轴运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis_num">轴数</param>
        /// <param name="axis_list">轴号列表数组，最大长度为axis_num</param>
        /// <param name="aim_pos">轴位置列表数组，最大长度为axis_num</param>
        /// <param name="posi_mode">运动模式,0:相对坐标模式,1:绝对坐标模式；</param>
        /// <returns>错误代码</returns>
        public int CardAxisPmotionSync(int CardNo, int axis_num, ushort[] axis_list, int[] aim_pos,
            int posi_mode)
        {
            return MCC.YK_pmotion_sync((ushort)CardNo, (ushort)axis_num,axis_list,aim_pos, (ushort)posi_mode);
        }

        #endregion

        #region 软着陆

        /// <summary>
        /// 单轴运动软着陆功能(MCC1200/MCC1600)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">制定轴号, 取值范围：MCC400:0~3，MCC800:0~7，MCC1200:0~11，MCC1600:0~15;</param>
        /// <param name="midPos">:第一段pmove 终点位置，单位unit;</param>
        /// <param name="targetPos">:第二段pmove 终点位置,单位unit;</param>
        /// <param name="startVel">起始速度，单位 unit/s;</param>
        /// <param name="maxVel">最大速度，单位 unit/s;</param>
        /// <param name="endVel">停止速度，单位 unit/</param>
        /// <param name="tAcc">加速时间,单位秒（S）;</param>
        /// <param name="tDec">减速时间，单位秒（S）;</param>
        /// <param name="posiMode">位置坐标模式：0-相对位置坐标；1-绝对位置坐标；</param>
        /// <returns></returns>
        public int CardAxisPmoveSoftLanding(int CardNo, int axis, double midPos, double targetPos,
            double startVel, double maxVel, double endVel,
            double tAcc, double tDec, int posiMode)
        {
            //MCC.YK_pmove_soft_landing(CardNo,axis,midPos,targetPos,startVel,Maxvel,endVel,
            //tAcc,tDec,posiMode);
            return 0;
        }

        /// <summary>
        /// 指定轴点位运动软着陆(MCC1200/MCC1600)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号,取值范围MCC400:0~3，MCC800:0~7，MCC1200:0~11，MCC1600:0~15;</param>
        /// <param name="aim_pos">第一段终点位置,单位:pulse;</param>
        /// <param name="target_pos">第二段终点位置，单位:pulse;</param>
        /// <param name="acc_time">加速时间,单位:s(最小值为0.001s)</param>
        /// <param name="dec_time">减速时间,单位:s(最小值为0.001s);</param>
        /// <param name="start_vel">起始速度,单位:pulse/s(最大值为2M)</param>
        /// <param name="max_vel">最大速度,单位:pulse/s(最大值为2M);</param>
        /// <param name="mid_vel">中间速度，单位:pulse/s(最大值为2M);</param>
        /// <param name="stop_vel">停止速度,单位:pulse/s(最大值为2M)</param>
        /// <param name="pos_mode">运动模式,0:相对坐标模式,1:绝对坐标模式</param>
        /// <returns></returns>
        public int CardAxisPMotionSoftLanding(int CardNo, int axis, double aim_pos,
            double target_pos, double acc_time, double dec_time, double start_vel,
            double max_vel, double mid_vel, double stop_vel, int pos_mode)
        {
            //MCC.YK_pmotion_soft_landing(CardNo,axis,aim_pos,target_pos,acc_time,
            //dec_time,start_vel,max_vel,mid_vel,stop_vel,pos_mode);
            return 0;
        }

        #endregion

        #region 多段插补（缓冲区指令） （MCC轨迹卡S系列）

        /// <summary>
        /// 设置坐标系参数，确立坐标系映射，建立坐标系（MCC轨迹卡S系列）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/MCC800S:取值范围:0-1;MCC1200S/MCC1600S:0-2)；</param>
        /// <param name="dimension">坐标系的维数，取值范围：[2, 8]；备注：插补坐标系最小维数为2；</param>
        /// <param name="profile">坐标系【x,y,z,u,v,w,c,c1】与物理轴0-最大轴的映射关系。备注：x,y,z,u,v,w,c,c1映射的物理轴号不能相同</param>
        /// <param name="synVelMax">该坐标系的最大合成速度。如果用户在输入插补段的时候所设置的目标速度大于了该速度，则将会被限制为该速度。单位：unit/s。</param>
        /// <param name="synAccMax">该坐标系的最大合成加速度。如果用户在输入插补段的时候所设置的加速度大于了该加速度，则将会被限制为该加速度。单位：unit/s²。</param>
        /// <param name="evenTime">保留值</param>
        /// <param name="setOriginFlag">保留值</param>
        /// <param name="originPos">保留值</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisCrdPrm(int CardNo, int crd, ushort dimension, ushort[] profile, 
            double synVelMax, double synAccMax, short evenTime, short setOriginFlag, int[] originPos)
        {
            //MCC.YK_buf_set_crd_prm(CardNo,crd,dimension,profile,synVelMax,synAccMax,
           // evenTime,setOriginFlag,originPos);
            return 0;
        }

        /// <summary>
        /// 查询坐标系参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/MCC800S:取值范围:0-1;MCC1200S/MCC1600S:0-2)；</param>
        /// <param name="dimension">坐标系的维数，取值范围：[2, 8]；备注：插补坐标系最小维数为2；</param>
        /// <param name="profile">坐标系【x,y,z,u,v,w,c,c1】与物理轴0-最大轴的映射关系。备注：x,y,z,u,v,w,c,c1映射的物理轴号不能相同</param>
        /// <param name="synVelMax">该坐标系的最大合成速度。如果用户在输入插补段的时候所设置的目标速度大于了该速度，则将会被限制为该速度。单位：unit/s。</param>
        /// <param name="synAccMax">该坐标系的最大合成加速度。如果用户在输入插补段的时候所设置的加速度大于了该加速度，则将会被限制为该加速度。单位：unit/s²。</param>
        /// <param name="evenTime">保留值</param>
        /// <param name="setOriginFlag">保留值</param>
        /// <param name="originPos">保留值</param>
        /// <returns></returns>
        public int GetCardAxisCrdPrm(int CardNo, int crd, ref ushort dimension, ushort[] profile, ref double synVelMax,
            ref double synAccMax, ref short evenTime, ref short setOriginFlag, int[] originPos)
        {
            //MCC.YK_buf_get_crd_prm(CardNo,crd,ref dimension,profile,ref synVelMax,
            //ref synAccMax,ref evenTime,ref setOriginFalg,originPos);
            return 0;
        }

        /// <summary>
        /// 向插补缓存区增加插补数据.用于使用前瞻时向插补缓存区增加插补数据，调用该指令表示后续没有新的数据，将会一次性把前瞻缓存区的数据压入运动缓存区
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisBufCrdData(int CardNo, int crd)
        {
            //MCC.YK_buf_crd_data(CardNo,crd);
            return 0;
        }

        /// <summary>
        /// 直线插补
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">坐标系号，取值范围: (MCC400S/MCC800S:取值范围:0-1;MCC1200S/MCC1600S:0-2)；</param>
        /// <param name="axisNum">插补轴数</param>
        /// <param name="AxisList">轴号列表</param>
        /// <param name="aim_pos">插补终点坐标值，单位：unit；</param>
        /// <param name="posi_mode">位置模式，0：相对模式 ；1：绝对模式；</param>
        /// <param name="synVel">插补段的目标合成速度，单位：unit/s；</param>
        /// <param name="synAcc">插补段的合成加速度，单位：unit/s²；</param>
        /// <param name="velEnd">插补段的终点速度，单位：unit/s。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为0</param>
        /// <param name="velStart">插补段的起跳速度，单位：unit/s。该值只有在没有使用前瞻预处理功能时才有意义否则该值无效。默认值为0；</param>
        /// <returns>错误代码</returns>
        public int CardAxisBufLineData(int CardNo, int Crd, ushort axisNum, ushort[] AxisList,
            double[] aim_pos, short posi_mode, double synVel, double synAcc,
            double velEnd, double velStart)
        {
            //MCC.YK_buf_line_move(CardNo,Crd,axisNum,AxisList,aim_pos,posi_mode,synVel,synAcc,velEnd,velStart);
            return 0;
        }


        /// <summary>
        /// 圆心插补
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">坐标系号，取值范围: (MCC400S/MCC800S:取值范围:0-1;MCC1200S/MCC1600S:0-2)；</param>
        /// <param name="axisNum">插补轴数</param>
        /// <param name="AxisList">轴号列表</param>
        /// <param name="aim_pos">插补终点坐标值，单位：unit；</param>
        /// <param name="cen_pos">圆心坐标单位：unit；</param>
        /// <param name="circleDir">圆弧的旋转方向，0：顺时针圆弧； 1：逆时针圆弧；</param>
        /// <param name="circle_num">圆弧圈数</param>
        /// <param name="posi_mode">位置模式，0：相对模式 ；1：绝对模式</param>
        /// <param name="synVel">插补段的目标合成速度，单位：unit/s</param>
        /// <param name="synAcc">插补段的合成加速度，单位：unit/s²</param>
        /// <param name="velEnd">插补段的终点速度，单位：unit/s。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为0</param>
        /// <param name="velStart">插补段的起跳速度，单位：unit/s。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为0</param>
        /// <returns>错误代码</returns>
        public int CardAxisBufArcnCenterMove(int CardNo, int Crd, ushort axisNum, ushort[] AxisList,
            double[] aim_pos, ref double cen_pos, ushort circleDir, ushort circle_num,
            short posi_mode, double synVel, double synAcc, double velEnd, double velStart)
        {
            //MCC.YK_buf_arcn_center_move(CardNo,Crd,axisNum,AxisList,aim_pos,cen_pos,circleDir,
            //circle_num,posi_mode,synVel,synAcc,velEnd,velStart);
            return 0;
        }

        /// <summary>
        /// 半径圆弧插补
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">坐标系号，取值范围: (MCC400S/MCC800S:取值范围:0-1;MCC1200S/MCC1600S:0-2)；</param>
        /// <param name="axisNum">插补轴数</param>
        /// <param name="AxisList">轴号列表</param>
        /// <param name="aim_pos">插补终点坐标值，单位：unit；</param>
        /// <param name="radius">圆弧插补的圆弧半径值，单位：unit；半径为正时，表示圆弧为小于等于180°圆弧，半径为负时，表示圆弧为大于180°圆弧；</param>
        /// <param name="circleDir">圆弧的旋转方向，0：顺时针圆弧；1：逆时针圆弧；</param>
        /// <param name="circle_num">圆弧圈数</param>
        /// <param name="posi_mode">位置模式，0：相对模式；1：绝对模式</param>
        /// <param name="synVel">插补段的目标合成速度，单位：unit/s；</param>
        /// <param name="synAcc">插补段的合成加速度，单位：unit/s²；</param>
        /// <param name="velEnd">插补段的终点速度，单位：unit/s。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为0</param>
        /// <param name="velStart">插补段的起跳速度，单位：unit/s。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为0</param>
        /// <returns>错误代码</returns>
        public int CardAxisBufArcnRadiusMove(int CardNo, int Crd, ushort axisNum, ushort[] AxisList,
            double[] aim_pos, double radius, ushort circleDir, ushort circle_num,
            short posi_mode, double synVel, double synAcc, double velEnd, double velStart)
        {
            //MCC.YK_buf_arcn_radius_move(CardNo,crd,axisNum,AxisList,aim_pos,radius,
            //circleDir,circle_num,posi_mode,synVel,synAcc,velEnd,velStart);
            return 0;
        }


        /// <summary>
        /// 三点圆弧插补
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">坐标系号，取值范围: (MCC400S/MCC800S:取值范围:0-1;MCC1200S/MCC1600S:0-2)；</param>
        /// <param name="axisNum">插补轴数</param>
        /// <param name="AxisList">轴号列表</param>
        /// <param name="aim_pos">插补终点坐标值，单位：unit；</param>
        /// <param name="radius">圆弧插补的圆弧半径值，单位：unit；半径为正时，表示圆弧为小于等于180°圆弧，半径为负时，表示圆弧为大于180°圆弧；</param>
        /// <param name="circleDir">圆弧的旋转方向，0：顺时针圆弧；1：逆时针圆弧；</param>
        /// <param name="circle_num">圆弧圈数</param>
        /// <param name="posi_mode">位置模式，0：相对模式；1：绝对模式</param>
        /// <param name="synVel">插补段的目标合成速度，单位：unit/s；</param>
        /// <param name="synAcc">插补段的合成加速度，单位：unit/s²；</param>
        /// <param name="velEnd">插补段的终点速度，单位：unit/s。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为0</param>
        /// <param name="velStart">插补段的起跳速度，单位：unit/s。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为0</param>
        /// <returns>错误代码</returns>
        public int CardAxisBufArcn3pointMove(int CardNo, int Crd, ushort axisNum, ushort[] AxisList,
             double[] aim_pos, ref double mid_pos, ushort circle_num, short posi_mode, double synVel,
                double synAcc, double velEnd, double velStart)
        {
            //YK_buf_arcn_3point_move(CardNo,Crd,axisNum,AxisList,aim_pos,ref mid_pos,circle_num,
            //posi_mode,synVel,synAcc,velEnd,velStart);
            return 0;
        }

        /// <summary>
        /// 缓存区内数字量IO输出设置指令
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">:坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S/MCC1600S:0-2)；</param>
        /// <param name="doType">数字量输出的类型：10：输出驱动器使能；11：输出驱动器报警清除；12：输出通用输出。</param>
        /// <param name="doMask">从bit0-bit15 按位表示指定的数字量输出是否有操作： 0：该路数字量输出无操作；1：该路数字量输出有操作。</param>
        /// <param name="doValue">从bit0-bit15 按位表示指定的数字量输出的值。</param>
        /// <returns>错误代码</returns>
        public int CardAxisBufIO(int CardNo, int crd, ushort doType, ushort doMask, ushort doValue)
        {
            //MCC.YK_buf_io(CardNo,crd,doType,doMask,doValue);
            return 0;
        }


        /// <summary>
        /// 缓存区指令，缓存区内延时设置指令；
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="delayTime">延时时间.单位：ms；</param>
        /// <returns></returns>
        public int CardAxisBufDelay(int CardNo, int crd, ushort delayTime)
        {
            //MCC.YK_buf_delay(CardNo,crd,delayTime);
            return 0;
        }

        /// <summary>
        /// 缓存区指令，缓存区内有效限位开关
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="axis">需要将限位有效的轴的编号，取值范围:MCC400:0~3,MCC800:0~7,MCC1200:0~11,MCC1600:0~15；</param>
        /// <param name="limitType">需要有效的限位类型：0：需要将该轴的正限位设置为有效；1：需要将该轴的负限位设置为有效；-1:需要将该轴的正限位和负限位都设置为有效，默认为该值</param>
        /// <returns>错误代码</returns>
        public int CardAxisBufLimitON(int CardNo, int crd, short axis, short limitType)
        {
            //MCC.YK_buf_limit_on(CardNo,crd,axis,limitType);
            return 0;
        }

        /// <summary>
        /// 设置缓存区内无效限位开关
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="axis">需要将限位有效的轴的编号，取值范围:MCC400:0~3,MCC800:0~7,MCC1200:0~11,MCC1600:0~15；</param>
        /// <param name="limitType">需要无效的限位类型；0：需要将该轴的正限位无效；1：需要将该轴的负限位无效；-1：需要将该轴的正限位和负限位都无效，默认为该值。</param>
        /// <returns>错误代码</returns>
        public int CardAxisBufLimitOff(int CardNo, int crd, short axis, short limitType)
        {
            //MCC.YK_buf_limit_off(CardNo,crd,axis,limitType);
            return 0;
        }

        /// <summary>
        /// 缓存区内设置axis 的停止IO信息
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="axis">需要停止轴的编号，取值范围:MCC400:0~3,MCC800:0~7,MCC1200:0~11,MCC1600:0~15</param>
        /// <param name="stopType">需要设置停止IO信息的停止类型；0：紧急停止类型；1：平滑停止类型</param>
        /// <param name="inputType">设置的数字量输入的类型；0：正限位；1：负限位；2：驱动报警；3：原点开关；4：通用输入；5：电机到位信号。</param>
        /// <param name="inputIndex">设置的数字量输入的索引号，取值范围根据inputType 的取值而定；当inputType= 0,1,2,4,5时，取值[0,7],当inputType= 4时，取值[1,31]</param>
        /// <returns>错误代码</returns>
        public int CardAxisBufSetStopIO(int CardNo, int crd, short axis,
            short stopType, short inputType, short inputIndex)
        {
            //MCC.YK_buf_set_stop_io(CardNo,crd,axis,stopType,inputType,iputIndex);
            return 0;
        }

        /// <summary>
        /// 实现刀向跟随功能，启动某个轴点位运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="moveAxis">需要进行点位运动的轴号，取值范围：[0, 7];该轴不能处于坐标系中；</param>
        /// <param name="pos">点位运动的目标位置，单位：unit；</param>
        /// <param name="vel">点位运动的目标速度，单位：unit/s</param>
        /// <param name="acc_time">点位运动的加减速时间，单位：s；</param>
        /// <returns>错误代码</returns>
        public int CardAxisBufMove(int CardNo, int crd, short moveAxis, double pos, double vel, double acc_time)
        {
            //MCC.YK_buf_move(CardNo,crd,moveAxis,pos,vel,acc_time);
            return 0;
        }


        /// <summary>
        /// 查询插补缓存区剩余空间
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="pSpace">读取插补缓存区中的剩余空间；最大为4096段</param>
        /// <returns>错误代码</returns>
        public int CardAxisBufCrdRemainSpace(int CardNo, int crd, ref int pSpace)
        {
            //MCC.YK_buf_crd_remain_space(CardNo,crd,ref pSpace);
            return 0;
        }

        /// <summary>
        /// 清除插补缓存区内的插补数据
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <returns></returns>
        public int CardAxisBufCrdClear(int CardNo, int crd)
        {
            //MCC.YK_buf_crd_clear(CardNo,crd);
            return 0;
        }

        /// <summary>
        /// 启动缓冲区插补运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <returns></returns>
        public int CardAxisBufCrdStart(int CardNo, int crd)
        {
            //MCC.YK_buf_crd_start(CardNo,crd);
            return 0;
        }

        /// <summary>
        /// 暂停缓冲区插补运动。在暂停缓冲区插补运动后，通过调用函数YK_buf_crd_start进行重新启动。
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <returns></returns>
        public int CardAxisBufCrdPause(int CardNo, int crd)
        {
            //MCC.YK_buf_crd_pause(CardNo,crd);
            return 0;
        }

        /// <summary>
        /// 停止缓冲区插补运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="mode">停止模式；0：减速停止；1：立即停止；</param>
        /// <returns></returns>
        public int CardAxisBufCrdStop(int CardNo, int crd, int mode)
        {
            //MCC.YK_buf_crd_stop(CardNo,crd,mode);
            return 0;
        }


        /// <summary>
        /// 查询插补运动坐标系状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="pRun">读取插补运动状态：0：运行状态；1：停止状态；2：暂停状态 3：激活状态 4：空闲状态  5：错误状态</param>
        /// <param name="pSegment">读取当前已经完成的插补段数。当重新建立坐标系或者调用MCC_CrdClear 指令后，该值会被清零</param>
        /// <returns>错误代码</returns>
        public int CardAxisBufCrdStatus(int CardNo, int crd, ref short pRun, ref int pSegment)
        {
            //MCC.YK_buf_crd_status(CardNo,crd,ref pRun,ref pSegment);
            return 0;
        }


        /// <summary>
        /// 设置自定义插补段段号。控制卡默认的自定义段号为1，之后每增加一个指令，就自动增加一。如果用户自己设定，则需要对每一条指令都指定相应的段号；使用YK_buf_get_user_seg_num可读回设置的相应段号。
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="segNum">设置用户自定义的插补段段号</param>
        /// <returns></returns>
        public int SetCardAxisBufUserSegNum(int CardNo, int crd, long segNum)
        {
            //MCC.YK_buf_set_user_seg_num(CardNo,crd,segNum);
            return 0;
        }

        /// <summary>
        /// 读取自定义插补段段号
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="segNum">读取的用户自定义的插补段段号</param>
        /// <returns></returns>
        public int GetCardAxisBufUserSegNum(int CardNo, int crd, ref long segNum)
        {
            //MCC.YK_buf_get_user_seg_num(CardNo,crd,ref segNum);
            return 0;
        }

        /// <summary>
        /// 读取未完成的插补段段数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="pSegment">读取的剩余插补段的段数</param>
        /// <returns></returns>
        public int GetCardAxisBufRemainSegNum(int CardNo, int crd, ref long pSegment)
        {
            //MCC.YK_buf_get_remain_seg_num(CardNo,crd,ref pSegment);
            return 0;
        }

        /// <summary>
        /// 设置插补运动目标合成速度倍率
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="synVelRatio">设置的插补目标速度倍率，取值范围：(0, 1]，系统默认该值为1；</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisBufSetOverride(int CardNo, int crd, double synVelRatio)
        {
            //MCC.YK_buf_set_override(CardNo,crd,synVelRatio);
            return 0;
        }


        /// <summary>
        /// 设置插补运动平滑停止、急停合成加速度
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="decSmoothStop">设置的坐标系合成平滑停止加速度，单位：unit/ s²；</param>
        /// <param name="decAbruptStop">设设置的坐标系合成急停加速度，单位：unit/ s²；</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisBufSetCrdStopDec(int CardNo, int crd, double decSmoothStop, double decAbruptStop)
        {
            //MCC.YK_buf_set_crd_stop_dec(CardNo,crd,decSmoothStop,decAbruptStop);
            return 0;
        }

        /// <summary>
        /// 查询插补运动平滑停止、急停合成加速度
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="decSmoothStop">查询坐标系合成平滑停止加速度，单位：unit/ s²；</param>
        /// <param name="decAbruptStop"> 查询坐标系合成急停加速度，单位：unit/ s²；</param>
        /// <returns></returns>
        public int GetCardAxisBufSetCrdStopDec(int CardNo, int crd, ref double decSmoothStop, ref double decAbruptStop)
        {
            //MCC.YK_buf_get_crd_stop_dec(CardNo,crd,ref decSmoothStop,ref decAbruptStop);
            return 0;
        }

        /// <summary>
        /// 查询该坐标系的当前坐标位置值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="pPos">读取的坐标系的坐标值，单位：unit。该参数为一个数组首元素的指针，数组的元素个数取决于该坐标系的维数；</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisBufGetCrdPos(int CardNo, int crd, ref double pPos)
        {
            //MCC.YK_buf_get_crd_pos(CardNo,crd,ref pPos);
            return 0;
        }


        /// <summary>
        /// 查询该坐标系的当前坐标速度值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="pSynVel">读取的坐标系的合成速度值, 单位：unit/s；</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisBufGetCrdVel(int CardNo, int crd, ref double pSynVel)
        {
            //MCC.YK_buf_get_crd_vel(CardNo,crd,ref pSynVel);
            return 0;
        }

        /// <summary>
        /// 初始化插补前瞻缓存区
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="T">轨迹误差控制参数。当T属于[0,50]，表示拐弯轨迹百分比；当T等于零表示轨迹没有过渡；当T小于零，表示轨迹误差；</param>
        /// <param name="accMax">最大加速度，单位：unit/ s²；</param>
        /// <param name="enable">前瞻使能标志。0:前瞻禁止； 1:前瞻使能；</param>
        /// <returns>错误代码</returns>
        public int CardAxisBufInitLookahead(int CardNo, int crd, double T, double accMax, int enable)
        {
            //MCC.YK_buf_init_lookahead(CardNo,crd,T,accMax,enable);
            return 0;
        }

        /// <summary>
        /// 查询插补前瞻缓存区
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="T">轨迹误差控制参数。当T属于[0,50]，表示拐弯轨迹百分比；当T等于零表示轨迹没有过渡；当T小于零，表示轨迹误差；</param>
        /// <param name="accMax">最大加速度,单位：unit/ s²。</param>
        /// <param name="enable">前瞻使能标志, 0:前瞻禁止； 1:前瞻使能；</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisBufInitLookahead(int CardNo, int crd, ref double T,
            ref double accMax, ref int enable)
        {
            //MCC.YK_buf_get_init_lookahead(CardNo,crd,ref T,ref accMax,ref enable);
            return 0;
        }

        /// <summary>
        ///  设置插补速度曲线平滑系数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="Smooth">插补速度曲线平滑系数，范围[0,1]</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisBufCrdSmooth(int CardNo, int crd, double Smooth)
        {
            //MCC.YK_buf_set_crd_smooth(CardNo,crd,Smooth);
            return 0;
        }

        /// <summary>
        /// 查询插补速度曲线平滑系数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="Smooth">插补速度曲线平滑系数，范围[0,1]</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisBufCrdSmooth(int CardNo, int crd, ref double Smooth)
        {
            //MCC.YK_buf_get_crd_smooth(CardNo,crd,ref Smooth);
            return 0;
        }


        /// <summary>
        /// 关闭坐标系
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <returns>错误代码</returns>
        public int CardAxisBufCrdReset(int CardNo, int crd)
        {
            //MCC.YK_buf_crd_reset(CardNo,crd);
            return 0;
        }


        /// <summary>
        /// 缓存区等待通用输入IO指令
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="dI_id">通用输入IO口序号</param>
        /// <param name="di_logic">需要等待的通用输入IO口电平</param>
        /// <param name="time_out">等待最大时间，单位 毫秒（ms）</param>
        /// <returns></returns>
        public int CardAxisBufWaitDI(int CardNo, int crd, ushort dI_id, ushort di_logic, long time_out)
        {
            //MCC.YK_buf_wait_di(CardNo,crd,dI_id,di_logic,time_out);
            return 0;
        }

        /// <summary>
        /// 连续插补缓冲区中PWM 输出
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="pwm_no">PWM 通道，取值范围：0-1；</param>
        /// <param name="pwm_duty">占空比，取值范围：0-1；</param>
        /// <param name="pwm_freq">频率，取值范围：0-2MHz；</param>
        /// <returns></returns>
        public int CardAxisBufPwmOutput(int CardNo, int crd, ushort pwm_no, double pwm_duty,
            double pwm_freq)
        {
            //MCC.YK_buf_pwm_output(CardNo,crd,pwm_no,pwm_duty,pwm_freq);
            return 0;
        }


        /// <summary>
        /// 连续插补缓冲区中PWM 跟随
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="pwm_no">PWM 通道，取值范围：0-1；</param>
        /// <param name="mode">跟随模式：1：占空比跟随；2：频率跟随；</param>
        /// <param name="start_speed">开始跟随速度, 单位：unit/s；</param>
        /// <param name="max_speed">最大跟随速度，单位：unit/s；</param>
        /// <param name="max_power">最大跟随能量(频率或占空比)：跟随模式为1时：占空比，取值范围：0-1；跟随模式为2时：频率，取值范围：0-2MHz。</param>
        /// <param name="min_power">最小跟随能量(频率或占空比)：跟随模式为1时：占空比，取值范围：0-1；跟随模式为2时：频率，取值范围：0-2MHz；</param>
        /// <param name="none_follow_value">非跟随值(频率或占空比)：跟随模式为1时：占空比，取值范围：0-1；跟随模式为2时：频率，取值范围：0-2MHz；</param>
        /// <returns></returns>
        public int CardAxisBufPwmFollow(int CardNo, int crd, ushort pwm_no, ushort mode,
            double start_speed, double max_speed, double max_power,
            double min_power, double none_follow_value)
        {
            //MCC.YK_buf_pwm_follow(CardNo,crd,pwm_no,mode,start_speed,max_speed,max_power,min_power,none_follow_value);
            return 0;
        }


        /// <summary>
        /// 连续插补中相对于轨迹段起点IO 滞后输出（段内执行）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="bitno">输出口号，取值范围：0-31;</param>
        /// <param name="on_off">电平状态，0：低电平，1：高电平;</param>
        /// <param name="delay_value">滞后值，单位：s（滞后时间模式）或 unit（滞后距离模式）;</param>
        /// <param name="delay_mode">滞后模式，：滞后时间，：滞后距离;</param>
        /// <param name="ReverseTime">电平输出后的延时翻转时间，单位：s;</param>
        /// <returns></returns>
        public int CardAxisBufDelayOutbitToStartPos(int CardNo, int crd, int bitno, int on_off,
            double delay_value, int delay_mode, double ReverseTime)
        {
            //MCC.YK_buf_delay_outbit_to_start_pos(CardNo,crd,bitno,on_off,delay_value,delay_ode,ReverseTime);
            return 0;
        }

        /// <summary>
        /// 连续插补中相对于轨迹段终点IO 滞后输出
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="bitno">输出口号，取值范围：0-31;</param>
        /// <param name="on_off">电平状态，0:低电平，1:高电平;</param>
        /// <param name="delay_time">滞后时间，单位：秒（s）;</param>
        /// <param name="ReverseTime">保留参数，固定值为0;</param>
        /// <returns></returns>
        public int CardAxisBufDelayOutbitToStopPos(int CardNo, int crd, int bitno,
            int on_off, double delay_time, double ReverseTime)
        {
            //MCC.YK_buf_delay_outbit_to_stop_pos(CardNo,crd,bitno,on_off,delay_time,ReverseTime);
            return 0;
        }

        /// <summary>
        /// 连续插补中相对于轨迹段终点IO 提前输出（段内执行）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="bitno">输出口号，取值范围:0~31</param>
        /// <param name="on_off">电平状态 ，0：低电平，1：高电平；</param>
        /// <param name="ahead_value">提前值，单位：s（提前时间模式）或 unit（提前距离模式）；</param>
        /// <param name="ahead_mode">提前模式，：0提前时间，1：提前距离；</param>
        /// <param name="ReverseTime">电平输出后的延时翻转时间，单位：s；</param>
        /// <returns></returns>
        public int CardAxisBufAheadOutbitToStopPos(int CardNo, int crd, int bitno,
            int on_off, double ahead_value, int ahead_mode, double ReverseTime)
        {
            //MCC.YK_buf_ahead_outbit_to_stop_pos(CardNo,crd,bitno,on_off,ahead_value,ahead_mode,ReverseTime);
            return 0;
        }

        /// <summary>
        /// 打开缓冲区PWM 开关
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="PwmNo">PWM 通道，取值范围：[0,1]</param>
        /// <returns></returns>
        public int CardAxisBufSetPwmON(int CardNo, int crd, int PwmNo)
        {
            //MCC.YK_buf_set_pwm_on(CardNo,crd,PwmNo);
            return 0;
        }

        /// <summary>
        /// 关闭缓冲区PWM 开关
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">坐标系号，取值范围: (MCC400S/800S:取值范围:0-1;MCC1200S:0-2)；</param>
        /// <param name="PwmNo">PWM 通道，取值范围：0-1；</param>
        /// <returns></returns>
        public int CardAxisBufSetPwmOFF(int CardNo, int crd, int PwmNo)
        {
            //MCC.YK_buf_set_pwm_off(CardNo,crd,PwmNo);
            return 0;
        }


        #endregion



      


    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
