using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using EmbeddedMonoInjector;

namespace Bhop02Executor
{
    internal static class Program
    {
        private static readonly string[] DefaultProcessNames =
        {
            "SSJJ_BattleClient_Unity",
            "SSJJ_BattleClient_Unity.exe"
        };

        private const string DefaultNamespace = "Bhop02";
        private const string DefaultClass = "Entry";
        private const string DefaultMethod = "Load";

        private static int Main(string[] args)
        {
            try
            {
                Options options = Options.Parse(args);

                if (options.Help)
                {
                    PrintUsage();
                    return 0;
                }

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string payloadPath = Path.GetFullPath(options.PayloadPath ?? Path.Combine(baseDir, "Bhop02.AirStrafe.dll"));

                if (!File.Exists(payloadPath))
                {
                    // Source-tree run fallback.
                    string fallback = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "payload", "Bhop02.AirStrafe.dll"));
                    if (File.Exists(fallback))
                    {
                        payloadPath = fallback;
                    }
                }

                if (!File.Exists(payloadPath))
                {
                    throw new FileNotFoundException("找不到 Bhop payload DLL", payloadPath);
                }

                Process process = ResolveProcess(options);
                if (process == null)
                {
                    Console.Error.WriteLine("[Bhop02Executor] 未找到游戏进程。请先进入战斗客户端，或用 --pid / --process 指定。");
                    return 2;
                }

                Console.WriteLine("[Bhop02Executor] Target PID : " + process.Id);
                Console.WriteLine("[Bhop02Executor] Target Name: " + process.ProcessName);
                Console.WriteLine("[Bhop02Executor] Payload    : " + payloadPath);
                Console.WriteLine("[Bhop02Executor] Entry      : " + options.NamespaceName + "." + options.ClassName + "." + options.MethodName);

                byte[] payload = File.ReadAllBytes(payloadPath);

                using (var injector = new Injector(process.Id))
                {
                    IntPtr assembly = injector.Inject(payload, options.NamespaceName, options.ClassName, options.MethodName);
                    Console.WriteLine("[Bhop02Executor] Inject OK. Assembly handle: 0x" + assembly.ToInt64().ToString("X"));
                }

                Console.WriteLine("[Bhop02Executor] 完成：游戏内应已加载FakeInput 落地跳 + UserCmd 空速组件。");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[Bhop02Executor] FAILED: " + ex.Message);
                Console.Error.WriteLine(ex.GetType().FullName);
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine("Inner: " + ex.InnerException.Message);
                }
                return 1;
            }
        }

        private static Process ResolveProcess(Options options)
        {
            if (options.Pid > 0)
            {
                return Process.GetProcessById(options.Pid);
            }

            string[] names = options.ProcessName != null
                ? new[] { options.ProcessName }
                : DefaultProcessNames;

            DateTime deadline = DateTime.Now.AddSeconds(options.WaitSeconds);
            do
            {
                Process found = FindProcess(names);
                if (found != null)
                {
                    return found;
                }

                if (options.WaitSeconds <= 0)
                {
                    break;
                }

                Thread.Sleep(500);
            }
            while (DateTime.Now < deadline);

            return null;
        }

        private static Process FindProcess(IEnumerable<string> names)
        {
            var normalized = names
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(NormalizeProcessName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var candidates = new List<Process>();
            foreach (string name in normalized)
            {
                candidates.AddRange(Process.GetProcessesByName(name));
            }

            return candidates
                .OrderByDescending(p => p.MainWindowHandle != IntPtr.Zero)
                .ThenByDescending(p => SafeStartTimeTicks(p))
                .FirstOrDefault();
        }

        private static string NormalizeProcessName(string name)
        {
            name = name.Trim().Trim('"');
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - 4);
            }
            return name;
        }

        private static long SafeStartTimeTicks(Process process)
        {
            try { return process.StartTime.Ticks; }
            catch { return 0; }
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"Bhop02Executor - embedded Mono injector runner for Bhop02

默认行为：查找 SSJJ_BattleClient_Unity.exe，使用内置 Mono 注入逻辑注入同目录 Bhop02.AirStrafe.dll，调用 Bhop02.Entry.Load。

用法：
  Bhop02Executor.exe
  Bhop02Executor.exe --pid 1234
  Bhop02Executor.exe --process SSJJ_BattleClient_Unity
  Bhop02Executor.exe --dll C:\path\Bhop02.AirStrafe.dll
  Bhop02Executor.exe --wait 60

参数：
  --pid <pid>          指定目标进程 PID
  --process <name>     指定目标进程名，带不带 .exe 都可以
  --dll <path>         指定要注入的 Bhop02 DLL
  --namespace <name>   默认 Bhop02
  --class <name>       默认 Entry
  --method <name>      默认 Load
  --wait <seconds>     等待游戏进程秒数，默认 30
  --help               显示帮助
");
        }

        private sealed class Options
        {
            public int Pid;
            public string ProcessName;
            public string PayloadPath;
            public string NamespaceName = DefaultNamespace;
            public string ClassName = DefaultClass;
            public string MethodName = DefaultMethod;
            public int WaitSeconds = 30;
            public bool Help;

            public static Options Parse(string[] args)
            {
                var o = new Options();
                for (int i = 0; i < args.Length; i++)
                {
                    string a = args[i];
                    switch (a.ToLowerInvariant())
                    {
                        case "--help":
                        case "-h":
                        case "/?":
                            o.Help = true;
                            break;
                        case "--pid":
                            o.Pid = int.Parse(Next(args, ref i, a));
                            break;
                        case "--process":
                        case "--proc":
                            o.ProcessName = Next(args, ref i, a);
                            break;
                        case "--dll":
                        case "--payload":
                            o.PayloadPath = Next(args, ref i, a);
                            break;
                        case "--namespace":
                        case "--ns":
                            o.NamespaceName = Next(args, ref i, a);
                            break;
                        case "--class":
                        case "--type":
                            o.ClassName = Next(args, ref i, a);
                            break;
                        case "--method":
                            o.MethodName = Next(args, ref i, a);
                            break;
                        case "--wait":
                            o.WaitSeconds = int.Parse(Next(args, ref i, a));
                            break;
                        default:
                            if (File.Exists(a))
                            {
                                o.PayloadPath = a;
                            }
                            else
                            {
                                o.ProcessName = a;
                            }
                            break;
                    }
                }
                return o;
            }

            private static string Next(string[] args, ref int index, string option)
            {
                if (index + 1 >= args.Length)
                {
                    throw new ArgumentException(option + " 缺少参数");
                }
                index++;
                return args[index];
            }
        }
    }
}



