﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/DMC1000S/dmc1000SConfig.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionCardRes.DMC1000S
{
    public class dmc1000SConfig
    {
        private static dmc1000SConfig _instance;
        public static dmc1000SConfig Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new dmc1000SConfig();
                return dmc1000SConfig._instance;
            }
            set { dmc1000SConfig._instance = value; }
        }

        private string ProductModePath = AppDomain.CurrentDomain.BaseDirectory + "CardUserConfigParameter";
        public CardConfigData configdata { get; set; } = new CardConfigData();

        public dmc1000SConfig()
        {
            if(!Directory.Exists(ProductModePath))
            {
                Directory.CreateDirectory(ProductModePath);
            }
        }

        /// <summary>
        /// 保存配置文件到磁盘  异常：Exception
        /// </summary>
        /// <param name="confidata"></param>
        /// <param name="filename"></param>
        public void SaveConfig(CardConfigData confidata, string filename)
        {
            try
            {
                var fpath = $"{ProductModePath}\\{filename}";
                var str1 = Newtonsoft.Json.JsonConvert.SerializeObject(confidata);
                WenQiZhi.Domain.MotionCard.Common.FileData data = new WenQiZhi.Domain.MotionCard.Common.FileData(fpath);
                data.SaveData(str1);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 载入配置文件 异常：ArgumentException
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public CardConfigData LoadConfig(string filename)
        {
            var fpath = $"{ProductModePath}\\{filename}";
            WenQiZhi.Domain.MotionCard.Common.FileData data = new WenQiZhi.Domain.MotionCard.Common.FileData(fpath);
            var str1 = data.ReadData();

            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<CardConfigData>(str1);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("读产品配置文件反序列化时失败，详细信息：" + ex.Message);
            }
        }

    }

    public class CardConfigData
    {
        /*
       DWORD d1000_set_sd (short axis, short SdMode) 
       功 能：设置减速信号是否使能 
       参 数：axis：轴号，范围 0～(n×4-1)， n 为卡数  
       SdMode： 减速使能模式  0：SD 信号无效  1：SD 信号有效，定长和定速运动有效 
       返回值：
       正确：返回 ERR_NoError
       */
        [DisplayName("设置减速开关是否有效"), Description("设置减速开关是否有效，0：SD信号无效; 1：SD信号有效，定长和定速运动有效 "), Category("配置其它信号")]
        /// <summary>
        /// 设置减速开关是否有效  
        /// </summary>
        public int SetSDStatusP { get; set; } = 0;


        /*
        DWORD d1000_set_HOME_pin_logic(WORD axis,WORD org_logic,WORD filter) 功 能：设置原点信号有效电平 参 数：axis：轴号，范围 0～(n×4-1)，n 为卡数  org_logic：ORG有效信号，0：低电平 1：高电平  filter： 保留参数，固定值为 0 返回值：正确：返回 ERR_NoError 错误：返回相关错误码
        */
        [DisplayName("设置原点信号有效电平"), Description("ORG有效信号，0：低电平 1：高电平"), Category("配置其它信号")]
        /// <summary>
        /// 设置减速开关是否有效  
        /// </summary>
        public int SetHomeLogicP { get; set; } = 0;

        /*
        DWORD d1000_counter_config(WORD axis,WORD mode) 
        功 能：设定编码器的计数方式 
        参 数： 
        axis：轴号，范围 0～(n×4-1)， n 为卡数 
        mode：编码器反馈输入模式 0 ：保留 1 ：1倍频AB相脉冲信号 2 ：2倍频AB相脉冲信号 3 ：4倍频AB相脉冲信号 
        返回值：正确：返回 ERR_NoError 错误：返回相关错误码 
        */
        [DisplayName("设定编码器的计数方式 "), Description(" 0 [保留]; 1 [1倍频AB相脉冲信号]; 2 [2倍频AB相脉冲信号]; 3 [4倍频AB相脉冲信号]"), Category("配置其它信号")]
        /// <summary>
        /// 设置减速开关是否有效  
        /// </summary>
        public int SetCounterConfigP { get; set; } = 1;


        /*
            DWORD d1000_set_pls_outmode (WORD axis, WORD pls_outmode) 
            功 能：设置控制卡脉冲输出模式 参 数：
            axis：轴号。范围 0～(n×4-1)，n 为卡数。多卡运行时，轴号参考  2-1 多卡运行时轴号对照表；  
            pls_outmode：脉冲输出模式：  0：pulse/dir 模式，脉冲上升沿有效  1：pulse/dir 模式，脉冲下降沿有效  2：CW/CCW 模式，脉冲上升沿有效  3：CW/CCW 模式，脉冲下降沿有效 
            返回值：
            正确：返回 ERR_NoError   
            错误：返回相关错误码
        */
        [DisplayName("设置卡的脉冲输出模式 "), Description("0：pulse/dir 模式，脉冲上升沿有效  1：pulse/dir 模式，脉冲下降沿有效  2：CW/CCW 模式，脉冲上升沿有效  3：CW/CCW 模式，脉冲下降沿有效"), Category("配置其它信号")]
        /// <summary>
        /// 设置减速开关是否有效  
        /// </summary>
        public int SetCardPlusOutModeP { get; set; } = 0;


        /*
        DWORD d1000_config_EL_MODE(WORD axis,WORD el_mode) 
        功 能：设置EL信号制动方式（默认立即停） 
        参 数：axis：轴号，范围 0～(n×4-1)， n 为卡数  
        el_mode：停止模式，0：立即停止 1：减速停止 
        返回值：
        正确：返回 ERR_NoError 
        错误：返回相关错误码 


        DWORD d1000_Enable_EL_PIN(WORD axis, WORD enable) 
        功 能：设置EL信号的使能状态（默认使能） 
        参 数：
        axis：轴号，范围 0～(n×4-1)， n 为卡数 
        enable：0：禁止 ， 1：使能 
        返回值：
        正确：返回 ERR_NoError 
        错误：返回相关错误码 
            */
        [DisplayName("设置减速开关是否有效"), Description("设置停止模式，0：立即停止 1：减速停止"), Category("配置EL信号")]
        /// <summary>
        /// 设置减速开关是否有效  
        /// </summary>
        public int SetELModeP { get; set; } = 0;

        [DisplayName("设置EL信号的使能状态"), Description("设置停止模式，0：禁止，1：使能"), Category("配置EL信号")]
        /// <summary>
        /// 设置减速开关是否有效  
        /// </summary>
        public int SetELEnableP { get; set; } = 1;

        /*
        DWORD d1000_set_ALM_PIN_Extern(WORD axis,WORD alm_enable,WORD alm_logic,WORD alm_all,WORD alm_action)(默认使能) 
        功 能：设置ALM信号(使能，停止所有轴或单轴配置) 
        参 数：axis：轴号，范围 0～(n×4-1)， 
        n 为卡数 
        alm_enable：ALM信号使能状态，0：禁止，1：允许（默认） 
        alm_logic：ALM信号有效电平，0：低电平（默认），1：高电平  
        alm_all：ALM信号控制方式，0：停止单轴（默认），1：停止所有轴 
        alm_action：停止模式，0：立即停止 
        返回值：正确：返回 ERR_NoError 
        错误：返回相关错误码 
            注意：IN1~IN4默认为伺服报警信号，如需作为通用输入口，需调用函数d1000_set_ALM_PIN_Extern禁止伺服报警使能功能。
            */
        [DisplayName("ALM信号使能状态"), Description("0：禁止，1：允许（默认）"), Category("配置轴使能ALM信号")]
        /// <summary>
        /// ALM信号使能状态
        /// </summary>
        public int SetALMStatusP { get; set; } = 1;

        [DisplayName("ALM信号有效电平"), Description("0：低电平（默认），1：高电平"), Category("配置轴使能ALM信号")]
        /// <summary>
        /// ALM信号有效电平
        /// </summary>
        public int SetALMLogicP { get; set; } = 0;

        [DisplayName("ALM信号控制方式"), Description("0：停止单轴（默认），1：停止所有轴 "), Category("配置轴使能ALM信号")]
        /// <summary>
        /// ALM信号有效电平
        /// </summary>
        public int SetALMAllP { get; set; } = 0;
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
