﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: HelperClass/FileData.cs
//   用途: 卡配置文件（INI 风格）的读写辅助类，DMC1000S / DigitalTwinCard 用到。
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
//         （原文件里唯一的 System.Windows.Forms 引用位于注释内，不影响移植。）
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace WenQiZhi.Domain.MotionCard.Common
{
    public class FileData
    {
        //private string fileName = "D:\\Temp\\TempRoi.txt";

        public FileData(string name)
        {
            this.fileName = name;
        }

        public FileData()
        {

        }

        private string fileName = "D:\\Temp\\TempTask.txt";

        public string ReadData()
        {
            string data = string.Empty;
            try
            {
                // v375：**先判存在**，别再用 new StreamReader 抛 FileNotFoundException 来当"文件不存在"的判断。
                // 缺文件是常态（新加的轴 / 新增的配置项第一次读都没有文件），原来每读一次要付：
                //   ① 异常构造 + 栈回溯；
                //   ② 下面 catch 里的 Warn 日志（一次文件 I/O）；
                //   ③ 宿主 WPF 的 AppDomain.FirstChanceException 再写一条 firstchance.log（Task + lock + I/O）。
                // 三者叠加在「每根轴 × 每个参数」的循环里，就是几秒级的卡顿。
                // 语义不变：文件不存在 → 返回空字符串（下面 FileData 的调用方本来就有默认值兜底）。
                if (!System.IO.File.Exists(fileName)) return data;

                string line;
                // 创建一个 StreamReader 的实例来读取文件 ,using 语句也能关闭 StreamReader
                using (System.IO.StreamReader sr = new System.IO.StreamReader(fileName))
                {
                    // 从文件读取并显示行，直到文件的末尾
                    while ((line = sr.ReadLine()) != null)
                    {
                        data = line;
                    }
                }
            }
            catch (Exception ex)
            {
                // 只有**真的读失败**（文件被占用 / 权限 / 盘符）才走到这里，才值得记一条。
                // 去掉 Console.WriteLine：本工程是 WinExe，没有控制台，写了也白写还多一次调用。
                string msg = String.Format("{0} {1} \r\n", "报警消息窗口", $"{ex.Message}\r\n{ex.StackTrace}");
                LogHelper.Helper.DefaultFileLogHelper.Warn(new LogHelper.Model.LogHelperModel()
                {
                    AddTime = DateTime.Now,
                    LogContent = msg,
                    ID = 12001
                });
            }

            return data;
        }
        public string ReadTextAutoDetect(string path)
        {
            string text = "";
            using (var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true))
            {

                text = reader.ReadToEnd();
                if (text.Contains("�"))
                {
                    text = File.ReadAllText(path, Encoding.GetEncoding("GB2312"));
                }
            }
            return text;
        }

        // 日志方法
        private void LogWarning(string message, int DeviceMessageTypeEnum = 0)
        {
            string msg = "";
            if (DeviceMessageTypeEnum == 0)
            {
                msg = String.Format("{0} {1} \r\n", "调试消息窗口", message);
                LogHelper.Helper.DebugMsgWindowLogHelper.Warn(new LogHelper.Model.LogHelperModel()
                {
                    AddTime = DateTime.Now,
                    LogContent = msg,
                    ID = 12001
                });
            }
            else if (DeviceMessageTypeEnum == 1)
            {
                msg = String.Format("{0} {1} \r\n", "报警消息窗口", message);
                LogHelper.Helper.DefaultFileLogHelper.Warn(new LogHelper.Model.LogHelperModel()
                {
                    AddTime = DateTime.Now,
                    LogContent = msg,
                    ID = 12001
                });
            }
        }
        static ConcurrentDictionary<string, bool> flieUsing = new ConcurrentDictionary<string, bool>();
        public  Task SaveData(string data)
        {
            //File.Delete(fileName);
            //var mname = AppDomain.CurrentDomain.BaseDirectory + "ProjectData";
            //DirectoryInfo root = new DirectoryInfo(mname);
            //foreach (FileInfo f in root.GetFiles())
            //{
            //    if (f.Name == fileName)
            //    {
            //        System.Windows.Forms.MessageBox.Show("重复");
            //        return;
            //    }
            //}
            try
            {
                if (!System.IO.File.Exists(fileName))
                {
                    using (File.Create(fileName)) { }
                }
                FileInfo fileInfo = new FileInfo(fileName);
                var length = fileInfo.Length;
                if (data.All(c => c == '\0'))
                {
                    string msg = String.Format("{0} {1} \r\n", "报警消息窗口", $"HelperClass.SaveData数据为空，data：【{data}】");
                    LogHelper.Helper.DefaultFileLogHelper.Warn(new LogHelper.Model.LogHelperModel()
                    {
                        AddTime = DateTime.Now,
                        LogContent = msg,
                        ID = 12001
                    });
                    return Task.CompletedTask;
                }
                if (false)
                {
                    System.IO.StreamWriter file = new System.IO.StreamWriter(fileName, false);
                    //保存数据到文件
                    file.Write(data);
                    //关闭文件
                    file.Close();
                    //释放对象
                    file.Dispose();
                }
                else
                {
                    int failuresNum = 0;
                    if (data.Length > 50000)
                    {
                        string str_flieName = fileName.ToString().Trim();
                        return Task.Run(() =>
                          {
                              if (!flieUsing.ContainsKey(str_flieName.Trim()))
                              {
                                  flieUsing.TryAdd(str_flieName.Trim(), true);
                                  bool saveReplace = false;
                              CallbackLabel:
                                  try
                                  {
                                      string temp = str_flieName + ".tmp";
                                      //int size = data.Length;
                                      //int buffer = size <= 4096 ? 4096 : size <= 16384 ? 8192 : 16384;
                                      int buffer = Math.Min(16384, Math.Max(4096, data.Length));
                                      //int buffer = 4096;
                                      using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, buffer, FileOptions.WriteThrough))
                                      {
                                          using (var writer = new StreamWriter(fs))
                                          {
                                              writer.Write(data);
                                              writer.Flush();
                                              fs.Flush(true);
                                              writer.Dispose();
                                          }
                                          fs.Dispose();
                                      }
                                      if (IsFileLocked(temp))
                                      {

                                      }
                                      if (IsFileLocked(str_flieName))
                                      {
                                      }
                                      File.SetAttributes(temp, FileAttributes.Normal);
                                      File.SetAttributes(fileName, FileAttributes.Normal);
                                      // 2. 原子替换目标文件
                                      File.Replace(temp, str_flieName, null);
                                  }
                                  catch (Exception ex)
                                  {
                                      saveReplace = true;
                                  }
                                  if (saveReplace)
                                  {
                                      failuresNum = failuresNum + 1;
                                      if (failuresNum > 3)
                                      {
                                          LogWarning($"源文件:{fileName + ".tmp"}替换目标文件:{fileName},连续3次失败");
                                      }
                                      saveReplace = false;
                                      Thread.Sleep(100);
                                      goto CallbackLabel;
                                  }
                                  flieUsing.TryRemove(str_flieName, out _);
                              }
                          }); 
                    }
                    else
                    {
                        int failuresNumLittle = 0;
                        bool LittleReplace = false;
                    CallbackLabelFile:
                        try
                        {
                            string temp = fileName + ".tmp";
                            //int size = data.Length;
                            //int buffer = size <= 4096 ? 4096 : size <= 16384 ? 8192 : 16384;
                            int buffer = 4096;
                            using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, buffer, FileOptions.WriteThrough))
                            using (var writer = new StreamWriter(fs))
                            {
                                writer.Write(data);
                                writer.Flush();
                                fs.Flush(true);
                            }

                            // 2. 原子替换目标文件
                            File.Replace(temp, fileName, null);
                        }
                        catch (Exception ex)
                        {
                            LittleReplace = true;
                            //System.Diagnostics.Debug.WriteLine($"开始执行保存异常:{DateTime.Now.ToString("yyMMdd HH:mm:ss ffff")}:{fileName}");
                        }
                        if (LittleReplace)
                        {
                            failuresNumLittle = failuresNumLittle + 1;
                            if (failuresNumLittle > 5)
                            {
                                LogWarning($"源文件:{fileName + ".tmp"}替换目标文件:{fileName},连续5次失败");
                            }
                            LittleReplace = false;
                            if (failuresNumLittle < 5)
                            {
                                Thread.Sleep(100);
                                goto CallbackLabelFile;
                            }
                        }
                        return Task.CompletedTask;
                    }
                }
                ////回读数据判断
                //var readBakc = ReadData();
                //if (!data.Trim().Equals(readBakc.Trim()) || string.IsNullOrEmpty(readBakc.Trim()))
                //{
                //    string msg = String.Format("{0} {1} \r\n", "报警消息窗口", $"readBakc回读数据失败，请查看【{fileName}】项目文件是否丢失！");
                //    LogHelper.Helper.DefaultFileLogHelper.Warn(new LogHelper.Model.LogHelperModel()
                //    {
                //        AddTime = DateTime.Now,
                //        LogContent = msg,
                //        ID = 12001
                //    });
                //}
                //System.Diagnostics.Debug.WriteLine($"执行保存完成:{data.Length}:{DateTime.Now.ToString("yyMMdd HH:mm:ss ffff")}");

                //return;
                //var backupdir = AppDomain.CurrentDomain.BaseDirectory + "ProjectDataBackup";
                //if (!Directory.Exists(backupdir))
                //{
                //    Directory.CreateDirectory(backupdir);
                //}
                //System.IO.StreamWriter filebackup = new System.IO.StreamWriter(backupdir + "\\" + DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss"), false);
                ////保存数据到文件
                //filebackup.Write(data);
                ////关闭文件
                //filebackup.Close();
                ////释放对象
                //filebackup.Dispose();
            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.StackTrace);
                string msg = String.Format("{0} {1} \r\n", "报警消息窗口", $"{ex.Message}\r\n{ex.StackTrace}");
                LogHelper.Helper.DefaultFileLogHelper.Warn(new LogHelper.Model.LogHelperModel()
                {
                    AddTime = DateTime.Now,
                    LogContent = msg,
                    ID = 12001
                });
                return Task.CompletedTask;
            }

        }

        bool IsFileLocked(string path)
        {
            try
            {
                using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                return false;
            }
            catch
            {
                return true;
            }
        }
        /// <summary>
        /// 备份删除的配置文件 
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <param name="path">文件的路径</param>
        public void SaveDeleteData(string filename, string path)
        {
            try
            {
                return;
                var backupdir = AppDomain.CurrentDomain.BaseDirectory + "ProjectDataBackup";
                if (!Directory.Exists(backupdir))
                {
                    Directory.CreateDirectory(backupdir);
                }
                var newfile = backupdir + "\\" + filename + DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss_fff");
                FileInfo f = new FileInfo(newfile);
                System.IO.File.Copy(path, newfile, true);
                f.LastWriteTimeUtc = f.CreationTime;
            }
            catch
            {

            }

        }

        /// <summary>
        /// 删除备份和其他记录文件（超过一定数量和一定日期才会删除）
        /// </summary>
        public void ScheduledSeletion()//todo:2022.11.26
        {
            var backupdir = AppDomain.CurrentDomain.BaseDirectory + "ProjectDataBackup";
            if (!Directory.Exists(backupdir))
            {
                Directory.CreateDirectory(backupdir);
            }
            DirectoryInfo dirInfo = new DirectoryInfo(backupdir);
            FileInfo[] fileInfos = dirInfo.GetFiles();
            if (fileInfos.Length > 1)
            {
                foreach (FileInfo fileInfo in fileInfos)
                {
                    if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-1))
                        fileInfo.Delete();
                }
            }

            var backupdir7 = AppDomain.CurrentDomain.BaseDirectory + "ProjectDataBackup";
            if (!Directory.Exists(backupdir7))
            {
                Directory.CreateDirectory(backupdir7);
            }
            DirectoryInfo dirInfo7 = new DirectoryInfo(backupdir7);
            DirectoryInfo[] fileInfos7 = dirInfo7.GetDirectories();
            if (fileInfos7.Length > 30)
            {
                foreach (DirectoryInfo fileInfo7 in fileInfos7)
                {
                    if (fileInfo7.LastWriteTime < DateTime.Now.AddDays(-60))
                        fileInfo7.Delete(true);
                }
            }

            var backupdir1 = AppDomain.CurrentDomain.BaseDirectory + "logfile";
            if (!Directory.Exists(backupdir1))
            {
                Directory.CreateDirectory(backupdir1);
            }
            DirectoryInfo dirInfo1 = new DirectoryInfo(backupdir1);
            DirectoryInfo[] fileInfos1 = dirInfo1.GetDirectories();
            if (fileInfos1.Length > 150)
            {
                foreach (DirectoryInfo fileInfo1 in fileInfos1)
                {
                    if (fileInfo1.LastWriteTime < DateTime.Now.AddDays(-60))
                        fileInfo1.Delete(true);
                }
            }

            //var backupdir2 = AppDomain.CurrentDomain.BaseDirectory + "Log";
            //if (!Directory.Exists(backupdir2))
            //{
            //    Directory.CreateDirectory(backupdir2);
            //}
            //DirectoryInfo dirInfo2 = new DirectoryInfo(backupdir2);
            //FileInfo[] fileInfos2 = dirInfo2.GetFiles();
            //if (fileInfos2.Length > 150)
            //{
            //    foreach (FileInfo fileInfo2 in fileInfos2)
            //    {
            //        if (fileInfo2.LastWriteTime < DateTime.Now.AddDays(-60))
            //            fileInfo2.Delete();
            //    }
            //}

            var backupdir5 = AppDomain.CurrentDomain.BaseDirectory + "dump";
            if (!Directory.Exists(backupdir5))
            {
                Directory.CreateDirectory(backupdir5);
            }
            DirectoryInfo dirInfo5 = new DirectoryInfo(backupdir5);
            FileInfo[] fileInfos5 = dirInfo5.GetFiles();
            if (fileInfos5.Length > 150)
            {
                foreach (FileInfo fileInfo5 in fileInfos5)
                {
                    if (fileInfo5.LastWriteTime < DateTime.Now.AddDays(-60))
                        fileInfo5.Delete();
                }
            }

            var backupdir3 = AppDomain.CurrentDomain.BaseDirectory + "DeviceInfoData";
            if (!Directory.Exists(backupdir3))
            {
                Directory.CreateDirectory(backupdir3);
            }
            DirectoryInfo dirInfo3 = new DirectoryInfo(backupdir3);
            FileInfo[] fileInfos3 = dirInfo3.GetFiles();
            if (fileInfos3.Length > 150)
            {
                foreach (FileInfo fileInfo3 in fileInfos3)
                {
                    if (fileInfo3.LastWriteTime < DateTime.Now.AddDays(-60))
                        fileInfo3.Delete();
                }
            }

            var backupdir6 = AppDomain.CurrentDomain.BaseDirectory + "AlarmLOG";
            if (!Directory.Exists(backupdir6))
            {
                Directory.CreateDirectory(backupdir6);
            }
            DirectoryInfo dirInfo6 = new DirectoryInfo(backupdir6);
            FileInfo[] fileInfos6 = dirInfo6.GetFiles();
            if (fileInfos6.Length > 200)
            {
                foreach (FileInfo fileInfo6 in fileInfos6)
                {
                    if (fileInfo6.LastWriteTime < DateTime.Now.AddDays(-60))
                        fileInfo6.Delete();
                }
            }

            var backupdir8 = AppDomain.CurrentDomain.BaseDirectory + "errlogHistory";
            if (!Directory.Exists(backupdir8))
            {
                Directory.CreateDirectory(backupdir8);
            }
            DirectoryInfo dirInfo8 = new DirectoryInfo(backupdir8);
            DirectoryInfo[] fileInfos8 = dirInfo8.GetDirectories();
            if (fileInfos8.Length > 150)
            {
                foreach (DirectoryInfo fileInfo8 in fileInfos8)
                {
                    if (fileInfo8.LastWriteTime < DateTime.Now.AddDays(-60))
                        fileInfo8.Delete(true);
                }
            }

            var backupdir4 = "D:\\SMCLOG";
            if (!Directory.Exists(backupdir4))
            {
                return;
            }
            DirectoryInfo dirInfo4 = new DirectoryInfo(backupdir4);
            FileInfo[] fileInfos4 = dirInfo4.GetFiles();
            if (fileInfos4.Length > 200)
            {
                foreach (FileInfo fileInfo4 in fileInfos4)
                {
                    if (fileInfo4.LastWriteTime < DateTime.Now.AddDays(-60))
                        fileInfo4.Delete();
                }
            }
        }

        public List<string> ReadDatas()
        {
            List<string> data = new List<string>();
            try
            {
                // v375：同 ReadData —— 缺文件先判存在，避免用异常做流程控制（还省掉 Console 输出）。
                if (!System.IO.File.Exists(fileName)) return data;

                string line;
                // 创建一个 StreamReader 的实例来读取文件 ,using 语句也能关闭 StreamReader
                using (System.IO.StreamReader sr = new System.IO.StreamReader(fileName))
                {
                    // 从文件读取并显示行，直到文件的末尾
                    while ((line = sr.ReadLine()) != null)
                    {
                        data.Add(line);
                    }
                }
            }
            catch
            {
                // 真读失败：返回已读到的部分即可（原先也只写 Console，等于没记）
            }

            return data;
        }

        public void SaveData(ref List<string> data)
        {
            File.Delete(fileName);

            System.IO.StreamWriter file = new System.IO.StreamWriter(fileName, false);

            foreach (string s in data)
            {
                //保存数据到文件
                file.Write(s);
            }

            //关闭文件
            file.Close();
            //释放对象
            file.Dispose();
        }

        /// <summary>
        /// 序列化保存配置 修改了这里
        /// </summary>
        public static void SaveJsonData<T>(string path, T obj)
        {
            try
            {
                var str1 = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                File.WriteAllText(path, str1);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 反序列化读取配置 修改了这里
        /// </summary>
        /// <returns></returns>
        public static T ReadJsonData<T>(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return default(T);
                }
                var strData = File.ReadAllText(path);
                if (string.IsNullOrEmpty(strData))
                {
                    return default(T);
                }
                else
                {
                    var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(strData);
                    return obj;
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        /// <summary>
        /// 获取文件的编码格式
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Encoding GetEncoder(string filePath)
        {
            string firstLine = ""; // 用于保存文件的第一行，用于检测BOM（字节顺序标记）  
            Encoding encoding = null; // 用于保存检测到的编码  

            try
            {
                // 读取文件的第一行，检查是否有BOM（字节顺序标记）  
                using (StreamReader reader = new StreamReader(filePath, Encoding.Default))
                {
                    firstLine = reader.ReadLine();
                }

                // 检查BOM来确定编码  
                if (firstLine.StartsWith("\ufeff")) // UTF-8 BOM  
                {
                    encoding = Encoding.UTF8;
                }
                else if (firstLine.StartsWith("\ufffe")) // UTF-16 BOM (little endian)  
                {
                    encoding = Encoding.Unicode; // 或者使用 Encoding.UTF16LE，它们是相同的编码  
                }
                else if (firstLine.StartsWith("\x0000\x0000\x0000\x0000")) // UTF-32 BOM (little endian)  
                {
                    encoding = Encoding.BigEndianUnicode; // 或者使用 Encoding.UTF32LE，它们是相同的编码  
                }
                else if (firstLine.StartsWith("\xef\xbb\xbf")) // UTF-8 without BOM  
                {
                    encoding = Encoding.UTF8; // 如果没有BOM，但内容是UTF-8编码，则使用UTF-8编码  
                }
                else // 默认使用系统的默认编码，通常为UTF-8或ASCII  
                {
                    encoding = Encoding.Default; // 或者直接使用 Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextEncoding) 来获取当前文化信息的默认编码。  
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return encoding;
        }



        /// <summary>
        /// 安全写入txt
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        /// <param name="encoding"></param>
        /// <param name="retryCount"></param>
        public void SafeFileWrite(string path, string content, Encoding encoding, int retryCount = 3)
        {
            // 参数防呆校验
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("文件路径不能为空或空白", nameof(path));

            // 保护重试次数下限
            if (retryCount < 1)
                retryCount = 1;

            // 自动创建父目录，多级目录一并创建，已存在则无副作用
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(fs, encoding))
                    {
                        writer.Write(content);
                    }
                    return;
                }
                catch (IOException ex)
                {
                    bool isLastAttempt = i >= retryCount - 1;

                    // 文件锁且不是最后一次：休眠重试
                    if (IsFileLocked(ex) && !isLastAttempt)
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    // 最后一次，或者非锁IO异常，直接抛出
                    throw;
                }
            }

            // 兜底，防止逻辑漏洞导致无返回，上层必须捕获异常感知失败
            throw new IOException($"SafeFileWrite 重试{retryCount}次全部失败，路径：{path}");
        }

        /// <summary>判断IO异常是否为文件被占用锁定</summary>
        private static bool IsFileLocked(IOException exception)
        {
            int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & 0xFFFF;
            // ERROR_SHARING_VIOLATION=32，ERROR_LOCK_VIOLATION=33
            return errorCode == 32 || errorCode == 33;
        } 


        /// <summary>
        /// 解析文本文件，按列名返回每行数据字典
        /// </summary>
        /// <param name="filePath">文本文件路径</param>
        /// <param name="columnNames">逗号分隔的列名，如 "编号,IO端子索引,IO设备"</param>
        /// <returns>每行数据的字典列表</returns>
        public  List<Dictionary<string, string>> ParseTextFile(string filePath, string columnNames)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return new List<Dictionary<string, string>>();// throw new ArgumentException("文件路径不能为空", nameof(filePath));
            if (!File.Exists(filePath))
                return new List<Dictionary<string, string>>();//  throw new FileNotFoundException("文件不存在", filePath);
            if (string.IsNullOrWhiteSpace(columnNames))
                return new List<Dictionary<string, string>>();//  throw new ArgumentException("列名字符串不能为空", nameof(columnNames));

            // 拆分列名并去除两端空格
            string[] columns = columnNames.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < columns.Length; i++)
                columns[i] = columns[i].Trim();

            var result = new List<Dictionary<string, string>>();
            Encoding encoding = GetEncoder(filePath);
            string[] lines = File.ReadAllLines(filePath, encoding);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue; // 跳过空行

                string[] fields = line.Split(',');

                var rowDict = new Dictionary<string, string>();
                for (int i = 0; i < columns.Length; i++)
                {
                    // 若当前行字段不足，则赋空字符串
                    string value = (i < fields.Length) ? fields[i].Trim() : "";
                    rowDict[columns[i]] = value;
                }
                result.Add(rowDict);
            }

            return result;
        }
    }


}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
