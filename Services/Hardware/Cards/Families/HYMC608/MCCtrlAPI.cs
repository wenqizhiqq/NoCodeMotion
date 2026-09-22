﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/HYMC608/MCCtrlAPI.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Samsun.Domain.MotionCard.Common.HYMC608
{
    //MCX08 重新封装类
    class MCCtrlAPI
    {
        static IntPtr g_handle = IntPtr.Zero;

        public const Int16 MAX_AXISES = 4;
        public const Int16 MAX_OUT   = 12;
        public const Int16 MAX_IN = 36;

        private bool Check_Para_Axis(byte iaxis)
        {
            if (iaxis >= MAX_AXISES) return  false;

            if (IntPtr.Zero == g_handle) return false;


            return true;
        }

        private bool MCX08_CheckDown(byte iaxis)
        {
            byte ret = 0;

            if (!Check_Para_Axis(iaxis)) return (false);


            ret = MCShell.MC_CheckDown(g_handle, iaxis);

            if (ret > 0) return true;


            return false;
        }

        public Int32 MCX08_Open()
        {
            return MCShell.MC_OpenEth("192.168.1.221",ref g_handle);
        }

        public void MCX08_Close()
        {

            MCShell.MC_Close(g_handle);
            g_handle = IntPtr.Zero;
            
        }
        //
        public UInt32 MCX08_ReadInPort()
        {
            UInt32 state;
            MCShell.MC_ReadInPort(g_handle, out state);

            return state;
        }
        //
        public UInt32 MCX08_ReadOutPort()
        {
            UInt32 state;
            MCShell.MC_ReadOutPort(g_handle, out state);

            return state;
        }
        //
        public byte MCX08_ReadInBit(byte ionum)
        {
            byte state = 0;
            MCShell.MC_ReadInBit(g_handle, ionum,ref state);

            return state;
        }
        //
        public byte MCX08_ReadOutBit(byte ionum)
        {
            byte state = 0;
            MCShell.MC_ReadOutBit(g_handle, (UInt16)ionum, out state);

            return state;
        }
        public byte MCX08_WriteOutBit(byte ionum,byte state)
        {
           // byte state = 0;
            MCShell.MC_WriteOutBit(g_handle, (UInt16)ionum, state);

            return 0;
        }
        //设置单轴速度参数
        public Int32 MCX08_Set_Profile(byte iaxis, UInt32 speed,UInt32 acc,UInt32 dec)
        {
            if (!MCX08_CheckDown(iaxis)) return 0;
            if (!Check_Para_Axis(iaxis)) return (-1);

            MCShell.MC_SetMotionAxisSpeed(g_handle, iaxis, (Int32)speed);
            MCShell.MC_SetAcceleration(g_handle, iaxis, (Int32)acc);
            return MCShell.MC_SetDeceleration(g_handle, iaxis, (Int32)dec);
        }
        //
        public byte MCX08_ReadEmgState()
        {
            byte emg = 0;

            MCShell.MC_ReadEMGState(g_handle, out emg);


            return emg;
        }
        //
        public Int32 MCX08_ReadPosition(byte iaxis)
        {
            return MCShell.MC_GetPulsePositon(g_handle, iaxis);
        }
        //
        public Int32 MCX08_ReadAllPosition(ref Int32 pos)
        {
            return MCShell.MC_GetAllPulsePositon(g_handle,out pos);
        }
        public UInt32 MCX08_ReadAxisState(byte iaxis)
        {
            MCShell.AxisStates strstate;
            
            UInt32 val = 0;
            if (!Check_Para_Axis(iaxis)) return (0);

            MCShell.MC_ReadAxisStates(g_handle, iaxis, out strstate);

            if ((strstate.m_HomeState&0x01) > 0)         val |= (0x01 << 0);
            if ((strstate.m_AlarmState&0x01) > 0)        val |= (0x01 << 1);
            if ((strstate.m_INPState&0x01) > 0)          val |= (0x01 << 2);
            if ((strstate.m_ElDecState&0x01) > 0)        val |= (0x01 << 3);
            if ((strstate.m_ElPlusState&0x01) > 0)       val |= (0x01 << 4);
            if ((strstate.m_SoftElDecState&0x01) > 0)    val |= (0x01 << 5);
            if ((strstate.m_SoftElPlusState&0x01) > 0)   val |= (0x01 << 6);

            

            return val;
        }
        //单轴点位运动
        public Int32 MCX08_Pmove(byte iaxis,Int32 distance,byte mode)
        {



            if (!Check_Para_Axis(iaxis)) return (-1);

            if (!MCX08_CheckDown(iaxis)) return 0;

            MCShell.MC_Pmove_Enter(g_handle,iaxis);
            if (mode < 1)
            {
                MCShell.MC_Pmove_SetAbsolute(g_handle, iaxis, distance);
                
            }
            else
            {
                MCShell.MC_Pmove_SetRelative(g_handle, iaxis, distance);
                
            }

            return MCShell.MC_Pmove_Start(g_handle,iaxis);
        }
        //减速停止
        public Int32 MCX08_DecStop(byte iaxis)
        {
            if (!Check_Para_Axis(iaxis)) return (-1);
            return MCShell.MC_DeclStop(g_handle, iaxis);
        }

        //立即停止
        public Int32 MCX08_ImdStop(byte iaxis)
        {
            if (!Check_Para_Axis(iaxis)) return (-1);
            return MCShell.MC_ImdStop(g_handle, iaxis);
        }
        //
        public Int32 MCX08_SetPosition(byte iaxis,Int32 pos)
        {
            if (!Check_Para_Axis(iaxis)) return (-1);
            return MCShell.MC_SetPulsePositon(g_handle,iaxis,pos);
        }

        //单轴速度运动
        public Int32 MCX08_Vmove(byte iaxis, UInt32 speed, byte dir)
        {
            if (!MCX08_CheckDown(iaxis)) return 0;
            if (!Check_Para_Axis(iaxis)) return (-1);

            MCShell.MC_Vmove_Enter(g_handle, iaxis);
            if (dir < 1)
            {
                MCShell.MC_Vmove_SetDir(g_handle, iaxis,0);
            }
            else
            {
                MCShell.MC_Vmove_SetDir(g_handle, iaxis, 1);
            }

            MCShell.MC_Vmove_SetSpeed(g_handle, iaxis, (Int32)speed);

            return MCShell.MC_Vmove_Start(g_handle, iaxis);
        }
        //单轴回原点运动
        public Int32 MCX08_HomeMove(byte iaxis, Int32 speed, byte dir,byte mode)
        {
            if (!MCX08_CheckDown(iaxis)) return 0;
            if (!Check_Para_Axis(iaxis)) return (-1);

            MCShell.MC_SetZeroMode(g_handle, iaxis, mode);
            MCShell.MC_SetZeroDir(g_handle, iaxis, dir);

            MCShell.MC_SetZeroSpeed(g_handle, iaxis, speed);

            return MCShell.MC_HomeMove(g_handle,iaxis);
        }
        //设置插补速度
        public Int32 MCX08_Set_LinerProfile(byte iliner, UInt32 speed, UInt32 acc, UInt32 dec)
        {
            MCShell.MC_SetLinerSpeed(g_handle, iliner, (Int32)speed);
            MCShell.MC_SetLinerAcceleration(g_handle, iliner, (Int32)acc);

            //MCShell.MC_SetLinerCornerSpeed(g_handle, iliner, (Int32)(speed / 4 + 10));

            return MCShell.MC_SetLinerDeceleration(g_handle, iliner, (Int32)dec);
        }
        //2轴直线插补运动
        public Int32 MCX08_Line2(byte iliner,byte iaxis0, Int32 dis0, byte iaxis1,Int32 dis1,byte mode)
        {
            byte[] iaxislist = {0,1,2,3};
            Int32[] distance = {0,0,0,0};

            iaxislist[0] = iaxis0;
            iaxislist[1] = iaxis1;

            distance[0] = dis0;
            distance[1] = dis1;

            MCShell.MC_Liner_Enter(g_handle, iliner,2, iaxislist);

            MCShell.MC_Liner_LineN(g_handle, iliner, 0, mode, 2,  iaxislist,  distance, 0);
            MCShell.MC_Liner_AddEnd(g_handle, iliner);


            return MCShell.MC_Liner_Start(g_handle,iliner);
        }
        public Int32 MCX08_Line3(byte iliner, byte iaxis0, Int32 dis0, byte iaxis1, Int32 dis1, byte iaxis2, Int32 dis2, byte mode)
        {
            byte[] iaxislist = { 0, 1, 2, 3 };
            Int32[] distance = { 0, 0, 0, 0 };

            iaxislist[0] = iaxis0;
            iaxislist[1] = iaxis1;
            iaxislist[2] = iaxis2;

            distance[0] = dis0;
            distance[1] = dis1;
            distance[2] = dis2;

            MCShell.MC_Liner_Enter(g_handle, iliner, 3,  iaxislist);

            MCShell.MC_Liner_LineN(g_handle, iliner, 0, mode, 3,  iaxislist,  distance, 0);

            MCShell.MC_Liner_AddEnd(g_handle, iliner);

            return MCShell.MC_Liner_Start(g_handle, iliner);
        }
        //
        public Int32 MCX08_Arc(byte iliner, byte iaxis0, Int32 dis0, byte iaxis1, Int32 dis1, Int32 center0, Int32 center1, byte dir, byte mode)
        {
            byte[] iaxislist = { 0, 1, 2, 3 };

            iaxislist[0] = iaxis0;
            iaxislist[1] = iaxis1;


            MCShell.MC_Liner_Enter(g_handle, iliner, 3, iaxislist);

            MCShell.MC_Liner_Arc(g_handle, iliner, 0, mode, iaxis0, iaxis1, dis0, dis1, center0, center1, dir, 0);


            MCShell.MC_Liner_AddEnd(g_handle, iliner);
            return MCShell.MC_Liner_Start(g_handle, iliner);
        }

        public Int32 MCX08_LineN(byte iliner, byte iaxises, byte[] iaxislist,  Int32[] dislist, byte mode)
        {


            MCShell.MC_Liner_Enter(g_handle, iliner, iaxises, iaxislist);

            MCShell.MC_Liner_LineN(g_handle, iliner, 0, mode, iaxises, iaxislist, dislist, 0);

            MCShell.MC_Liner_AddEnd(g_handle,iliner);
            return MCShell.MC_Liner_Start(g_handle, iliner);
        }

        public Int32 MCX08_Conti_Enter(byte iliner, byte iaxises, byte[] iaxislist)
        {

            
            MCShell.MC_Liner_Enter(g_handle, iliner, iaxises, iaxislist);


            return MCShell.MC_Liner_Start(g_handle, iliner);
        }
        public Int32 MCX08_Conti_LineN(byte iliner, byte iaxises,  byte[] iaxislist,  Int32[] dislist, byte mode)
        {


            //MCShell.MC_Liner_Enter(g_handle, iliner, iaxises, ref iaxislist);

            return MCShell.MC_Liner_LineN(g_handle, iliner, 0, mode, iaxises, iaxislist, dislist, 0);


            //return MCShell.MC_Liner_Start(g_handle, iliner);
        }

        public Int32 MCX08_Conti_Arc(byte iliner, byte iaxis0, Int32 dis0, byte iaxis1, Int32 dis1, Int32 center0, Int32 center1, byte dir, byte mode)
        {
            //byte[] iaxislist = { 0, 1, 2, 3 };

           // iaxislist[0] = iaxis0;
            //iaxislist[1] = iaxis1;


           // MCShell.MC_Liner_Enter(g_handle, iliner, 3, ref iaxislist[0]);

            return MCShell.MC_Liner_Arc(g_handle, iliner, 0, mode, iaxis0, iaxis1, dis0, dis1, center0, center1, dir, 0);



            //return MCShell.MC_Liner_Start(g_handle, iliner);
        }

        public Int32 MCX08_Conti_End(byte iliner)
        {

            return MCShell.MC_Liner_AddEnd(g_handle, iliner);
        }


    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
