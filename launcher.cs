using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

class DshLauncher
{
    [DllImport("kernel32.dll")]
    static extern bool SetConsoleOutputCP(uint cp);

    const int PORT = 3080;
    const string URL = "http://127.0.0.1:3080/";

    static bool IsServerUp()
    {
        try
        {
            var req = (HttpWebRequest)WebRequest.Create(URL);
            req.Timeout = 1500;
            using (var resp = (HttpWebResponse)req.GetResponse())
                return resp.StatusCode == HttpStatusCode.OK;
        }
        catch
        {
            return false;
        }
    }

    static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine("无法自动打开浏览器: " + ex.Message + "，请手动访问 " + url);
        }
    }

    static void Pause()
    {
        Console.WriteLine();
        Console.WriteLine("按任意键退出...");
        try { Console.ReadKey(true); } catch { }
    }

    static void Main()
    {
        SetConsoleOutputCP(65001);
        Console.OutputEncoding = Encoding.UTF8;
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string node = Path.Combine(exeDir, "node", "node.exe");
        string harness = Path.Combine(exeDir, "harness");
        string serverUrl = "http://127.0.0.1:" + PORT + "/";

        Console.WriteLine("========================================");
        Console.WriteLine("  DeepSeek Harness 启动器");
        Console.WriteLine("========================================");
        Console.WriteLine();

        if (IsServerUp())
        {
            Console.WriteLine("服务已在运行: " + serverUrl);
            OpenBrowser(serverUrl);
            return;
        }

        if (!File.Exists(node) || !Directory.Exists(harness))
        {
            Console.WriteLine("错误: 找不到 node 或 harness 目录。");
            Console.WriteLine("请保持 exe、node 目录和 harness 目录的完整结构。");
            Pause();
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = node,
            WorkingDirectory = harness,
            Arguments = "--import tsx/esm apps/cli/src/bin.ts web --host 127.0.0.1 --port " + PORT,
            UseShellExecute = false,
        };
        string path = Path.Combine(exeDir, "node") + ";" + Environment.GetEnvironmentVariable("PATH");
        psi.EnvironmentVariables["PATH"] = path;

        Console.WriteLine("正在启动 DeepSeek Harness（首次启动需编译，约 10-60 秒）...");
        Console.WriteLine("提示: 模型 API 密钥从环境变量读取（如 OPENCODE_GO_API_KEY），");
        Console.WriteLine("      请确保在系统环境变量中已配置。");
        Console.WriteLine();

        try
        {
            using (var proc = Process.Start(psi))
            {
                DateTime deadline = DateTime.UtcNow.AddSeconds(180);
                bool ready = false;
                while (DateTime.UtcNow < deadline && !proc.HasExited)
                {
                    if (IsServerUp()) { ready = true; break; }
                    Thread.Sleep(500);
                }

                if (ready)
                {
                    Console.WriteLine("服务已就绪: " + serverUrl);
                    OpenBrowser(serverUrl);
                    Console.WriteLine();
                    Console.WriteLine("关闭本窗口或按 Ctrl+C 即停止服务。");
                    proc.WaitForExit();
                }
                else if (proc.HasExited)
                {
                    Console.WriteLine("服务进程异常退出，退出码: " + proc.ExitCode);
                    Console.WriteLine("请检查上方输出中的错误信息。");
                }
                else
                {
                    Console.WriteLine("等待超时（180 秒），服务未能就绪。");
                    try { proc.Kill(); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("启动失败: " + ex.Message);
        }
        Pause();
    }
}
