﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/DigitalTwinCard/ModubsComm.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using EasyModbus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotionCardRes.DigitalTwinCard
{
    public class ModubsComm
    {
        public bool isLinkServer { get; set; } = false;
        public string receiveData { get; set; } = null;
        public string sendData { get; set; } = null;
        /// <summary>
        /// 读写继电器区
        /// </summary>
        public List<bool> readwriteCols { get; set; } = new List<bool>();
        /// <summary>
        /// 输入继电器区
        /// </summary>
        public List<bool> readOnlyCols { get; set; } = new List<bool>();
        /// <summary>
        /// 读写寄存器区
        /// </summary>
        public List<int> readwriteReg { get; set; } = new List<int>();
        /// <summary>
        /// 只读寄存器区
        /// </summary>
        public List<int> readOnlyReg { get; set; } = new List<int>();


        private ModbusClient modbusClientReadCoils;
        private ModbusClient modbusClientWriteCoils;

        private ModbusClient modbusClientReadDiscreteInputs;

        private ModbusClient modbusClientReadHoldingRegisters;
        private ModbusClient modbusClientWriteHoldingRegisters;

        private ModbusClient modbusClientReadReadInputRegisters;


        ConcurrentQueue<Tuple<int, int>> WriteSingleCoilBuffer = new ConcurrentQueue<Tuple<int, int>>();
        ConcurrentQueue<Tuple<int, int[]>> WriteMultipleRegistersBuffer = new ConcurrentQueue<Tuple<int, int[]>>();


        public ModubsComm()
        {
            modbusClientReadCoils = new ModbusClient();
            //modbusClientReadCoils.ReceiveDataChanged += new EasyModbus.ModbusClient.ReceiveDataChangedHandler(UpdateReceiveData);
            //modbusClientReadCoils.SendDataChanged += new EasyModbus.ModbusClient.SendDataChangedHandler(UpdateSendData);
            //modbusClientReadCoils.ConnectedChanged += new EasyModbus.ModbusClient.ConnectedChangedHandler(UpdateConnectedChanged);
            modbusClientWriteCoils = new ModbusClient();

            modbusClientReadDiscreteInputs = new ModbusClient();

            modbusClientReadHoldingRegisters = new ModbusClient();
            modbusClientWriteHoldingRegisters = new ModbusClient();

            modbusClientReadReadInputRegisters = new ModbusClient();


            Task.Factory.StartNew(() =>
            {
                try
                {
                    while (true)
                    {
                        try
                        {
                            Thread.Sleep(2);
                            if (WriteSingleCoilBuffer.Count > 0)
                            {
                                Tuple<int, int> data = new Tuple<int, int>(0, 0);
                                if (WriteSingleCoilBuffer.TryDequeue(out data))
                                {
                                    try
                                    {

                                        modbusClientWriteCoils.WriteSingleCoil(data.Item1, data.Item2 == 0 ? true : false);
                                    }
                                    catch (Exception ex)
                                    {
                                        int k = 1;

                                    }
                                }
                                else
                                {

                                }
                            }
                            Thread.Sleep(1);
                            if (WriteMultipleRegistersBuffer.Count > 0)
                            {
                                Tuple<int, int[]> data1 = new Tuple<int, int[]>(0, new int[] { 0, 0 });
                                if (WriteMultipleRegistersBuffer.TryDequeue(out data1))
                                {
                                    try
                                    {

                                        modbusClientWriteHoldingRegisters.WriteMultipleRegisters(data1.Item1, data1.Item2);
                                    }
                                    catch (Exception ex)
                                    {
                                        int k = 1;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            int k = 1;
                        }

                    }
                }
                catch (Exception ex)
                {
                    int k = 1;
                }
                Console.WriteLine("队列处理线程已经退出");
            });
        }

        private static readonly Lazy<ModubsComm> lazy = new Lazy<ModubsComm>(() => new ModubsComm());
        public static ModubsComm Instance { get { return lazy.Value; } }



        public static bool isRead = false;
        public void Read4AreaData()
        {
            try
            {
                if (isLinkServer)
                {
                    isRead = true;
                    //output io
                    readwriteCols = modbusClientReadCoils.ReadCoils(0, 272).ToList();

                    // input io
                    readOnlyCols = modbusClientReadDiscreteInputs.ReadDiscreteInputs(0, 272).ToList();

                    readwriteReg = modbusClientReadHoldingRegisters.ReadHoldingRegisters(0, 99).ToList();
                    readOnlyReg = modbusClientReadReadInputRegisters.ReadInputRegisters(0, 99).ToList();
                    isRead = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.Message},{ex.StackTrace}");
                int k = 1;
            }
        }

        public bool open()
        {
            try
            {
                isLinkServer = false;
                modbusClientReadCoils.IPAddress = "127.0.0.1"; // 服务器IP地址
                modbusClientReadCoils.Port = 502; // 端口号
                modbusClientReadCoils.ConnectionTimeout = 5000;
                modbusClientWriteCoils.UnitIdentifier = 0;
                modbusClientReadCoils.Connect(); // 建立连接

                modbusClientWriteCoils.IPAddress = "127.0.0.1";
                modbusClientWriteCoils.Port = 502; // 端口号
                modbusClientWriteCoils.ConnectionTimeout = 5000;
                modbusClientWriteCoils.UnitIdentifier = 0;
                modbusClientWriteCoils.Connect(); // 建立连接


                modbusClientReadHoldingRegisters.IPAddress = "127.0.0.1"; // 服务器IP地址
                modbusClientReadHoldingRegisters.Port = 502; // 端口号
                modbusClientReadHoldingRegisters.ConnectionTimeout = 5000;
                modbusClientWriteCoils.UnitIdentifier = 0;
                modbusClientReadHoldingRegisters.Connect(); // 建立连接

                modbusClientWriteHoldingRegisters.IPAddress = "127.0.0.1";
                modbusClientWriteHoldingRegisters.Port = 502; // 端口号
                modbusClientWriteHoldingRegisters.ConnectionTimeout = 5000;
                modbusClientWriteCoils.UnitIdentifier = 0;
                modbusClientWriteHoldingRegisters.Connect(); // 建立连接



                modbusClientReadDiscreteInputs.IPAddress = "127.0.0.1";
                modbusClientReadDiscreteInputs.Port = 502; // 端口号
                modbusClientReadDiscreteInputs.ConnectionTimeout = 5000;
                modbusClientWriteCoils.UnitIdentifier = 0;
                modbusClientReadDiscreteInputs.Connect(); // 建立连接

                modbusClientReadReadInputRegisters.IPAddress = "127.0.0.1";
                modbusClientReadReadInputRegisters.Port = 502; // 端口号
                modbusClientReadReadInputRegisters.ConnectionTimeout = 5000;
                modbusClientWriteCoils.UnitIdentifier = 0;
                modbusClientReadReadInputRegisters.Connect(); // 建立连接

                //Debug.WriteLine("modbusClient is connecnt ok(127.0.0.1:502)");
                isLinkServer = true;

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool close()
        {
            try
            {
                if (isLinkServer)
                {
                    modbusClientReadCoils.Disconnect();
                    modbusClientWriteCoils.Disconnect();

                    modbusClientReadHoldingRegisters.Disconnect();
                    modbusClientWriteHoldingRegisters.Disconnect();

                    modbusClientReadDiscreteInputs.Disconnect();
                    modbusClientReadReadInputRegisters.Disconnect();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void set_t_profile(int axis, float start_vel, float max_vel, float acc, float dec)
        {
            if (!isLinkServer) return;

            if (axis == 0)
            {

                //x轴
                //Thread.Sleep(5);
                set_do_regdata(1, start_vel);
                //Thread.Sleep(5);
                set_do_regdata(2, max_vel);
                //Thread.Sleep(5);
                set_do_regdata(3, acc);
                //Thread.Sleep(5);
                //modbusClient.WriteMultipleRegisters(12, ModbusClient.ConvertDoubleToRegisters(dec));
            }
            else if (axis == 1)
            {
                //z轴
                //Thread.Sleep(5);
                set_do_regdata(6, start_vel);
                //Thread.Sleep(5);
                set_do_regdata(7, max_vel);
                //Thread.Sleep(5);
                set_do_regdata(8, acc);
                //Thread.Sleep(5);
                //modbusClient.WriteMultipleRegisters(28, ModbusClient.ConvertDoubleToRegisters(dec));
            }
            else if (axis == 2)
            {
                //u轴
                //Thread.Sleep(5);
                set_do_regdata(11, start_vel);
                //Thread.Sleep(5);
                set_do_regdata(12, max_vel);
                //Thread.Sleep(5);
                set_do_regdata(13, acc);
                //Thread.Sleep(5);
                //modbusClient.WriteMultipleRegisters(28, ModbusClient.ConvertDoubleToRegisters(dec));
            }

        }


        /// <summary>
        /// 读可读可写寄存器区的float数据
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public float get_do_regdata(int addr)
        {
            //var data= modbusClientWriteHoldingRegisters.ReadHoldingRegisters(addr, 1);
            if (addr < 1) return -1;
            return get_do_inputRegFloatData(addr, 1)[0];
        }


        /// <summary>
        /// 写plc output 寄存器区,写float
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="data"></param>
        public void set_do_regdata(int addr, float data)
        {
            if (!isLinkServer) return;
            if (addr < 1) return;
            var ary1 = ModbusClient.ConvertFloatToRegisters(data);
            var ary2 = new int[2];
            ary2[0] = ary1[1];
            ary2[1] = ary1[0];

            modbusClientWriteHoldingRegisters.WriteMultipleRegisters(addr, ary2);
            //modbusClient.WriteSingleRegister(addr, data);
        }

        /// <summary>
        /// 读寄存器区的float数据
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="sum"></param>
        /// <returns></returns>
        public List<float> get_do_regFloatData(int addr, int sum)
        {
            
            var list1 = new List<float>();
            if (addr < 1) return list1;
            if (!isLinkServer) return list1;
            try
            {
                //var data=modbusClient.ReadInputRegisters(addr, sum*2);
                var templist = new List<int>();
                for (int i = addr; i < addr + sum * 2; i++)
                {
                    templist.Add(readOnlyReg[i]);
                }
                var data = templist.ToArray();
                for (int i = 0; i < sum * 2; i += 2)
                {
                    if (data.Length < 2) return list1;
                    var tempary = new int[2];
                    tempary[0] = data[i + 1];
                    tempary[1] = data[i];
                    list1.Add(ModbusClient.ConvertRegistersToFloat(tempary));
                }
                return list1;
            }
            catch
            {
                return list1;
            }
        }

        public List<float> get_do_inputRegFloatData(int addr, int sum)
        {
           
            var list1 = new List<float>();
            if (!isLinkServer) return list1;
            try
            {
                //var data=modbusClient.ReadInputRegisters(addr, sum*2);
                var templist = new List<int>();
                for (int i = addr; i < addr + sum * 2; i++)
                {
                    templist.Add(readwriteReg[i]);
                }
                var data = templist.ToArray();
                for (int i = 0; i < sum * 2; i += 2)
                {
                    if (data.Length < 2) return list1;
                    var tempary = new int[2];
                    tempary[0] = data[i + 1];
                    tempary[1] = data[i];
                    list1.Add(ModbusClient.ConvertRegistersToFloat(tempary));
                }
                return list1;
            }
            catch
            {
                return list1;
            }
        }

        private static readonly Object obj1 = new Object();
        private static readonly Object obj2 = new Object();

        /// <summary>
        /// 写输出io位  0低电平 1高电平
        /// </summary>
        /// <param name="card_no"></param>
        /// <param name="bit_no"></param>
        /// <param name="data">0低电平 1高电平</param>
        public void set_do_bit(int card_no, int bit_no, int data)
        {
            if (!isLinkServer) return;
            //while (true)
            //{
            //    if (!isRead)
            //    {
            //        break;
            //    }
            //    Thread.Sleep(2);
            //}
            //lock (obj1)
            //{
            if (bit_no < 1) return;
            WriteSingleCoilBuffer.Enqueue(new Tuple<int, int>(bit_no, data));
            //}
            //lock (obj1)
            //{
            //modbusClientWriteCoils.WriteSingleCoil(bit_no, data == 0 ? true : false);
            //}
        }

        public void set_do_mulBit(int card_no, int startBitno, List<bool> data)
        {
            if (!isLinkServer) return;
            //if (startBitno < 1) return;
            modbusClientWriteCoils.WriteMultipleCoils(startBitno, data.ToArray());
        }

        /// <summary>
        /// 读输出继电器状态， 0为低电平，1为高电平
        /// </summary>
        /// <param name="card_no"></param>
        /// <param name="bit_no"></param>
        /// <returns>0为低电平，1为高电平</returns>
        public int get_do_bit(int card_no,int bit_no)
        {
            if (!isLinkServer) return 1;
            if (bit_no < 1) return 1;
            return readwriteCols[bit_no] == true ? 0 : 1;
        }
     
        /// <summary>
        /// 读指输入继电器状态
        /// </summary>
        /// <param name="card_no"></param>
        /// <param name="bit_no"></param>
        /// <returns>0为有信号，1为无信号</returns>
        public int get_di_bit(int card_no, int bit_no)
        {
            if (!isLinkServer) return 1;
            if (bit_no < 1) return 1;
            return readOnlyCols[bit_no] == true ? 0 : 1;
            //return modbusClientReadDiscreteInputs.ReadDiscreteInputs(bit_no, 1)[0]==true?0:1;
            //return modbusClient.ReadCoils(bit_no, 1)[0]==true?0:1;
        }

        /// <summary>
        /// 读全部输出IO状态
        /// </summary>
        /// <param name="card_no"></param>
        /// <param name="pData"></param>
        public bool[] get_do(int card_no)
        {
            if (!isLinkServer) return new bool[] { false };
            return modbusClientReadCoils.ReadCoils(0, 10);
        }

    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
