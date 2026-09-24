﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/DLL/PCI7800.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;


namespace WenQiZhi.Domain.MotionCard.Common
{
    public class PCI400
    {
        //---------------------   板卡初始和配置函数  ----------------------

         [DllImport("PCI400.dll", EntryPoint = "PCI400_card_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_card_init();
        
         [DllImport("PCI400.dll", EntryPoint = "PCI400_card_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern void  PCI400_card_close();

         /// <summary>
         ///功  能：为控制卡分配系统资源，并初始化控制卡<para />
         ///参  数：cardid –卡号（拨码开关说明）<para /> 
         ///返回值： 0表示开卡失败，大于0表示开卡成功<para />
         ///备注：开卡请使用该函数，如需开多张卡，多次调用该函数即可。
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_card_init_one", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_card_init_one(UInt16 cardid);

         /// <summary>
         ///功  能：关闭控制卡，释放系统资源<para />
         ///参  数：card id –卡号（拨码开关说明）<para />
         ///返回值：0表示控制卡关闭成功
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_card_close_one", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_card_close_one(UInt16 cardid);

         /// <summary>
         ///功  能：列表所有开卡成功的卡号<para />
         ///参  数：cardnum:PC机上卡的张数<para />
         ///Cardtypelist:保留<para />
         ///Cardidlist:卡号列表（保留）<para />
         ///返回值：错误代码。
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_card_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_card_list(ref UInt16 cardnum,UInt32[] cardtypelist,UInt16[] cardidlist);
        
         [DllImport("PCI400.dll", EntryPoint = "PCI400_set_debug_info", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_set_debug_info(UInt16 debugon); 
       
         [DllImport("PCI400.dll", EntryPoint = "PCI400_card_init_type", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_card_init_type(UInt16 cardid,UInt16 type); 
		
         [DllImport("PCI400.dll", EntryPoint = "PCI400_test_software", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_test_software(UInt16 card, UInt16 testid, UInt16 para1, UInt16 para2, UInt16 para3);

         [DllImport("PCI400.dll", EntryPoint = "PCI400_test_hardware", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_test_hardware(UInt16 card);

         [DllImport("PCI400.dll", EntryPoint = "PCI400_download_firmware", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_download_firmware(UInt16 card, string pfilename);

         //---------------------   板卡复位  ----------------------
         /// <summary>
         ///功能：复位指定卡号的控制卡，只能在初始化完成之后调用<para />
         ///参  数：控制卡卡号（拨码开关说明）<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_card_reset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_card_reset(UInt16 card);
        
         //---------------------   版本号和加密----------------------

         /// <summary>
         ///功  能：读取卡的硬件版本号<para />
         ///参  数：控制卡卡号（拨码开关说明）<para />
         ///返回值：硬件版本号
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_card_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_get_card_version(UInt16 card);

         /// <summary>
         ///功  能：读取卡硬件的固件版本号<para />
         ///参  数：控制卡卡号（通过拨码开关设置）<para />
         ///Firm_id: 固件版本号id<para />
         ///sub_firm_id：固件版本号子id<para />
         //返回值：错误代码。
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_card_soft_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_get_card_soft_version(UInt16 card, ref UInt16 firm_id, ref UInt32 sub_firm_id);
        
         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_client_ID", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_get_client_ID(UInt16 card);

         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_lib_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_get_lib_version();
        
         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_card_ID", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_get_card_ID(UInt16 card);

         /// <summary>
         ///功  能：读取控制卡唯一序列号<para />
         ///参  数：  card   控制卡卡号（（拨码开关说明）<para />
         ///sn    控制卡序列号（32位）<para />
         ///返回值：错误代码。
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_controller_sn", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_get_controller_sn(UInt16 card, UInt32 []sn);

         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_link_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_get_link_state(UInt16 card, ref UInt16 state);

         /// <summary>
         ///功  能：读取当前控制卡总轴数<para />
         ///参  数：  card   控制卡卡号（（拨码开关说明）<para />
         ///返回值：当前控制卡总轴数。
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_total_axes", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_get_total_axes(UInt16 card);
         
         //设置控制卡密码
       
         /// <summary>
         /// 功  能：设置控制卡密码，用于程序加密（会自动掉电保存）<para />
         /// 参  数：card            指定控制卡号（通过拨码开关设置）<para />
         /// pwd          控制卡密码<para />
         ///返回值：错误代码<para />
         ///注意：如果不需要重新设置控制卡密码，该函数调用一次即可；设置控制卡密码成功后，该密码会自动掉电保存，无需重复设置。
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_write_user_password", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_write_user_password(UInt16 card, UInt32 pwd);

         /// <summary>
         ///功 能：读取控制卡密码<para />
         ///参 数：card 指定控制卡号（通过拨码开关设置）<para />
         ///返回值：控制卡当前密码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_read_user_password", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_read_user_password(UInt16 card);
		
         //参数文件操作

         /// <summary>
         /// 功  能：把参数文件（.ini文件）里面保存的参数写入控制卡<para />
         ///参  数：card            指定控制卡号（通过拨码开关设置）<para />
         /// fpath        参数文件路径<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_LoadConfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_LoadConfig(UInt16 card, string fpath);

         /// <summary>
         /// 功  能：把控制卡的参数配置读取出来保存在参数文件（.ini文件）中<para />
         ///参  数：card            指定控制卡号（通过拨码开关设置）<para />
         /// fpath        参数文件路径<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_ReadConfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_ReadConfig(UInt16 card, string fpath);

          //脉冲输入输出配置

         /// <summary>
         ///功  能：设置指定轴的脉冲输出模式<para />
         ///参  数：card     控制卡卡号（通过拨码开关设置）<para />
         ///axis  		    指定轴号（轴号范围说明）<para />
         ///outmode		    脉冲输出方式选择，其值如下表所示：<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_set_pulse_outmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_set_pulse_outmode(UInt16 card,UInt16 axis,UInt16 outmode);

         /// <summary>
         ///功  能：设置编码器的计数方式<para />
         ///参  数：card       控制卡卡号（通过拨码开关设置）<para />
         ///        axis  	  指定轴号（轴号范围说明）<para />         
         ///        mode		  编码器器的计数方式：<para />
         ///0 -- 非A/B相脉冲信号（脉冲+方向信号）<para />
         ///1 -- 1倍 A/B相脉冲信号<para />
         ///2 -- 2倍A/B相脉冲信号<para />
         ///3 -- 4倍A/B相脉冲信号<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_counter_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_counter_config(UInt16 card, UInt16 axis, UInt16 mode);

         /// <summary>
         ///功  能：设置指定轴编码器反馈脉冲计数值方向<para />
         ///参  数：card         控制卡卡号（通过拨码开关设置）<para />
         ///        axis		    指定轴号（轴号范围说明）<para />
         ///        ifreverse    编码器的计数方向：0－不取反，1－取反<para />
         ///返回值：错误代码
         /// </summary>
		 [DllImport("PCI400.dll", EntryPoint = "PCI400_config_encoder_dir", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_config_encoder_dir(UInt16 card, UInt16 axis, UInt16 ifreverse);

         /// <summary>
         ///功  能：读取指定轴编码器反馈脉冲计数值方向<para />
         ///参  数：card         控制卡卡号（通过拨码开关设置）<para />
         ///        axis		    指定轴号（轴号范围说明）<para />
         ///        ifreverse    编码器的计数方向：0－不取反，1－取反<para />
         ///返回值：错误代码
         /// </summary>
		  [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_encoder_dir", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern UInt32 PCI400_get_config_encoder_dir(UInt16 card, UInt16 axis, ref UInt16 ifreverse);
          //添加配置读

          /// <summary>
          //功  能：读取指定轴的脉冲输出模式<para />
          //参  数：card     控制卡卡号（通过拨码开关设置）<para />
          //axis  		    指定轴号（轴号范围说明）<para />
          //outmode		    脉冲输出方式选择，其值如下表所示：<para />
          //返回值：错误代码
          /// </summary>
          [DllImport("PCI400.dll", EntryPoint = "PCI400_get_pulse_outmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern UInt32  PCI400_get_pulse_outmode(UInt16 card,UInt16 axis,ref UInt16 outmode);

          /// <summary>
          ///功  能：读取编码器的计数方式<para />
          ///参  数：card       控制卡卡号（通过拨码开关设置）<para />
          ///        axis  	  指定轴号（轴号范围说明）<para />         
          ///        mode		  编码器器的计数方式：<para />
          ///0 -- 非A/B相脉冲信号（脉冲+方向信号）<para />
          ///1 -- 1倍 A/B相脉冲信号<para />
          ///2 -- 2倍A/B相脉冲信号<para />
          ///3 -- 4倍A/B相脉冲信号<para />
          ///返回值：错误代码
          /// </summary>
          [DllImport("PCI400.dll", EntryPoint = "PCI400_get_counter_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern UInt32  PCI400_get_counter_config(UInt16 card,UInt16 axis,ref UInt16 mode);

          /// <summary>
          /// 功  能：读取SD信号使能及其有效电平方式和输出方式<para />
          ///参数：card               控制卡卡号（通过拨码开关设置）<para />
          ///axis				指定轴号（轴号范围说明）<para />
          ///enable			    允许/禁止信号功能：0－无效，1－有效<para />
          ///sd_logic			设置SD信号的有效电平：0－低电平有效，1－高电平有效<para />
          ///sd_mode           0-SD信号有效后立即切换低速，1-SD信号有效后减速至低速<para />
          ///返回值：错误代码
          /// </summary>
          [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_SD_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern UInt32  PCI400_get_config_SD_PIN(UInt16 card,UInt16 axis,ref UInt16 enable, ref UInt16 sd_logic,ref UInt16 sd_mode);
          
          /// <summary>
          /// 功  能：读取INP信号使能及其有效电平方式<para />
          ///参  数：card             控制卡卡号（通过拨码开关设置）<para />
          ///axis				指定轴号（轴号范围说明）<para />
          ///enable			允许/禁止信号功能：0－无效，1－有效<para />
          ///inp_logic			设置INP信号的有效电平：0－低电平有效，1－高电平有效<para />
          ///返回值：错误代码
          /// </summary>
          [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_INP_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern UInt32  PCI400_get_config_INP_PIN(UInt16 card,UInt16 axis,ref UInt16 enable,ref UInt16 inp_logic);

          /// <summary>
          /// 功  能：读取ERC信号使能及其有效电平和输出方式<para />
          ///参  数：card             控制卡卡号（通过拨码开关设置）<para />
          ///axis				指定轴号（轴号范围说明）<para />
          ///enable           范围：0 - 3;<para /> 
          ///       0－不自动输出ERC信号<para />
          ///       1－输出ERC信号<para />
          ///erc_logic			 ERC信号的有效电平：0－低电平有效，1－高电平有效<para />
          ///erc_width		误差清除信号ERC有效输出宽度时间(单位:us )<para />
          ///erc_off_time		 ERC信号的关断时间(单位:us )<para />
          ///返回值：错误代码
          /// </summary>
          [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_ERC_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern UInt32  PCI400_get_config_ERC_PIN(UInt16 card,UInt16 axis,ref UInt16 enable,ref UInt16 erc_logic,
                            ref UInt16 erc_width,ref UInt16 erc_off_time);

          /// <summary>
          /// 功  能：读取ALM信号使能和有效电平及其工作方式<para />
          ///参  数：card              控制卡卡号（通过拨码开关设置）<para />
          ///axis				指定轴号（轴号范围说明）<para />
          ///enable           允许/禁止信号功能：0－无效，1－有效<para />
          ///alm_logic		ALM信号的输入电平：0－低电平有效，1－高电平有效<para />
          ///alm_action		ALM信号的制动方式：0－立即停止，1－减速停止<para />
          ///返回值：错误代码
          /// </summary>
          [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_ALM_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern UInt32  PCI400_get_config_ALM_PIN(UInt16 card,UInt16 axis, ref UInt16 enable, ref UInt16 alm_logic,ref UInt16 alm_action);
        
          /// <summary>
          /// 功  能：读取EL信号的有效电平及制动方式<para />
          ///参  数：card              控制卡卡号（通过拨码开关设置）<para />    
          ///axis			     指定轴号（轴号范围说明）<para />
          ///enable           允许/禁止信号功能：0－正负限位禁止<para />
          ///1－正负限位允许<para />
          ///2－正限位禁止、负限位允许<para />
          ///3－正限位允许、负限位禁止<para />
          /// el_logic 		     EL有效电平：0－正负限位低电平有效<para />
          ///1－正负限位高电平有效<para />
          ///2－正限位低电平有效，负限位高有效<para />
          ///3－正限位高电平有效，负限位低有效<para />
          ///el_mode   EL有效电平和制动方式：<para />
          /// 0－立即停止<para />
          ///1－减速停止<para />
          ///返回值：错误代码<para />
          /// </summary>
          [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_EL_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern UInt32 PCI400_get_config_EL_PIN(UInt16 card,UInt16 axis, ref UInt16 enable, ref UInt16 el_logic, ref UInt16 el_mode);

          /// <summary>
          /// 功  能：读取ORG信号的有效电平，以及允许/禁止滤波功能<para />
          ///参  数：card              控制卡卡号（通过拨码开关设置）<para />
          ///axis			     指定轴号（轴号范围说明）<para />
          ///org_logic		     ORG信号的有效电平：0－低电平有效，1－高电平有效<para />
          ///filter			     保留<para />
          ///返回值：错误代码
          /// </summary>
          [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_HOME_PIN_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern UInt32  PCI400_get_config_HOME_PIN_logic(UInt16 card,UInt16 axis,ref UInt16 org_logic,ref UInt16 filter);
          
          /// <summary>
          /// 功  能：设定/读取指定轴的回原点模式<para />
          ///参  数：card             控制卡卡号（通过拨码开关设置）<para />
          ///axis			    指定轴号（轴号范围说明）<para />
          ///home_dir	   回零方向， 1正向, 2:负向<para />
          ///vel           回零速度，0为低速(该轴起始速度)回原点，1为高速(该轴运行速度)回原点。<para />
          ///mode			回原点的信号模式<para />
          ///0–一次回零<para />
          ///		    1–二次回零<para />
          ///	    2–一次回零加回找<para />
          ///3–一次回零后再找一个EZ<para />
          ///	        4–以EZ作为原点进行一次回零<para />
          ///EZ_count		    保留参数<para />
          ///返回值：错误代码
          /// </summary>
          [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_home_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern UInt32  PCI400_get_config_home_mode(UInt16 card,UInt16 axis,ref UInt16 home_dir, ref double vel,ref UInt16 home_mode,ref UInt16 EZ_count);
          
          /// <summary>
          /// 功  能：设置/读取手轮脉冲信号的计数方式<para />
          ///参  数：card            控制卡卡号（通过拨码开关设置）<para />
          ///axis	       指定轴号（轴号范围说明）<para />
          ///inmode	      表示输入方式：0－A、B相位正交计数，1－双脉冲信号<para />
          ///multi        计数器的计数方向及倍率设置：设置手轮的倍率, >=0表示默认方向, <0表示与默认方向相反。<para />
          ///返回值：错误代码
          /// </summary>
          [DllImport("PCI400.dll", EntryPoint = "PCI400_get_handwheel_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern UInt32  PCI400_get_handwheel_inmode(UInt16 card,UInt16 axis,ref UInt16 inmode,ref double multi);
          
          /// <summary>
          /// 功  能：设置/读取指定轴“锁存”信号的有效电平及其和工作方式。<para />
          ///参  数：card                 控制卡卡号（通过拨码开关设置）<para />
          ///axis					指定轴号（轴号范围说明）<para />
          ///ltc_logic				LTC信号逻辑电平：0－低有效，1－高有效<para />
          ///ltc_mode				0－EZ信号锁存，1－原点信号锁存<para />
          ///返回值：错误代码
          /// </summary>
          [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_LTC_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern UInt32  PCI400_get_config_LTC_PIN(UInt16 card,UInt16 axis,ref UInt16 ltc_logic, ref UInt16 ltc_mode);
        
        //专用信号设置函数

         /// <summary>
         /// 功  能：设置SD信号使能及其有效电平方式和输出方式<para />
         ///参数：card               控制卡卡号（通过拨码开关设置）<para />
         ///axis				指定轴号（轴号范围说明）<para />
         ///enable			    允许/禁止信号功能：0－无效，1－有效 <para />
         ///sd_logic			设置SD信号的有效电平：0－低电平有效，1－高电平有效 <para />
         ///sd_mode           0-SD信号有效后立即切换低速，1-SD信号有效后减速至低速 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_SD_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_config_SD_PIN(UInt16 card,UInt16 axis,UInt16 enable, UInt16 sd_logic,UInt16 sd_mode);

        /// <summary>
        /// 功  能：设置INP信号使能及其有效电平方式 <para />
        ///参  数：card             控制卡卡号（通过拨码开关设置） <para />
        ///axis				指定轴号（轴号范围说明） <para />
        ///enable			允许/禁止信号功能：0－无效，1－有效 <para />
        ///inp_logic			设置INP信号的有效电平：0－低电平有效，1－高电平有效 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_INP_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_config_INP_PIN(UInt16 card,UInt16 axis,UInt16 enable,UInt16 inp_logic);

        /// <summary>
        /// 功  能：设置/读取ERC信号使能及其有效电平和输出方式<para />
        ///参  数：card             控制卡卡号（通过拨码开关设置）<para />
        ///axis				指定轴号（轴号范围说明）<para />
        ///enable           范围：0 - 3; <para />
        ///         0－不自动输出ERC信号<para />
        ///         1－输出ERC信号<para />
        ///	 ERC信号的有效电平：0－低电平有效，1－高电平有效<para />
        ///erc_width		误差清除信号ERC有效输出宽度时间（单位: us）<para />
        ///erc_off_time		 ERC信号的关断时间（单位: us）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_ERC_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_config_ERC_PIN(UInt16 card,UInt16 axis,UInt16 enable,UInt16 erc_logic,
                        UInt16 erc_width,UInt16 erc_off_time);

        /// <summary>
        ///功	能:  设置EMG信号的有效电平，急停信号有效后会立即停止所有轴。<para />
        ///参	数:   cardno          控制卡卡号（通过拨码开关设置） <para />
        ///enable          0-不使能    1-使能<para />
        ///emg_logic:      EMG信号的有效电平：0－低电平有效，1－高电平有效<para />
        ///备注：EMG信号硬件接口HY7400为IN35，HY7600为IN29，HY7800为IN23，HY7C00为IN11。<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_EMG_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_config_EMG_PIN(UInt16 cardno, UInt16 option, UInt16 emg_logic);

        /// <summary>
        ///功	能:  读取EMG信号的有效电平，急停信号有效后会立即停止所有轴。<para />
        ///参	数:   cardno          控制卡卡号（通过拨码开关设置） <para />
        ///enable          0-不使能    1-使能<para />
        ///emg_logic:      EMG信号的有效电平：0－低电平有效，1－高电平有效<para />
        ///备注：EMG信号硬件接口HY7400为IN35，HY7600为IN29，HY7800为IN23，HY7C00为IN11。<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_EMG_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_get_config_EMG_PIN(UInt16 cardno, ref UInt16 enbale, ref UInt16 emg_logic);

        /// <summary>
        /// 功  能：设置ALM信号使能和有效电平及其工作方式<para />
        ///参  数：card              控制卡卡号（通过拨码开关设置）<para />
        ///axis				指定轴号（轴号范围说明）<para />
        ///enable           允许/禁止信号功能：0－无效，1－有效<para />
        ///alm_logic		ALM信号的输入电平：0－低电平有效，1－高电平有效<para />
        ///alm_action		ALM信号的制动方式：0－立即停止，1－减速停止<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_ALM_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_config_ALM_PIN(UInt16 card,UInt16 axis, UInt16 enable, UInt16 alm_logic,UInt16 alm_action);

        /// <summary>
        /// 功  能：设置EL信号的有效电平及制动方式<para />
        ///参  数：card              控制卡卡号（通过拨码开关设置）<para />    
        ///axis			     指定轴号（轴号范围说明）<para />
        ///enable           允许/禁止信号功能：0－正负限位禁止<para />
        ///1－正负限位允许<para />
        ///2－正限位禁止、负限位允许<para />
        ///3－正限位允许、负限位禁止<para />
        /// el_logic 		     EL有效电平：0－正负限位低电平有效<para />
        ///1－正负限位高电平有效<para />
        ///2－正限位低电平有效，负限位高有效<para />
        ///3－正限位高电平有效，负限位低有效<para />
        ///el_mode   EL有效电平和制动方式：<para />
        /// 0－立即停止<para />
        ///1－减速停止<para />
        ///返回值：错误代码<para />
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_EL_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_config_EL_PIN(UInt16 card,UInt16 axis, UInt16 enable, UInt16 el_logic, UInt16 el_mode);

        /// <summary>
        /// 功  能：设置ORG信号的有效电平，以及允许/禁止滤波功能<para />
        ///参  数：card              控制卡卡号（通过拨码开关设置）<para />
        ///axis			     指定轴号（轴号范围说明）<para />
        ///org_logic		     ORG信号的有效电平：0－低电平有效，1－高电平有效<para />
        ///filter			     保留<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_HOME_PIN_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_config_HOME_PIN_logic(UInt16 card,UInt16 axis,UInt16 org_logic,UInt16 filter);

        /// <summary>
        /// 功  能：输出对指定轴的伺服使能端子的控制<para />
        ///参  数：card              控制卡卡号（通过拨码开关设置）<para />
        ///axis			     指定轴号（轴号范围说明）<para />
        ///on_off		     设定电平状态：0－低电平，1－高电平。<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_write_SEVON_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_write_SEVON_PIN(UInt16 card,UInt16 axis, UInt16 on_off);

        /// <summary>
        /// 功  能：读取指定轴的伺服使能端子的电平状态<para />
        ///参  数：card              控制卡卡号（通过拨码开关设置）<para />
        ///axis			     指定轴号（轴号范围说明）<para />
        ///返回值：0－低电平，1－高电平
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_read_SEVON_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_read_SEVON_PIN(UInt16 card,UInt16 axis);

        /// <summary>
        /// 功  能：控制指定轴“误差清除”端子信号的输出<para />
        ///参  数：card              控制卡卡号（通过拨码开关设置）<para />
        ///axis			     指定轴号（轴号范围说明）<para />
        ///sel			     0-复位ERC信号，1－关闭ERC信号<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_write_ERC_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_write_ERC_PIN(UInt16 card,UInt16 axis, UInt16 sel);

        /// <summary>
        /// 功  能：读取指定运动轴的“伺服准备好”端子的电平状态<para />
        ///参  数：card              控制卡卡号（通过拨码开关设置）<para />
        ///axis			     指定轴号（轴号范围说明）<para />
        ///返回值：0－低电平，1－高电平
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_read_RDY_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_read_RDY_PIN(UInt16 card,UInt16 axis);
		
        /// <summary>
        /// 功  能：读取指定轴“误差清除”端子信号的电平状态<para />
        ///参  数：card              控制卡卡号（通过拨码开关设置）<para />
        ///axis			     指定轴号（轴号范围说明）<para />	     
        ///返回值：0¬－输出ERC信号，1－关闭ERC信号<para />
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_read_ERC_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_read_ERC_PIN(UInt16 card,UInt16 axis);

        /// <summary>
        /// 功  能：设置间隙补偿值<para />
        ///参  数：card              控制卡卡号（通过拨码开关设置）<para />
        ///axis			     指定轴号（轴号范围说明）<para />
        ///backlash	     间隙补偿值， 单位：脉冲<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_backlash", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_set_backlash(UInt16 card,UInt16 axis, Int32 backlash);

        /// <summary>
        /// 功  能：读取间隙补偿值<para />
        ///参  数：card              控制卡卡号（通过拨码开关设置）<para />
        ///axis			     指定轴号（轴号范围说明）<para />
        ///backlash	     间隙补偿值， 单位：脉冲<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_backlash", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_get_backlash(UInt16 card,UInt16 axis, ref Int32 backlash);

        //通用输入/输出控制函数

        /// <summary>
        /// 功  能：读取指定控制卡的某一位输入口的电平状态<para />
        ///参  数：cardno			指定控制卡号（通过拨码开关设置）<para />
        ///bitno			指定输入口序号<para />
        ///返回值：0表示低电平；1表示高电平
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_read_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_read_inbit(UInt16 cardno, UInt16 bitno);

        /// <summary>
        /// 功  能：设置指定控制卡的某一位输出口的电平状态<para />
        ///参  数：cardno			指定控制卡号（通过拨码开关设置）<para /> 
        ///bitno			指定输出口序号（范围：0-23）<para />
        ///on_off			输出电平：0－表示输出低电平，1－表示输出高电平。<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_write_outbit(UInt16 cardno, UInt16 bitno,UInt16 on_off);

        /// <summary>
        /// 功  能：设置控制卡的多个通用输出口的电平状态<para />
        ///参  数：card			   指定控制卡号（通过拨码开关设置）<para />
        ///Bitmask        输出端口值（如果掩码对应的位为0，不进行任何操作，掩码对应的位为1，将指定输出口置为on_off相应位的值）<para />
        ///bit0 – bit23位值分别代表第0 – 23号输出端口值。<para />
        ///on_off           IO值（把对应位置0：打开输出口，对应位置1：关闭输出口）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_write_outbit_mask", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_write_outbit_mask(UInt16 cardno, UInt32 bitmask, UInt32 on_off);
		
        /// <summary>
        /// 功  能：立即输出PWM <para />
        ///参  数：card            指定控制卡号（通过拨码开关设置）<para />
        ///channel         通道号 范围（0－3）（对应OUT12-15） <para />        
        ///ioduty          占空比 范围（0～100），0是常开，100是常闭<para />
        /// iofreq           频率（单位：赫兹）（范围：1-5000）<para />
        /// timems          输出时间（单位：100微秒）（范围：0-65535） <para /> 
        ///               （设为0则关闭PWM,设为65535则一直输出PWM）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_write_pwm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_write_pwm(UInt16 cardno,UInt16 channel,UInt16 ioduty,UInt16 iofreq,UInt16 timems);
		
        [DllImport("PCI400.dll", EntryPoint = "PCI400_write_pwmf", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_write_pwmf(UInt16 cardno,UInt16 channel,double ioduty,UInt16 iofreq,UInt16 timems);

        /// <summary>
        /// 功  能：读取指定控制卡的某一位输出口的电平状态 <para />
        ///参  数：cardno			指定控制卡号（通过拨码开关设置） <para />
        ///bitno			指定输入口位号（取值范围：0－39）<para />
        ///返回值：0表示低电平；1表示高电平。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_read_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32   PCI400_read_outbit(UInt16 cardno, UInt16 bitno) ;

        /// <summary>
        /// 功  能：读取指定控制卡的全部通用输入口的电平状态 <para />
        ///参  数：cardno			指定控制卡号（通过拨码开关设置）<para />
        ///iport          （范围：0-2）<para />
        ///返回值：bit0 – bit31位值分别代表第0 – 31号输入端口值。<para />
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_read_inport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 PCI400_read_inport(UInt16 cardno, UInt16 iport);

        /// <summary>
        /// 功  能：读取指定控制卡的全部通用输出口的电平状态<para />
        ///参  数：cardno			指定控制卡号（通过拨码开关设置）<para />
        ///返回值：位号0-23位对应输出口OUT0-23
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_read_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_read_outport(UInt16 cardno) ;

        /// <summary>
        /// 功  能：读取指定控制卡的全部通用输出口的电平状态<para />
        ///参  数：cardno			指定控制卡号（通过拨码开关设置）<para />
        ///portnum           0或1  <para />
        ///返回值
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_read_outport_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_read_outport_ex(UInt16 cardno,UInt16 portnum) ;

        /// <summary>
        /// 功  能：设置控制卡的全部通用输出口的电平状态 <para />
        ///参  数：cardno			指定控制卡号（通过拨码开关设置）<para />
        ///port_value		输出口端口值 <para />
        ///bit0 – bit23位值分别代表第0 – 23号输出端口值。 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_write_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_write_outport(UInt16 cardno, UInt32 port_value);

        //制动函数

        /// <summary>
        /// 功  能：指定轴减速停止，调用此函数时立即减速，直到停止脉冲输出 <para />
        ///参  数：card			   控制卡卡号（通过拨码开关设置）<para />
        ///axis		       指定轴号（轴号范围说明）<para />
        ///dec	            减速度，保留，暂不起作用。 <para />
        ///返回值：错误代码 <para />
        ///注意：减速停止的减速度等于PCI400_set_profile_ex参数中的最大运行速度（Max_Vel）减去结束速度（stopspeed）的差除以减速时间（Tdec）
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_decel_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_decel_stop(UInt16 card,UInt16 axis,double dec);

        /// <summary>
        /// 功  能:    使指定轴立即停止，没有任何减速的过程 <para />
        ///参  数:card   控制卡卡号（通过拨码开关设置）<para />
        ///axis   指定轴号（轴号范围说明）<para />
        ///返回值:  错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_imd_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_imd_stop(UInt16 card,UInt16 axis);
        
        /// <summary>
        /// 功  能：使所有的运动轴紧急停止。 <para />
        ///参  数：cardid		   控制卡卡号（通过拨码开关设置）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_emg_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_emg_stop(UInt16 card) ;
		
        /// <summary>
        /// 功  能：单轴停止函数 <para />
        ///参  数：card          控制卡卡号（通过拨码开关设置） <para />
        ///iaxis          指定轴号（轴号范围说明）<para />
        ///Type        停止方式： <para />
        ///0－立即停止   1－减速停止 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_stop(UInt16 card, UInt16 axis, UInt16 Type) ;

        /// <summary>
        /// 功  能：使多卡上的所有轴同时停止运行。 <para />
        ///该函数在指定轴的CSTP端子上输出一个单脉冲的停止信号，如果有多个轴的CSTP信号相连接时则所有CSTP相连运动轴将同时停止运行。 </summary>
        ///参  数：card			   控制卡卡号（通过拨码开关设置） <para />
        ///axis		       指定轴号（轴号范围说明） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_simultaneous_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_simultaneous_stop(UInt16 card,UInt16 axis) ;

        /// <summary>
        ///位置设置和读取函数<para />
        ///功  能：读取指定轴的指令脉冲位置<para />
        ///参  数：card            控制卡卡号（通过拨码开关设置）<para />
        ///        axis			  指定轴号（轴号范围说明）<para />
        ///返回值：指定运动轴的命令脉冲数，单位：脉冲
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_get_position(UInt16 card,UInt16 axis);

        /// <summary>
        ///功  能：读取所有轴的指令脉冲位置 <para />
        ///参  数：card            控制卡卡号（通过拨码开关设置）<para />
        ///        position		  绝对位置值（以数组方式读取，例如数组的第0号元素 对应第0轴的指令位置）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_position_all", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 PCI400_get_position_all(UInt16 card,Int32[] position);

        /// <summary>
        ///功  能：设置指定轴的指令脉冲位置<para /> 
        ///参  数：card               控制卡卡号（通过拨码开关设置）<para />
        ///        axis			     指定轴号（轴号范围说明）<para />
        ///        current_position	 绝对位置值<para />
        ///返回值：错误代码<para />
        ///注意：在总线模式下需要进入CSP模式后PCI400_set_position才能生效（HY7800E）
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_set_position(UInt16 card,UInt16 axis,Int32 current_position);


        //状态检测函数

        /// <summary>
        /// 功  能：检测指定轴的运动状态，停止或是在运行中。<para />
        ///参  数：card		控制卡卡号（通过拨码开关设置）<para />
        ///axis	    指定轴号（轴号范围说明）<para />
        ///返回值：0表示指定轴正在运行，1表示指定轴已停止。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32   PCI400_check_done(UInt16 card,UInt16 axis) ;

        /// <summary>
        /// 功  能：检测所有轴的运动状态，停止或是在运行中。 <para />
        ///参  数：card		控制卡卡号（通过拨码开关设置）<para />
        ///返回值：0表示还有至少一个轴正在运行中，1表示所有轴已停止。<para />
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_check_done_all", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 PCI400_check_done_all(UInt16 card);

        /// <summary>
        /// 功  能：检测指定轴的运动状态并返回指定轴的停止原因 <para />
        ///参  数：card		控制卡卡号（通过拨码开关设置） <para />
        ///iaxis       指定轴号（轴号范围说明） <para />
        ///stopreason  停止原因   0—运动结束后正常停止 <para />
        ///                          1—EL（硬件限位信号）产生停止 <para />
        ///                          2—EMG（急停信号）产生停止 <para />
        ///4—ALM（报警信号）产生停止 <para />
        ///8—SOFTLIMITION（软限位）产生停止 <para />
        ///16—中途强制停止, 上层软件调用 <para />
        ///32—途要求暂停, 目前只有插补器使用 <para />
        ///64—回零时内部使用，检测到HOME时 <para />
        ///128—手轮时一段时间没有摇动，则停止 <para />
        ///256—未知原因 <para />
        ///返回值：0表示指定轴正在运行，1表示指定轴已停止。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_check_done_reason", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 PCI400_check_done_reason(UInt16 card, UInt16 axis, ref UInt16 stopreason);

        /// <summary>
        /// 功  能：读取指定轴有关运动信号的状态，包含指定轴的专用I/O状态。<para />
        ///参   数：  card              控制卡卡号（通过拨码开关设置）<para />
        ///axis			   指定轴号（轴号范围说明）<para />
        ///返回值：见表
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_axis_io_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_axis_io_status(UInt16 card,UInt16 axis);

        /// <summary>
        /// 功  能：停止原因查询<para />
        ///参  数：card			  控制卡卡号（通过拨码开关设置）<para />
        ///axis           指定轴号（轴号范围说明）<para />
        ///返回值： 停止原因     0—运动结束后正常停止<para />
        ///                1—EL（硬件限位信号）产生停止<para />
        ///                2—EMG（急停信号）产生停止<para />
        ///4—ALM（报警信号）产生停止<para />
        ///8—SOFTLIMITION（软限位）产生停止<para />
        ///16—中途强制停止, 上层软件调用<para />
        ///32—途要求暂停, 目前只有插补器使用<para />
        ///64—回零时内部使用，检测到HOME时<para />
        ///128—手轮时一段时间没有摇动，则停止<para />
        ///256—未知原因
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_stopreason", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt16  PCI400_get_stopreason(UInt16 card,UInt16 axis);

        //速度设置

        /// <summary>
        /// 功  能：读取指定轴的当前速度值（单位：脉冲/秒）<para />
        ///参  数：card			   控制卡卡号（通过拨码开关设置）<para />
        ///axis		       指定轴号（轴号范围说明）<para />
        ///返回值：指定轴的速度脉冲数
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_read_current_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double  PCI400_read_current_speed(UInt16 card, UInt16 axis);

        /// <summary>
        /// 读取指定轴的当前速度值（单位：脉冲/秒）
        /// </summary>
        /// <param name="card"> 控制卡卡号（通过拨码开关设置）</param>
        /// <param name="axis">指定轴号（轴号范围说明）</param>
        /// <returns>轴目标位置</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_aimposition", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 PCI400_get_aimposition(UInt16 card, UInt16 axis);

        /// <summary>
        /// 功  能：读取当前卡的插补速度（单位：脉冲/秒）<para />
        ///参  数：card	            控制卡卡号（通过拨码开关设置）<para />
        ///返回值：插补速度
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_read_vector_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double  PCI400_read_vector_speed(UInt16 card);
        
        /// <summary>
        /// 功  能：在线改变指定轴的当前运动速度。该函数只适用于单轴点位运动中的变速。<para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        ///axis				要设置的轴号（轴号范围说明）<para />
        ///Curr_Vel			新的运行速度（单位：脉冲/秒），不能设为负数。<para />
        ///返回值：错误代码<para />
        ///注意：使用该函数只能改变点位运动中当前运行速度的大小，不能改变速度的方向。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_change_pmove_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_change_pmove_speed(UInt16 card, UInt16 axis, double Curr_Vel);

        /// <summary>
        /// 功  能：在线改变指定轴的当前运动速度。该函数只适用于单轴运动中连续运动的变速。<para />
        ///参  数：card			   控制卡卡号（通过拨码开关设置）<para />
        ///axis			   指定轴号（轴号范围说明）<para />
        ///Curr_Vel		   新的运行速度（单位：脉冲/秒），不能设为负数。<para />
        ///返回值：错误代码<para />
        ///注意：使用该函数只能改变连续运动中当前运行速度的大小，不能改变速度的方向。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_change_vmove_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_change_vmove_speed(UInt16 card, UInt16 axis, double Curr_Vel);
		
        /// <summary>
        /// 功  能：单轴变速函数<para />
        ///参  数：card          控制卡卡号（通过拨码开关设置）<para />
        ///iaxis          指定轴号（轴号范围说明）<para />
        ///Curr_Vel     新的运行速度（单位：脉冲/秒），不能设为负数。<para />
        ///返回值：错误代码<para />
        ///注意：如果当前为点位运动，则执行点位在线变速，如果当前为速度运动，则执行速度运动在线变速。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_change_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_change_speed(UInt16 card, UInt16 axis, double Curr_Vel);

        /// <summary>
        ///功  能：设置指定轴速度曲线运动模式的起始速度、最大速度、结束速度、加速时间、减速时间<para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        ///        axis        指定轴号（轴号范围说明）<para />
        ///        Min_Vel     起始速度，单位：脉冲/秒 <para />
        ///        Max_Vel	   最大速度，单位：脉冲/秒<para />
        ///        stopspeed   结束速度，单位：脉冲/秒<para />
        ///        Tacc		   加速时间，单位：秒<para />
        ///        Tdec		   减速时间，单位：秒<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_profile_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_set_profile_ex(UInt16 card,UInt16 axis, double option, double Max_Vel, double stopvel, double acc, double dec);

        /// <summary>
        //功  能：读取指定轴速度曲线运动模式的起始速度、最大速度、结束速度、加速时间、减速时间<para />
        //参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        //axis			    指定轴号（轴号范围说明）<para />
        //Min_Vel	        起始速度，单位：脉冲/秒<para />
        //Max_Vel		    最大速度，单位：脉冲/秒<para />
        //stopspeed	        结束速度，单位：脉冲/秒<para />
        //Tacc		        加速时间，单位：秒<para />
        //Tdec		        减速时间，单位：秒<para />
        //返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_profile_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_get_profile_ex(UInt16 card,UInt16 axis, ref double option, ref double Max_Vel, ref double stopvel, ref double acc, ref double dec);

        /// <summary>
        /// 功  能：设置速度曲线运动模式的参数。<para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        ///axis		    指定轴号（轴号范围说明）<para />
        /// s_para          S形加速段占总加速段的比例：当参数设置为0时，为梯形速度曲线运动模式，当参数为0～1.0时，为S形速度曲线运动模式。<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_set_s_profile(UInt16 card,UInt16 axis, double s_para);

        /// <summary>
        /// 功  能：读取速度曲线运动模式的参数。<para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        ///axis		        指定轴号（轴号范围说明）<para />
        /// s_para          S形加速段占总加速段的比例：当参数设置为0时，为梯形速度曲线运动模式，当参数为0～1.0时，为S形速度曲线运动模式。<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_get_s_profile(UInt16 card,UInt16 axis, ref double s_para);

        /// <summary>
        /// 功  能：在单轴点位运动中改变目标位置。<para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        ///axis			指定轴号（轴号范围说明）<para />
        ///dist			绝对位置值<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_reset_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_reset_target_position(UInt16 card,UInt16 axis,Int32 dist);

        /// <summary>
        /// 功  能：在单轴点位运动中改变目标位置。<para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        ///axis			指定轴号（轴号范围说明）<para />
        ///dist			绝对位置值<para />
        ///runtimems    运动时间(单位：毫秒)
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_reset_target_position_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_reset_target_position_time(UInt16 card, UInt16 axis, Int32 dist, ref double runtimems);



        //单轴定长运动

        /// <summary>
        /// 功  能：使指定轴做点位运动。<para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        ///axis			指定轴号（轴号范围说明）<para />
        /// Dist		  （绝对/相对）位移值，单位：脉冲数<para />
        /// posi_mode	   位移模式设定：0表示相对位移，1表示绝对位移<para />
        /// 返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_pmove(UInt16 card,UInt16 axis,Int32 Dist,UInt16 posi_mode);

        /// <summary>
        /// 功  能：使指定轴做点位运动。<para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        ///axis			指定轴号（轴号范围说明）<para />
        /// Dist		  （绝对/相对）位移值，单位：脉冲数<para />
        /// posi_mode	   位移模式设定：0表示相对位移，1表示绝对位移<para />
        /// profiletimems  运动时间(单位：毫秒)
        /// 返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pmove_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pmove_time(UInt16 card, UInt16 axis, Int32 Dist, UInt16 posi_mode, ref double profiletimems);

        /// <summary>
        /// 功  能：设置指定轴点位运动参数。<para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        ///axis			指定轴号（轴号范围说明）<para />
        /// Dist		  （绝对/相对）位移值，单位：脉冲数<para />
        /// posi_mode	   位移模式设定：0表示相对位移，1表示绝对位移<para />
        /// vss            起始速度<para />
        /// vms            运行速度<para />
        /// ves            结束速度<para />
        /// accs           加速时间<para />
        /// decs           减速时间<para />
        /// 返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pmove_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pmove_profile(UInt16 card, UInt16 axis, Int32 Dist, UInt16 posi_mode, double vss, double vms, double ves, double accs, double decs);


        /// <summary>
        /// 功  能：设置指定轴点位运动参数。<para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        ///axis			指定轴号（轴号范围说明）<para />
        /// Dist		  （绝对/相对）位移值，单位：脉冲数<para />
        /// posi_mode	   位移模式设定：0表示相对位移，1表示绝对位移<para />
        /// vss            起始速度<para />
        /// vms            运行速度<para />
        /// ves            结束速度<para />
        /// accs           加速时间<para />
        /// decs           减速时间<para />
        /// Profiletimems  运动时间(单位：毫秒)<para />
        /// 返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pmove_profile_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pmove_profile_time(UInt16 card, UInt16 axis, Int32 Dist, UInt16 posi_mode, double vss, double vms, double ves, double accs, double decs, ref double Profiletimems);

        /// <summary>
        /// 功  能：使指定轴做点位运动。（软着陆）<para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        ///axis		    指定轴号（轴号范围说明）<para />
        /// Dist		  （绝对/相对）位移值，单位：脉冲数<para />
        /// posi_mode	    位移模式设定：0表示相对位移，1表示绝对位移<para />
        ///distlex        低速长度（单位：脉冲）<para />
        ///speedlow      低速速度（单位：脉冲/秒）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pmove_flex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pmove_flex(UInt16 card, UInt16 axis, Int32 Dist, UInt16 posi_mode, Int32 distflex, Int32 speedlow);

        /// <summary>
        /// 功  能：使指定轴做点位运动。（软着陆）<para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        ///axis		    指定轴号（轴号范围说明）<para />
        /// Dist		  （绝对/相对）位移值，单位：脉冲数<para />
        /// posi_mode	    位移模式设定：0表示相对位移，1表示绝对位移<para />
        ///distlex        低速长度（单位：脉冲）<para />
        ///speedlow      低速速度（单位：脉冲/秒）<para />
        ///profiletime   运动时间（单位：毫秒）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pmove_flex_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pmove_flex_time(UInt16 card, UInt16 axis, Int32 Dist, UInt16 posi_mode, Int32 distflex, Int32 speedlow, ref double profiletime);

        /// <summary>
        /// 功能:点位运动同步启动。能保证轴列表中的轴同时启动，但是不能保证同时停止。启动后的速度、加速度、减速度为单轴运动的速度和加速度。<para />
        ///参数： CardNo       控制卡卡号（通过拨码开关设置）<para />   
        ///iaxisnum      轴数<para />
        ///iaxislist       轴列表<para />
        ///iposlist        位置列表<para />
        ///mode         相对或绝对运动 <para />
        ///0－相对运动   1－绝对运动<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pmove_sync", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pmove_sync(UInt16 card, UInt16 axisnum,UInt16[] iaxislist,Int32[] iposlist, UInt16 mode);

        /// <summary>
        /// 计算运动整体时间
        /// </summary>
        /// <param name="card">控制卡卡号</param>
        /// <param name="plantype">0-梯形曲线，1-S型</param>
        /// <param name="vs">启动速度（单位：脉冲/秒）</param>
        /// <param name="vm">运行速度（单位：脉冲/秒）</param>
        /// <param name="ve">结束速度（单位：脉冲/秒）</param>
        /// <param name="ts">加速时间（单位：秒）</param>
        /// <param name="te">减速时间（单位：秒）</param>
        /// <param name="s_ratio"> S曲线比例（范围：0-1）</param>
        /// <param name="length">总运行距离（单位：脉冲）</param>
        /// <param name="disflex">缓降距离（单位：脉冲）</param>
        /// <param name="flexvm">缓降速度（单位：脉冲/秒）</param>
        /// <param name="timems">返回运动时间（单位：ms）</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_profile_calc_all", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_profile_calc_all(UInt16 card, UInt16 plantype, Int32 vs, Int32 vm, Int32 ve, double ts, double te, double s_ratio, Int32 length, Int32 disflex, Int32 flexvm, ref double timems);

        //单轴连续运动
        
        /// <summary>
        /// 功  能：使指定轴做连续运动<para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置）<para />
        ///axis		    指定轴号（轴号范围说明）<para />
        ///dir			    指定运动的方向，其中0表示负方向，1表示正方向<para />
        /// vel           vmove的速度，单位：脉冲/秒<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_vmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_vmove(UInt16 card,UInt16 axis,UInt16 dir, double vel);

        //跟随运动

        /// <summary>
        /// 功  能：轴跟随功能 <para />
        ///参  数：card：             控制卡卡号（通过拨码开关设置）<para />
        ///iaxismaster       主轴轴号（轴号范围说明） <para />
        ///iaxisslave	       从轴轴号（从轴跟随主轴运动）（轴号范围说明）<para />
        ///masterposstart	   跟随主轴的起始位置（绝对位置）<para />
        ///masterposend	   跟随主轴的终点位置（绝对位置）<para />
        ///muti             倍率 <para />
        ///ifreverse          是否反向跟随（0-不反向 1-反向） <para />
        ///ifencoder          是否跟随编码器位置：0-跟随指令位置  <para />
        ///1-跟随编码器位置  <para />  
        ///返回值：错误代码 <para />
        ///备注：如果需要结束轴跟随功能，调用立即停止（PCI400_imd_stop）或者减速停止（PCI400_decel_stop）指令停止从轴即可。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_follow_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_follow_move(UInt16 card, UInt16 iaxismaster, UInt16 iaxisslave, Int32 masterposstart, Int32 masterposend, UInt16 muti, UInt16 ifreverse, UInt16 ifencoder);

        /// <summary>
        /// 功  能：轴跟随功能 <para />
        ///参  数：card：             控制卡卡号（通过拨码开关设置）<para />
        ///iaxismaster       主轴轴号（轴号范围说明） <para />
        ///iaxisslave	       从轴轴号（从轴跟随主轴运动）（轴号范围说明）<para />
        ///masterposstart	   跟随主轴的起始位置（绝对位置）<para />
        ///masterposend	   跟随主轴的终点位置（绝对位置）<para />
        ///muti             倍率（分子） <para />
        ///muti_de          倍率（分母） <para />
        ///ifreverse          是否反向跟随（0-不反向 1-反向） <para />
        ///ifencoder          是否跟随编码器位置：0-跟随指令位置  <para />
        ///1-跟随编码器位置  <para />  
        ///返回值：错误代码 <para />
        ///备注：如果需要结束轴跟随功能，调用立即停止（PCI400_imd_stop）或者减速停止（PCI400_decel_stop）指令停止从轴即可。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_follow_move_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_follow_move_ratio(UInt16 card, UInt16 iaxismaster, UInt16 iaxisslave, Int32 masterposstart, Int32 masterposend, UInt16 muti, UInt16 muti_de, UInt16 ifreverse, UInt16 ifencoder);

        //线性插补

        /// <summary>
        /// 功  能：设置插补矢量运动的起始速度、运行速度、加速时间、减时间  <para />
        ///参  数：cardno：       控制卡卡号（通过拨码开关设置）  <para />
        ///s_para       起始速度（单位：脉冲/秒）  <para />
        ///Max_Vel		  运行速度（单位：脉冲/秒）  <para />
        ///Tacc		       加速时间（单位：秒）  <para />
        ///Tdec		       减速时间（单位：秒） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_vector_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_set_vector_profile(UInt16 cardno, double s_para, double Max_Vel, double acc, double dec );

        /// <summary>
        /// 功  能：读取插补矢量运动的起始速度、运行速度、加速时间、减时间  <para />
        ///参  数：cardno：       控制卡卡号（通过拨码开关设置）  <para />
        ///s_para       起始速度（单位：脉冲/秒）  <para />
        ///Max_Vel		  运行速度（单位：脉冲/秒）  <para />
        ///Tacc		       加速时间（单位：秒）  <para />
        ///Tdec		       减速时间（单位：秒） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_vector_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_get_vector_profile(UInt16 cardno, ref double s_para, ref double Max_Vel, ref double acc, ref double dec );

        /// <summary>
        /// 功  能：设置插补矢量运动的起始速度、运行速度、加速时间、减时间  <para />
        ///参  数：cardno：       控制卡卡号（通过拨码开关设置）  <para />
        ///CrdNum      插补器序号（0或1）
        ///s_para       起始速度（单位：脉冲/秒）  <para />
        ///Max_Vel		  运行速度（单位：脉冲/秒）  <para />
        ///Tacc		       加速时间（单位：秒）  <para />
        ///Tdec		       减速时间（单位：秒） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_vector_profile_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_set_vector_profile_muticoor(UInt16 card,UInt16 CrdNum, double s_para, double Max_Vel, double acc,double dec );

        /// <summary>
        /// 功  能：读取插补矢量运动的起始速度、运行速度、加速时间、减时间  <para />
        ///参  数：cardno：       控制卡卡号（通过拨码开关设置）  <para />
        ///CrdNum      插补器序号（0或1）
        ///s_para       起始速度（单位：脉冲/秒）  <para />
        ///Max_Vel		  运行速度（单位：脉冲/秒）  <para />
        ///Tacc		       加速时间（单位：秒）  <para />
        ///Tdec		       减速时间（单位：秒） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_vector_profile_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_get_vector_profile_muticoor(UInt16 cardno,UInt16 CrdNum, ref double s_para, ref double Max_Vel, ref double acc,ref double dec );

        /// <summary>
        /// 功  能：设置插补矢量运动的S形加速段占总加速段的比例 <para />
        ///参  数：cardno：       控制卡卡号（通过拨码开关设置）<para />
        ///CrdNum      插补器序号（0或1）<para />
        ///s_para        S形加速段占总加速段的比例：当参数设置为0时，为梯形速度曲线运动模式，当参数为0～1.0时，为S形速度曲线运动模式。<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_vector_s_profile_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_set_vector_s_profile_muticoor(UInt16 card, UInt16 CrdNum, double s_para);

        /// <summary>
        /// 功  能：读取插补矢量运动的S形加速段占总加速段的比例 <para />
        ///参  数：cardno：       控制卡卡号（通过拨码开关设置）<para />
        ///CrdNum      插补器序号（0或1）<para />
        ///s_para        S形加速段占总加速段的比例：当参数设置为0时，为梯形速度曲线运动模式，当参数为0～1.0时，为S形速度曲线运动模式。<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_vector_s_profile_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_get_vector_s_profile_muticoor(UInt16 card, UInt16 CrdNum, ref double s_para);
		
        /// <summary>
        /// 功  能：指定任意两轴做直线插补运动 <para />
        ///参  数：card				控制卡卡号（通过拨码开关设置）<para />
        ///axis1			指定两轴插补的第一轴（轴号范围说明）<para />
        ///Dist1			指定axis1的位移值，单位：脉冲数 <para />
        ///axis2			指定两轴插补的第二轴（轴号范围说明）<para />
        ///Dist2			指定axis2的位移值，单位：脉冲数 <para />
        ///synendspeed      插补结束速度（单位：脉冲/秒）<para />
        /// synspeed         插补速度（单位：脉冲/秒）<para />
        /// synacc           加速时间（单位：秒） <para />     
        /// posi_mode       位移方式（0表示相对位置，1表示绝对位置）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_line2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_line2(UInt16 card,UInt16 axis1,Int32 Dist1,UInt16 axis2,Int32 Dist2,Int32 endspeed,Int32 speed,Double acc,UInt16 posi_mode);

        /// <summary>
        /// 功  能：指定任意两轴做直线插补运动 <para />
        ///参  数：card				控制卡卡号（通过拨码开关设置）<para />
        ///coor           插补器序号（0或1）
        ///axis1			指定两轴插补的第一轴（轴号范围说明）<para />
        ///Dist1			指定axis1的位移值，单位：脉冲数 <para />
        ///axis2			指定两轴插补的第二轴（轴号范围说明）<para />
        ///Dist2			指定axis2的位移值，单位：脉冲数 <para />
        ///synendspeed      插补结束速度（单位：脉冲/秒）<para />
        /// synspeed         插补速度（单位：脉冲/秒）<para />
        /// synacc           加速时间（单位：秒） <para />     
        /// posi_mode       位移方式（0表示相对位置，1表示绝对位置）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_line2_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_line2_muticoor(UInt16 card,UInt16 CrdNum,UInt16 axis1,Int32 Dist1,UInt16 axis2,Int32 Dist2,Int32 endspeed,Int32 speed,Double acc,UInt16 posi_mode);

        /// <summary>
        /// 功  能：指定任意三轴做直线插补运动 <para />
        ///参  数：card				控制卡卡号（通过拨码开关设置）<para />
        ///*axis			轴号列表的指针 <para />
        ///Dist1			指定axis[0]轴的位移值，单位：脉冲数 <para />
        ///Dist2			指定axis[1]轴的位移值，单位：脉冲数 <para />
        ///Dist3			指定axis[2]轴的位移值，单位：脉冲数 <para />
        ///synendspeed      插补结束速度（单位：脉冲/秒） <para />
        ///synspeed         插补速度（单位：脉冲/秒）<para />
        /// synacc           加速时间（单位：秒）<para />
        /// posi_mode       位移方式（0表示相对位置，1表示绝对位置） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_line3", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_line3(UInt16 card, UInt16[] axis, Int32 Dist1, Int32 Dist2, Int32 Dist3, Int32 endspeed, Int32 speed, Double acc, UInt16 posi_mode);

        /// <summary>
        /// 功  能：指定任意三轴做直线插补运动 <para />
        ///参  数：card				控制卡卡号（通过拨码开关设置）<para />
        ///coor           插补器序号（0或1）
        ///*axis			轴号列表的指针 <para />
        ///Dist1			指定axis[0]轴的位移值，单位：脉冲数 <para />
        ///Dist2			指定axis[1]轴的位移值，单位：脉冲数 <para />
        ///Dist3			指定axis[2]轴的位移值，单位：脉冲数 <para />
        ///synendspeed      插补结束速度（单位：脉冲/秒） <para />
        ///synspeed         插补速度（单位：脉冲/秒）<para />
        /// synacc           加速时间（单位：秒）<para />
        /// posi_mode       位移方式（0表示相对位置，1表示绝对位置） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_line3_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_line3_muticoor(UInt16 card, UInt16 CrdNum, UInt16[] axis, Int32 Dist1, Int32 Dist2, Int32 Dist3, Int32 endspeed, Int32 speed, Double acc, UInt16 posi_mode);
		
        [DllImport("PCI400.dll", EntryPoint = "PCI400_line4", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_line4(UInt16 cardno, Int32 Dist1, Int32 Dist2, Int32 Dist3, Int32 Dist4, Int32 endspeed, Int32 speed, Double acc, UInt16 posi_mode);
		
        /// <summary>
        /// 功  能：多轴直线插补运动 <para />
        ///参  数：cardno			指定插补运动的控制卡卡号（通过拨码开关设置） <para />
        ///iaxisnum       参与插补轴数  <para /> 
        ///*iaxislist       插补轴号列表 <para />
        ///*poslist        位置列表 <para />
        ///synendspeed     插补结束速度（单位：脉冲/秒）<para />
        ///synspeed         插补速度（单位：脉冲/秒）<para />
        /// synacc           加速时间（单位：秒）<para />
        /// posi_mode       位移方式（0表示相对位置，1表示绝对位置）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_lineN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_lineN(UInt16 cardno, UInt16 iaxisnum, UInt16[] iaxislist, Int32[] target_pos, Int32 endspeed, Int32 speed, Double acc, UInt16 posi_mode);

        /// <summary>
        /// 功  能：多轴直线插补运动
        ///参  数：cardno			指定插补运动的控制卡卡号（通过拨码开关设置）<para />
        ///coor           插补器序号（0或1）<para />
        ///iaxisnum       参与插补轴数   <para />
        ///*iaxislist       插补轴号列表 <para />
        ///*poslist        位置列表 <para />
        ///synendspeed     插补结束速度（单位：脉冲/秒） <para />
        ///synspeed         插补速度（单位：脉冲/秒）<para />
        /// synacc           加速时间（单位：秒）<para />
        /// posi_mode       位移方式（0表示相对位置，1表示绝对位置）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_lineN_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_lineN_muticoor(UInt16 cardno, UInt16 CrdNum, UInt16 iaxisnum, UInt16[] iaxislist, Int32[] target_pos, Int32 endspeed, Int32 speed, Double acc, UInt16 posi_mode);
          
        
       //圆弧插补
        
        /// <summary>
        /// 功  能：指定任意的两轴以当前位置为起点，按指定的圆心、目标绝对位置和方向作圆弧插补运动（绝对位置）（两轴圆弧插补） <para />
        ///参  数： card			控制卡卡号（通过拨码开关设置） <para />
        ///*axis           轴号列表指针（轴号范围说明） <para />
        ///  *target_pos     目标绝对位置列表指针（单位：脉冲数）<para />
        ///  *cen_pos       圆心绝对位置列表指针（单位：脉冲数）<para />
        ///  arc_dir          圆弧方向（0表示顺时针，1表示逆时针）<para />
        ///  synendspeed     插补结束速度（单位：脉冲/秒）<para />
        ///  synspeed        插补速度（单位：脉冲/秒）<para />
        ///  synacc          加速时间（单位：秒）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_arc_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_arc_move(UInt16 card, UInt16[] axislist, Int32[] target_poslist, Int32[] cen_poslist, UInt16 arc_dir,Int32 endspeed,Int32 speed,Double acc);

        /// <summary>
        /// 功  能：指定任意的两轴以当前位置为起点，按指定的圆心、目标相对位置和方向作圆弧插补运动（相对位置）（两轴圆弧插补） <para />
        ///参  数： card			控制卡卡号（通过拨码开关设置） <para />
        ///*axis           轴号列表指针（轴号范围说明） <para />
        ///  *rel_pos        目标相对位置列表指针（单位：脉冲数） <para />
        ///  *rel_cen        圆心相对位置列表指针（单位：脉冲数） <para />
        ///  arc_dir          圆弧方向（0表示顺时针，1表示逆时针） <para />
        ///  synendspeed     插补结束速度（单位：脉冲/秒） <para />
        ///  synspeed        插补速度（单位：脉冲/秒） <para />
        ///  synacc          加速时间（单位：秒） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_rel_arc_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_rel_arc_move(UInt16 card, UInt16[] axislist, Int32[] rel_poslist, Int32[] rel_cenlist, UInt16 arc_dir,Int32 endspeed,Int32 speed,Double acc);

        /// <summary>
        /// 功  能：指定任意的两轴以当前位置为起点，按指定的圆心、目标绝对位置和方向作圆弧插补运动（绝对位置）（两轴圆弧插补） <para />
        ///参  数： card			控制卡卡号（通过拨码开关设置） <para />
        ///coor          插补器（范围：0或1）
        ///*axis           轴号列表指针（轴号范围说明） <para />
        ///  *target_pos     目标绝对位置列表指针（单位：脉冲数）<para />
        ///  *cen_pos       圆心绝对位置列表指针（单位：脉冲数）<para />
        ///  arc_dir          圆弧方向（0表示顺时针，1表示逆时针）<para />
        ///  synendspeed     插补结束速度（单位：脉冲/秒）<para />
        ///  synspeed        插补速度（单位：脉冲/秒）<para />
        ///  synacc          加速时间（单位：秒）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_arc_move_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_arc_move_muticoor(UInt16 card, UInt16 CrdNum, UInt16[] axislist, Int32[] target_poslist, Int32[] cen_poslist, UInt16 arc_dir, Int32 endspeed, Int32 speed, Double acc);

        /// <summary>
        /// 功  能：指定任意的两轴以当前位置为起点，按指定的圆心、目标相对位置和方向作圆弧插补运动（相对位置）（两轴圆弧插补） <para />
        ///参  数： card			控制卡卡号（通过拨码开关设置） <para />
        ///coor          插补器（范围：0或1）
        ///*axis           轴号列表指针（轴号范围说明） <para />
        ///  *rel_pos        目标相对位置列表指针（单位：脉冲数） <para />
        ///  *rel_cen        圆心相对位置列表指针（单位：脉冲数） <para />
        ///  arc_dir          圆弧方向（0表示顺时针，1表示逆时针） <para />
        ///  synendspeed     插补结束速度（单位：脉冲/秒） <para />
        ///  synspeed        插补速度（单位：脉冲/秒） <para />
        ///  synacc          加速时间（单位：秒） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_rel_arc_move_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_rel_arc_move_muticoor(UInt16 card, UInt16 CrdNum, UInt16[] axislist, Int32[] rel_poslist, Int32[] rel_cenlist, UInt16 arc_dir,Int32 endspeed,Int32 speed,Double acc);
	
		/// <summary>
        /// 功  能：指定任意的两轴设置起点（当前位置），中间点、终点的绝对位置坐标（通过一个圆上的三个点确定一个圆）作圆弧插补运动（两轴圆弧插补）<para />
        ///参  数： card			控制卡卡号（通过拨码开关设置） <para />
        ///*axis           轴号列表指针（轴号范围说明） <para />
        ///  *mid_pos       中间点绝对位置列表指针（单位：脉冲数） <para />
        ///  *cen_pos        终点绝对位置列表指针（单位：脉冲数） <para />
        ///  synendspeed     插补结束速度（单位：脉冲/秒） <para />
        ///  synspeed        插补速度（单位：脉冲/秒） <para />
        ///  synacc          加速时间（单位：秒） <para />
        ///返回值：错误代码 <para />
        ///注意：起点、中间点和终点不可以在同一直线上。
		/// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_arc_move3p", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_arc_move3p(UInt16 card, UInt16[] axislist, Int32[] mid_poslist, Int32[] target_poslist, Int32 endspeed, Int32 speed, Double acc);
		
        /// <summary>
        /// 功  能：指定任意的两轴设置起点（当前位置），中间点、终点的相对位置坐标（通过一个圆上的三个点确定一个圆）作圆弧插补运动（两轴圆弧插补） <para />
        ///参  数： card			控制卡卡号（通过拨码开关设置） <para />
        ///*axis           轴号列表指针（轴号范围说明） <para />
        ///  *mid_pos       中间点相对位置列表指针（单位：脉冲数） <para />
        ///  *cen_pos        终点相对位置列表指针（单位：脉冲数） <para />
        ///  synendspeed     插补结束速度（单位：脉冲/秒）<para />
        ///   synspeed        插补速度（单位：脉冲/秒） <para />
        ///  synacc          加速时间（单位：秒） <para />
        ///返回值：错误代码 <para />
        ///注意：起点、中间点和终点不可以在同一直线上。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_rel_arc_move3p", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_rel_arc_move3p(UInt16 card, UInt16[] axislist, Int32[] mid_poslist, Int32[] target_poslist, Int32 endspeed, Int32 speed, Double acc);

        /// <summary>
        /// 功  能：指定任意的两轴设置起点（当前位置），中间点、终点的绝对位置坐标（通过一个圆上的三个点确定一个圆）作圆弧插补运动（两轴圆弧插补）<para />
        ///参  数： card			控制卡卡号（通过拨码开关设置） <para />
        ///coor          插补器（范围：0或1）<para />
        ///*axis           轴号列表指针（轴号范围说明） <para />
        ///  *mid_pos       中间点绝对位置列表指针（单位：脉冲数） <para />
        ///  *cen_pos        终点绝对位置列表指针（单位：脉冲数） <para />
        ///  synendspeed     插补结束速度（单位：脉冲/秒） <para />
        ///  synspeed        插补速度（单位：脉冲/秒） <para />
        ///  synacc          加速时间（单位：秒） <para />
        ///返回值：错误代码 <para />
        ///注意：起点、中间点和终点不可以在同一直线上。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_arc_move3p_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_arc_move3p_muticoor(UInt16 card, UInt16 CrdNum, UInt16[] axislist, Int32[] mid_poslist, Int32[] target_poslist, Int32 endspeed, Int32 speed, Double acc);

        /// <summary>
        /// 功  能：指定任意的两轴设置起点（当前位置），中间点、终点的相对位置坐标（通过一个圆上的三个点确定一个圆）作圆弧插补运动（两轴圆弧插补） <para />
        ///参  数： card			控制卡卡号（通过拨码开关设置） <para />
        ///coor          插补器（范围：0或1）<para />
        ///*axis           轴号列表指针（轴号范围说明） <para />
        ///  *mid_pos       中间点相对位置列表指针（单位：脉冲数） <para />
        ///  *cen_pos        终点相对位置列表指针（单位：脉冲数） <para />
        ///  synendspeed     插补结束速度（单位：脉冲/秒）<para />
        ///   synspeed        插补速度（单位：脉冲/秒） <para />
        ///  synacc          加速时间（单位：秒） <para />
        ///返回值：错误代码 <para />
        ///注意：起点、中间点和终点不可以在同一直线上。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_rel_arc_move3p_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_rel_arc_move3p_muticoor(UInt16 card, UInt16 CrdNum, UInt16[] axislist, Int32[] mid_poslist, Int32[] target_poslist, Int32 endspeed, Int32 speed, Double acc);
      	  
        //椭圆插补

        /// <summary>
        /// 功  能：椭圆插补 <para />
        ///参  数： card			控制卡卡号（通过拨码开关设置） <para />
        ///*axis           轴号列表指针（轴号范围说明）<para />
        ///   *endpos        终点位置列表指针（单位：脉冲数）<para />
        ///  *cen_pos       中间点位置列表指针（单位：脉冲数）<para />
        ///  Adis            长轴（单位：脉冲数）<para />
        ///  Bdis            短轴（单位：脉冲数）<para />
        ///arc_dir           圆弧方向（0表示顺时针，1表示逆时针）<para />
        ///  synendspeed     插补结束速度（单位：脉冲/秒）<para />
        ///  synspeed        插补速度（单位：脉冲/秒）<para />
        ///  synacc          加速时间（单位：秒）<para />
        ///  posi_mode      位移方式（0表示相对位置，1表示绝对位置）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ellipse_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_ellipse_move(UInt16 card, UInt16[] axislist, Int32[] target_poslist, Int32[] cen_poslist, Int32 Adis, Int32 Bdis, UInt16 arc_dir,Int32 endspeed, Int32 speed, Double acc);

        /// <summary>
        /// 功  能：椭圆插补 <para />
        ///参  数： card			控制卡卡号（通过拨码开关设置） <para />
        ///coor          插补器（范围：0或1）             <para />
        ///*axis           轴号列表指针（轴号范围说明）<para />
        ///   *endpos        终点位置列表指针（单位：脉冲数）<para />
        ///  *cen_pos       中间点位置列表指针（单位：脉冲数）<para />
        ///  Adis            长轴（单位：脉冲数）<para />
        ///  Bdis            短轴（单位：脉冲数）<para />
        ///arc_dir           圆弧方向（0表示顺时针，1表示逆时针）<para />
        ///  synendspeed     插补结束速度（单位：脉冲/秒）<para />
        ///  synspeed        插补速度（单位：脉冲/秒）<para />
        ///  synacc          加速时间（单位：秒）<para />
        ///  posi_mode      位移方式（0表示相对位置，1表示绝对位置）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ellipse_move_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_ellipse_move_muticoor(UInt16 card, UInt16 CrdNum, UInt16[] axislist, Int32[] target_poslist, Int32[] cen_poslist, Int32 Adis, Int32 Bdis, UInt16 arc_dir, Int32 endspeed, Int32 speed, Double acc);
        
        //空间圆弧插补
        /// <summary>
        /// 功  能：空间三点圆弧插补 （三轴圆弧插补） <para />
        ///参  数： card			控制卡卡号（通过拨码开关设置） <para />
        ///*axis           轴号列表指针（轴号范围说明）<para />
        ///  *mid_pos       中间点列表指针（单位：脉冲数） <para />
        ///  *cen_pos        终点列表指针（单位：脉冲数）<para />
        ///  synendspeed     插补结束速度（单位：脉冲/秒）<para />
        ///  synspeed        插补速度（单位：脉冲/秒）<para />
        ///  synacc          加速时间（单位：秒）<para />
        ///  posi_mode      位移方式（0表示相对位置，1表示绝对位置）<para />
        /// 返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_arc3d_move3p", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_arc3d_move3p(UInt16 card, UInt16[] axislist, Int32[] mid_poslist, Int32[] target_poslist, Int32 endspeed, Int32 speed, Double acc, UInt16 posi_mode);

        //空间圆弧插补
        /// <summary>
        /// 功  能：空间三点圆弧插补 （三轴圆弧插补） <para />
        ///参  数： card			控制卡卡号（通过拨码开关设置） <para />
        ///CrdNum          插补器（范围：0或1） <para />
        ///axislist           轴号列表指针（轴号范围说明）<para />
        ///  *mid_pos       中间点列表指针（单位：脉冲数） <para />
        ///  *cen_pos        终点列表指针（单位：脉冲数）<para />
        ///  synendspeed     插补结束速度（单位：脉冲/秒）<para />
        ///  synspeed        插补速度（单位：脉冲/秒）<para />
        ///  synacc          加速时间（单位：秒）<para />
        ///  posi_mode      位移方式（0表示相对位置，1表示绝对位置）<para />
        /// 返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_arc3d_move3p_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_arc3d_move3p_muticoor(UInt16 card, UInt16 CrdNum, UInt16[] axislist, Int32[] mid_poslist, Int32[] target_poslist, Int32 endspeed, Int32 speed, Double acc, UInt16 posi_mode);
		
        //连续插补

        /// <summary>
        /// 功  能：设置连续插补参数 <para />
        ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
        /// conti_startsp    起始速度（单位：脉冲/秒）<para />
        /// conti_synspeed  最大速度（单位：脉冲/秒）<para />
        /// conti_synacc    加速时间（单位：秒）<para />
        ///mineventime    削尖峰时间（单位：秒）<para />
        ///返回值：错误代码 <para />
        ///注：在进行连续插补运动时请一定调用该函数配置连续插补运动的运动参数，否则会引起连续插补运动不起作用。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_set_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_set_mode(UInt16 card, Double vs, Double speed, Double acc, Double mineventime);

        /// <summary>
        /// 功  能：读取连续插补参数 <para />
        ///参  数： card             控制卡卡号（通过拨码开关设置） <para />
        ///  *conti_startsp    起始速度（单位：脉冲/秒）<para />
        ///  *conti_synspeed  最大速度（单位：脉冲/秒）<para />
        ///  *conti_synacc    加速时间（单位：秒）<para />
        ///*mineventime    削尖峰时间（单位：秒）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_get_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_get_mode(UInt16 card, ref Double vs, ref Double speed, ref Double acc, ref  Double mineventime);

        /// <summary>
        /// 功  能：速度前瞻初始化 <para />
        /// 参  数： card               控制卡卡号（通过拨码开关设置）<para />
        ///  LookAheadNum   前瞻段数 <para />
        ///  corner_time        拐角时间（单位：秒） <para />
        ///  corner_accmax 拐角最大加速度（脉冲/秒2）注：此处是加速度不是加速时间 <para />
        ///注意：拐角速度=拐角时间乘以拐角最大加速度 <para />
        ///返回值： 错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_lookahead_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_lookahead_init(UInt16 card, UInt16 looknum, Double corner_time, Double corner_accmax);

        /// <summary>
        /// 功 能：插补缓冲区IO<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///IoMask 掩码（如果掩码为0，不进行任何操作，掩码对应的位为1，将制定输出口置为IoValue相应位的值）<para />
        ///IoValue IO 值（把对应位置0：打开输出口，把对应位置1：关闭输出口）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_io", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_io(UInt16 card, Int32 IoMask, Int32 IoValue);

        /// <summary>
        /// 功 能：查询插补IO剩余缓冲大小（带插补器参数）<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///CrdNum 插补器序号（范围：0 或 1）<para />
        ///返回值：插补IO剩余缓冲大小（最大为256段）
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_io_action_remained", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_io_action_remained(UInt16 card, UInt16 CrdNum);

        /// <summary>
        /// 功 能：清除插补IO缓冲（带插补器参数） <para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///CrdNum 插补器序号（范围：0 或 1）<para />
        ///Io_Mask 保留<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_clear_io_action", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_clear_io_action(UInt16 card, UInt16 CrdNum, UInt32 Io_Mask);

        /// <summary>
        /// 功 能：连续插补中相对于轨迹段起点IO滞后输出（带插补器参数） <para />
        ///参 数： card 控制卡卡号（通过拨码开关设置） <para />
        ///CrdNum 插补器序号（范围：0 或 1）<para />
        ///bitno 指定输出口序号（范围：0-23）<para />
        ///on_off 0-打开输出口 1-关闭输出口<para />
        ///delay_value 延后的距离或者时间<para />
        ///距离：（单位：脉冲）（超出下段轨迹距离则在下段轨迹终点执行）<para />
        ///时间：（单位：毫秒）<para />
        ///delay_mode 延时模式（1-时间 2-距离）<para />
        ///ReverseTime 翻转时间（单位：毫秒）（设为0则不翻转）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_delay_outbit_to_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_delay_outbit_to_start(UInt16 card, UInt16 CrdNum, UInt16 bitno, UInt16 on_off, Double delay_value, UInt16 delay_mode, Double ReverseTime);

        /// <summary>
        /// 功 能：连续插补中相对于轨迹段终点IO滞后输出（带插补器参数）<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///CrdNum 插补器序号（范围：0 或 1）<para />
        ///bitno 指定输出口序号（范围：0-23）<para />
        ///on_off 0-打开输出口 1-关闭输出口<para />
        ///delay_value 延后时间（单位：毫秒）<para />
        ///ReverseTime 翻转时间（单位：毫秒）（设为0则不翻转）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_delay_outbit_to_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_delay_outbit_to_stop(UInt16 card, UInt16 CrdNum, UInt16 bitno, UInt16 on_off, Double delay_time, Double ReverseTime);

        /// <summary>
        /// 功 能：连续插补中相对于轨迹段终点IO提前输出（带插补器参数）<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///CrdNum 插补器序号（范围：0 或 1）<para />
        ///bitno 指定输出口序号（范围：0-23）<para />
        ///on_off 0-打开输出口 1-关闭输出口<para />
        ///ahead_value 提前的距离或者时间<para />
        ///距离：（单位：脉冲）<para />
        ///时间：（单位：毫秒）<para />
        ///ahead_mode 延时模式（1-时间 2-距离）<para />
        ///ReverseTime 翻转时间（单位：毫秒）（设为0则不翻转）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_ahead_outbit_to_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_ahead_outbit_to_stop(UInt16 card, UInt16 CrdNum, UInt16 bitno, UInt16 on_off, Double ahead_value, UInt16 ahead_mode, Double ReverseTime);
		
        /// <summary>
        /// 功 能：在插补缓冲中添加PWM输出<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///channel 通道号：0–3 （对应OUT12-15）<para />
        ///duty 占空比 范围（0～100），0是常开，100是常闭<para />
        ///freq 频率（单位：赫兹）（范围：1-5000）<para />
        ///timems 输出时间（单位：毫秒）（范围：0-65535）<para />
        ///（设为0则关闭PWM，设为65535则一直输出PWM）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_pwm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_pwm(UInt16 card, UInt16 channel,UInt16 duty,UInt16 freq,UInt16 timems);

        /// <summary>
        /// 插补缓冲区延时
        /// </summary>
        /// <param name="card">card 控制卡卡号（通过拨码开关设置）</param>
        /// <param name="delaytime">延时时间（单位：ms）</param>
        /// <returns>错误代码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_delay(UInt16 card, UInt16 delaytime);

        /// <summary>
        /// 插补缓冲区延时
        /// </summary>
        /// <param name="card">card 控制卡卡号（通过拨码开关设置）</param>
        /// <param name="CrdNum">插补器序号（范围：0或1）</param>
        /// <param name="delaytime">延时时间（单位：ms）</param>
        /// <param name="imark">mark序号</param>
        /// <returns>错误代码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_extern_delay_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_extern_delay_muticoor(UInt16 card, UInt16 CrdNum, UInt16 delaytime, int imark);

        /// <summary>
        /// 功 能：刀向跟随功能（后续必须紧跟插补运动指令）<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///axisNum 轴数<para />
        ///*piaxisListw 插补轴号列表<para />
        ///*pPosList 位置列表（绝对位置坐标）<para />
        ///返回值： 错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_gear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_gear(UInt16 card, UInt16 axisNum, UInt16[] iaxilist, Int32[] poslist);

        /// <summary>
        /// 功 能：配置插补运动拐角圆弧过渡（带插补器参数）<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///CrdNum 插补器序号（范围：0 或 1）<para />
        ///radius 圆弧半径（单位：脉冲）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_set_corner_arc_radius_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_set_corner_arc_radius_muticoor(UInt16 card, UInt16 CrdNum, double radius);

        /// <summary>
        /// 直线插补点位运动功能（带插补器参数）
        /// </summary>
        /// <param name="card">控制卡卡号（通过拨码开关设置）</param>
        /// <param name="CrdNum">插补器序号（范围：0或1）</param>
        /// <param name="iaxis">指定轴号（轴号范围说明）</param>
        /// <param name="ipos">点位运动目标位置</param>
        /// <param name="ifabs">位移模式设定：0表示相对位移，1表示绝对位移</param>
        /// <param name="iwaitdone">插补点位运动的模式 ：0-表示启动点位运动下一段不等待 1-等待当前点位运动结束运行下一段</param>
        /// <param name="iMark">插补段序号</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_pmove_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_pmove_muticoor(UInt16 card, UInt16 CrdNum, UInt16 iaxis, Int32 ipos, UInt16 ifabs, UInt16 iwaitdone, UInt16 iMark);

        /// <summary>
        /// 功 能：连续直线插补<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///axisNum 轴数<para />
        ///*piaxisList 连续插补轴号列表（轴号范围说明）<para />
        ///*pPosList 位置列表<para />
        ///synspeed 插补速度（单位：脉冲/秒）<para />
        ///synacc 加速时间（单位：秒）<para />
        ///synendvel 插补结束速度（单位：脉冲/秒）<para />
        ///posi_mode 位移方式（0表示相对位置，1表示绝对位置）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_lines", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_lines(UInt16 card,UInt16 axisNum, UInt16[] iaxilist, Int32[] poslist, Double speed, Double acc, Double ve, UInt16 posmode);
        
        /// <summary>
        /// 功 能：连续两轴圆弧插补 <para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///*axis 轴号列表指针（轴号范围说明）<para />
        ///*rel_pos 目标相对位置列表指针（单位：脉冲数）<para />
        ///*rel_cen 圆心相对位置列表指针（单位：脉冲数）<para />
        ///arc_dir 圆弧方向（0表示顺时针，1表示逆时针）<para />
        ///synspeed 插补速度（单位：脉冲/秒）<para />
        ///synacc 加速时间（单位：秒）<para />
        ///synendvel 插补结束速度（单位：脉冲/秒）<para />
        ///posi_mode 位移方式（0表示相对位置，1表示绝对位置）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_arc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_arc(UInt16 card, UInt16[] iaxilist, Int32[] poslist, Int32[] cenposlist, UInt16 arcdir, Double speed, Double acc, Double ve, UInt16 posmode);

        /// <summary>
        /// 功 能：指定任意的两轴设置起点（当前位置），中间点、终点的坐标（通过一个圆上的三个点确定一个圆）作连续两轴圆弧插补运动<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///*axis 轴号列表指针（轴号范围说明）<para />
        ///*rel_midpos 中间点坐标列表指针（单位：脉冲数）<para />
        ///*rel_endpos 终点坐标列表指针（单位：脉冲数）<para />
        ///synspeed 插补速度（单位：脉冲/秒）<para />
        ///synacc 加速时间（单位：秒）<para />
        ///synendvel 插补结束速度（单位：脉冲/秒）<para />
        ///posi_mode 位移方式（0表示相对位置，1表示绝对位置）<para />
        ///返回值：错误代码<para />
        ///注意：起点、中间点和终点不可以在同一直线上。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_arc_3p", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_arc_3p(UInt16 card, UInt16[] iaxilist, Int32[] rel_midposlist, Int32[] rel_endposlist, Double synspeed, Double synacc, Double synendvel, UInt16 posmode);

        /// <summary>
        /// 功 能：连续空间三点圆弧插补（三轴圆弧插补）<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///*axis 轴号列表指针（轴号范围说明）<para />
        ///*mid_pos 目标列表指针（单位：脉冲数） <para />    
        ///*end_pos 圆心列表指针（单位：脉冲数）<para />
        ///synspeed 插补速度（单位：脉冲/秒）<para />
        ///synacc 加速时间（单位：秒）<para />
        ///synendvel 插补结束速度（单位：脉冲/秒）<para />
        ///posi_mode 位移方式（0表示相对位置，1表示绝对位置）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_arc3d", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_arc3d(UInt16 card, UInt16[] iaxilist, Int32[] rel_midposlist, Int32[] rel_endposlist, Double synspeed, Double synacc, Double synendvel, UInt16 posmode);

        /// <summary>
        /// 功 能：连续直线插补（imark-指定该段mark标志）<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///axisNum 轴数<para />
        ///*piaxisListw 连续插补轴号列表（轴号范围说明）<para />
        ///*pPosList 位置列表<para />
        ///synspeed 插补速度（单位：脉冲/秒）<para />
        ///synacc 加速时间（单位：秒）<para />
        ///synendvel 插补结束速度（单位：脉冲/秒）<para />
        ///posi_mode 位移方式（0表示相对位置，1表示绝对位置）<para />
        ///imark imark 指定该段mark标志<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_extern_lines", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_extern_lines(UInt16 card, UInt16 axisNum, UInt16[] iaxilist, Int32[] poslist, Double speed, Double acc, Double ve, UInt16 posmode, Int32 imark);
              
        /// <summary>
        /// 功 能：连续两轴圆弧插补（imark-指定该段mark标志）<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///*axis 轴号列表指针（轴号范围说明）<para />
        ///*rel_pos 目标位置列表指针（单位：脉冲数）<para />
        ///*rel_cen 圆心位置列表指针（单位：脉冲数）<para />
        ///arc_dir 圆弧方向（0表示顺时针，1表示逆时针）<para />
        ///synspeed 插补速度（单位：脉冲/秒）<para />
        ///synacc 加速时间（单位：秒）<para />
        ///synendvel 插补结束速度（单位：脉冲/秒）<para />
        ///posi_mode 位移方式（0表示相对位置，1表示绝对位置）<para />
        ///imark imark指定该段mark标志<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_extern_arc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_extern_arc(UInt16 card, UInt16[] iaxilist, Int32[] poslist, Int32[] cenposlist, UInt16 arcdir, Double speed, Double acc, Double ve, UInt16 posmode, Int32 imark);

        /// <summary>
        /// 功 能：连续三点圆弧插补（imark-指定该段mark标志）（两轴圆弧插补）<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///*axis 轴号列表指针（轴号范围说明）<para />
        ///*rel_midepos 中间位置列表指针（单位：脉冲数）<para />
        ///*rel_endpos 目标位置列表指针（单位：脉冲数）<para />
        ///synspeed 插补速度（单位：脉冲/秒）<para />
        ///synacc 加速时间（单位：秒）<para />
        ///synendvel 插补结束速度（单位：脉冲/秒）<para />
        ///posi_mode 位移方式（0表示相对位置，1表示绝对位置）<para />
        ///imark imark指定该段mark标志<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_extern_arc_3p", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_extern_arc_3p(UInt16 card, UInt16[] iaxilist, Int32[] rel_midposlist, Int32[] rel_endposlist, Double synspeed, Double synacc, Double synendvel, UInt16 posmode, Int32 imark);

        /// <summary>
        /// 功 能：打开插补缓冲区<para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///axisNum 轴数（轴号范围说明）<para />
        ///*piaxisList 连续插补轴号列表<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_open_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_open_list(UInt16 card,UInt16 axisNum, UInt16[] iaxilist);

        /// <summary>
        /// 功 能：关闭插补缓冲区 <para />
        ///参 数： card 控制卡卡号（通过拨码开关设置） <para />
        ///返回值：错误代码 <para />
        ///注：如果在插补运动结束后不调用该函数，会导致PCI400_check_done 函数返回值一直为0，即检测到指定轴仍在运行之中。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_close_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_close_list(UInt16 card);
       
        /// <summary>
        /// 功 能：启动插补运动 <para />
        ///参 数： card 控制卡卡号（通过拨码开关设置） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_start_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_start_list(UInt16 card);

        /// <summary>
        /// 功 能：插补暂停 <para />
        ///参 数： card 控制卡卡号（通过拨码开关设置） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_pause_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_pause_list(UInt16 card);

        /// <summary>
        /// 功 能：插补减速停止 <para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_decel_stop_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_decel_stop_list(UInt16 card);

        /// <summary>
        /// 功 能：插补立即停止 <para />
        ///参 数： card 控制卡卡号（通过拨码开关设置） <para />
        ///返回值：错误代码 <para />
        ///注意：可以调用该函数清除连续插补缓冲
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_sudden_stop_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_sudden_stop_list(UInt16 card);

        /// <summary>
        /// 功 能：读取连续插补剩余缓冲段数  <para />
        ///参 数： card 控制卡卡号（通过拨码开关设置）  <para />
        ///返回值：连续插补剩余缓冲段数
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_check_remain_space", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_check_remain_space(UInt16 card);

        /// <summary>    
        /// 功 能：将前瞻缓冲区数据压入控制卡
        ///参 数： card 控制卡卡号（通过拨码开关设置）
        ///mode 阻塞方式（0表示非阻塞方式，1表示阻塞方式）
        ///返回值：错误代码
        ///注：如果缓冲区满，需多次压入。
        ///编程建议：在线程中使用该函数建议用阻塞方式，在按钮的消息响应函数中使用该函数建议用非阻塞方式。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_pushdata", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_pushdata(UInt16 card,UInt16 mode);
		
         //多插补器连续插补
         /// <summary>
         /// 功  能：设置连续插补参数 <para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
         ///CrdNum      插补器序号（范围：0或1）
         /// conti_startsp    起始速度（单位：脉冲/秒）<para />
         /// conti_synspeed  最大速度（单位：脉冲/秒）<para />
         /// conti_synacc    加速时间（单位：秒）<para />
         ///mineventime    削尖峰时间（单位：秒）<para />
         ///返回值：错误代码 <para />
         ///注：在进行连续插补运动时请一定调用该函数配置连续插补运动的运动参数，否则会引起连续插补运动不起作用。
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_set_mode_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_set_mode_muticoor(UInt16 card, UInt16 CrdNum, Double vs, Double speed, Double acc, Double mineventime);

         /// <summary>
         /// 功  能：读取连续插补参数 <para />
         ///参  数： card             控制卡卡号（通过拨码开关设置） <para />
         ///CrdNum      插补器序号（范围：0或1）
         ///  *conti_startsp    起始速度（单位：脉冲/秒）<para />
         ///  *conti_synspeed  最大速度（单位：脉冲/秒）<para />
         ///  *conti_synacc    加速时间（单位：秒）<para />
         ///*mineventime    削尖峰时间（单位：秒）<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_get_mode_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_get_mode_muticoor(UInt16 card, UInt16 CrdNum, ref Double vs, ref Double speed, ref Double acc, ref  Double mineventime);

         /// <summary>
         /// 功  能：速度前瞻初始化 <para />
         /// 参  数： card               控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）
         ///  LookAheadNum   前瞻段数 <para />
         ///  corner_time        拐角时间（单位：秒） <para />
         ///  corner_accmax 拐角最大加速度（脉冲/秒2）注：此处是加速度不是加速时间 <para />
         ///注意：拐角速度=拐角时间乘以拐角最大加速度 <para />
         ///返回值： 错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_lookahead_init_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_lookahead_init_muticoor(UInt16 card, UInt16 CrdNum, UInt16 looknum, Double corner_time, Double corner_accmax);

         /// <summary>
         /// 功 能：插补缓冲区IO（带插补器参数）<para />
         ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
         ///CrdNum 插补器序号（范围：0 或 1）<para />
         ///IoMask 掩码（如果掩码对应的位为0，不进行任何操作，掩码对应的位为1，将指定输出口置为IoValue相应位的值）<para />
         ///IoValue IO值（把对应位置0：打开输出口，把对应位置1：关闭输出口）<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_io_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_io_muticoor(UInt16 card, UInt16 CrdNum,Int32 IoMask, Int32 IoValue);
		
         /// <summary>
         /// 功  能：在插补缓冲中添加PWM输出（带插补器参数）<para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
         ///CrdNum       插补器序号（范围：0或1）<para />
         ///channel         通道号：0–3 （对应OUT12-15）<para />
         ///duty            占空比 范围（0～100），0是常开，100是常闭<para />
         ///freq             频率（单位：赫兹）（范围：1-5000）<para />
         ///timems          输出时间（单位：毫秒）（范围：0-65535）（设为0则关闭PWM，设为65535则一直输出PWM）<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_pwm_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_pwm_muticoor(UInt16 card, UInt16 CrdNum, UInt16 channel, UInt16 duty, UInt16 freq, UInt16 timems);

         /// <summary>
         /// 功 能：连续插补等待 IO 输入信号再向下运行 <para />
         ///参 数： card 控制卡卡号（通过拨码开关设置 <para />
         ///CrdNum 插补器序号（范围： 0 或 1) <para />
         ///IoNum 输入口序号 <para />
         ///IoValue 电平状态 <para />
         ///Timems 超时时间（单位：ms） <para />
         ///iMark imark指定该段mark标志 <para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_wait_input_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_wait_input_muticoor(UInt16 card, UInt16 CrdNum, UInt16 IoNum, UInt16 IoValue, UInt32 Timems, Int32 iMark);

         /// <summary>
         /// 功  能：缓冲区延时（带插补器参数）<para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
         ///CrdNum      插补器序号（范围：0或1）<para />
         ///delaytime       延时时间（单位：毫秒）<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_delay_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_delay_muticoor(UInt16 card, UInt16 CrdNum, UInt16 delaytime);

         /// <summary>
         /// 功  能：刀向跟随功能（后续必须紧跟插补运动指令）<para />
         ///参  数：  card              控制卡卡号（通过拨码开关设置）<para />
         ///  CrdNum      插补器序号（范围：0或1）<para />
         ///axisNum         轴数<para />
         ///  *piaxisListw      插补轴号列表 <para />
         ///  *pPosList         位置列表（绝对位置坐标）<para />
         ///返回值： 错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_gear_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_gear_muticoor(UInt16 card, UInt16 CrdNum, UInt16 axisNum, UInt16[] iaxilist, Int32[] poslist);

         /// <summary>
         /// 功  能：连续直线插补（带插补器参数）
         ///参  数： card             控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///axisNum         轴数<para />
         /// *piaxisList       连续插补轴号列表（轴号范围说明）<para />
         /// *pPosList        位置列表<para />
         ///  synspeed         插补速度（单位：脉冲/秒）<para />
         /// synacc           加速时间（单位：秒）<para />
         /// synendvel        插补结束速度（单位：脉冲/秒）<para />
         /// posi_mode        位移方式（0表示相对位置，1表示绝对位置）<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_lines_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_lines_muticoor(UInt16 card, UInt16 CrdNum, UInt16 axisNum, UInt16[] iaxilist, Int32[] poslist, Double speed, Double acc, Double ve, UInt16 posmode);
        
         /// <summary>
         /// 功  能：连续两轴圆弧插补（带插补器参数）<para />
         ///参  数： card             控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///*axis            轴号列表指针（轴号范围说明）<para />
         /// *rel_pos         目标相对位置列表指针（单位：脉冲数）<para />
         /// *rel_cen         圆心相对位置列表指针（单位：脉冲数）<para />
         /// arc_dir           圆弧方向（0表示顺时针，1表示逆时针）<para />
         /// synspeed         插补速度（单位：脉冲/秒）<para />
         /// synacc           加速时间（单位：秒）<para />
         /// synendvel        插补结束速度（单位：脉冲/秒）<para />
         /// posi_mode       位移方式（0表示相对位置，1表示绝对位置）<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_arc_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_arc_muticoor(UInt16 card, UInt16 CrdNum, UInt16[] iaxilist, Int32[] poslist, Int32[] cenposlist, UInt16 arcdir, Double speed, Double acc, Double ve, UInt16 posmode);

         /// <summary>
         /// 功  能：指定任意的两轴设置起点（当前位置），中间点、终点的坐标（通过一个圆上的三个点确定一个圆）作连续两轴圆弧插补运动（带插补器参数）<para />
         ///参  数： card             控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///*axis            轴号列表指针（轴号范围说明）<para />
         /// *rel_midpos     中间点坐标列表指针（单位：脉冲数）<para />
         /// *rel_endpos      终点坐标列表指针（单位：脉冲数）<para />
         /// synspeed         插补速度（单位：脉冲/秒）<para />
         ///  synacc           加速时间（单位：秒）<para />
         /// synendvel        插补结束速度（单位：脉冲/秒）<para />
         /// posi_mode       位移方式（0表示相对位置，1表示绝对位置）<para />
         ///返回值：错误代码<para />
         ///注意：起点、中间点和终点不可以在同一直线上。
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_arc_3p_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_arc_3p_muticoor(UInt16 card, UInt16 CrdNum, UInt16[] iaxilist, Int32[] rel_midposlist, Int32[] rel_endposlist, Double synspeed, Double synacc, Double synendvel, UInt16 posmode);

         /// <summary>
         /// 功  能：连续空间三点圆弧插补（带插补器参数）（三轴圆弧插补）<para />
         ///参  数： card             控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///*axis            轴号列表指针（轴号范围说明）<para />
         // *mid_pos         目标列表指针（单位：脉冲数）<para />
         // *end_pos        圆心列表指针（单位：脉冲数）<para />
         // synspeed         插补速度（单位：脉冲/秒）<para />
         // synacc           加速时间（单位：秒）<para />
         // synendvel        插补结束速度（单位：脉冲/秒）<para />
         // posi_mode       位移方式（0表示相对位置，1表示绝对位置）<para />
         //返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_arc3d_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_arc3d_muticoor(UInt16 card, UInt16 CrdNum, UInt16[] iaxilist, Int32[] rel_midposlist, Int32[] rel_endposlist, Double synspeed, Double synacc, Double synendvel, UInt16 posmode);

         /// <summary>
         /// 功  能：连续直线插补（imark-指定该段mark标志）（带插补器参数）<para />
         ///参  数： card             控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///axisNum         轴数 <para />
         /// *piaxisListw     连续插补轴号列表（轴号范围说明）<para />
         /// *pPosList        位置列表 <para />
         /// synspeed         插补速度（单位：脉冲/秒） <para />
         /// synacc           加速时间（单位：秒）<para />
         ///  synendvel        插补结束速度（单位：脉冲/秒）<para />
         /// posi_mode       位移方式（0表示相对位置，1表示绝对位置）<para />
         /// imark            imark指定该段mark标志 <para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_extern_lines_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_extern_lines_muticoor(UInt16 card, UInt16 CrdNum, UInt16 axisNum, UInt16[] iaxilist, Int32[] poslist, Double speed, Double acc, Double ve, UInt16 posmode, Int32 imark);
         
         /// <summary>
         /// 功  能：连续两轴圆弧插补（imark-指定该段mark标志）（带插补器参数）<para />
         ///参  数： card             控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///*axis            轴号列表指针（轴号范围说明）<para />
         /// *rel_pos         目标位置列表指针（单位：脉冲数）<para />
         /// *rel_cen         圆心位置列表指针（单位：脉冲数）<para />
         /// arc_dir           圆弧方向（0表示顺时针，1表示逆时针）<para />
         /// synspeed         插补速度（单位：脉冲/秒）<para />
         /// synacc           加速时间（单位：秒）<para />
         /// synendvel        插补结束速度（单位：脉冲/秒）<para />
         /// posi_mode       位移方式（0表示相对位置，1表示绝对位置）<para />
         ///imark            imark指定该段mark标志 <para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_extern_arc_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_extern_arc_muticoor(UInt16 card, UInt16 CrdNum, UInt16[] iaxilist, Int32[] poslist, Int32[] cenposlist, UInt16 arcdir, Double speed, Double acc, Double ve, UInt16 posmode, Int32 imark);

         /// <summary>
         /// 功  能：连续三点圆弧插补（imark-指定该段mark标志）（带插补器参数）（两轴圆弧插补）<para />
         ///参  数： card             控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///*axis            轴号列表指针（轴号范围说明）<para />
         /// *rel_midepos    中间位置列表指针（单位：脉冲数）<para />
         ///  *rel_endpos      目标位置列表指针（单位：脉冲数）<para />
         /// synspeed         插补速度（单位：脉冲/秒）<para />
         /// synacc           加速时间（单位：秒）<para />
         /// synendvel        插补结束速度（单位：脉冲/秒）<para />
         /// posi_mode       位移方式（0表示相对位置，1表示绝对位置）<para />
         ///imark            imark指定该段mark标志<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_extern_arc_3p_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_extern_arc_3p_muticoor(UInt16 card, UInt16 CrdNum, UInt16[] iaxilist, Int32[] rel_midposlist, Int32[] rel_endposlist, Double synspeed, Double synacc, Double synendvel, UInt16 posmode, Int32 imark);

         /// <summary>
         /// 功 能：连续两轴圆弧插补分段均匀打点 <para />
         ///参 数： card 控制卡卡号（通过拨码开关设置） <para />
         ///CrdNum  插补器序号（范围： 0 或 1) <para />
         ///axis 轴号列表指针 <para />
         ///rel_pos 目标位置列表指针 <para />
         ///rel_cen 圆心位置列表指针 <para />
         ///arc_dir 圆弧方向，0：顺时针，1：逆时针 <para />
         ///synspeed 插补速度（单位：脉冲/秒）<para />
         ///synacc 加速时间（单位：秒）<para />
         ///synendvel 插补结束速度（单位：脉冲/秒）<para />
         ///ipos_mode 位移方式（0表示相对位置，1表示绝对位置）<para />
         ///imark imark指定该段mark标志 <para />
         ///pwmChannel PWM通道号（范围：0-3 对应OUT12-OUT15） <para />
         ///pwmSetPara PWM参数列表指针 0- duty占空比 范围（0～100），0是常开，100是常闭 <para />
         ///1- frq 频率 （单位：赫兹）（范围：1-5000） <para />
         ///2- time100us 输出时间（单位：100us） <para />
         ///Aheaddelay PWM输出前延时时间（单位：ms）<para />
         ///FinishDelay PWM输出后延时时间（单位：ms）<para />
         ///SegLength 分段长度（单位：脉冲）<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_arc_glue", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_arc_glue(UInt16 card, UInt16 CrdNum, UInt16[] axis, Int32[] rel_pos, Int32[] rel_cen, UInt16 arc_dir, Double synspeed, Double synacc, Double synendvel, UInt16 posmode, Int32 imark, UInt16 pwmChannel, double[] pwmSetPara, double Aheaddelay, double FinishDelay, double SegLength);

         /// <summary>
         /// 功 能：连续两轴直线插补分段均匀打点 <para />
         ///参 数： card 控制卡卡号（通过拨码开关设置） <para />
         ///CrdNum  插补器序号（范围： 0 或 1) <para />
         ///piaxisListw 轴号列表指针 <para />
         ///pPosList 目标位置列表指针 <para />
         ///synspeed 插补速度（单位：脉冲/秒）<para />
         ///synacc 加速时间（单位：秒）<para />
         ///synendvel 插补结束速度（单位：脉冲/秒）<para />
         ///ipos_mode 位移方式（0表示相对位置，1表示绝对位置）<para />
         ///imark imark指定该段mark标志 <para />
         ///pwmChannel PWM通道号（范围：0-3 对应OUT12-OUT15） <para />
         ///pwmSetPara PWM参数列表指针 0- duty占空比 范围（0～100），0是常开，100是常闭 <para />
         ///1- frq 频率 （单位：赫兹）（范围：1-5000） <para />
         ///2- time100us 输出时间（单位：100us） <para />
         ///Aheaddelay PWM输出前延时时间（单位：ms）<para />
         ///FinishDelay PWM输出后延时时间（单位：ms）<para />
         ///SegLength 分段长度（单位：脉冲）<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_line_glue", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_line_glue(UInt16 card, UInt16 CrdNum, UInt16[] piaxisListw, Int32[] pPosList, Double synspeed, Double synacc, Double synendvel, UInt16 posmode, Int32 imark, UInt16 pwmChannel, double[] pwmSetPara, double Aheaddelay, double FinishDelay, double SegLength);

         /// <summary>
         /// 功  能：打开插补缓冲区（带插补器参数） <para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///axisNum        轴数（轴号范围说明） <para />    
         /// *piaxisList       连续插补轴号列表 <para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_open_list_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_open_list_muticoor(UInt16 card, UInt16 CrdNum, UInt16 axisNum, UInt16[] iaxilist);
         
         /// <summary>
         /// 功  能：关闭插补缓冲区（带插补器参数）<para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///返回值：错误代码<para />
         ///注：如果在插补运动结束后不调用该函数，会导致PCI400_check_done函数返回值一直为0，即检测到指定轴仍在运行之中。
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_close_list_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_close_list_muticoor(UInt16 card,UInt16 CrdNum);
         
         /// <summary>
         /// 功  能：启动插补运动（带插补器参数）<para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_start_list_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_start_list_muticoor(UInt16 card,UInt16 CrdNum);
         
         /// <summary>
         /// 功  能：插补暂停（带插补器参数）<para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_pause_list_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_pause_list_muticoor(UInt16 card,UInt16 CrdNum);
         
         /// <summary>
         /// 功  能：插补减速停止（带插补器参数）<para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_decel_stop_list_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_decel_stop_list_muticoor(UInt16 card,UInt16 CrdNum);
         
         /// <summary>
         /// 功  能：插补立即停止（带插补器参数）<para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///返回值：错误代码<para />
         ///注意：可以调用该函数清除连续插补缓冲
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_sudden_stop_list_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_sudden_stop_list_muticoor(UInt16 card,UInt16 CrdNum);
         
         /// <summary>
         /// 功  能：读取连续插补剩余缓冲段数 <para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///返回值：连续插补剩余缓冲段数
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_check_remain_space_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_check_remain_space_muticoor(UInt16 card,UInt16 CrdNum);
         
         /// <summary>
         /// 功  能：将前瞻缓冲区数据压入控制卡（带插补器参数）<para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
         /// CrdNum      插补器序号（范围：0或1）<para />
         /// mode           阻塞方式（0表示非阻塞方式，1表示阻塞方式）<para />
         ///返回值：错误代码<para />
         ///注：如果缓冲区满，需多次压入。<para />
         ///编程建议：在线程中使用该函数建议用阻塞方式，在按钮的消息响应函数中使用该函数建议用非阻塞方式。
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_pushdata_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_pushdata_muticoor(UInt16 card,UInt16 CrdNum,UInt16 mode);
		
         //插补状态
         /// <summary>
         /// 功  能：检测插补运动状态 <para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />        
         ///返回值：1-插补运行中  2-插补暂停  3-所有轴插补停止
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt16 PCI400_conti_state(UInt16 card);
		 
         /// <summary>
         ///  功  能：插补过程中检测插补状态 <para />  
         ///参  数： card            控制卡卡号（通过拨码开关设置） <para />         
         ///返回值：1-插补完成，0-插补未完成
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt16 PCI400_conti_check_done(UInt16 card);

         //插补状态
         /// <summary>
         /// 功  能：检测插补运动状态 <para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />    
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///返回值：1-插补运行中  2-插补暂停  3-所有轴插补停止
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_state_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_state_muticoor(UInt16 card,UInt16 CrdNum);

         /// <summary>
         ///  功  能：插补过程中检测插补状态 <para />  
         ///参  数： card            控制卡卡号（通过拨码开关设置） <para />    
         /// CrdNum      插补器序号（范围：0或1）<para />
         ///返回值：1-插补完成，0-插补未完成
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_check_done_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt16 PCI400_conti_check_done_muticoor(UInt16 card,UInt16 CrdNum);
		 
         /// <summary>
         /// 功  能：读取当前运动段mark标志 <para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
         ///返回值：当前运动段mark标志
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_read_current_mark", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_read_current_mark(UInt16 card);

         /// <summary>
         /// 功  能：读取当前运动段mark标志 <para />
         ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
         ///CrdNum      插补器序号（范围：0或1）<para />
         ///返回值：当前运动段mark标志
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_read_current_mark_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_read_current_mark_muticoor(UInt16 card,UInt16 CrdNum);		
		 
         /// <summary>
         /// 功  能：读取连续插补总插补长度<para />
         ///参  数： card             控制卡卡号（通过拨码开关设置）<para />
         ///返回值：当前连续插补总插补长度
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_get_total_length", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern double PCI400_conti_get_total_length(UInt16 card);
		 
         /// <summary>
         /// 功  能：读取连续插补缓冲剩余插补长度<para />
         ///参  数： card             控制卡卡号（通过拨码开关设置）<para />
         ///返回值：当前连续插补缓冲剩余插补长度
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_get_remain_length", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern double PCI400_conti_get_remain_length(UInt16 card);
		 
         /// <summary>
         /// 功  能：读取连续插补总插补长度（带插补器参数） <para />
         ///参  数： card             控制卡卡号（通过拨码开关设置）<para />
         /// coor          插补器序号（范围：0或1）<para />
         ///返回值：当前连续插补总插补长度
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_get_total_length_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern double PCI400_conti_get_total_length_muticoor(UInt16 card,UInt16 CrdNum);
		 
         /// <summary>
         /// 功  能：读取连续插补缓冲剩余插补长度（带插补器参数）<para />
         ///参  数： card             控制卡卡号（通过拨码开关设置）<para />
         /// coor          插补器序号（范围：0或1）<para />
         ///返回值：当前连续插补缓冲剩余插补长度
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_get_remain_length_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern double PCI400_conti_get_remain_length_muticoor(UInt16 card,UInt16 CrdNum);
		
         //插补速度
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_change_speed_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_change_speed_ratio(UInt16 card,double percent);
		
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_change_speed_ratio_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_conti_change_speed_ratio_muticoor(UInt16 card,UInt16 CrdNum,double percent);
		
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_get_current_speed_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern double PCI400_conti_get_current_speed_ratio(UInt16 card);
		
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_get_current_speed_ratio_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern double PCI400_conti_get_current_speed_ratio_muticoor(UInt16 card,UInt16 CrdNum);
		
		
         //手轮运动
         /// <summary>
         /// 功  能：设置手轮脉冲信号的计数方式 <para />
         ///参  数：card            控制卡卡号（通过拨码开关设置）<para />
         ///axis	       指定轴号（轴号范围说明） <para />
         ///inmode	      表示输入方式：0－A、B相位正交计数，1－双脉冲信号 <para />
         ///multi        计数器的计数方向及倍率设置：设置手轮的倍率, >=0表示默认方向, <0表示与默认方向相反。<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_set_handwheel_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_set_handwheel_inmode(UInt16 card,UInt16 axis,UInt16 inmode, double multi);

         /// <summary>
         /// 功  能：启动指定轴的手轮脉冲运动 <para />
         ///参  数：card            控制卡卡号（通过拨码开关设置）<para />
         ///axis	       指定轴号（轴号范围说明）<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_handwheel_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_handwheel_move(UInt16 card,UInt16 axis);

         //找原点

         /// <summary>
         ///功  能：设定/读取指定轴的回原点模式<para />
         ///参  数：card     控制卡卡号（通过拨码开关设置）<para />
         ///axis			    指定轴号（轴号范围说明）<para />
         ///home_dir	        回零方向， 1正向, 2:负向<para />
         ///vel              回零速度，0为低速(该轴起始速度)回原点，1为高速(该轴运行速度)回原点。<para />
         /// mode			回原点的信号模式<para />
         ///          0–一次回零   <para />
         ///		     1–二次回零<para />
         ///		     2–一次回零加回找   <para />
         ///          3–一次回零后再找一个EZ<para />
         ///	         4–以EZ作为原点进行一次回零<para />
         ///	         16-原点锁存回零<para />
         ///	         18-EZ锁存回零<para />
         ///EZ_count		保留参数<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_config_home_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_config_home_mode(UInt16 card, UInt16 axis, UInt16 home_dir, double vel, UInt16 mode, UInt16 EZ_count);

         /// <summary>
         ///功  能：单轴回原点运动 <para />
         ///参  数：card     控制卡卡号（通过拨码开关设置）<para />
         ///        axis	    指定轴号（轴号范围说明）<para />		
         ///返回值：错误代码<para />
         ///注意：回零运动完成后指令位置和编码器位置不会自动清零
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_home_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_home_move(UInt16 card, UInt16 axis);


         /// <summary>
         ///功  能：单轴回原点运动 <para />
         ///参  数：card     控制卡卡号（通过拨码开关设置）<para />
         ///        axis	    指定轴号（轴号范围说明）<para />		
         ///返回值：返回 32 位数据
        ///低 16 位表示是否正在回零：
        ///0-回零运动结束 1-回零运动进行中
         ///高 16 位表示回零运动停止原因：
         ///高 16 位为 0-无任何停止原因
        ///第 0 位-回零正常停止
        ///第 1 位-回零异常停止，可能 EL(正负硬件限位)，ALM(伺服报警)，EMG(急
         ///停)信号在回零过程中触发<para />
         ///注意：请在使用 PCI400_check_done 检测到当前轴处于停止状态时再读取回零完成的停止原因
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_home_stopreason", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_home_stopreason(UInt16 card, UInt16 axis);


         /// <summary>
         ///原点锁存
         ///功  能：设置原点锁存参数 <para />	
         ///参  数：CardNo           控制卡卡号（通过拨码开关设置）<para />	
         ///        axis				 指定轴号（轴号范围说明）<para />	
         ///        enable			 允许/禁止功能：0－无效，1－有效 <para />	
         ///        logic			 设置原点信号锁存方式：0－下降沿，1－上升沿 <para />	
         ///        source    	     设置原点信号锁存源：0－指令位置，1－编码器位置 <para />	
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_set_homelatch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt16  PCI400_set_homelatch_mode(UInt16 card, UInt16 axis, UInt16 enable, UInt16 logic, UInt16 source);

         /// <summary>
         ///功  能：读取原点锁存参数 <para />	
         ///参  数：CardNo           控制卡卡号（通过拨码开关设置）<para />	
         ///        axis				 指定轴号（轴号范围说明）<para />	
         ///        enable			 允许/禁止功能：0－无效，1－有效 <para />	
         ///        logic			 设置原点信号锁存方式：0－下降沿，1－上升沿 <para />	
         ///        source    	     设置原点信号锁存源：0－指令位置，1－编码器位置 <para />	
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_homelatch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt16 PCI400_get_homelatch_mode(UInt16 card, UInt16 axis, ref UInt16 enable, ref UInt16 logic, ref UInt16 source);

         /// <summary>
         ///功  能：读取原点锁存标志 <para />	
         ///参  数：CardNo            控制卡卡号（通过拨码开关设置）<para />	
         ///        axis				 指定轴号（轴号范围说明）<para />	
         ///返回值：0－无锁存触发，1－指定轴有原点信号被锁存
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_homelatch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_get_homelatch_flag(UInt16 card, UInt16 axis);

         /// <summary>
         ///功  能：清除原点锁存标志  <para />
         ///参  数：CardNo             控制卡卡号（通过拨码开关设置）<para />
         ///        axis				  指定轴号（轴号范围说明）<para />
         ///返回值：错误码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_reset_homelatch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt16  PCI400_reset_homelatch_flag(UInt16 card, UInt16 axis);

         /// <summary>
         ///功  能：读取原点锁存值  <para />
         ///参  数：CardNo             控制卡卡号（通过拨码开关设置）  <para />
         ///        axis				  指定轴号（轴号范围说明）  <para />
         ///返回值：原点锁存值
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_homelatch_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32  PCI400_get_homelatch_value(UInt16 card, UInt16 axis);
       
         ///一维位置比较功能

         /// <summary>
         ///功  能：设置比较器配置
         ///参	数：card		   控制卡卡号（通过拨码开关设置）<para />
         ///         enable		    1：使能比较功能， 0：禁止比较功能<para />
         ///         axis 		   轴号（轴号范围说明）<para />
         ///         cmp_source	   比较源， 0：比较指令位置， 1：比较编码器位置<para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_compare_config(UInt16 card, UInt16 enable, UInt16 axis,  UInt16 cmp_source);

         /// <summary>
         ///功  能：读取比较器配置 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para />
         ///         enable		    1：使能比较功能， 0：禁止比较功能 <para />
         ///         axis 		   轴号（轴号范围说明） <para />
         ///         cmp_source	   比较源， 0：比较指令位置， 1：比较编码器位置 <para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_compare_get_config(UInt16 card, ref UInt16 enable, ref UInt16 axis,  ref UInt16 cmp_source);

         /// <summary>
         ///功  能：清除所有比较点 <para />
         ///参	数:	 card		   控制卡卡号（通过拨码开关设置）<para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_clear_points", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_compare_clear_points(UInt16 card);

         /// <summary>
         ///功  能：添加比较点 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para />
         ///         Pos： 	       位置坐标 <para />
         ///         dir：         比较方向 0: 小于等于  1： 大于等于 <para />
         ///         action：      比较点触发动作 <para />
         ///         actpara：     比较点触发对应的输出口序号 <para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32  PCI400_compare_add_point(UInt16 card, Int32 pos, UInt16 dir,  UInt16 action, Int32 actpara);

         /// <summary>
         ///功  能：读取当前比较点 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para />
         ///返回值：当前比较点对应的触发位置
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_current_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32  PCI400_compare_get_current_point(UInt16 card);

         /// <summary>
         ///功  能：查询已经比较过的点 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para />
         ///返回值：一维比较已经比较过的比较点
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_points_runned", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32  PCI400_compare_get_points_runned(UInt16 card);

         /// <summary>
         ///功  能：查询可以加入的比较点数量 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para />
         ///返回值：一维比较可以加入的比较点数量
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_points_remained", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32  PCI400_compare_get_points_remained(UInt16 card);

         ///二维位置比较功能

         /// <summary>
         ///功  能：设置比较器配置 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para />
         ///         enable		    1：使能比较功能， 0：禁止比较功能 <para />
         ///         cmp_source	   比较源， 0：比较指令位置， 1：比较编码器位置 <para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_config_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_compare_config_extern(UInt16 card, UInt16 enable, UInt16 cmp_source);

         /// <summary>
         ///功  能：读取比较器配置 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置）<para />
         ///         enable		    1：使能比较功能， 0：禁止比较功能 <para />
         ///         cmp_source	   比较源， 0：比较指令位置， 1：比较编码器位置 <para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_config_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_compare_get_config_extern(UInt16 card, ref UInt16 enable, ref UInt16 cmp_source);
         ///二维位置比较功能

         /// <summary>
         ///功  能：设置比较器配置 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para />
         ///         enable		    1：使能比较功能， 0：禁止比较功能 <para />
         ///         cmp_source	   比较源， 0：比较指令位置， 1：比较编码器位置 <para />
		 ///         gap     	   比较误差范围1-65535 <para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_config_extern_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_compare_config_extern_ex(UInt16 card, UInt16 enable, UInt16 cmp_source,UInt32 gap);

         /// <summary>
         ///功  能：读取比较器配置 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置）<para />
         ///         enable		    1：使能比较功能， 0：禁止比较功能 <para />
         ///         cmp_source	   比较源， 0：比较指令位置， 1：比较编码器位置 <para />
		 ///         gap     	   比较误差范围1-65535 <para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_config_extern_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_compare_get_config_extern_ex(UInt16 card, ref UInt16 enable, ref UInt16 cmp_source,ref UInt32 gap);
         /// <summary>
         ///功  能：清除所有比较点 <para />
         ///参	数:	 card		   控制卡卡号（通过拨码开关设置） <para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_clear_points_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_compare_clear_points_extern(UInt16 card);

         /// <summary>
         ///功  能：添加比较点 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置）<para />
         ///*iaxis        轴号列表 <para />
         ///*Pos： 	   二维位置坐标 <para />
         ///*dir：        比较方向列表 0: 小于等于  1： 大于等于 <para />
         ///action：      比较点触发动作 <para />
         ///actpara：     比较点触发对应的输出口序号 <para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_add_point_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_compare_add_point_extern(UInt16 card, UInt16[] axis,Int32[] pos, UInt16[] dir, UInt16 action, Int32 actpara);

         /// <summary>
         ///功  能：读取当前比较点的触发位置 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para />
         ///Pos          二维比较点位置 <para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_current_point_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_compare_get_current_point_extern(UInt16 card, Int32[] pos);

         /// <summary>
         ///功  能：查询已经比较过的点数量 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置）<para />
         ///返回值：二维比较已经比较过的比较点数量
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_points_runned_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_compare_get_points_runned_extern(UInt16 card);

         /// <summary>
         ///功  能：查询可以加入的比较点数量 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置）<para />
         ///返回值：二维比较可以加入的比较点数量
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_points_remained_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_compare_get_points_remained_extern(UInt16 card);
		
         ///多组比较

         /// <summary>
         ///功  能：设置比较器配置 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para />
         ///         queue        比较器序号（0-23）<para />
         ///         enable        1：使能比较功能   0：禁止比较功能<para />
         ///         axis          指定轴号（轴号范围说明）<para />
         ///         cmp_source    比较源（0：比较指令位置   1：比较编码器位置） <para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_config_Muti", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_compare_config_Muti(UInt16 card, UInt16 queue, UInt16 enable, UInt16 axis, UInt16 cmp_source);

         /// <summary>
         ///功  能：读取比较器配置 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置）<para /> 
         ///         queue        比较器序号（0-23）<para />
         ///         enable        1：使能比较功能   0：禁止比较功能 <para />
         ///         axis          指定轴号（轴号范围说明）<para />
         ///         cmp_source    比较源（0：比较指令位置   1：比较编码器位置）<para />   
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_config_Muti", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_compare_get_config_Muti(UInt16 card, UInt16 queue, ref UInt16 enable, ref UInt16 axis, ref UInt16 cmp_source);

         /// <summary>
         ///功  能：清除所有比较点 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置）<para /> 
         ///         queue          比较器序号（0-23）<para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_clear_points_Muti", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_compare_clear_points_Muti(UInt16 card, UInt16 queue);

         /// <summary>
         ///功  能：添加比较点 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para />
         ///queue        比较器序号（0-23）<para />
         ///pos          对应轴的触发位置 <para />
         ///dir           比较方向（0：小于等于 1：大于等于）<para />
         ///action        比较点触发动作 <para />
         ///actpara       比较点触发对应的输出口序号 <para />
         ///PCI400_compare_add_point_ Muti 函数 action, actpara 参数值含义： <para />
         ///action      	  actpara                功能   <para />
         ///1               IO号                 打开IO <para />
         ///2               IO号                 关闭IO <para />
         ///3               IO号                 取反IO <para />
         ///5               IO号                 输出100us 脉冲 <para />
         ///6               IO号                 输出1ms 脉冲 <para />
         ///7               IO号                 输出10ms 脉冲 <para />
         ///8               IO号                 输出100ms 脉冲 <para />
         ///9               IO号                 输出脉冲，脉冲宽度由函数制定 v
         ///11             速度值                当前轴变速 <para />
         ///13             轴号                  停止指定轴 <para />
         ///注意：使用功能号9时，需要先调用函数PCI400_compare_set_pulsetimes_Muti设置脉冲宽度，单位毫秒。 <para />
         ///返回值：错误代码
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_add_point_Muti", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_compare_add_point_Muti(UInt16 card, UInt16 queue, Int32 pos, UInt16 dir, UInt16 action, Int32 actpara);

         /// <summary>
         ///功  能：读取当前比较点位置 <para />
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para /> 
         ///         queue          比较器序号（0-23） <para />
         ///返回值：当前比较点对应的触发位置
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_current_point_Muti", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_compare_get_current_point_Muti(UInt16 card, UInt16 queue);

         ///<summary>
         ///功  能：查询已经比较过的比较点数量 <para /> 
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para /> 
         ///         queue          比较器序号（0-23）<para /> 
         ///返回值：已经比较过的比较点数量
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_points_runned_Muti", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_compare_get_points_runned_Muti(UInt16 card, UInt16 queue);

         ///<summary>
         ///功  能：查询可添加的比较点数量 <para /> 
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para /> 
         ///queue        比较器序号（0-23）<para /> 
         ///返回值：可添加的比较点数量 <para /> 
         ///注意：每成功添加一个比较点，可添加的比较点数量就会减1；当比较点成功触发后可添加的比较点数量就会加1，已经比较的比较点数量会加1。
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_points_remained_Muti", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_compare_get_points_remained_Muti(UInt16 card, UInt16 queue);

         ///<summary>
         ///功  能：设置功能号9的脉冲宽度，单位:ms <para /> 
         ///参	数：card		   控制卡卡号（通过拨码开关设置） <para /> 
         ///queue        比较器序号（0-23）<para /> 
         ///ftimes        脉冲宽度  （单位：100微秒） <para /> 
         ///返回值：错误代码 <para /> 
         ///注 意：如果设置脉冲宽度数值不需要改变，该函数只调用一次即可。
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_set_pulsetimes_Muti", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_compare_set_pulsetimes_Muti(UInt16 card, UInt16 queue,float ftimes);
		
         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_set_filter_Muti", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_compare_set_filter_Muti(UInt16 card, UInt32 filterfrq);

         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_filter_Muti", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_compare_get_filter_Muti(UInt16 card, ref UInt32 filterfrq);

         ///<summary>
         ///功  能：配置高速比较输出。 <para /> 
         ///参  数：CardNo            控制卡卡号（通过拨码开关设置）<para /> 
         ///hcmp             高速比较通道号（范围: 0-3）<para /> 
         ///enable             使能高速比较 0-禁止 1-使能 <para /> 
         ///axis               保留参数，写0 <para /> 
         ///cmp_source         比较源，保留参数，默认为编码器位置 <para /> 
         ///返回值：错误代码 <para /> 
         ///注意：高速比较通道号0-3分别对应（OUT12,OUT15,OUT14,OUT13），轴号分别对应0-3轴
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_hcmp_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_hcmp_config(UInt16 card, UInt16 hcmp, UInt16 ennable, UInt16 axis, UInt16 cmp_source);

         ///<summary>
         ///功  能：读取配置高速比较输出。<para />
         ///参  数：CardNo            控制卡卡号（通过拨码开关设置）<para />
         ///hcmp             高速比较通道号（范围: 0-3）<para />
         ///enable             使能高速比较 0-禁止 1-使能 <para />
         ///axis               保留参数，写0 <para />
         ///cmp_source         比较源，保留参数，默认为编码器位置 <para />
         ///返回值：错误代码 <para />
         ///注意：高速比较通道号0-3分别对应（OUT12,OUT15,OUT14,OUT13），轴号分别对应0-3轴
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_hcmp_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_hcmp_get_config(UInt16 card, UInt16 hcmp, ref UInt16 ennable, ref UInt16 axis, ref UInt16 cmp_source);

         ///<summary>
         ///功  能：清除高速比较输出数据 <para />
         ///参  数：CardNo             控制卡卡号（通过拨码开关设置）<para />
         ///hcmp         	    高速比较通道号（范围：0-3）<para />
         ///返回值：错误代码 <para />
         ///注意：高速比较通道号0-3分别对应（OUT12,OUT15,OUT14,OUT13），轴号分别对应0-3轴
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_hcmp_clear_points", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_hcmp_clear_points(UInt16 card, UInt16 hcmp);

         ///<summary>
         ///功  能：高速比较输出位置配置 <para />
         ///参  数：CardNo             控制卡卡号（通过拨码开关设置）<para />
         ///hcmp         	   高速比较通道号（范围：0-3）<para />
         ///pos                 高速比较输出位置，只支持编码器位置 <para />
         ///dir                 保留参数 <para />
         ///action              保留参数 <para />
         ///actpara             比较输出的脉冲宽度（单位：微秒）<para />
         ///返回值：错误代码 <para />
         ///注意：高速比较通道号0-3分别对应（OUT12,OUT15,OUT14,OUT13），轴号分别对应0-3轴
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_hcmp_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_hcmp_add_point(UInt16 card, UInt16 hcmp, int pos, UInt16 dir, UInt16 action, int actpara);

         ///<summary>
         ///功  能：读取当前运行的比较点数据 <para />
         ///参  数：CardNo             控制卡卡号（通过拨码开关设置） <para />
         ///        hcmp         	    高速比较通道号（范围：0-3）<para />
         ///返回值：当前正在运行的比较点数据 <para />
         ///注意：高速比较通道号0-3分别对应（OUT12,OUT15,OUT14,OUT13），轴号分别对应0-3轴
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_hcmp_get_current_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern int PCI400_hcmp_get_current_point(UInt16 card, UInt16 hcmp);

         ///<summary>
         ///功  能：读取已经运行的比较点个数 <para />
         ///参  数：CardNo             控制卡卡号（通过拨码开关设置） <para />
         ///        hcmp         	    高速比较通道号（范围：0-3）<para />
         ///返回值：已经运行的比较点个数 <para />
         ///注意：高速比较通道号0-3分别对应（OUT12,OUT15,OUT14,OUT13），轴号分别对应0-3轴
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_hcmp_get_points_runned", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern int PCI400_hcmp_get_points_runned(UInt16 card, UInt16 hcmp);

         ///<summary>
         ///功  能：查询可以加入的比较点数量 <para />
         ///参  数：CardNo             控制卡卡号（通过拨码开关设置）<para />
         ///        hcmp         	    高速比较通道号（范围：0-3）<para />
         ///返回值：剩余比较点空间（范围：0-256）<para />
         ///注意：高速比较通道号0-3分别对应（OUT12,OUT15,OUT14,OUT13），轴号分别对应0-3轴
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_hcmp_get_points_remained", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern int PCI400_hcmp_get_points_remained(UInt16 card, UInt16 hcmp);

         ///<summary>
         ///功  能：操作高速比较输出口 <para />
         ///参  数：CardNo             控制卡卡号（通过拨码开关设置）<para />
         ///hcmp         	    高速比较通道号（范围：0-3）<para />
         ///state                高速比较口的状态 <para />
         ///返回值：错误代码 <para />
         ///注意：高速比较通道号0-3分别对应（OUT12,OUT15,OUT14,OUT13），轴号分别对应0-3轴
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_write_cmp_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern int PCI400_write_cmp_pin(UInt16 card, UInt16 hcmp, UInt16 state);

         ///<summary>
         ///功  能：读取高速比较输出口 <para />
         ///参  数：CardNo             控制卡卡号（通过拨码开关设置）<para />
         ///hcmp         	    高速比较通道号（范围：0-3）<para />
         ///返回值：高速比较口的状态 <para />
         ///注意：高速比较通道号0-3分别对应（OUT12,OUT15,OUT14,OUT13），轴号分别对应0-3轴
         ///</summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_read_cmp_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern int PCI400_read_cmp_pin(UInt16 card, UInt16 hcmp);

        ///输入口高速计数
         
         //// <summary>
         //// 功  能：设置输入口高速计数功能参数 <para />
         ////参  数：CardNo             控制卡卡号（通过拨码开关设置）<para />
         ////bitno			    通道号（范围：0-3）（对应ORG0-ORG3）<para />
         ////enable              是否使能（0：禁止，1：使能）<para />
         ////mode               输入口计数模式： <para />
         ////0－下降沿计数（默认为下降沿计数，无法更改）
         //// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_set_h_count_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_set_h_count_mode(UInt16 card, UInt16 bitno, UInt16 enable, UInt16 mode);

         //// <summary>
         //// 功  能：读取输入口高速计数功能参数 <para />
         ////参  数：CardNo             控制卡卡号（通过拨码开关设置）<para />
         ////bitno			    通道号（范围：0-3）（对应ORG0-ORG3）<para />
         ////enable              是否使能（0：禁止，1：使能）<para />
         ////mode               输入口计数模式： <para />
         ////0－下降沿计数（默认为下降沿计数，无法更改）
         //// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_h_count_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_get_h_count_mode(UInt16 card, UInt16 bitno, ref UInt16 enable, ref UInt16 mode);

         /// <summary>
         /// 功  能：设置输入口高速功能计数值 <para />
         ///参  数：CardNo             控制卡卡号（通过拨码开关设置） <para />
         ///bitno			    通道号（范围：0-3）（对应ORG0-ORG3） <para />
         ///CountValue          计数值 <para />           
         ///返回值：错误码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_set_h_count_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_set_h_count_value(UInt16 card, UInt16 bitno, UInt32 CountValue);

         /// <summary>
         /// 功  能：读取输入口高速功能计数值 <para />
         ///参  数：CardNo             控制卡卡号（通过拨码开关设置） <para />
         ///bitno			    通道号（范围：0-3）（对应ORG0-ORG3） <para />
         ///CountValue          计数值 <para />        
         ///返回值：错误码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_h_count_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_get_h_count_value(UInt16 card, UInt16 bitno, ref UInt32 CountValue);

         //相机提速增加函数
        //typedef unsigned int (__stdcall *LATCH_OPERATE_FUN)(WORD card,WORD iaxis,WORD ifmingap,int32 latchpos,void* operate_data);
        // typedef unsigned int (__stdcall *COMPARE_OPERATE_FUN)(WORD card,WORD queue,DWORD runnedNo,void* operate_data);


         public delegate UInt16 LATCH_OPERATE_FUN(UInt16 card, UInt16 iaxis, UInt16 ifmingap, Int32 latchpos, IntPtr operate_data);
         public delegate UInt16 COMPARE_OPERATE_FUN(UInt16 card, UInt16 queue, UInt32 runnedNo, IntPtr operate_data);

         [DllImport("PCI400.dll", EntryPoint = "PCI400_compare_get_points_runned_Muti_all", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_compare_get_points_runned_Muti_all(UInt16 card, UInt32[] flag, UInt32[] data);

         [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_get_latch", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_sort_get_latch(UInt16 card, UInt16 axis, Int32 offset, LATCH_OPERATE_FUN funcIntHandler, IntPtr data);

         [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_camera_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_sort_camera_config(UInt16 card, UInt16 maxcamera, UInt16 dir, UInt16 triggeraction, ref Int32 camerapos, UInt16 start_index, ref UInt16 triggerport, ref UInt16 enable, ref float triggertime100us);
     
         /// <summary>
         /// 功 能：读取物料锁存位置  <para />
         ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
         ///flag 锁存标志（对应位为 1 说明对应轴有锁存成功，否则 data 的值无意义）<para />
         ///data 锁存值（缓冲型数据，函数调用一次，就会从锁存队列中取一个锁存值）<para />
         ///返回值：错误码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_get_latch_value_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_get_latch_value_ex(UInt16 card, ref UInt32 flag, UInt32[] data);

         [DllImport("PCI400.dll", EntryPoint = "PCI400_clear_latch_buf", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_clear_latch_buf(UInt16 card);


         //---连续锁存功能新2022.6.9
         /// <summary>
         /// 功 能：启动连续锁存功能  <para />
         ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
         ///iaxis 编码器号(0-3、0-5、0-7)<para />
         ///enable 是否使能<para />
         ///src    锁存源(保留)<para />
         ///返回值：错误码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_latch_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_conti_latch_config(UInt16 card, UInt16 iaxis, UInt16 enable, UInt16 src);
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_latch_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_conti_latch_get_config(UInt16 card, UInt16 iaxis, ref UInt16 enable,ref UInt16 src);

         /// <summary>
         /// 功 能：启动连续锁存功能  <para />
         ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
         ///iaxis 编码器号(0-3、0-5、0-7)<para />
         ///返回值：错误码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_latch_clear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_conti_latch_clear(UInt16 card, UInt16 iaxis);


         /// <summary>
         /// 功 能：启动连续锁存功能  <para />
         ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
         ///iaxis 编码器号(0-3、0-5、0-7)<para />
         ///cnt 已经缓存和个数<para />
         ///返回值：错误码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_latch_count", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_conti_latch_count(UInt16 card, UInt16 iaxis, ref UInt16 cnt);


         /// <summary>
         /// 功 能：启动连续锁存功能  <para />
         ///参 数： card 控制卡卡号（通过拨码开关设置）<para />
         ///iaxis 编码器号(0-3、0-5、0-7)<para />
         ///data 锁存数据<para />
         ///返回值：错误码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_latch_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern Int32 PCI400_conti_latch_value(UInt16 card, UInt16 iaxis, Int32[] data);
		 //---分选功能新

         /// <summary>
         /// 功 能：启动主机异常检测功能 <para />
         ///参 数： CardNo 控制卡卡号（通过拨码开关设置）<para />
         ///doIndex 输出口序号（0-23）<para />
         ///dologic 输出电平（0：低电平 1：高电平）<para />
         ///timems 超时报警的阈值（单位：ms）。超过这个时间没有调用PCI400_feed_watchdog ,则控制卡认为主机故障，控制器停止所有轴的运动，复位 IO，并在指定输出口输出指定电平。
         ///返回值：错误代码
         /// </summary>
		 [DllImport("PCI400.dll", EntryPoint = "PCI400_start_watchdog", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_start_watchdog(UInt16 card,UInt16 doIndex,UInt16 dologic,UInt16 timems );
		 
         /// <summary>
         /// 功 能：停止主机异常检测功能 <para />
         ///参 数： CardNo 控制卡卡号（通过拨码开关设置）<para />
         ///返回值：错误代码
         /// </summary>
		 [DllImport("PCI400.dll", EntryPoint = "PCI400_stop_watchdog", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_stop_watchdog(UInt16 card );

        /// <summary>
        /// 功 能：在超时报警阈值内需要调用该指令，清零控制卡内部时间计数 <para />
        ///参 数： CardNo 控制卡卡号（通过拨码开关设置） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_feed_watchdog", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_feed_watchdog(UInt16 card );
		 
        /// <summary>
        /// 功 能：读取编码器速度 <para />
        ///参 数： CardNo 控制卡卡号（通过拨码开关设置）<para />
        ///axis 指定轴号 <para />
        ///返回值：编码器速度（单位：脉冲/秒）
        /// </summary>
		 [DllImport("PCI400.dll", EntryPoint = "PCI400_read_encspeed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_read_encspeed(UInt16 card ,UInt16 iaxis);

         /// <summary>
         /// 功 能：设置分选相机参数 <para />
         ///参 数： CardNo 控制卡卡号（通过拨码开关设置） <para />
         ///cameraCount 相机个数(最大 16 个) <para />
         ///pCameraPos 和相机个数对应的相机位置列表，相对于检测位置的距离，对应相机参数设置为 0，改相机触发将被禁止 <para />
         ///cameraTime100us 相机触发脉冲宽度，100us 为单位，例：1 表示 100us <para />
         ///cameraReverse 相机触发电平（0：输出低电平 1：输出高电平）<para />
         ///返回值：错误代码
         /// </summary>
		 [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_set_cameraconfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_set_cameraconfig(UInt16 card, UInt16 cameraCount, UInt32[] pCameraPos, UInt32 cameraTime100us, UInt16 cameraReverse );

        /// <summary>
         /// 功 能：设置分选相机和输出口的映射关系 <para />
         /// 参 数： CardNo 控制卡卡号（通过拨码开关设置）<para />
         ///cameraCount 相机个数(最大 16 个) <para />
         ///pCameraIo 和相机个数对应的相机触发 IO 列表 <para />
         ///cameraReverse 相机触发电平（0：输出低电平 1：输出高电平），保留参数 <para />
         ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_set_camera_iomap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_set_camera_iomap(UInt16 card, UInt16 cameraCount, UInt32[] pCameraIo,  UInt16 cameraReverse );
		
         /// <summary>
        /// 功 能：设置分选吹气参数 <para />
        ///参 数： CardNo 控制卡卡号（通过拨码开关设置）<para />
        ///blowCount 吹气口个数 <para />
        ///pBlowPos 和吹气口个数对应的吹气口位置列表，相对于检测位置的距离 <para />
        ///blowTime100us 吹气口触发脉冲宽度，100us 为单位，例：1 表示 100us <para />
        ///blowReverse 吹气口触发电平（0：输出低电平 1：输出高电平） <para />
        ///返回值：错误代码
         /// </summary>
		[DllImport("PCI400.dll", EntryPoint = "PCI400_sort_set_blowconfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_set_blowconfig(UInt16 card, UInt16 blowCount,UInt32[] pBlowPos, UInt32 blowTime100us, UInt16 blowReverse );

        /// <summary>
        /// 功 能：设置吹气装置和输出口的映射关系 <para />
        ///参 数： CardNo 控制卡卡号（通过拨码开关设置） <para />
        ///blowCount 吹气个数(最大 8 个) <para />
        ///pBlowIo 和吹气个数对应的吹气装置触发 IO 列表 <para />
        ///blowReverse 吹气触发电平（0：输出低电平 1：输出高电平），保留参数 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_set_blow_iomap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_set_blow_iomap(UInt16 card, UInt16 blowCount, UInt32[] pBlowIo,  UInt16 blowReverse );

        /// <summary>
        /// 功 能：读取分选相机参数 <para />
        ///参 数： CardNo 控制卡卡号（通过拨码开关设置） <para />
        ///cameraCount 相机个数(最大 16 个) <para />
        ///pCameraPos 和相机个数对应的相机位置列表，相对于检测位置的距离，对应相机参数设置为 0，改相机触发将被禁止 <para />
        ///cameraTime100us 相机触发脉冲宽度，100us 为单位，例：1 表示 100us <para />
        ///cameraReverse 相机触发电平（0：输出低电平 1：输出高电平）<para />
        ///返回值：错误代码
        /// </summary>
		[DllImport("PCI400.dll", EntryPoint = "PCI400_sort_get_cameraconfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_get_cameraconfig(UInt16 card,ref  UInt16 cameraCount, UInt32[] pCameraPos,ref  UInt32 cameraTime100us,ref  UInt16 cameraReverse );

        /// <summary>
        /// 功 能：读取选相机参数和输出口的映射关系 <para />
        /// 参 数： CardNo 控制卡卡号（通过拨码开关设置）<para />
        ///cameraCount 相机个数(最大 16 个) <para />
        ///pCameraIo 和相机个数对应的相机触发 IO 列表 <para />
        ///cameraReverse 相机触发电平（0：输出低电平 1：输出高电平），保留参数 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_get_camera_iomap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_get_camera_iomap(UInt16 card,ref  UInt16 cameraCount, UInt32[] pCameraIo, ref  UInt16 cameraReverse );

        /// <summary>
        /// 功 能：读取分选吹气参数 <para />
        ///参 数： CardNo 控制卡卡号（通过拨码开关设置）<para />
        ///blowCount 吹气口个数 <para />
        ///pBlowPos 和吹气口个数对应的吹气口位置列表，相对于检测位置的距离 <para />
        ///blowTime100us 吹气口触发脉冲宽度，100us 为单位，例：1 表示 100us <para />
        ///blowReverse 吹气口触发电平（0：输出低电平 1：输出高电平） <para />
        ///返回值：错误代码
        /// </summary>	 
		[DllImport("PCI400.dll", EntryPoint = "PCI400_sort_get_blowconfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_get_blowconfig(UInt16 card,ref   UInt16 blowCount, UInt32[] pBlowPos,ref   UInt32 blowTime100us,ref   UInt16 blowReverse );

        /// <summary>
        /// 功 能：读取吹气装置和输出口的映射关系 <para />
        ///参 数： CardNo 控制卡卡号（通过拨码开关设置） <para />
        ///blowCount 吹气个数(最大 8 个) <para />
        ///pBlowIo 和吹气个数对应的吹气装置触发 IO 列表 <para />
        ///blowReverse 吹气触发电平（0：输出低电平 1：输出高电平），保留参数 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_get_blow_iomap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_get_blow_iomap(UInt16 card,ref   UInt16 blowCount, UInt32[] pBlowIo, ref   UInt16 blowReverse );
		 
        /// <summary>
        /// 功 能：设置工件信息 <para />
        ///参 数： CardNo 控制卡卡号（通过拨码开关设置） <para />
        ///Maxwidth 工件最大宽度（单位：脉冲） <para />
        ///Minwidth 工件最小宽度（单位：脉冲） <para />
        ///Mindistance 工件最小间距（单位：脉冲） <para />
        ///MintimeDistance 工件最小时间间隔（单位：100us）例：1 表示 100us。如果设置为 65535，则该工件最小间隔偏小，不会影响下一个料。 <para />
        ///返回值：错误代码
        /// </summary>
		[DllImport("PCI400.dll", EntryPoint = "PCI400_sort_set_piececonfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_set_piececonfig(UInt16 card, UInt32 Maxwidth, UInt32 Minwidth, UInt32 Mindistance,UInt32 MintimeDistance);

        /// <summary>
        /// 功 能：读取工件信息 <para />
        ///参 数： CardNo 控制卡卡号（通过拨码开关设置） <para />
        ///Maxwidth 工件最大宽度（单位：脉冲） <para />
        ///Minwidth 工件最小宽度（单位：脉冲） <para />
        ///Mindistance 工件最小间距（单位：脉冲） <para />
        ///MintimeDistance 工件最小时间间隔（单位：100us）例：1 表示 100us。如果设置为 65535，则该工件最小间隔偏小，不会影响下一个料。 <para />
        ///返回值：错误代码
        /// </summary>
		[DllImport("PCI400.dll", EntryPoint = "PCI400_sort_get_piececonfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_get_piececonfig(UInt16 card, ref   UInt32 Maxwidth, ref   UInt32 Minwidth, ref   UInt32 Mindistance,ref   UInt32 MintimeDistance);
		 
		/// <summary>
        /// 功 能：设置分选时的编码器方向 <para />
        ///参 数： CardNo 控制卡卡号（通过拨码开关设置） <para />
        ///dir 编码器方向（0：负方向 1：正方向） <para />
        ///返回值：错误代码 <para />
        ///注意：默认正方向
		/// </summary>
		[DllImport("PCI400.dll", EntryPoint = "PCI400_sort_config_encdir", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_config_encdir(UInt16 card, UInt16 dir);
		 
        /// <summary>
        /// 功 能：指定吹气装置是否使能 <para />
        ///参 数： CardNo 控制卡卡号（通过拨码开关设置） <para />
        ///index 吹气装置的序号，参考默认列表 <para />
        ///state 吹气装置的状态（0：不使能 1：使能）<para />
        ///返回值：错误代码 <para />
        ///注意：当指定吹气装置使能时，该吹气装置才能正常触发吹气
        /// </summary>
		[DllImport("PCI400.dll", EntryPoint = "PCI400_sort_blow_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_blow_enable(UInt16 card, UInt16 index, UInt16 state);
		 
        /// <summary>
        /// 功 能：获取吹气状态，可以用于判断相机判决是否错位或者吹气时料已经过吹气口等异常情况 <para />
        ///参 数： CardNo 控制卡卡号（通过拨码开关设置）<para />
        ///blowCmdMore 吹气指令个数比最后一个相机触发次数多(也就是吹气指令个数比穿过最后一个相机的料的个数多)，（0-吹气正常，1-吹气异常） <para />
        ///blowCmdlittle （当前物料为待吹气队列中的第一个物料，正常情况下用户下发吹气指令后，控制卡会把待吹气队列中的第一个物料添加到 <para />
        ///用户指定的吹气口比较队列中，同时会把这个料从待吹气队列中去除）；当前物料到达第一个吹气口时用户还未下发吹气指令，<para />
        /// 认为是吹气异常（漏吹或者是晚吹），正常情况下用户应该=在物料到达第一个吹气口之前下发吹气指令吹气（0-吹气正常，1-吹气异常）<para />
        ///blowCmdNum 吹气指令数量<para />
        ///返回值：错误代码
        /// </summary>
		 [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_get_blow_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_get_blow_status(UInt16 card,ref UInt16 blowCmdMore,ref  UInt16 blowCmdlittle,ref UInt32 blowCmdNum);
		 
         /// <summary>
         /// 功 能：获取工件状态 <para />
         ///参 数： CardNo 控制卡卡号（通过拨码开关设置）<para />
         ///countFind 通过检测装置工件个数（包括不满足工件参数设置的工件）<para />
         ///camerCount 相机触发次数<para />
         ///blowCount 吹气个数<para />
         ///pieceLength 工件的宽度<para />
         ///返回值：错误代码
         /// </summary>
		 [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_piece_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_piece_status(UInt16 card,ref UInt32 countFind,ref  UInt32 camerCount,ref UInt32 blowCount,ref  UInt32 pieceLength);

         /// <summary>
         /// 功 能：获取工件状态 （返回工件个数为过滤前的）<para />
         ///参 数： CardNo 控制卡卡号（通过拨码开关设置）<para />
         ///countFind 通过检测装置工件个数（包括不满足工件参数设置的工件）<para />
         ///camerCount 相机触发次数<para />
         ///blowCount 吹气个数<para />
         ///pieceLength 工件的宽度<para />
         ///返回值：错误代码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_piece_status_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_piece_status_ex(UInt16 card, ref UInt32 countFind, ref  UInt32 camerCount, ref UInt32 blowCount, ref  UInt32 pieceLength);
		 
        /// <summary>
         /// 功 能：读取吹气触发个数 <para />
         ///参 数： CardNo 控制卡卡号（通过拨码开关设置） <para />
         ///index 吹气装置的序号，参考默认列表 <para />
         ///icount 一次连续获取的吹气装置个数 <para />
         ///idata 吹气触发次数数组，长度应等于 icount(icount=1 时则为一个变量) <para />
         ///获取从指定吹气编号开始连续 icount 个吹气的触发次数数值。 <para />
         ///返回值：错误代码
        /// </summary>
		  [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_get_blow_triggercount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_get_blow_triggercount(UInt16 card,UInt16 index, UInt16 icount,UInt32[] idata);
		 
         /// <summary>
          /// 功 能：读取相机触发个数 <para />
          ///参 数： CardNo 控制卡卡号（通过拨码开关设置） <para />
          ///index 相机的序号，参考默认列表 <para />
          ///icount 一次连续获取的相机个数 <para />
          ///idata 相机触发次数数组，长度应等于 icount(icount=1 时则为一个变量） <para />
          ///获取从指定相机编号开始连续 icount 个相机的触发次数数值。 <para />
         ///返回值：错误代码
         /// </summary>
		 [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_get_camera_triggercount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_get_camera_triggercount(UInt16 card,UInt16 index, UInt16 icount, UInt32[] idata);
		 
         /// <summary>
         /// 功 能：添加指定吹气口触发指令 <para />
         ///参 数： CardNo 控制卡卡号（通过拨码开关设置） <para />
         ///index 吹气装置的序号，参考默认列表 <para />
         ///返回值：错误代码 
         /// </summary>
		 [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_blow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_blow(UInt16 card,UInt16 index);

         /// <summary>
         /// 添加指定吹气口指定物料序号触发指令
         /// </summary>
         /// <param name="card">控制卡卡号</param>
         /// <param name="index">吹气装置序号</param>
         /// <param name="picNum">物料序号</param>
         /// <returns>错误码</returns>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_blowex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_blowex(UInt16 card, UInt16 index, UInt32 picNum);
		 
         /// <summary>
         /// 功  能：启动分选功能 <para />
         ///参	数： CardNo		        控制卡卡号（通过拨码开关设置）<para /> 
         ///返回值：错误代码
         /// </summary>
		 [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_start(UInt16 card);
		 
         /// <summary>
         /// 功  能：结束分选功能，不会清除物料信息 <para /> 
         ///参	数： CardNo		        控制卡卡号（通过拨码开关设置） <para /> 
         ///返回值：错误代码
         /// </summary>
		 [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_stop(UInt16 card);
		 
         /// <summary>
         /// 功  能：结束分选功能并清除所有数据 <para /> 
         ///参	数： CardNo		        控制卡卡号（通过拨码开关设置） <para />  
         ///返回值：错误代码
         /// </summary>
		 [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_clear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_clear(UInt16 card);

         /// <summary>
         /// 功  能：启动计算料宽功能 <para /> 
         ///参	数： CardNo		      控制卡卡号（通过拨码开关设置）<para />  
         ///返回值：错误码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_start_calc_picelength", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_start_calc_picelength(UInt16 card);

         /// <summary>
         /// 功  能：读取物料料宽 <para /> 
         ///参	数： CardNo		      控制卡卡号（通过拨码开关设置） <para />  
         ///Piecelength        控制卡计算的料宽值（单位：脉冲） <para /> 
         ///返回值：错误码
         /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_stop_calc_picelength", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_stop_calc_picelength(UInt16 card, ref UInt32 piecelength);

         /// <summary>
         /// 设置吹气异常检测配置
         /// </summary>
         /// <param name="card">控制卡卡号（通过拨码开关设置）</param>
         /// <param name="blowindex">检测吹气口序号（强制为吹气口0）</param>
         /// <param name="offset">吹气异常检测光纤到吹气口0的距离</param>
         /// <param name="highcnt">检测光纤对应的输入口序号（范围：2-7）</param>
         /// <param name="enable">是否使能（0-禁止 1-使能）</param>
         /// <returns>错误码</returns>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_set_blowcheckconfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_set_blowcheckconfig(UInt16 card, UInt16 blowindex, Int32 offset, UInt16 highcnt, UInt16 enable);

         /// <summary>
         /// 读取吹气异常检测配置
         /// </summary>
         /// <param name="card">控制卡卡号（通过拨码开关设置）</param>
         /// <param name="blowindex">检测吹气口序号（强制为吹气口0）</param>
         /// <param name="offset">吹气异常检测光纤到吹气口0的距离</param>
         /// <param name="highcnt">检测光纤对应的输入口序号（范围：2-7）</param>
         /// <param name="enable">是否使能（0-禁止 1-使能）</param>
         /// <returns>错误码</returns>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_set_blowcheckconfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_set_blowcheckconfig(UInt16 card, ref UInt16 blowindex, ref Int32 offset, ref UInt16 highcnt, ref UInt16 enable);

        /// <summary>
         /// 读取吹气异常检测状态
        /// </summary>
         /// <param name="card">控制卡卡号（通过拨码开关设置）</param>
         /// <param name="highcnt">经过检测装置的物料数量</param>
         /// <param name="blowednum">吹气口0已经触发吹气的物料数量</param>
         /// <param name="runnednum">已经经过吹气口0的物料数量（包含料宽和料间距不合格的料）</param>
         /// <param name="checkresult">未吹进吹气口0的物料数量（返回值为负数则表示检测到异常）</param>
         /// <returns>错误码</returns>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_blowcheck_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_blowcheck_status(UInt16 card, ref UInt32 highcnt, ref UInt32 blowednum, ref UInt32 runnednum, ref Int32 checkresult);

        /// <summary>
         /// 设置待料口吹气信息，该吹气口用于将不符合料宽、料间隔等物料自动吹至指定位置，吹气口位置等信息使用吹气口配置函数配置
        /// </summary>
         /// <param name="card">控制卡卡号（通过拨码开关设置）</param>
         /// <param name="blowindex">吹气口序号</param>
         /// <param name="enable">是否使能该待料吹气口（0-禁止 1-使能）</param>
        /// <returns>错误码</returns>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_blowinspect_setconfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_blowinspect_setconfig(UInt16 card, UInt16 blowindex, UInt16 enable);

         /// <summary>
         /// 读取待料口吹气信息，该吹气口用于将不符合料宽、料间隔等物料自动吹至指定位置，吹气口位置等信息使用吹气口配置函数配置
         /// </summary>
         /// <param name="card">控制卡卡号（通过拨码开关设置）</param>
         /// <param name="blowindex">吹气口序号</param>
         /// <param name="enable">是否使能该待料吹气口（0-禁止 1-使能）</param>
         /// <returns>错误码</returns>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_sort_blowinspect_getconfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern UInt32 PCI400_sort_blowinspect_setconfig(UInt16 card, ref UInt16 blowindex, ref UInt16 enable);


        //---------------------   编码器计数功能  ----------------------//

        /// <summary>
        ///功  能：读取指定轴编码器反馈位置脉冲计数值，范围：28位有符号数 <para /> 
        ///参  数： card          控制卡卡号（通过拨码开关设置） <para /> 
        ///axis		   指定轴号（轴号范围说明） <para /> 
        ///返回值：位置反馈脉冲值，单位：脉冲数
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_get_encoder(UInt16 card, UInt16 axis);

        /// <summary>
        ///功  能：读取指定轴编码器反馈位置脉冲计数值，范围：28位有符号数 <para /> 
        ///参  数： card            控制卡卡号（通过拨码开关设置） <para /> 
        ///enposall	 位置反馈脉冲值（以数组方式读取，例如数组的第0号元素对应第0轴的位置反馈脉冲值）<para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_encoder_all", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 PCI400_get_encoder_all(UInt16 card,  Int32[] enposall);

        /// <summary>
        ///功  能：设置指定轴编码器反馈脉冲计数值，范围：28位有符号数 <para /> 
        ///参  数：card            控制卡卡号（通过拨码开关设置）<para /> 
        ///        axis		      指定轴号（轴号范围说明）<para /> 
        ///        encoder_value	  编码器的设定值。<para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_set_encoder(UInt16 card,UInt16 axis,Int32 encoder_value);

        /// <summary>
        /// 功  能：设置指定轴的EZ信号的有效电平及其作用 <para /> 
        ///参  数：card              控制卡卡号（通过拨码开关设置）<para /> 
        ///axis				 指定轴号（轴号范围说明） <para /> 
        ///ez_logic			 EZ信号有效电平：0－低有效，1－高有效 <para /> 
        ///ez_mode			 EZ信号的工作方式：<para /> 
        ///0－EZ信号无效 <para /> 
        ///1－EZ是计数器复位信号 <para /> 
        ///2－EZ是原点信号，且不复位计数器 <para /> 
        ///3－EZ是原点信号，且复位计数器<para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_EZ_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_config_EZ_PIN(UInt16 card,UInt16 axis,UInt16 ez_logic, UInt16 ez_mode);

        /// <summary>
        /// 功  能：读取指定轴的EZ信号的有效电平及其作用 <para /> 
        ///参  数：card              控制卡卡号（通过拨码开关设置）<para /> 
        ///axis				 指定轴号（轴号范围说明） <para /> 
        ///ez_logic			 EZ信号有效电平：0－低有效，1－高有效 <para /> 
        ///ez_mode			 EZ信号的工作方式：<para /> 
        ///0－EZ信号无效 <para /> 
        ///1－EZ是计数器复位信号 <para /> 
        ///2－EZ是原点信号，且不复位计数器 <para /> 
        ///3－EZ是原点信号，且复位计数器<para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_EZ_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_get_config_EZ_PIN(UInt16 card,UInt16 axis,ref UInt16 ez_logic, ref UInt16 ez_mode);

        /// <summary>
        /// 功  能：设置/读取指定轴“锁存”信号的有效电平及其和工作方式。<para />
        ///参  数：card                 控制卡卡号（通过拨码开关设置）<para />
        ///axis					指定轴号（轴号范围说明）<para />
        ///ltc_logic				LTC信号逻辑电平：0－低有效，1－高有效<para />
        ///ltc_mode				0－EZ信号锁存，1－原点信号锁存<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_LTC_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_config_LTC_PIN(UInt16 card,UInt16 axis,UInt16 ltc_logic, UInt16 ltc_mode);

        /// <summary>
        ///功  能：设置锁存方式为单轴锁存或是八轴同时锁存 <para />
        ///参  数：cardno		控制卡卡号（通过拨码开关设置） <para />
        ///        all_enable	锁存方式 ：0－单独锁存， 1－八轴同时锁存 <para />
        ///返回值：错误代码
        ///</summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_latch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_config_latch_mode(UInt16 cardno, UInt16 all_enable);

        /// <summary>
        ///功  能：读取编码器锁存器的值 <para />
        ///参  数：card           控制卡卡号（通过拨码开关设置） <para />
        ///        axis		     指定轴号（轴号范围说明）<para />
        ///返回值：锁存器内的编码器脉冲数，单位：脉冲
        ///</summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_latch_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_get_latch_value(UInt16 card,UInt16 axis);

        /// <summary>
        ///功  能：读取指定控制卡的锁存器的标志位 <para />
        ///参  数：cardno		 控制卡卡号（通过拨码开关设置） <para />
        ///返回值：见表
        ///</summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_latch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_get_latch_flag(UInt16 cardno);

        /// <summary>
        ///功  能：复位指定控制卡所有轴的锁存器的标志位 <para />
        ///参  数：cardno		   控制卡卡号（通过拨码开关设置）<para />
        ///返回值：错误代码
        ///</summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_reset_latch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_reset_latch_flag(UInt16 cardno);

        /// <summary>
        ///功  能：复位指定控制卡指定轴的锁存器的标志位 <para />
        ///参  数：cardno		   控制卡卡号（通过拨码开关设置） <para />
        ///        axis 		       轴号（轴号范围说明）<para />
        ///返回值：错误代码
        ///</summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_reset_latch_flag_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_reset_latch_flag_ex(UInt16 cardno, UInt16 iaxis);

        /// <summary>
        ///功  能：读取指定控制卡的计数器的标识位 <para />
        ///参  数：cardno		   控制卡卡号（通过拨码开关设置） <para />
        ///</summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_counter_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_get_counter_flag(UInt16 cardno);

        /// <summary>
        ///功  能：复位计数器的计数标志位, 范围（0－N - 1,N为卡数） <para />
        ///参  数：cardno		   控制卡卡号（通过拨码开关设置）<para />
        ///返回值：错误代码
        ///</summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_reset_counter_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_reset_counter_flag(UInt16 cardno);

        /// <summary>
        ///功  能：复位计数器的清零标志位, 范围（0－N - 1,N为卡数）<para />
        ///参  数：cardno		   控制卡卡号（通过拨码开关设置）<para />
        ///返回值：错误代码
        ///</summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_reset_clear_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_reset_clear_flag(UInt16 cardno);

        [DllImport("PCI400.dll", EntryPoint = "PCI400_triger_chunnel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_triger_chunnel(UInt16 cardno, UInt16 num);

        ///<summary>
        ///功  能：设置编码器Speaker和LED的输出逻辑, 默认为低有效 <para />
        ///输  入： cardno      控制卡卡号（通过拨码开关设置）<para />
        ///logic        0：低有效， 1：高有效 <para />
        ///返回值：错误代码
        ///</summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_speaker_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_set_speaker_logic(UInt16 cardno, UInt16 logic);

        ///<summary>
        ///功  能：读取编码器Speaker和LED的输出逻辑, 默认为低有效 <para />
        ///输  入： cardno      控制卡卡号（通过拨码开关设置） <para />
        ///logic        0：低有效， 1：高有效 <para />
        ///返回值：错误代码
        ///</summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_speaker_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_get_speaker_logic(UInt16 cardno, ref UInt16 logic);

        ///<summary>
        ///功  能：设置/获取锁存方式为单轴锁存或是八轴同时锁存 <para />
        ///参  数：cardno		控制卡卡号（通过拨码开关设置） <para />
        ///        all_enable	锁存方式 ：0－单独锁存， 1－八轴同时锁存 <para />
        ///返回值：错误代码
        ///</summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_latch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_get_config_latch_mode(UInt16 cardno, ref UInt16 all_enable);

        //软件限位功能

        /// <summary>
        /// 功  能：设置软件限位的使能, 限位数值, 响应动作 <para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置） <para />
        ///axis			    指定轴号（轴号范围说明） <para />
        ///ON_OFF	    软限位使能， 0 –禁止； 1 –使能 <para />
        ///source_sel   	    比较源选择， 保留， 设置为指令脉冲。  <para />
        ///SL_action	    限位动作， 0 –减速停止， 1 –立即停止 <para />
        ///N_limit		    负限位值 <para />
        ///P _limit 			正限位值 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_softlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_config_softlimit(UInt16 card,UInt16 axis,UInt16 ON_OFF, UInt16 source_sel,UInt16 SL_action, Int32 N_limit,Int32 P_limit);

        /// <summary>
        /// 功  能：读取软件限位的使能, 限位数值, 响应动作 <para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置） <para />
        ///axis			    指定轴号（轴号范围说明） <para />
        ///ON_OFF	    软限位使能， 0 –禁止； 1 –使能 <para />
        ///source_sel   	    比较源选择， 保留， 设置为指令脉冲。  <para />
        ///SL_action	    限位动作， 0 –减速停止， 1 –立即停止 <para />
        ///N_limit		    负限位值 <para />
        ///P _limit 			正限位值 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_softlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_get_config_softlimit(UInt16 card,UInt16 axis,ref UInt16 ON_OFF,ref UInt16 source_sel,ref UInt16 SL_action,ref Int32 N_limit,ref Int32 P_limit);


        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_softlimit_area", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_config_softlimit_area(UInt16 card, UInt16 index, UInt16 axis0, UInt16 axis1, UInt16 ifencode, Int32 center0, Int32 center1,Int32 limit_radius,UInt16 enable);
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_softlimit_area", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_get_config_softlimit_area(UInt16 card, UInt16 index, ref UInt16 axis0,ref  UInt16 axis1, ref UInt16 ifencode, ref Int32 center0, ref Int32 center1,ref Int32 limit_radius,ref UInt16 enable);
        
        /// <summary>
        /// 功  能：设置区域软件限位2轴脉冲当量比率 <para />
        ///     用于设置使用区域软限位时，两轴脉冲当量不一样的情况，如相同可以不用设置该参数 <para />
        ///     如第1轴5000脉冲走1mm，第2轴10000脉冲走1mm，可以将ratio0设置为2，ratio1设置为1 <para />
        ///参  数：card		   控制卡卡号（通过拨码开关设置） <para />
        ///        iratio0           第1个轴比率<para />
        ///        iratio1           第2个轴比率<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_softlimit_area_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_config_softlimit_area_ratio(UInt16 card,UInt16 axis,Double iratio0,Double iratio1);
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_softlimit_area_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_get_config_softlimit_area_ratio(UInt16 card,UInt16 axis,ref Double iratio0,ref Double iratio1);



   		
		//can io 扩展
        /// <summary>
        /// 功  能：初始化CAN IO扩展模块 <para />
        /// 参  数：card            指定控制卡号（通过拨码开关设置） <para />
        ///canid            can io扩展模块的ID号，(取值范围：1－4) <para />
        ///ifenable          can io扩展模块是否使能（0为不使能，1为使能） <para />
        ///baudrate         保留参数 <para />
        ///返回值：错误代码
        /// </summary>
		[DllImport("PCI400.dll", EntryPoint = "PCI400_init_canio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_init_canio(UInt16 cardno, UInt16 canid,UInt16 ifenable,UInt16 baudrate);
		
        /// <summary>
        /// 功  能：读取CAN IO 输入口状态<para />
        ///参  数：card            指定控制卡号（通过拨码开关设置）<para />
        ///canid            can io扩展模块的ID号，(取值范围：1－4)<para />
        ///bitno            指定输入口位号（取值范围：0－19）<para />
        ///返回值：0表示低电平；1表示高电平
        /// </summary>
	    [DllImport("PCI400.dll", EntryPoint = "PCI400_canio_readinbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_canio_readinbit(UInt16 cardno, UInt16 canid,UInt16 bitno);

        /// <summary>
        /// 功  能：设置CAN IO输出口的状态 <para />
        ///参  数：card            指定控制卡号（通过拨码开关设置）<para />
        ///canid            can io扩展模块的ID号，(取值范围：1－4) <para />
        ///bitno            指定输出口位号（取值范围：0－19）<para />
        ///state          输出电平：0－表示输出低电平，1－表示输出高电平<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_canio_writeoutbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_canio_writeoutbit(UInt16 cardno,  UInt16 canid,UInt16 bitno,UInt16 on_off);

        /// <summary>
        /// 功  能：读取CAN IO 指定输出口状态 <para />
        ///参  数：card            指定控制卡号（通过拨码开关设置）<para />
        ///canid            can io扩展模块的ID号，(取值范围：1－4) <para />
        ///bitno            指定输出口位号（取值范围：0－19）<para />
        ///返回值：0表示低电平；1表示高电平
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_canio_readoutbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_canio_readoutbit(UInt16 cardno, UInt16 canid, UInt16 bitno);

        /// <summary>
        /// 功  能：读取CAN IO输入端口的值 <para />
        ///参  数：card            指定控制卡号（通过拨码开关设置）<para />
        ///canid           can io扩展模块的ID号，(取值范围：1－4) <para />
        ///返回值：通过读取该函数返回值的第0-19位分别获取CANIO模块硬件上的IN1-IN20的状态。
        /// </summary>
         [DllImport("PCI400.dll", EntryPoint = "PCI400_canio_readinport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 PCI400_canio_readinport(UInt16 cardno, UInt16 canid);

        /// <summary>
         /// 功  能：读取所有CAN IO输出口的状态 <para />
         ///参  数：card            指定控制卡号（通过拨码开关设置） <para />
         ///canid            can io扩展模块的ID号，(取值范围：1－4) <para />
        ///返回值：通过读取该函数返回值的第0-19位分别获取CANIO模块硬件上的OUT1-OUT20的状态。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_canio_readoutport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32  PCI400_canio_readoutport(UInt16 cardno, UInt16 canid) ;

        /// <summary>
        /// 功  能：设置CAN IO输出端口的值 <para />
        ///参  数：card            指定控制卡号（通过拨码开关设置） <para />
        ///canid            can io扩展模块的ID号，(取值范围：1－4) <para />
        ///state          bit0 – bi19位值分别代表第1–20号输出端口值。 <para />
        ///返回值：错误代码 <para />
        ///例子：PCI400_canio_writeoutport(0,1,0xFFFFF);  //关闭0号卡的1号CANIO模块的1–20号输出口。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_canio_writeoutport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32  PCI400_canio_writeoutport(UInt16 cardno, UInt16 canid, UInt32 port_value);

        /// <summary>
        /// 读取CAN IO扩展模块的连接状态
        /// </summary>
        /// <param name="cardno">指定控制卡号（通过拨码开关设置）</param>
        /// <param name="canid">can io扩展模块的ID号，(取值范围：1－4)</param>
        /// <param name="statelink">是否连接(0-断开，1-连接)</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_canio_linkstate", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_canio_linkstate(UInt16 cardno, UInt16 canid, ref UInt16 statelink);


        /// <summary>
        /// 功  能：初始化CAN 模拟量扩展模块 <para />
        ///参  数：card            指定控制卡号（通过拨码开关设置） <para />
        ///canid            can io扩展模块的ID号，(取值范围：1－4) <para />
        ///ifenable         can 扩展模块是否使能（0为不使能，1为使能） <para />
        ///type 1-(为4路AD输入,2路DA输出模块)   <para />
        ///     2- 4路DA输出模块 <para />
        ///返回值：错误代码 <para />
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_init_can_analog", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_init_can_analog(UInt16 cardno, UInt16 canid, UInt16 ifenable, UInt16 type);

        /// <summary>
        /// 功  能：读取CAN模块的指定通道号AD（模拟量输入） <para />
        ///参  数：card            指定控制卡号（通过拨码开关设置） <para />
        ///canid            can io扩展模块的ID号，(取值范围：1－4) <para />
        ///channel 通道号（范围：0-3） <para />
        ///返回值：指定通道号的AD值 <para />
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_canio_read_ain", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 PCI400_canio_read_ain(UInt16 card, UInt16 canid, UInt16 channel);

        /// <summary>
        /// 功  能：读取CAN模块的指定通道号DA（模拟量输出） <para />
        ///参  数：card            指定控制卡号（通过拨码开关设置） <para />
        ///canid            can io扩展模块的ID号，(取值范围：1－4) <para />
        ///channel 通道号（范围：0-3） <para />
        ///返回值：指定通道号的DA值 <para />
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_canio_read_aout", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_canio_read_aout(UInt16 card, UInt16 canid, UInt16 channel);

        /// <summary>
        /// 功  能：设置CAN模块的指定通道号DA（模拟量输出） <para />
        /// 参  数：card            指定控制卡号（通过拨码开关设置） <para />
        ///canid            can io扩展模块的ID号，(取值范围：1－4) <para />
        ///channel       通道号（范围：0-3） <para />
        ///value         DA值（范围：0-4096）对应（电压输出：0～10V；电流输出：0～20mA 或 4～20mA； DAC 分辨率：12 位）
        ///返回值：错误码 <para />
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_canio_set_aout", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_canio_set_aout(UInt16 card, UInt16 canid, UInt16 channel, UInt16 value);


		//io计数功能
        /// <summary>
        /// 功  能：设置/读取输入口计数参数 <para />
        ///参  数：CardNo             控制卡卡号（通过拨码开关设置） <para />
        ///bitno			    指定输入口号 <para />
        ///mode               输入口计数模式： <para />
        ///0－电平计数，只要电平发生变化计数加1 v
        ///1－下降沿计数 <para />
        ///2－上升沿计数 <para />
        ///Filterms             滤波时间（单位：毫秒） <para />             
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_io_count_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_set_io_count_mode(UInt16 cardno, UInt16 bitno, UInt16 mode, UInt16 filterms);

        /// <summary>
        /// 功  能：读取输入口计数参数 <para />
        ///参  数：CardNo             控制卡卡号（通过拨码开关设置） <para />
        ///bitno			    指定输入口号 <para />
        ///mode               输入口计数模式： <para />
        ///0－电平计数，只要电平发生变化计数加1 v
        ///1－下降沿计数 <para />
        ///2－上升沿计数 <para />
        ///Filterms             滤波时间（单位：毫秒） <para />             
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_io_count_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_get_io_count_mode(UInt16 cardno, UInt16 bitno, ref UInt16 mode, ref UInt16 filterms);

        /// <summary>
        /// 功  能：设置输入口计数值 <para /> 
        ///参  数：CardNo             控制卡卡号（通过拨码开关设置）<para /> 
        ///bitno			    指定输入口 <para /> 
        ///CountValue          计数值  <para />           
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_io_count_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_set_io_count_value(UInt16 cardno, UInt16 bitno, UInt32 CountValue);

        /// <summary>
        /// 功  能：读取输入口计数值 <para /> 
        ///参  数：CardNo             控制卡卡号（通过拨码开关设置）<para /> 
        ///bitno			    指定输入口 <para /> 
        ///CountValue          计数值  <para />           
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_io_count_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_get_io_count_value(UInt16 cardno, UInt16 bitno, ref UInt32 CountValue);
		
        /// <summary>
        /// 输出口延时翻转
        /// </summary>
        /// <param name="cardno">控制卡卡号（通过拨码开关设置）</param>
        /// <param name="bitno">指定输出口序号（范围：0-23）</param>
        /// <param name="reversetimems">输出口翻转时间（单位：ms）</param>
        /// <returns>错误代码</returns>
		[DllImport("PCI400.dll", EntryPoint = "PCI400_reverse_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_reverse_outbit(UInt16 cardno, UInt16 bitno, double reversetimems);

        /// <summary>
        /// 输出口设置电平并延时翻转
        /// </summary>
        /// <param name="cardno">控制卡卡号（通过拨码开关设置）</param>
        /// <param name="bitno">指定输出口序号（范围：0-23）</param>
        /// <param name="state">0表示低电平，1表示高电平</param>
        /// <param name="reversetimems">输出口翻转时间（单位：ms）</param>
        /// <returns>错误代码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_reverse_outbit_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_reverse_outbit_ex(UInt16 cardno, UInt16 bitno, UInt16 state, double reversetimems);

        //IO映射功能
        /// <summary>
        /// 功  能：设置IO映射配置 <para />
        ///参  数：CardNo             控制卡卡号（通过拨码开关设置） <para />
        ///axis			        轴号 <para />
        ///IoType             需要映射的类型 <para />  
        ///0-PEL正限位，1-NEL负限位，2-ORG原点，<para />
        ///        3-EMG急停，4-SD减速信号（保留），5-ALM报警信号，6-RDY伺服准备信号，7-INP伺服到位信号）<para />
        ///MapIoType        目标信号的类型（0-PEL正限位，1-NEL负限位，2-OR原点，3-ALM报警信号，4-RDY伺服准备信号，5-INP伺服到位信号，6-IN通用输入）<para />
        ///（设置成255则取消原有映射关系）<para />
        ///MapIoIndex        轴IO映射索引号<para />
        ///1）当目标信号的类型设置为6时,表示该映射对应的通用输入端口号,具体范围见下表<para />
        ///2）当目标信号的类型设置为0~5时，此参数可设置为该映射所对应的具体轴号，（轴号范围说明）<para />
        ///3）设置该值为255表示取消轴IO映射关系<para />
        ///Filter               特殊信号滤波时间（单位：250us）<para />
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_axis_io_map", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_set_axis_io_map(UInt16 cardno, UInt16 axis, UInt16 IoType, UInt16 MapIoType, UInt16 MapIoIndex, double Filter);

        /// <summary>
        /// 功  能：读取IO映射配置 <para />
        ///参  数：CardNo             控制卡卡号（通过拨码开关设置） <para />
        ///axis			        轴号 <para />
        ///IoType             需要映射的类型 <para />  
        ///0-PEL正限位，1-NEL负限位，2-ORG原点，<para />
        ///        3-EMG急停，4-SD减速信号（保留），5-ALM报警信号，6-RDY伺服准备信号，7-INP伺服到位信号）<para />
        ///MapIoType        目标信号的类型（0-PEL正限位，1-NEL负限位，2-OR原点，3-ALM报警信号，4-RDY伺服准备信号，5-INP伺服到位信号，6-IN通用输入）<para />
        ///（设置成255则取消原有映射关系）<para />
        ///MapIoIndex        轴IO映射索引号<para />
        ///1）当目标信号的类型设置为6时,表示该映射对应的通用输入端口号,具体范围见下表<para />
        ///2）当目标信号的类型设置为0~5时，此参数可设置为该映射所对应的具体轴号，（轴号范围说明）<para />
        ///3）设置该值为255表示取消轴IO映射关系<para />
        ///Filter               特殊信号滤波时间（单位：250us）<para />
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_axis_io_map", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_get_axis_io_map(UInt16 cardno, UInt16 axis, UInt16 IoType, ref UInt16 MapIoType, ref UInt16 MapIoIndex, ref double Filter);

        //轴类型和等待停止的问题
        /// <summary>
        /// 功  能：设置轴类型和到位距离 <para />
        ///参  数：CardNo       控制卡卡号（通过拨码开关设置） <para />
        ///iaxis          指定轴号（轴号范围说明） <para />
        ///type          轴类型 <para />
        ///1－步进类型，到位距离表示的是轴指令位置 <para />
        ///9－伺服类型，到位距离表示的是编码器值 <para />
        ///inpgap        到位距离(范围：0－65535) <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_axis_type", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_set_axis_type(UInt16 cardno, UInt16 iaxis, UInt16 type, UInt16 inpgap);

        /// <summary>
        /// 功  能：读取轴类型和到位距离 <para />
        ///参  数：CardNo       控制卡卡号（通过拨码开关设置） <para />
        ///iaxis          指定轴号（轴号范围说明） <para />
        ///type          轴类型 <para />
        ///1－步进类型，到位距离表示的是轴指令位置 <para />
        ///9－伺服类型，到位距离表示的是编码器值 <para />
        ///inpgap        到位距离(范围：0－65535) <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_axis_type", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_get_axis_type(UInt16 cardno, UInt16 iaxis, ref UInt16 type, ref UInt16 inpgap);

        /// <summary>
        /// 功  能：检测轴运动状态，如果PCI400_set_axis_type调用时，设置为伺服类型，则该函数先检测轴的指令发送完成，即PCI400_check_done返回1，然后再检测编码器值是否到达指定的到位区间内。<para />
        ///参  数：CardNo        控制卡卡号（通过拨码开关设置） <para />
        ///iaxis           指定轴号（轴号范围说明） <para />
        ///返回值：0－轴运动没有停止或指令发送完成但编码器没有到位，1－轴运动停止，对于伺服轴指令发送完成而且编码器到达指定的区间
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_check_done_inp", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt16 PCI400_check_done_inp(UInt16 cardno, UInt16 iaxis);

        /// <summary>
        /// 检测轴运动状态，还有检测指令位置和编码器位置误差
        /// </summary>
        /// <param name="cardno">卡号</param>
        /// <param name="iaxis">轴号</param>
        /// <param name="gap">指令位置和编码器位置误差</param>
        /// <returns></returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_check_done_epos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt16 PCI400_check_done_epos(UInt16 cardno, UInt16 iaxis, UInt16 gap);

		//缓存指令运动
        /// <summary>
        /// 功  能：缓冲区点位运动，立即执行 <para />
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para />
        ///fifo         	        缓存空间FIFO序号(0-3) <para />
        ///iaxis                指定运动轴（轴号范围说明） <para />
        ///distance             运动距离（单位：脉冲数）<para />
        ///mode               运动模式 0-相对运动1-绝对运动 <para />
        ///vss                 起始速度（单位：脉冲/秒） <para />
        ///vms                运行速度（单位：脉冲/秒）<para />
        ///ves                 结束速度（单位：脉冲/秒）<para />
        ///accs                加速时间（单位：秒）<para />
        ///accs                减速时间（单位：秒）<para />
        ///imark               标号(0-255) <para />
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_pmove(UInt16 cardno, UInt16 fifo, UInt16 iaxis, Int32 distance, UInt16 ifabs, double vs, double vm, double ve, double ts, double te, UInt16 imark);

        /// <summary>
        /// 功  能：缓存区执行多轴直线插补运动
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para />
        ///fifo         	       缓存空间FIFO序号(0-3)<para />
        ///iaxisnum           插补总轴数<para />
        ///iaxislist             插补轴号列表<para />
        ///distancelist          插补位置列表<para />
        ///mode               运动模式 0-相对运动1-绝对运动<para />
        ///imark               标号(0-255)<para />
        ///返回值：错误码<para />
        ///注意：该函数通过PCI400_set_vector_profile配置速度参数
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_line", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_line(UInt16 cardno, UInt16 fifo, UInt16 iaxisnum, UInt16[] iaxislist, Int32[] distancelist, UInt16 mode, UInt16 imark);

        /// <summary>
        /// 功  能：缓存区等待指定输入口到位<para />
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para />
        ///fifo         	       缓存空间FIFO序号(0-3)<para />
        ///ionum              输入口序号<para />
        ///state                输入口状态（0-低电平 1-高电平）<para />
        ///timeoutms          等待超时时间(单位：毫秒，最大65535)<para />
        ///imark               标号(0-255)<para />
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_waitin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_waitin(UInt16 cardno, UInt16 fifo, UInt16 inbitno, UInt16 state, UInt32 timeoutms, UInt16 imark);

        /// <summary>
        /// 功  能：缓存区操作输出口 <para />
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para />
        ///fifo         	       缓存空间FIFO序号(0-3) <para />
        ///ionum              输出口序号（范围：0-23）<para />
        ///state                输出口状态（0-低电平 1-高电平）<para />
        ///reversetimems       翻转时间（单位：毫秒）<para />
        ///imark               标号(0-255) <para />
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_outbit(UInt16 cardno, UInt16 fifo, UInt16 outbitno, UInt16 state, UInt32 reversetimems,UInt16 imark);

        /// <summary>
        /// 功  能：缓存区等待轴停止，该功能受PCI400_set_axis_type影响，作用同PCI400_check_done_inp<para />
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para />
        ///fifo         	        缓存空间FIFO序号(0-3)<para />
        ///iaxis                指定运动轴（轴号范围说明）<para />
        ///timeoutms           等待超时时间(单位：毫秒，最大65535)<para />
        ///imark               标号(0-255)<para />
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_checkdown", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_checkdown(UInt16 cardno, UInt16 fifo, UInt16 iaxis, UInt32 timeoutms, UInt16 imark);

        /// <summary>
        /// 功  能：缓存区等待轴停止，该功能受PCI400_set_axis_type影响，作用同PCI400_check_done_inp<para />
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para />
        ///fifo         	        缓存空间FIFO序号(0-3)<para />
        ///iaxismask                轴号掩码，按位操作轴，把对应位置1该函数就会等待对应位号轴是否停止，可以配置多轴<para />
        ///timeoutms               等待超时时间(单位：毫秒，最大65535)<para />
        ///imark                   标号(0-255)<para />
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_checkdown_more", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_checkdown_more(UInt16 cardno, UInt16 fifo, UInt16 iaxismask, UInt32 timeoutms, UInt16 imark);

        /// <summary>
        /// 功  能：缓存区等待轴停止，并检测停止原因<para />
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para />
        ///fifo         	        缓存空间FIFO序号(0-3)<para />
        ///iaxis                    轴号掩码，按位操作轴，把对应位置1该函数就会等待对应位号轴是否停止，可以配置多轴<para />
        ///timeoutms               等待超时时间(单位：毫秒，最大65535)<para />
        ///imark                   标号(0-255)<para />
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_checkdown_reason", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_checkdown_reason(UInt16 cardno, UInt16 fifo, UInt16 iaxis, UInt32 timeoutms, UInt16 imark);

        /// <summary>
        ///功  能：缓存区等待轴到达指定位置 <para />
        ///参  数：card                控制卡卡号（通过拨码开关设置） <para />
        ///fifo         	       缓存空间FIFO序号(0-3) <para />
        ///iaxis                指定运动轴（轴号范围说明） <para />
        ///distance             要求到达的位置（绝对位置） <para />
        ///ifencode            是否等待编码器（0-不等待  1-等待）<para />
        ///timeoutms          等待超时时间(单位：毫秒，最大65535) <para />
        ///imark               标号(0-255) <para />
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_waitpos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_waitpos(UInt16 cardno, UInt16 fifo, UInt16 iaxis, Int32 position, UInt16 ifencode, UInt32 timeoutms, UInt16 imark);
		
        /// <summary>
        /// 功  能：缓存区等待轴到达指定位置  <para />
        ///参  数：card                控制卡卡号（通过拨码开关设置） <para />
        ///fifo         	       缓存空间FIFO序号(0-3) <para />
        ///iaxis                指定运动轴（轴号范围说明） <para />
        ///distance             要求到达的位置（绝对位置） <para />
        ///ifencode            是否等待编码器（0-不等待  1-等待） <para />
        ///timeoutms          等待超时时间(单位：毫秒，最大65535) <para />
        ///dir                  0-负向(小于等于)，1-正向(大于等于) <para />
        ///imark               标号(0-255) <para />
        ///返回值：错误码
        /// </summary>
		[DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_comparepos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_comparepos(UInt16 cardno, UInt16 fifo, UInt16 iaxis, Int32 position, UInt16 ifencode, UInt16 dir, UInt32 timeoutms, UInt16 imark);

        /// <summary>
        /// 功  能：缓存区延时指定时间
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para /> 
        ///fifo         	        缓存空间FIFO序号(0-3) <para />
        ///iaxis                指定运动轴（轴号范围说明）<para />
        ///delayms             延时时间(单位：毫秒，最大65535)<para />
        ///imark               标号(0-255)<para />
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_delay(UInt16 cardno, UInt16 fifo, UInt16 delayms, UInt16 imark);

        /// <summary>
        /// 功  能：清除缓存空间 )<para />
        ///参  数：card                控制卡卡号（通过拨码开关设置）)<para />
        ///fifo         	       缓存空间FIFO序号(0-3) )<para />
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_clear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_clear(UInt16 cardno, UInt16 fifo);

        /// <summary>
        /// 功  能：缓存空间剩余空间大小 <para />
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para />
        ///fifo         	       缓存空间FIFO序号(0-3) <para />
        ///返回值：剩余空间大小(0-128)
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_remainspace", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_remainspace(UInt16 cardno, UInt16 fifo);

        /// <summary>
        /// 功  能：读取当前运行的缓存段标号 <para />
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para />
        ///fifo         	       缓存空间FIFO序号(0-3) <para />
        ///返回值：缓存段标号(0-255)
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_mark", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 PCI400_fifo_mark(UInt16 cardno, UInt16 fifo);

        /// <summary>
        /// 功  能：缓存区错误码 <para />
        ///参  数：card                控制卡卡号（通过拨码开关设置） <para />
        ///fifo         	       缓存空间FIFO序号(0-3) <para />
        ///返回值：错误码 <para />
        ///(0)//没有错误  <para />
        ///(1)//没有完成 <para />
        ///(2)//输入口超时出现 <para />
        ///(4)//等待编码器超时出现 <para />
        ///(8)//指定轴运动超时出现 <para />
        ///(16)//运动出现错误 <para />
        ///(32)//急停按钮有效 <para />
        ///(64)// 缓存满了 <para />
        ///(128)// 其他错误 <para />
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_state(UInt16 cardno, UInt16 fifo);

        /// <summary>
        /// 功  能：缓存区停止指定轴 <para />
        ///参  数：card                控制卡卡号（通过拨码开关设置） <para />
        ///fifo         	       缓存空间FIFO序号(0-3) <para />
        ///iaxis                指定运动轴（轴号范围说明） <para />
        ///mode               0-减速停止   1-立即停止 <para />
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_iaxisstop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_iaxisstop(UInt16 cardno, UInt16 fifo, UInt16 iaxis, UInt16 mode);

        /// <summary>
        /// 功  能：缓冲区设置变量 <para />
        ///参  数：card                控制卡卡号（通过拨码开关设置） <para />
        ///fifo         	       缓存空间FIFO序号(0-3) <para />
        ///index                变量序号（范围：0-15） <para />
        ///val                  变量值 <para />
        ///imark                标号(0-255)
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_set_val", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_set_val(UInt16 cardno, UInt16 fifo, UInt16 index, int val, UInt16 imark);

        /// <summary>
        /// 功  能：缓冲区等待变量 <para />
        ///参  数：card                控制卡卡号（通过拨码开关设置） <para />
        ///fifo         	       缓存空间FIFO序号(0-3) <para />
        ///index                变量序号（范围：0-15） <para />
        ///val                  变量值 <para />
        ///timeoutms            等待超时时间（单位：ms）<para />
        ///imark                标号(0-255)
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_wait_val", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_wait_val(UInt16 cardno, UInt16 fifo, UInt16 index, int val, UInt32 timeoutms,UInt16 imark);

        /// <summary>
        /// 功  能：缓冲区设置点位运动IO <para />
        ///参  数：card                控制卡卡号（通过拨码开关设置） <para />
        ///fifo         	       缓存空间FIFO序号(0-3) <para />
        ///iaxis                   指定运动轴号 <para />
        ///ioNum                   输出口序号 <para />
        ///iostate                 输出口状态（0-打开, 1-关闭）<para />
        ///delaytype               0-基于下一段点位运动的开始 ,1- 基于下一段点位运动结束
        ///iodelay100us            delaytype为0时是基于下一段点位运动的开始延时时间,为1时是基于下一段点位运动的结束提前时间（单位：100us） <para />
        ///reverse100us            输出口翻转时间（单位：100us），设置为0就不翻转<para />
        ///imark                   标号(0-255)
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_ptpio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_ptpio(UInt16 cardno, UInt16 fifo, UInt16 iaxis, UInt16 ioNum, UInt16 iostate, UInt16 delaytype, UInt32 iodelay100us, UInt32 reverse100us, UInt16 imark);

        /// <summary>
        /// 功  能：缓冲区清除IO缓存 <para />
        ///参  数：card                控制卡卡号（通过拨码开关设置） <para />
        ///fifo         	       缓存空间FIFO序号(0-3) <para />
        ///iaxis                   指定运动轴号 <para />
        ///imark                   标号(0-255)
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_clear_ptpio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_clear_ptpio(UInt16 cardno, UInt16 fifo, UInt16 iaxis, UInt16 imark);

        /// <summary>
        /// 缓存区做点位运动。（软着陆）
        /// </summary>
        /// <param name="cardno">控制卡卡号（通过拨码开关设置）</param>
        /// <param name="fifo">缓存空间FIFO序号(0-3)</param>
        /// <param name="iaxis">指定运动轴号</param>
        /// <param name="distance">（绝对/相对）位移值，单位：脉冲数</param>
        /// <param name="mode"> 位移模式设定：0表示相对位移，1表示绝对位移</param>
        /// <param name="disflex">低速长度（单位：脉冲）</param>
        /// <param name="speedlow">低速速度（单位：脉冲/秒）</param>
        /// <param name="imark"> 标号(0-255)</param>
        /// <returns></returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_pmove_flex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_pmove_flex(UInt16 cardno, UInt16 fifo, UInt16 iaxis, int distance, UInt16 mode, int disflex, double speedlow, UInt16 imark);

        /// <summary>
        /// 缓存区做点位运动。（前段缓拉）
        /// </summary>
        /// <param name="cardno">控制卡卡号（通过拨码开关设置）</param>
        /// <param name="fifo">缓存空间FIFO序号(0-3)</param>
        /// <param name="iaxis">指定运动轴号</param>
        /// <param name="distance">（绝对/相对）位移值，单位：脉冲数</param>
        /// <param name="mode"> 位移模式设定：0表示相对位移，1表示绝对位移</param>
        /// <param name="disflex">低速长度（单位：脉冲）</param>
        /// <param name="speedlow">低速速度（单位：脉冲/秒）</param>
        /// <param name="imark"> 标号(0-255)</param>
        /// <returns></returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_pmove_flex_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_pmove_flex_ex(UInt16 cardno, UInt16 fifo, UInt16 iaxis, int distance, UInt16 mode, int disflex, double speedlow, UInt16 imark);

        /// <summary>
        /// 缓存区配置轴的起始速度、最大速度、停止速度、加速时间、减速时间
        /// </summary>
        /// <param name="cardno">控制卡卡号</param>
        /// <param name="fifo">缓存空间FIFO序号(0-3)</param>
        /// <param name="iaxis">指定运动轴号</param>
        /// <param name="vss">起始速度（单位：脉冲/秒）</param>
        /// <param name="vms">最大速度（单位：脉冲/秒）</param>
        /// <param name="ves">停止速度（单位：脉冲/秒）</param>
        /// <param name="accs">加速时间（单位：秒）</param>
        /// <param name="decs">减速时间（单位：秒）</param>
        /// <param name="imark">标号(0-255)</param>
        /// <returns></returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_axispara", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_axispara(UInt16 cardno, UInt16 fifo, UInt16 iaxis, double vss, double vms, double ves, double accs, double decs, UInt16 imark);

        /// <summary>
        /// 设置缓存区状态
        /// </summary>
        /// <param name="cardno">控制卡卡号</param>
        /// <param name="fifo">缓存空间FIFO序号(0-3)</param>
        /// <param name="state">1-运行  2-暂停 3-停止</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_set_runstate", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_set_runstate(UInt16 cardno, UInt16 fifo, UInt16 state);

        /// <summary>
        /// 读取缓存区状态
        /// </summary>
        /// <param name="cardno">控制卡卡号</param>
        /// <param name="fifo">缓存空间FIFO序号(0-3)</param>
        /// <returns>缓存区状态:1-运行  2-暂停 3-停止</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_fifo_get_runstate", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_fifo_get_runstate(UInt16 cardno, UInt16 fifo);
    
		//串口灯板控制
        /// <summary>
        /// 功  能：控制灯板四个通道的光源亮度 <para />
        ///参  数：CardNo            指定控制卡号（通过拨码开关设置） <para />
        /// ports            通道号（数值范围：0-3） <para />
        ///value            光源亮度值（数值范围：0-255）  <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_write_ledports", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_write_ledports(UInt16 cardno, UInt16 port, UInt16 value);

        /// <summary>
        /// 功 能：设置四个通道的DA值  <para />
        ///参 数：CardNo 指定控制卡号（通过拨码开关设置） <para />
        ///ports 通道号（数值范围：0-3） <para />
        ///daval DA 值（数值范围：0-5000 对应 0-5V） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_da_out", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_set_da_out(UInt16 cardno, UInt16 ports, Int32 daval);

        /// <summary>
        /// 功 能：设置四个通道的DA值  <para />
        ///参 数：CardNo 指定控制卡号（通过拨码开关设置） <para />
        ///ports 通道号（数值范围：0-3） <para />
        ///daval DA 值（数值范围：0-5000 对应 0-5V） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_read_adports", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_read_adports(UInt16 cardno, UInt16 ports, ref Int32 daval);


        /// <summary>
        /// 功 能：设置四个通道的DA值  <para />
        ///参 数：CardNo 指定控制卡号（通过拨码开关设置） <para />
        ///ports 通道号（数值范围：0-3） <para />
		///ifuplimit 是否设置值为上限（数值范围：0-1） <para />
        ///adval AD 上下限值（数值范围：0-5000 对应 0-5V） <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_adlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_set_adlimit(UInt16 cardno, UInt16 ports, UInt16 ifuplimit,Int32 adval);
        //总线控制函数
        /// <summary>
        /// 功  能：读取总线的周期 <para />
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para />
        ///portnum                端口号(保留参数) <para />
        /// slaveid                 从站号(1001开始，1001表示第一个从站)  <para />
        /// icycletime              总线周期（单位：微秒）<para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_cycletime", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_cycletime(UInt16 card, UInt16 portnum, ref UInt32 icycletime);

        /// <summary> <para />
        /// 功  能：读取总线的错误码 <para />
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para />
        ///portnum                端口号(保留参数) <para />
        ///Errcode                 总线错误码 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_errcode(UInt16 card, UInt16 portnum, ref UInt32 errcode);

        /// <summary>
        /// 功  能：EtherCat SDO设置操作 <para />
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para />
        /// portnum                端口号(保留参数) <para />
        /// slaveid                 从站号(1001开始，1001表示第一个从站) <para /> 
        /// index                  对象字典的索引  <para /> 
        /// subindex               对象字典的子索引 <para />
        /// bitlength               数据位宽（单位：bit）<para />
        ///  ivalue                 写入的数值 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_write_sdo", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_write_sdo(UInt16 card, UInt16 portnum, UInt16 slaveid, UInt16 index, UInt16 subindex, UInt16 bitlength, UInt32 ivalue);

        /// <summary>
        /// 功  能：EtherCat SDO读取操作 <para />
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para />
        ///portnum                端口号(保留参数) <para />
        ///slaveid                 从站号(1001开始，1001表示第一个从站) <para />   
        ///index                   对象字典的索引 <para />   
        ///subindex                对象字典的子索引 <para />
        ///bitlength                数据位宽（单位：bit）<para />
        ///*ivalue                 写入的数值 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_read_sdo", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_read_sdo(UInt16 card, UInt16 portnum, UInt16 slaveid, UInt16 index, UInt16 subindex, UInt16 bitlength, ref UInt32 ivalue);

        /// <summary>
        /// 功  能：EtherCat RXPDO读取操作 <para />
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para />
        /// portnum                端口号(保留参数) <para />
        /// slaveid                 从站号(1001开始，1001表示第一个从站) <para />    
        /// index                   对象字典的索引 <para />   
        /// subindex               该RxPDO的子索引 <para />
        /// varbitlength            数据位宽（单位：bit）<para />
        ///  *dwvalue               读取的数值 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_read_rxpdo", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_read_rxpdo(UInt16 card, UInt16 portnum, UInt16 slaveid, UInt16 index, UInt16 subindex, UInt16 varbitlength, ref UInt32 dwvalue);

        /// <summary>
        /// 功  能：EtherCat TXPDO读取操作
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para />  
        ///portnum                端口号(保留参数) <para />  
        ///slaveid                 从站号(1001开始，1001表示第一个从站) <para />     
        ///index                   对象字典的索引  <para />   
        ///subindex               该TxPDO的子索引 <para />  
        ///varbitlength            数据位宽（单位：bit）<para />  
        ///*dwvalue               读取的数值 <para />  
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_read_txpdo", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_read_txpdo(UInt16 card, UInt16 portnum, UInt16 slaveid, UInt16 index, UInt16 subindex, UInt16 varbitlength, ref UInt32 dwvalue);

        /// <summary>
        /// 功  能：EtherCat  RXPDO设置操作 <para />
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para />
        ///portnum                端口号(保留参数) <para />
        ///slaveid                 从站号(1001开始，1001表示第一个从站) <para /> 
        ///index                  对象字典的索引  <para /> 
        ///subindex              该RxPDO的子索引 <para />
        ///varbitlength           数据位宽（单位：bit）<para />
        ///dwvalue               写入的数值 <para />
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_write_rxpdo", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_write_rxpdo(UInt16 card, UInt16 portnum, UInt16 slaveid, UInt16 index, UInt16 subindex, UInt16 varbitlength, UInt32 dwvalue);

        /// <summary>
        /// EtherCat  读取轴错误码实际操作603F对象字典
        /// </summary>
        /// <param name="card">控制卡卡号（通过拨码开关设置）</param>
        /// <param name="portnum">端口号(保留参数) </param>
        /// <param name="iaxis">指定运动轴号</param>
        /// <returns>轴错误码实际操作603F对象字典</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_read_axis_errorcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_read_axis_errorcode(UInt16 card, UInt16 portnum, UInt16 iaxis);

        /// <summary>
        /// 功  能：EtherCat 使能电机轴 <para />
        ///参  数： card            控制卡卡号（通过拨码开关设置）<para />
        ///iaxs                    轴号 <para />
        ///ifenable                 1-电机使能，0关闭使能  <para />       
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_set_axis_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_set_axis_enable(UInt16 card, UInt16 iaxis, UInt16 ifenable);

        /// <summary>
        /// 功  能：EtherCat 设置从站的状态 <para /> 
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para /> 
        /// portnum                端口号(保留参数) <para /> 
        /// slaveid                 从站号(1001开始，1001表示第一个从站) <para /> 
        /// state                   从站的状态   <para /> 
        /// timeout                超时时间（单位：毫秒）<para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_set_slave_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_set_slave_state(UInt16 card, UInt16 portnum, UInt16 slaveid, UInt16 state, UInt16 timeout);

        /// <summary>
        /// 功  能：EtherCat 读取从站的状态 <para /> 
        ///参  数：card                    控制卡卡号（通过拨码开关设置） <para /> 
        /// portnum                端口号(保留参数) <para /> 
        /// slaveid                 从站号(1001开始，1001表示第一个从站)  <para /> 
        /// *state                  从站的状态  <para />  
        /// timeout                超时时间（单位：毫秒）<para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_slave_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_slave_state(UInt16 card, UInt16 portnum, UInt16 slaveid, ref UInt16 state, UInt16 timeout);

        /// <summary>
        /// 功  能：EtherCat 设置轴的状态，可以变为虚轴或总线轴 <para />  
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para /> 
        /// iaxis                   轴号（范围：0-31） <para /> 
        /// type                   轴的状态 <para /> 
        ///                         1-脉冲轴 <para /> 
        ///                         2-虚拟轴 <para /> 
        ///                         3-总线轴  <para />  
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_set_axistype", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_set_axistype(UInt16 card, UInt16 portnum, UInt16 iaxis, UInt16 type);

        /// <summary>
        /// 功  能：EtherCat 读取轴的状态  <para />  
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para /> 
        /// iaxis                   轴号（范围：0-31） <para /> 
        /// type                   轴的状态 <para /> 
        ///                         1-脉冲轴 <para /> 
        ///                         2-虚拟轴 <para /> 
        ///                         3-总线轴  <para />  
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_axistype", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_axistype(UInt16 card, UInt16 portnum, UInt16 iaxis, ref UInt16 type);

        /// <summary>
        /// 功  能：EtherCat 设置轴的映射 <para />  
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para />  
        /// portnum                端口号(保留参数) <para />  
        /// slaveid                 从站号(1001开始，1001表示第一个从站) <para />  
        /// iaxis                   轴号 <para />  
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_set_axismap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_set_axismap(UInt16 card, UInt16 portnum, UInt16 slaveid, UInt16 iaxis);

        /// <summary>
        /// 功  能：EtherCat 读取轴的映射 <para /> 
        ///参  数：card                    控制卡卡号（通过拨码开关设置） <para /> 
        /// portnum                端口号(保留参数) <para /> 
        /// slaveid                 从站号(1001开始，1001表示第一个从站) <para /> 
        /// *iaxis                  轴号 <para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_axismap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_axismap(UInt16 card, UInt16 portnum, UInt16 slaveid, ref UInt16 iaxis);

        /// <summary>
        /// 功  能：EtherCat 读取轴对应的从站地址 <para /> 
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para /> 
        ///iaxis                   轴号 <para /> 
        ///slaveaddr               从站地址(1001开始)   <para /> 
        ///slaveaddr_1             从站地址(65536开始,自增地址) <para />             
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_axis_address", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_axis_address(UInt16 card, UInt16 iaxis, ref UInt16 slaveaddr, ref UInt16 slaveaddr_1);

        /// <summary>
        /// 功  能：EtherCat 设置主站为模拟运行 <para />  
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para />  
        ///portnum                端口号(保留参数) <para />  
        ///simrun                 0-关闭模拟运行 <para />  
        ///1-启动模拟运行（用于没有伺服电机时进行总线调试）<para />  
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_set_simrun", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_set_simrun(UInt16 card, UInt16 portnum, UInt16 simrun);

        /// <summary>
        /// 功  能：EtherCat 读取所有从站个数<para /> 
        ///参  数： card                    控制卡卡号（通过拨码开关设置）<para /> 
        ///portnum                端口号(保留参数)<para /> 
        ///  num                   返回从站个数  <para />      
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_totalslave", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_totalslave(UInt16 card, UInt16 portnum, ref UInt16 num);

        /// <summary>
        /// 功  能：EtherCat 读取指定从站的信息 <para />  
        ///参  数：card                    控制卡卡号（通过拨码开关设置）<para />  
        /// portnum                端口号(保留参数) v
        /// slaveid                 从站号(1001开始，1001表示第一个从站) <para />  
        /// info                    0- EC_DEVICE_TYPE <para />  
        ///DEVICE_TYPE_IN = 0      输入信号 <para />  
        ///                        DEVICE_TYPE_OUT=1     输出信号 <para />  
        ///                        DEVICE_TYPE_INOUT=2   输入输出信号 <para />  
        ///                        DEVICE_TYPE_AIN=3      模拟量输入信号 <para />  
        ///                        DEVICE_TYPE_AOUT=4    模拟量输出信号 <para />  
        ///                        DEVICE_TYPE_AINOUT=5  模拟量输入输出信号 <para />  
        ///                        DEVICE_TYPE_MOTOR=6   电机信息 <para />  
        ///                         
        ///                        1- FIXID <para />  
        ///                        2-Vendor            供应商  <para />  
        ///                        3-Product Code       产品标识号 <para />  
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_slaveinfo", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_slaveinfo(UInt16 card, UInt16 portnum, UInt16 slaveid,  UInt32 []info);

        /// <summary>
        /// 功  能：EtherCat 设置主站的状态 <para />  
        ///参  数：card                    控制卡卡号（通过拨码开关设置） <para />  
        ///portnum                端口号(保留参数) <para />  
        /// state                   主站的状态：UNKNOWN=0 <para />  
        ///                                             INIT=1 <para />  
        ///                                          PREOP=2 <para />  
        ///                                         SAFEOP=4 <para />  
        ///                                               OP=8 <para />  
        ///                                     BOOTSTRAP=3 <para />  
        ///                                     BCppDummy=0xFFF <para />  
        ///
        /// timeout                 超时时间（单位：毫秒）<para />          
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_set_master_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_set_master_state(UInt16 card, UInt16 portnum, UInt16 state, UInt16 timeout);

        /// <summary>
        /// 功  能：EtherCat 读取主站的状态 <para />  
        ///参  数：card                    控制卡卡号（通过拨码开关设置） <para />  
        ///portnum                端口号(保留参数) <para />  
        /// state                   主站的状态：UNKNOWN=0 <para />  
        ///                                             INIT=1 <para />  
        ///                                          PREOP=2 <para />  
        ///                                         SAFEOP=4 <para />  
        ///                                               OP=8 <para />  
        ///                                     BOOTSTRAP=3 <para />  
        ///                                     BCppDummy=0xFFF <para />  
        ///                                               <para /> 
        /// timeout                 超时时间（单位：毫秒）<para />          
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_master_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_master_state(UInt16 card, UInt16 portnum, ref UInt16 state, UInt16 timeout);

        /// <summary>
        /// 功  能： 读取总线轴状态，输出值为TxPDO，对象6041的值。<para /> 
        ///参  数： card                    控制卡卡号（通过拨码开关设置）<para /> 
        ///iaxis                   轴号  <para /> 
        ///  statemachine            返回总线轴的状态机<para /> 
        ///位序号	名称	描述<para /> 
        ///第0位	伺服准备好	READ TO SWITCH ON	1-有效，0-无效<para /> 
        ///第1位	可以开启伺服运行	SWITCH ON	1-有效，0-无效<para /> 
        ///第2位	伺服运行	OPERATION ENABLED	1-有效，0-无效<para /> 
        ///第3位	故障	FAULT	1-有效，0-无效<para /> 
        ///第4位	主回路上电	VOLTAGE ENABLED	1-有效，0-无效<para /> 
        ///第5位	快速停机	QUICK STOP	0-有效，1-无效<para /> 
        ///第6位	伺服不可运行	SWITCH ON DISABLED	1-有效，0-无效<para /> 
        ///第7位	警告	WARNING	1-有效，0-无效<para /> 
        ///第8位	保留位		<para /> 
        ///第9位	远程控制	REMOTE	1-有效，0-无效<para /> 
        ///第10位	目标到达	TAGET REACH	1-有效，0-无效<para /> 
        ///第11位	内部限制有效	INTERNAL LIMIT ACTIVE	1-有效，0-无效<para /> 
        ///第12位	保留位		<para /> 
        ///第13位	保留位	<para /> 	
        ///第14位	保留位	<para /> 	
        ///第15位	保留位	<para /> 	            
        ///返回值：错误代码 
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_axis_state_machine", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_axis_state_machine(UInt16 card, UInt16 iaxis, ref UInt16 axis_statemachine);

        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_set_axis_controlmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_set_axis_controlmode(UInt16 card, UInt16 iaxis, UInt16 mode);

        /// <summary>
        /// 功  能：EtherCat 设置回原点参数，会将电机的模式强制设置为6，回原点模式 <para /> 
        ///参  数： card                    控制卡卡号（通过拨码开关设置）<para /> 
        ///iaxis                   轴号 <para /> 
        ///  homemode             回原点模式 <para /> 
        ///  Highvel                回原点高速（单位：脉冲/秒） <para /> 
        ///  low_vel                回原点低速（单位：脉冲/秒） <para /> 
        ///  tacc                    加速时间（单位：秒）  <para />  
        ///  tdec                    减速时间（单位：秒）   <para /> 
        ///  offsetpos               原点偏移(保留参数，请设置为0)    <para />            
        /// 返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_set_home_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_set_home_profile(UInt16 card, UInt16 iaxis, UInt16 homemode, double Highvel, double low_vel, double tacc, double tdec, double offsetpos);

        /// <summary>
        /// 功  能：EtherCat 读取回原点参数，会将电机的模式强制设置为6，回原点模式 <para /> 
        ///参  数： card                    控制卡卡号（通过拨码开关设置）<para /> 
        ///iaxis                   轴号 <para /> 
        ///  homemode             回原点模式 <para /> 
        ///  Highvel                回原点高速（单位：脉冲/秒） <para /> 
        ///  low_vel                回原点低速（单位：脉冲/秒） <para /> 
        ///  tacc                    加速时间（单位：秒）  <para />  
        ///  tdec                    减速时间（单位：秒）   <para /> 
        ///  offsetpos               原点偏移(保留参数，请设置为0)    <para />            
        /// 返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_home_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_home_profile(UInt16 card, UInt16 iaxis, ref UInt16 homemode, ref double Highvel, ref double low_vel, ref double tacc, ref double tdec, ref double offsetpos);

        /// <summary>
        /// 功  能：EtherCat 启动总线轴回原点 ，强制设置为6模式，<para /> 
        ///参  数： card                    控制卡卡号（通过拨码开关设置）<para /> 
        ///iaxis                   轴号  <para />           
        ///返回值：错误代码 <para /> 
        ///注意：回原点过程中，要退出回原点运动，可以使用PCI400_ect_home_abort；请使用PCI400_ect_get_axis_state_machine来检测回原点运动是否完成。回原点运动完成后，清除编码器和当前位置。然后再将伺服模式切换回CSP模式。
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_home_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_home_move(UInt16 card, UInt16 iaxis);

        /// <summary>
        /// 功  能：总线轴退出原点运动 <para /> 
        ///参  数： card                    控制卡卡号（通过拨码开关设置）<para /> 
        ///iaxis                   轴号  <para />           
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_home_abort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_home_abort(UInt16 card, UInt16 iaxis);

        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_axisenable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_axisenable(UInt16 card, UInt16 iaxis);

        /// <summary>
        /// 功  能：EtherCat 读取回零标志 <para /> 
        ///参  数： card                    控制卡卡号（通过拨码开关设置） <para /> 
        ///iaxis                   轴号 <para /> 
        ///state                    0—回零未完成   1—回零完成  2—回零错误  <para />     
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_IfHoming", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_IfHoming(UInt16 card, UInt16 iaxis);

        /// <summary>
        /// 功  能：设置总线轴模式 <para />  
        ///参  数： card                    控制卡卡号（通过拨码开关设置） <para />  
        ///portnum                端口号(保留参数)
        ///iaxis                   轴号  <para />  
        ///mode                  支持以下两种模式 <para />             
        ///6 -回原点模式 <para />  
        ///8-CSP模式   <para />  
        ///10-力矩模式    <para />    
        ///返回值：错误代码 
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_set_axis_opmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_set_axis_opmode(UInt16 card, UInt16 portnum, UInt16 iaxis, UInt16 mode);

        /// <summary>
        /// 功  能：读取总线轴模式 <para />  
        ///参  数： card                    控制卡卡号（通过拨码开关设置） <para />  
        ///portnum                端口号(保留参数)
        ///iaxis                   轴号  <para />  
        ///mode                  支持以下两种模式 <para />             
        ///6 -回原点模式 <para />  
        ///8-CSP模式   <para />  
        ///10-力矩模式    <para />    
        ///返回值：错误代码 
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_axis_opmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_axis_opmode(UInt16 card, UInt16 portnum, UInt16 iaxis, ref UInt16 mode);

        /// <summary>
        /// 功  能：读取总线错误 <para />  
        ///参  数： card            控制卡卡号（通过拨码开关设置）<para />  
        ///portnum                端口号(保留参数) <para />  
        ///state                   总线状态（返回非0值说明总线无法建立）<para />  
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_bus_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_bus_state(UInt16 card, UInt16 portnum, ref UInt32 state);

                /// <summary>
        /// 功  能：读取总线错误 <para />  
        ///参  数： card            控制卡卡号（通过拨码开关设置）<para />  
        ///type                     类型 0-IN  1- OUT 2-AD 3-DA <para />  
        ///usIoNum                  返回值，表示指定类型的个数<para />  
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ect_get_device_ionum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ect_get_device_ionum(UInt16 card, UInt16 type, ref UInt16 usIoNum);


		//pvt 运动
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pt_table", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pt_table(UInt16 cardno, UInt16 iaxis, UInt16 icount, double[] pttimes, double[] points);

        [DllImport("PCI400.dll", EntryPoint = "PCI400_pvt_table", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pvt_table(UInt16 cardno, UInt16 iaxis, UInt16 icount, double[] pttimes, double[] points,  double[] vel);

        /// <summary>
        /// 功  能：进入PT模式  <para />  
        ///参  数：card                控制卡卡号（通过拨码开关设置）  <para />  
        ///iaxis			   指定轴号（轴号范围说明）  <para />       
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pt_enter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pt_enter(UInt16 cardno, UInt16 iaxis);

        /// <summary>
        /// 功  能：进入PVT模式 <para /> 
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para /> 
        ///iaxis			   指定轴号（轴号范围说明）<para /> 
        ///mode               保留参数  <para />           
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pvt_enter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pvt_enter(UInt16 cardno, UInt16 iaxis, UInt16 mode);

        /// <summary>
        /// 功  能：启动PT或PVT运动 <para />  
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para />  
        ///iaxisnum   	       轴个数 <para />  
        ///iaxislist             轴号列表  <para />       
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pvt_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pvt_move(UInt16 cardno, UInt16 iaxisnum,  UInt16[] iaxislist);

        /// <summary>
        /// 功  能：向缓存空间内增加PT数据 <para />  
        ///参  数：card                控制卡卡号（通过拨码开关设置） <para />  
        ///iaxis			   指定轴号（轴号范围说明） <para />      
        ///icount               数据个数 <para />  
        ///pttimes              时间列表(ms) <para />  
        ///pos                  位置列表 <para />  
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pt_fifoin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pt_fifoin(UInt16 cardno, UInt16 iaxis, UInt16 icount, double[] pttimes, double[] points);        

        /// <summary>
        /// 功  能：向缓存空间内增加PVT数据 <para /> 
        ///参  数：card                控制卡卡号（通过拨码开关设置） <para /> 
        ///iaxis			   指定轴号（轴号范围说明） <para />     
        ///mode               保留参数 <para /> 
        ///icount               数据个数  <para /> 
        ///pttimes              时间列表(ms) <para /> 
        ///pos                 位置列表 <para /> 
        ///vel                  速度列表 <para /> 
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pvt_fifoin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pvt_fifoin(UInt16 cardno, UInt16 iaxis, UInt16 mode, UInt16 icount, double[] pttimes, double[] points, double[] vel);
     
        /// <summary>
        /// 功  能：读取PVT或PT运动的缓存区已经存储数据个数 <para /> 
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para /> 
        ///iaxis			   指定轴号（轴号范围说明）  <para />      
        ///返回值：数据个数(0-2048)
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pvt_getsavenum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pvt_getsavenum(UInt16 cardno, UInt16 iaxis);

        /// <summary>
        /// 功  能：读取PVT或PT运动的缓存区剩余空间大小 <para /> 
        ///参  数：card                控制卡卡号（通过拨码开关设置）<para /> 
        ///iaxis			   指定轴号（轴号范围说明） <para />     
        ///返回值：剩余空间个数(0-2048)
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_pvt_remainbuf", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pvt_remainbuf(UInt16 cardno, UInt16 iaxis);

        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_lead_screwcomp", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_set_lead_screwcomp(UInt16 cardno, UInt16 iaxis, UInt16 n, Int32 startpos, Int32 lenPos, Int32[] pComPos,Int32[] pComNeg);

        [DllImport("PCI400.dll", EntryPoint = "PCI400_enable_lead_screwcomp", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_enable_lead_screwcomp(UInt16 cardno, UInt16 iaxis, UInt16 Ifenable);

        [DllImport("PCI400.dll", EntryPoint = "PCI400_set_lead_screwcomp2d", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_set_lead_screwcomp2d(UInt16 cardno, UInt16[] iaxislist, UInt16[] count,  Int32[] Interval, ref Int32 startpos, double[][] compval_x, double[][] compval_y);

        [DllImport("PCI400.dll", EntryPoint = "PCI400_enable_lead_screwcomp2d", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_enable_lead_screwcomp2d(UInt16 cardno, UInt16 Ifenable);

        [DllImport("PCI400.dll", EntryPoint = "PCI400_pmove_comp2d", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_pmove_comp2d(UInt16 cardno, UInt16[] iaxislist, Int32[] iposlist, UInt16 mode);

        [DllImport("PCI400.dll", EntryPoint = "PCI400_Cam_Move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_Cam_Move(UInt16 cardno, UInt16 iaxismaster, Int32 iaxisslave, Int32[] iposlistmaster, Int32[] iposlistslave, UInt16 pointnum);

        /// <summary>
        /// 功 能：操作脚本程序  <para /> 
        ///参 数：card 指定控制卡号（通过拨码开关设置）  <para /> 
        ///state 1-运行脚本程序  <para /> 
        ///2-暂停脚本程序  <para /> 
        ///3-恢复运行脚本程序（用于暂停后恢复运行程序） <para /> 
        ///4-停止脚本程序 <para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_shell_operate", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_shell_operate(UInt16 cardno, UInt16 state);

        /// <summary>
        /// 功 能：下载脚本程序 <para /> 
        ///参 数：card 指定控制卡号（通过拨码开关设置） <para /> 
        ///pfilename 脚本程序文件路径 <para /> 
        ///ifrun （保留） <para /> 
        ///返回值：错误代码 
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_shell_download_run", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_shell_download_run(UInt16 cardno, string pfilename, UInt16 ifrun);

        /// <summary>
        /// 功 能：发送在线指令 <para /> 
        ///参 数：card 指定控制卡号（通过拨码开关设置）<para /> 
        ///stringcmd 需要发送的指令字符串 <para /> 
        ///bufack 应答缓冲空间（发送指令之后收到的返回信息） <para /> 
        ///ackbufsize 需要的指令字符串缓冲空间大小（置为 2048 即可）<para /> 
        ///返回值：错误代码
        /// </summary>
        /// <param name="cardno"></param>
        /// <param name="stringcmd"></param>
        /// <param name="bufack"></param>
        /// <param name="ackbufsize"></param>
        /// <returns></returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_shell_onlinecmd", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_shell_onlinecmd(UInt16 cardno, string stringcmd, StringBuilder bufack, UInt16 ackbufsize);

        /// <summary>
        /// 功 能：读取脚本程序返回信息 <para /> 
        ///参 数：card 指定控制卡号（通过拨码开关设置）<para /> 
        ///bufack 接收到脚本信息（可用于读取程序错误） <para /> 
        ///ackbufsize 需要的指令字符串缓冲空间大小（置为 2048 即可）<para /> 
        ///返回值：错误代码
        /// </summary>
        /// <param name="cardno"></param>
        /// <param name="bufack"></param>
        /// <param name="ackbufsize"></param>
        /// <returns></returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_shell_readmsg", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_shell_readmsg(UInt16 cardno, StringBuilder bufack, UInt16 ackbufsize);

        /// <summary>
        /// 功 能：设置特殊输入信号滤波时间（包括限位(±EL)，伺服报警(ALM)，急停(EMG)信号） <para /> 
        ///参 数：Card 指定控制卡号（通过拨码开关设置） <para /> 
        ///timems 滤波时间（单位：250us）<para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_special_input_filter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_config_special_input_filter(UInt16 cardno, double timems);

        /// <summary>
        /// 功 能：读取特殊输入信号滤波时间（包括限位(±EL)，伺服报警(ALM)，急停(EMG)信号） <para /> 
        ///参 数：Card 指定控制卡号（通过拨码开关设置） <para /> 
        ///timems 滤波时间（单位：250us）<para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_special_input_filter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_get_config_special_input_filter(UInt16 cardno, ref double timems);

        /// <summary>
        /// 功 能：设置通用输入（IN）信号滤波时间 <para /> 
        ///参 数：Card 指定控制卡号（通过拨码开关设置） <para /> 
        ///timems 滤波时间（单位：250us）<para /> 
        ///返回值：错误代码
        /// </summary>
        /// <param name="cardno"></param>
        /// <param name="timems"></param>
        /// <returns></returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_input_filter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_config_input_filter(UInt16 cardno, double timems);

        /// <summary>
        /// 功 能：读取通用输入（IN）信号滤波时间 <para /> 
        ///参 数：Card 指定控制卡号（通过拨码开关设置） <para /> 
        ///timems 滤波时间（单位：250us）<para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_input_filter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_get_config_input_filter(UInt16 cardno, ref double timems);

        /// <summary>
        /// 功 能：设置原点（ORG）信号滤波时间 <para /> 
        ///参 数：Card 指定控制卡号（通过拨码开关设置） <para /> 
        ///timems 滤波时间（单位：250us）<para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_config_home_pin_filter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_config_home_pin_filter(UInt16 cardno, double timems);

        /// <summary>
        /// 功 能：读取原点（ORG）信号滤波时间 <para /> 
        ///参 数：Card 指定控制卡号（通过拨码开关设置） <para /> 
        ///timems 滤波时间（单位：250us）<para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_get_config_home_pin_filter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_get_config_home_pin_filter(UInt16 cardno, ref double timems);


        /// <summary>
        /// 功  能：取消凸轮参考轴和从轴链接  <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置）  <para /> 
        ///iaxis			    指定轴号（轴号范围说明）  <para /> 
        ///mode           停止模式，当前保留 （1-减速停止 0-立即停止）  <para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ecam_disengage", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ecam_disengage(UInt16 cardno, UInt16 iaxis, UInt16 mode);

        /// <summary>
        /// 功  能：启动凸轮参考轴和从站链接 <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置） <para /> 
        ///iaxis			    指定轴号（轴号范围说明） <para /> 
        ///imaster          主轴轴号 <para /> 
        ///istartpos         主轴到达该位置时启动跟随（单位：脉冲） <para /> 
        ///ifencoder        主轴是否为编码器轴（0-不是 1-是） <para /> 
        ///mode           跟随模式（保留） <para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ecam_engage", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ecam_engage(UInt16 cardno, UInt16 iaxis, UInt16 imaster, Int32 istartpos, UInt16 ifencoder, UInt16 mode);

        /// <summary>
        /// 功  能：凸轮数据压入，运动距离为负向就反向跟随，否则就正向跟随，对于一些输出或其他操作可以使用比较功能 <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置） <para /> 
        ///iaxis			指定轴号（轴号范围说明） <para /> 
        ///slave_endpos     当前段从轴运行的相对长度，即从电子凸轮起点的长度 <para /> 
        ///master_endpos     当前段参考轴运行的相对长度，即从电子凸轮起点的长度 <para /> 
        ///speed_ratio_start    当前段起点从轴和主轴的速度比率，即从/主速度比率，保留3位小数 <para /> 
        ///speed_ratio_end   当前段终点从轴和主轴的速度比率，即从/主速度比率，保留3位小数 <para /> 
        ///mode             保留参数 <para /> 
        ///iMark            当前数据段标号 <para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ecam_fifo_datain", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ecam_fifo_datain(UInt16 cardno, UInt16 iaxis, Int32 slave_endpos, Int32 master_endpos, float speed_ratio_start, float speed_ratio_end, UInt16 mode, UInt16 iMark);

        /// <summary>
        /// 功  能：凸轮数据清除  <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置）  <para /> 
        ///iaxis			    指定轴号（轴号范围说明）  <para /> 
        ///返回值：错误代码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ecam_fifo_clear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ecam_fifo_clear(UInt16 cardno, UInt16 iaxis);

        /// <summary>
        /// 功  能：查询凸轮数据剩余段数  <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置）  <para /> 
        ///iaxis			    指定轴号（轴号范围说明）  <para /> 
        ///返回值：凸轮数据剩余段数（最大64段）
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ecam_fifo_remian", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ecam_fifo_remian(UInt16 cardno, UInt16 iaxis);

        /// <summary>
        /// 功  能：当前正在运行的凸轮数据段标号  <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置）  <para /> 
        ///iaxis			    指定轴号（轴号范围说明）  <para /> 
        ///返回值：正在运行的凸轮数据段标号
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_ecam_get_curmark", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_ecam_get_curmark(UInt16 cardno, UInt16 iaxis);
		
		
		
		
		/// <summary>
        /// 功  能：定制功能 初始化多段运动  <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置）  <para /> 
        ///iaxis			    指定轴号（轴号范围说明）  <para /> 
		///iaxislist			指定轴号列表（轴号范围说明）  <para /> 
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_cstm_tbs_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_cstm_tbs_init(UInt16 cardno, UInt16 iaxis,ref UInt16 iaxislist);
		
		
		/// <summary>
        /// 功  能：定制功能 配置最后一段比较数据  <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置）  <para /> 
        ///iaxis			    指定轴号（轴号范围说明）  <para /> 
		///poslist   			位置列表和初始化轴数相同  <para /> 
		///dir       			比较方向，0-小于等于 1-大于等于  <para /> 
		///ifencoder   			是否比较编码器  <para /> 
		///timeoutms   			比较超时时间毫秒  <para /> 
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_cstm_tbs_compare_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_cstm_tbs_compare_config(UInt16 cardno, Int32 poslist,ref UInt16 dir,ref UInt16 ifencoder,UInt16 timeoutms);


		/// <summary>
        /// 功  能：定制功能 配置最后一段比较数据  <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置）  <para /> 
        ///seg   			    段号（0-2）  <para /> 
		///poslist   			位置列表和初始化轴数相同  <para /> 
		///vs       			起始速度列表  <para /> 
		///vm       			运行速度列表  <para /> 
		///ve       			结束速度列表  <para /> 
		///acc       			加速时间列表，单位秒  <para /> 
		///dec       			减速时间列表，单位秒  <para /> 
		///ifabs                是否为绝对运动，0-相对 1-绝对  <para /> 
		///delayms   			段最后是否延迟，单位毫秒  <para /> 
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_cstm_tbs_moveseg_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_cstm_tbs_moveseg_config(UInt16 cardno, UInt16 seg, Int32 poslist,ref double vs,ref double vm,ref double ve,ref double acc,ref double dec,UInt16 ifabs,UInt16 delayms);


		/// <summary>
        /// 功  能：定制功能 起动多段运动  <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置）  <para /> 
        ///iaxis			    指定轴号（轴号范围说明）  <para />  
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_cstm_tbs_move_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_cstm_tbs_move_start(UInt16 cardno);
		
		/// <summary>
        /// 功  能：定制功能 停止多段运动  <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置）  <para /> 
        ///iaxis			    指定轴号（轴号范围说明）  <para />  
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_cstm_tbs_move_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_cstm_tbs_move_stop(UInt16 cardno);
		
		/// <summary>
        /// 功  能：定制功能 读取比较完成标志  <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置）  <para />         
        ///返回值：完成标志，0没有完成  1-完成 2-错误
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_cstm_tbs_get_compare_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_cstm_tbs_get_compare_flag(UInt16 cardno);
		
		
		/// <summary>
        /// 功  能：定制功能 PCI400_cstm_tbs_get_seg_flag  <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置）  <para />   
        ///参  数：seg  			指定段号   <para />  		
        ///返回值：完成标志，0没有完成  1-完成 2-错误
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_cstm_tbs_get_seg_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_cstm_tbs_get_seg_flag(UInt16 cardno,UInt16 seg);
		
        enum STATE_CODE_ECT
        {
            eEcatState_UNKNOWN = 0,                        /*< unknown */
            eEcatState_INIT = 1,                          /*< init */
            eEcatState_PREOP = 2,                         /*< pre-operational */
            eEcatState_SAFEOP = 4,                        /*< safe operational */
            eEcatState_OP = 8,                            /*< operational */
            eEcatState_BOOTSTRAP = 3,                       /*< BootStrap */
            eEcatState_BCppDummy = 0xFFFF
        };
        
	    /// <summary>
        /// 功  能：设置暂停输出IO  <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置）  <para /> 
        ///icoor			    插补器号0-1  <para /> 
        ///iaction              动作  <para /> 
        ///iomask               io掩码  <para /> 
        ///ioutval              io值  <para /> 
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_set_pause_output_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_set_pause_output_muticoor(UInt16 CardNo, UInt16 icoor, UInt16 iaction, UInt32 iomask, UInt32 ioutval);


        /// <summary>
        /// 功  能：设置暂停输出IO  <para /> 
        ///参  数：CardNo			指定控制卡号（通过拨码开关设置）  <para /> 
        ///CrdNum			    插补器号0-1  <para /> 
        ///canid              扩展CAN模块ID号(1-4)  <para /> 
        ///channel               通道号(0-3)  <para /> 
        ///daval              输出DA值(0-4096) <para /> 
        ///返回值：错误码
        /// </summary>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_conti_canio_da_muticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_conti_canio_da_muticoor(UInt16 CardNo, UInt16 CrdNum, UInt16 canid, UInt16 channel, UInt16 daval);

        /// <summary>
        /// 功能描述: 高精度延时
        /// </summary>
        /// <param name="timeus">延时时间(单位:us)</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_HighPerformaceDelay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int PCI400_HighPerformaceDelay(Int32 timeus);

        /// <summary>
        /// 高精度计数器时间
        /// </summary>
        /// <returns>时间(单位:毫秒)</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_HighPerformaceCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double PCI400_HighPerformaceCount();

        /// <summary>
        /// 设置输入口事件
        /// </summary>
        /// <param name="cardid">指定控制卡号（通过拨码开关设置）</param>
        /// <param name="group">组号（范围：0-7）</param>
        /// <param name="innum">输入口序号</param>
        /// <param name="delayms">延时时间（单位：ms）（范围：0-65535）</param>
        /// <param name="trigType">触发类型 0-低电平触发 1-高电平触发 2-上升沿触发 3-下降沿触发 4-边沿触发</param>
        /// <param name="iaxis">操作的轴号（轴号范围说明）</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_in_event_set_iopara", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_in_event_set_iopara(UInt16 cardid, UInt16 group, UInt16 innum, UInt16 delayms, UInt16 trigType, UInt16 iaxis);

        /// <summary>
        /// 读取输入口事件
        /// </summary>
        /// <param name="cardid">指定控制卡号（通过拨码开关设置）</param>
        /// <param name="group">组号（范围：0-7）</param>
        /// <param name="innum">输入口序号</param>
        /// <param name="delayms">延时时间（单位：ms）（范围：0-65535）</param>
        /// <param name="trigType">触发类型 0-低电平触发 1-高电平触发 2-上升沿触发 3-下降沿触发 4-边沿触发</param>
        /// <param name="iaxis">操作的轴号（轴号范围说明）</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_in_event_get_iopara", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_in_event_get_iopara(UInt16 cardid, UInt16 group, ref UInt16 innum, ref UInt16 delayms, ref UInt16 trigType, ref UInt16 iaxis);

        /// <summary>
        /// 设置输入口事件运动参数
        /// </summary>
        /// <param name="cardid">指定控制卡号（通过拨码开关设置）</param>
        /// <param name="group">组号（范围：0-7）</param>
        /// <param name="movetype">触发类型 0-绝对坐标点位运动 1-JOG运动 2-减速停止 3-立即停止 4-更新运动参数 </param>
        /// <param name="aimpos">目标位置（单位：脉冲）</param>
        /// <param name="vs">起始速度</param>
        /// <param name="vm">运行速度</param>
        /// <param name="acc">加速时间（单位：秒）</param>
        /// <param name="dec">减速时间（单位：秒）</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_in_event_set_movepara", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_in_event_set_movepara(UInt16 cardid, UInt16 group, UInt16 movetype, Int32 aimpos, double vs, double vm, double acc, double dec);

        /// <summary>
        /// 读取输入口事件运动参数
        /// </summary>
        /// <param name="cardid">指定控制卡号（通过拨码开关设置）</param>
        /// <param name="group">组号（范围：0-7）</param>
        /// <param name="movetype">触发类型 0-绝对坐标点位运动 1-JOG运动 2-减速停止 3-立即停止 4-更新运动参数 </param>
        /// <param name="aimpos">目标位置（单位：脉冲）</param>
        /// <param name="vs">起始速度</param>
        /// <param name="vm">运行速度</param>
        /// <param name="acc">加速时间（单位：秒）</param>
        /// <param name="dec">减速时间（单位：秒）</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_in_event_get_movepara", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_in_event_get_movepara(UInt16 cardid, UInt16 group, ref UInt16 movetype, ref Int32 aimpos, ref double vs, ref double vm, ref double acc, ref double dec);

        /// <summary>
        /// 设置输入口事件是否使能
        /// </summary>
        /// <param name="cardid">指定控制卡号（通过拨码开关设置）</param>
        /// <param name="group">组号（范围：0-7）</param>
        /// <param name="ifenable">是否使能（0：不使能 1：使能）</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_in_event_set_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_in_event_set_enable(UInt16 cardid, UInt16 group, UInt16 ifenable);

        /// <summary>
        /// 读取输入口事件是否使能
        /// </summary>
        /// <param name="cardid">指定控制卡号（通过拨码开关设置）</param>
        /// <param name="group">组号（范围：0-7）</param>
        /// <param name="ifenable">是否使能（0：不使能 1：使能）</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_in_event_get_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_in_event_get_enable(UInt16 cardid, UInt16 group, ref UInt16 ifenable);

        /// <summary>
        /// 配置采集参数
        /// </summary>
        /// <param name="card">卡号</param>
        /// <param name="channel">通道号，0-3</param>
        /// <param name="target">采集对象序号</param>
        /// <param name="type">采集类型0-指令位置 1-指令速度2-运动状态(运动或停止)3-编码器位置4-其他状态待定5- 输入口状态6- 输出口状态</param>
        /// <param name="time500us">时间间隔，单位500us，写1表示500us采样一次</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_daq_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_daq_config(UInt16 card, UInt16 channel, UInt16 target, UInt16 type, UInt16 time500us);

        /// <summary>
        /// 读取采集数据，每次调用出栈一个采集数据
        /// </summary>
        /// <param name="card">卡号</param>
        /// <param name="channel">通道号，0-3</param>
        /// <param name="data">采集数据</param>
        /// <param name="datanum">剩余缓存数据个数，非0表示缓存有存储数据</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_daq_data", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_daq_data(UInt16 card, UInt16 channel, ref Int32 data, ref Int32 datanum);

        /// <summary>
        /// 停止采集数据
        /// </summary>
        /// <param name="card">卡号</param>
        /// <param name="channel">通道号，0-3 </param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_daq_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_daq_stop(UInt16 card, UInt16 channel);

        /// <summary>
        /// 启动采集数据
        /// </summary>
        /// <param name="card">卡号</param>
        /// <param name="channel">通道号，0-3 </param>
        /// <param name="type">启动类型 :0-	表示正常启动,1-	表示先清除缓存再启动</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_daq_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_daq_start(UInt16 card, UInt16 channel, UInt16 type);

        /// <summary>
        /// 清除采集数据
        /// </summary>
        /// <param name="card">卡号</param>
        /// <param name="channel">通道号，0-3 </param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_daq_clear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_daq_clear(UInt16 card, UInt16 channel);

        /// <summary>
        /// 设置机械手模式
        /// </summary>
        /// <param name="card">卡号</param>
        /// <param name="type"> 100(4轴直角坐标系机械手，第3轴为旋转轴)，101(5轴直角坐标系机械手，第3轴为旋转轴，第4轴为摆动轴)</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_set_frametype", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_set_frametype(UInt16 card, UInt16 type);

        /// <summary>
        /// 读取机械手模式
        /// </summary>
        /// <param name="card">卡号</param>
        /// <param name="type"> 100(4轴直角坐标系机械手，第3轴为旋转轴)，101(5轴直角坐标系机械手，第3轴为旋转轴，第4轴为摆动轴)</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_get_frametype", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_get_frametype(UInt16 card, ref UInt16 type);

        /// <summary>
        /// 设置轴映射关系和轴的脉冲当量
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="indexs">0表示X轴 1-表示Y轴 2表示Z轴  3表示旋转轴 4表示摆动轴,对应关节坐标系</param>
        /// <param name="map2axis">映射到对应的那个轴</param>
        /// <param name="unit">当量，除旋转轴和摆轴当量表示一圈脉冲数外，其余轴设为1</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_set_act_axismap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_set_act_axismap(UInt16 CardNo, UInt16 indexs, UInt16 map2axis, UInt32 unit);

        /// <summary>
        /// 读取轴映射关系和轴的脉冲当量
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="indexs">0表示X轴 1-表示Y轴 2表示Z轴  3表示旋转轴 4表示摆动轴,对应关节坐标系</param>
        /// <param name="map2axis">映射到对应的那个轴</param>
        /// <param name="unit">当量，除旋转轴和摆轴当量表示一圈脉冲数外，其余轴设为1</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_get_act_axismap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_get_act_axismap(UInt16 CardNo, UInt16 indexs, ref UInt16 map2axis, ref UInt32 unit);

        /// <summary>
        /// 设置虚轴映射关系和轴的脉冲当量
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="indexs">0表示X轴 1-表示Y轴 2表示Z轴  3表示旋转轴 4表示摆动轴,对应关节坐标系</param>
        /// <param name="map2axis">映射到对应的那个轴</param>
        /// <param name="unit">当量，除旋转轴和摆轴当量表示一圈脉冲数外，其余轴设为1</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_set_virtual_axismap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_set_virtual_axismap(UInt16 CardNo, UInt16 indexs, ref UInt16 map2axis, ref UInt32 unit);

        /// <summary>
        /// 设置虚轴映射关系和轴的脉冲当量
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="indexs">0表示X轴 1-表示Y轴 2表示Z轴  3表示旋转轴 4表示摆动轴,对应关节坐标系</param>
        /// <param name="map2axis">映射到对应的那个轴</param>
        /// <param name="unit">当量，除旋转轴和摆轴当量表示一圈脉冲数外，其余轴设为1</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_get_virtual_axismap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_get_virtual_axismap(UInt16 CardNo, UInt16 indexs, ref UInt16 map2axis, ref UInt32 unit);

        /// <summary>
        /// 对机械手进行正逆解运算
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="pt_in_list">(维数至少为8) 输入的坐标，逆解时为世界坐标系坐标，正解时为关节坐标系</param>
        /// <param name="pt_ret_list">(维数至少为8)   计算结果，逆解时为关节坐标系，正解时为世界坐标系坐标</param>
        /// <param name="mode"> 0-为正解 1-逆解</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_frame_calc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_frame_calc(UInt16 CardNo, double[] pt_in_list, double[] pt_ret_list, UInt16 mode);

        /// <summary>
        /// 设置校正坐标输入
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="indexs">校正点序号</param>
        /// <param name="pt_g_list">(维数至少为8) 输入关节轴的坐标</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_calibrate_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_calibrate_config(UInt16 CardNo, UInt16 indexs, double[] pt_g_list);

        /// <summary>
        /// 读取校正坐标输入
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="indexs">校正点序号</param>
        /// <param name="pt_g_list">(维数至少为8) 输入关节轴的坐标</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_calibrate_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_calibrate_get_config(UInt16 CardNo, UInt16 indexs, double[] pt_g_list);

        /// <summary>
        /// 对机械手进行校正运算
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="pt_g_list">(维数至少为8) 计算结果</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_calibrate_calc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_calibrate_calc(UInt16 CardNo, double[] pt_g_list);

        /// <summary>
        /// 启动机械手运动
        /// </summary>
        /// <param name="CardNo">卡号 </param>
        /// <param name="Recalc">保留参数</param>
        /// <param name="mode">模式z:0-各轴用自己的加减速进行跟随，只能保证终点准确,1-各轴紧密跟随世界坐标，不关注加减速度，可能会有冲击</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_frame_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_frame_start(UInt16 CardNo, UInt16 Recalc, UInt16 mode);


        /// <summary>
        /// 退出机械手模式
        /// </summary>
        /// <param name="CardNo">卡号 </param>
        /// <param name="mode">保留参数</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_frame_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_frame_stop(UInt16 CardNo, UInt16 mode);

        /// <summary>
        /// 设置机械手跟随皮带等流水线功能，实现流水线作业
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="mode">1 停止跟随,10-从轴列表中的第一个轴跟随,20-从轴列表中的第二个轴跟随,30-从轴列表中的第三个轴跟随</param>
        /// <param name="angle">皮带轴或者流水线(以下都称主轴)和从轴列表中跟随轴1/2正向的夹角</param>
        /// <param name="followtimems">从启动跟随到和主轴速度同步需要的时间</param>
        /// <param name="master_axis">主轴的轴序号或者编码器序号</param>
        /// <param name="master_ifencoder">主轴是否为编码器</param>
        /// <param name="synpos_master">启动同步时主轴的位置</param>
        /// <param name="master_unit">主轴脉冲当量，即1mm对应多少脉冲</param>
        /// <param name="max_slaves">跟随轴个数</param>
        /// <param name="slave_list">跟随轴列表</param>
        /// <param name="slave_poslist">启动跟随时从轴位置列表和主轴时间点相同</param>
        /// <param name="slave_flow_ratio">从轴跟随比率列表</param>
        /// <param name="slave_unit">从轴脉冲当量列表，即1mm对应多少脉冲</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_frame_movesync", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_frame_movesync(UInt16 CardNo, UInt16 mode, double angle, double followtimems, UInt16 master_axis, UInt16 master_ifencoder, Int32 synpos_master,
                                                       UInt32 master_unit, UInt16 max_slaves, UInt16[] slave_list, Int32[] slave_poslist, double[] slave_flow_ratio, Int32[] slave_unit);

        /// <summary>
        /// 设置机械手退出跟随皮带等流水线功能，回到开始跟随位置
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="mode">1 停止跟随</param>
        /// <returns>错误码</returns>
        [DllImport("PCI400.dll", EntryPoint = "PCI400_robot_frame_movesync_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 PCI400_robot_frame_movesync_stop(UInt16 CardNo, UInt16 mode);

        enum ERR_CODE_HYCARD
		{
			ERR_HY_NOERR = 0,          //成功
			ERR_HY_UNKNOWN = 1,
			ERR_HY_PARAERR = 2,
				
			ERR_HY_TIMEOUT = 3,
			ERR_HY_CONTROLLERBUSY = 4,
			ERR_HY_CONNECT_TOOMANY = 5,

			
			ERR_HY_CONTILINE = 6,
			ERR_HY_CANNOT_CONNECTETH = 8,
			ERR_HY_HANDLEERR = 9,
			ERR_HY_SENDERR = 10,
			ERR_HY_FIRMWAREERR = 12, //固件文件错误
			ERR_HY_FIRMWAR_MISMATCH = 14, //固件不匹配

			ERR_HY_FIRMWARE_INVALID_PARA    = 20,  //固件参数错误
			ERR_HY_FIRMWARE_PARA_ERR    = 20,  //固件参数错误2
			ERR_HY_FIRMWARE_STATE_ERR    = 22, //固件当前状态不允许操作
			ERR_HY_FIRMWARE_LIB_STATE_ERR    = 22, //固件当前状态不允许操作2
			ERR_HY_FIRMWARE_CARD_NOT_SUPPORT    = 24,  //固件不支持的功能 控制器不支持的功能
			ERR_HY_FIRMWARE_LIB_NOTSUPPORT    = 24,    //固件不支持的功能2
			ERR_HY_LOOKAHEAD_REMAIN           = 900000,//控制卡内部空间已经满了，但是速度前瞻区还有缓存段没有进去
		};


		//PC库错误码
		enum ERR_CODE_DMC
		{
			ERR_NOERR = 0,          //成功
			ERR_UNKNOWN = 1,
			ERR_PARAERR = 2,
				
			ERR_TIMEOUT = 3,
			ERR_CONTROLLERBUSY = 4,
			ERR_CONNECT_TOOMANY = 5,
				
			ERR_CONTILINE = 6,
			ERR_CANNOT_CONNECTETH = 8,
			ERR_HANDLEERR = 9,
			ERR_SENDERR = 10,
			ERR_FIRMWAREERR = 12, //固件文件错误
			ERR_FIRMWAR_MISMATCH = 14, //固件不匹配
				
			ERR_FIRMWARE_INVALID_PARA    = 20,  //固件参数错误
			ERR_FIRMWARE_PARA_ERR    = 20,  //固件参数错误2
			ERR_FIRMWARE_STATE_ERR    = 22, //固件当前状态不允许操作
			ERR_FIRMWARE_LIB_STATE_ERR    = 22, //固件当前状态不允许操作2
			ERR_FIRMWARE_CARD_NOT_SUPPORT    = 24,  //固件不支持的功能 控制器不支持的功能
			ERR_FIRMWARE_LIB_NOTSUPPORT    = 24,    //固件不支持的功能2
			ERR_LOOKAHEAD_REMAIN           = 900000,//控制卡内部空间已经满了，但是速度前瞻区还有缓存段没有进去
		};

    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
