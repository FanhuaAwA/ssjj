using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

class Injector
{
    const uint PROCESS_CREATE_THREAD = 0x0002;
    const uint PROCESS_VM_OPERATION = 0x0008;
    const uint PROCESS_VM_WRITE = 0x0020;
    const uint PROCESS_VM_READ = 0x0010;
    const uint PROCESS_QUERY_INFORMATION = 0x0400;
    const uint PROCESS_VM_ALL = PROCESS_CREATE_THREAD | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ | PROCESS_QUERY_INFORMATION;
    const uint MEM_COMMIT = 0x1000;
    const uint MEM_RESERVE = 0x2000;
    const uint MEM_RELEASE = 0x8000;
    const uint PAGE_READWRITE = 0x04;
    const uint PAGE_EXECUTE_READWRITE = 0x40;

    static bool IsAdmin()
    {
        using (var id = WindowsIdentity.GetCurrent())
            return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
    }

    static void Main(string[] args)
    {
        Console.Title = "SSJJ Injector v2.1";
        Print("========================================", ConsoleColor.Cyan);
        Print("       SSJJ Plugin Injector v2.1       ", ConsoleColor.Cyan);
        Print("========================================", ConsoleColor.Cyan);
        Console.WriteLine();

        if (!IsAdmin())
        {
            Print("[!] Not running as Administrator!", ConsoleColor.Red);
            Print("[*] Re-launching with elevation...", ConsoleColor.Yellow);
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Process.GetCurrentProcess().MainModule.FileName,
                    Arguments = string.Join(" ", args.Select(a => $"\"{a}\"")),
                    Verb = "runas",
                    UseShellExecute = true
                });
                return;
            }
            catch { Print("[!] Elevation denied.", ConsoleColor.Red); WaitExit(); return; }
        }

        Print("[+] Running as Administrator", ConsoleColor.Green);

        string dllPath = Path.GetFullPath(args.Length > 0 ? args[0] : @"D:\ssjj\Plugin\Robin\bin\x64\Release\Plugins.dll");
        if (!File.Exists(dllPath))
        {
            dllPath = @"D:\ssjj\Plugin\Robin\bin\Debug\Plugins.dll";
            if (!File.Exists(dllPath)) { Print("[!] DLL not found.", ConsoleColor.Red); WaitExit(); return; }
        }
        Print($"[*] DLL: {dllPath}", ConsoleColor.White);
        Console.WriteLine();

        // Find game process
        var proc = FindGame();
        if (proc == null) { Print("[!] Game process not found.", ConsoleColor.Red); WaitExit(); return; }
        Print($"[+] Game: {proc.ProcessName} (PID: {proc.Id})", ConsoleColor.Green);

        // Find mono module in target
        string monoName = null;
        string monoPath = null;
        IntPtr monoBase = IntPtr.Zero;
        try
        {
            foreach (ProcessModule m in proc.Modules)
            {
                string name = m.ModuleName.ToLower();
                if (name.Contains("mono") && name.Contains("bdwgc"))
                {
                    monoName = m.ModuleName;
                    monoPath = m.FileName;
                    monoBase = m.BaseAddress;
                    break;
                }
            }
            if (monoBase == IntPtr.Zero)
            {
                foreach (ProcessModule m in proc.Modules)
                {
                    if (m.ModuleName.ToLower().Contains("mono"))
                    {
                        monoName = m.ModuleName;
                        monoPath = m.FileName;
                        monoBase = m.BaseAddress;
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Print($"[!] Failed to enumerate modules: {e.Message}", ConsoleColor.Red);
            Print("[*] Try running the game without anti-cheat protection.", ConsoleColor.Yellow);
            WaitExit(); return;
        }

        if (monoBase == IntPtr.Zero)
        {
            Print("[!] mono.dll not found in game process.", ConsoleColor.Red);
            WaitExit(); return;
        }

        Print($"[+] Mono: {monoName}", ConsoleColor.Green);
        Print($"    Path: {monoPath}", ConsoleColor.Gray);
        Print($"    Base: 0x{monoBase.ToInt64():X16}", ConsoleColor.Gray);

        // Load mono.dll locally to resolve function offsets
        IntPtr localMono = LoadLibrary(monoPath);
        if (localMono == IntPtr.Zero)
        {
            string[] candidates = {
                Path.Combine(Path.GetDirectoryName(monoPath), monoName),
                Path.Combine(Path.GetDirectoryName(monoPath), "mono-2.0-bdwgc.dll"),
                Path.Combine(Path.GetDirectoryName(monoPath), "mono.dll")
            };
            foreach (var c in candidates)
            {
                if (File.Exists(c)) { localMono = LoadLibrary(c); if (localMono != IntPtr.Zero) break; }
            }
        }
        if (localMono == IntPtr.Zero)
        {
            Print("[!] Failed to load mono.dll locally.", ConsoleColor.Red);
            WaitExit(); return;
        }

        // Resolve Mono API with address rebasing
        // localMono base != target mono base, so we compute offset = local_addr - local_base
        // then target_addr = target_base + offset
        long localBase = localMono.ToInt64();
        long targetBase = monoBase.ToInt64();

        string[] apiNames = {
            "mono_get_root_domain", "mono_thread_attach",
            "mono_assembly_open", "mono_assembly_get_image",
            "mono_class_from_name", "mono_class_get_method_from_name",
            "mono_runtime_invoke"
        };
        var api = new Dictionary<string, IntPtr>();
        foreach (var name in apiNames)
        {
            IntPtr localAddr = GetProcAddress(localMono, name);
            if (localAddr == IntPtr.Zero)
            {
                api[name] = IntPtr.Zero;
                Print($"    {name}: NOT FOUND", ConsoleColor.Red);
            }
            else
            {
                long offset = localAddr.ToInt64() - localBase;
                long targetAddr = targetBase + offset;
                api[name] = new IntPtr(targetAddr);
                Print($"    {name}: 0x{targetAddr:X} (offset +0x{offset:X})", ConsoleColor.Gray);
            }
        }
        FreeLibrary(localMono);

        if (api["mono_get_root_domain"] == IntPtr.Zero || api["mono_runtime_invoke"] == IntPtr.Zero)
        {
            Print("[!] Critical Mono API functions missing.", ConsoleColor.Red);
            WaitExit(); return;
        }

        Console.WriteLine();
        Print("[*] Injecting...", ConsoleColor.White);

        // Open process
        IntPtr hProc = OpenProcess(PROCESS_VM_ALL, false, proc.Id);
        if (hProc == IntPtr.Zero)
        {
            Print($"[!] OpenProcess failed. Error: {Marshal.GetLastWin32Error()}", ConsoleColor.Red);
            WaitExit(); return;
        }

        try
        {
            // Allocate and write strings
            IntPtr pPath = WriteAnsi(hProc, dllPath);
            IntPtr pNs = WriteAnsi(hProc, "SSJJPlugin");
            IntPtr pClass = WriteAnsi(hProc, "Loader");
            IntPtr pMethod = WriteAnsi(hProc, "Load");

            // Allocate space for MonoObject* exception pointer (used by mono_runtime_invoke)
            IntPtr pExc = VirtualAllocEx(hProc, IntPtr.Zero, 8, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            // Zero it out
            WriteProcessMemory(hProc, pExc, new byte[8], 8, out _);

            Print("[+] Strings written to target", ConsoleColor.Green);

            // Build x64 shellcode
            byte[] sc = BuildShellcode(api, pPath, pNs, pClass, pMethod, pExc);

            IntPtr pCode = VirtualAllocEx(hProc, IntPtr.Zero, (uint)sc.Length,
                MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
            WriteProcessMemory(hProc, pCode, sc, (uint)sc.Length, out _);

            Print("[+] Shellcode ready, executing...", ConsoleColor.Green);

            IntPtr hThread = CreateRemoteThread(hProc, IntPtr.Zero, 0, pCode, IntPtr.Zero, 0, out _);
            if (hThread != IntPtr.Zero)
            {
                WaitForSingleObject(hThread, 15000);
                CloseHandle(hThread);
                Print("[+] Injection complete! Check game for menu (Home key).", ConsoleColor.Green);
            }
            else
            {
                Print($"[!] CreateRemoteThread failed. Error: {Marshal.GetLastWin32Error()}", ConsoleColor.Red);
            }

            // Cleanup
            VirtualFreeEx(hProc, pCode, 0, MEM_RELEASE);
            VirtualFreeEx(hProc, pPath, 0, MEM_RELEASE);
            VirtualFreeEx(hProc, pNs, 0, MEM_RELEASE);
            VirtualFreeEx(hProc, pClass, 0, MEM_RELEASE);
            VirtualFreeEx(hProc, pMethod, 0, MEM_RELEASE);
            VirtualFreeEx(hProc, pExc, 0, MEM_RELEASE);
        }
        finally
        {
            CloseHandle(hProc);
        }

        Console.WriteLine();
        WaitExit();
    }

    static IntPtr WriteAnsi(IntPtr hProc, string text)
    {
        byte[] bytes = Encoding.Default.GetBytes(text + "\0");
        IntPtr ptr = VirtualAllocEx(hProc, IntPtr.Zero, (uint)bytes.Length, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        WriteProcessMemory(hProc, ptr, bytes, (uint)bytes.Length, out _);
        return ptr;
    }

    static byte[] BuildShellcode(Dictionary<string, IntPtr> api, IntPtr pPath, IntPtr pNs, IntPtr pClass, IntPtr pMethod, IntPtr pExc)
    {
        var sc = new List<byte>();

        // Prolog
        sc.AddRange(new byte[] { 0x53, 0x55, 0x56, 0x57 });
        sc.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x28 });

        // domain = mono_get_root_domain()
        CallAbs(sc, api["mono_get_root_domain"]);
        sc.AddRange(new byte[] { 0x48, 0x89, 0xC3 }); // rbx = domain

        // mono_thread_attach(domain)
        sc.AddRange(new byte[] { 0x48, 0x89, 0xD9 }); // rcx = rbx
        CallAbs(sc, api["mono_thread_attach"]);

        // assembly = mono_assembly_open(path, NULL)
        MovRcx(sc, pPath);
        sc.AddRange(new byte[] { 0x48, 0x31, 0xD2 }); // rdx = 0
        CallAbs(sc, api["mono_assembly_open"]);
        sc.AddRange(new byte[] { 0x48, 0x89, 0xC6 }); // rsi = assembly

        // if (!assembly) goto fail
        sc.AddRange(new byte[] { 0x48, 0x85, 0xC0 });
        int jzPos1 = EmitJz(sc);

        // image = mono_assembly_get_image(assembly)
        sc.AddRange(new byte[] { 0x48, 0x89, 0xF1 }); // rcx = rsi
        CallAbs(sc, api["mono_assembly_get_image"]);
        sc.AddRange(new byte[] { 0x48, 0x89, 0xC7 }); // rdi = image

        // klass = mono_class_from_name(image, ns, class)
        sc.AddRange(new byte[] { 0x48, 0x89, 0xF9 }); // rcx = rdi
        MovRdx(sc, pNs);
        MovR8(sc, pClass);
        CallAbs(sc, api["mono_class_from_name"]);
        sc.AddRange(new byte[] { 0x48, 0x89, 0xC3 }); // rbx = klass

        // if (!klass) goto fail
        sc.AddRange(new byte[] { 0x48, 0x85, 0xC0 });
        int jzPos2 = EmitJz(sc);

        // method = mono_class_get_method_from_name(klass, method_name, -1)
        sc.AddRange(new byte[] { 0x48, 0x89, 0xD9 }); // rcx = rbx
        MovRdx(sc, pMethod);
        sc.AddRange(new byte[] { 0x49, 0xC7, 0xC0, 0xFF, 0xFF, 0xFF, 0xFF }); // r8 = -1
        CallAbs(sc, api["mono_class_get_method_from_name"]);
        sc.AddRange(new byte[] { 0x48, 0x89, 0xC3 }); // rbx = method

        // if (!method) goto fail
        sc.AddRange(new byte[] { 0x48, 0x85, 0xC0 });
        int jzPos3 = EmitJz(sc);

        // mono_runtime_invoke(method, NULL, NULL, &exc)
        sc.AddRange(new byte[] { 0x48, 0x89, 0xD9 }); // rcx = method
        sc.AddRange(new byte[] { 0x48, 0x31, 0xD2 }); // rdx = 0 (obj)
        sc.AddRange(new byte[] { 0x4D, 0x31, 0xC0 }); // r8 = 0 (args)
        MovR9(sc, pExc);                                // r9 = &exc
        CallAbs(sc, api["mono_runtime_invoke"]);

        // Epilog (success)
        int epilog = sc.Count;
        sc.AddRange(new byte[] { 0x48, 0x83, 0xC4, 0x28 });
        sc.AddRange(new byte[] { 0x5F, 0x5E, 0x5D, 0x5B });
        sc.AddRange(new byte[] { 0xC3 });

        // Patch jumps
        PatchJz(sc, jzPos1, epilog);
        PatchJz(sc, jzPos2, epilog);
        PatchJz(sc, jzPos3, epilog);

        return sc.ToArray();
    }

    static void CallAbs(List<byte> sc, IntPtr addr)
    {
        // mov rax, imm64; call rax
        sc.AddRange(new byte[] { 0x48, 0xB8 });
        sc.AddRange(BitConverter.GetBytes(addr.ToInt64()));
        sc.AddRange(new byte[] { 0xFF, 0xD0 });
    }

    static void MovRcx(List<byte> sc, IntPtr val)
    {
        sc.AddRange(new byte[] { 0x48, 0xB9 });
        sc.AddRange(BitConverter.GetBytes(val.ToInt64()));
    }

    static void MovRdx(List<byte> sc, IntPtr val)
    {
        sc.AddRange(new byte[] { 0x48, 0xBA });
        sc.AddRange(BitConverter.GetBytes(val.ToInt64()));
    }

    static void MovR8(List<byte> sc, IntPtr val)
    {
        sc.AddRange(new byte[] { 0x49, 0xB8 });
        sc.AddRange(BitConverter.GetBytes(val.ToInt64()));
    }

    static void MovR9(List<byte> sc, IntPtr val)
    {
        sc.AddRange(new byte[] { 0x49, 0xB9 });
        sc.AddRange(BitConverter.GetBytes(val.ToInt64()));
    }

    static int EmitJz(List<byte> sc)
    {
        sc.AddRange(new byte[] { 0x0F, 0x84 }); // jz rel32
        int pos = sc.Count;
        sc.AddRange(new byte[] { 0, 0, 0, 0 }); // placeholder
        return pos;
    }

    static void PatchJz(List<byte> sc, int patchPos, int targetPos)
    {
        int offset = targetPos - (patchPos + 4);
        byte[] bytes = BitConverter.GetBytes(offset);
        sc[patchPos] = bytes[0]; sc[patchPos + 1] = bytes[1];
        sc[patchPos + 2] = bytes[2]; sc[patchPos + 3] = bytes[3];
    }

    static Process FindGame()
    {
        string[] names = { "SSJJ_BattleClient_Unity", "SSJJ_BattleClient", "battle" };
        foreach (var n in names)
        {
            var p = Process.GetProcessesByName(n);
            if (p.Length > 0) return p[0];
        }
        return null;
    }

    static void Print(string msg, ConsoleColor color)
    {
        Console.ForegroundColor = color; Console.WriteLine(msg); Console.ResetColor();
    }

    static void WaitExit()
    {
        Console.WriteLine("\nPress any key to exit...");
        try { Console.ReadKey(true); } catch { }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr a, uint sz, uint type, uint prot);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool VirtualFreeEx(IntPtr h, IntPtr a, uint sz, uint type);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteProcessMemory(IntPtr h, IntPtr a, byte[] buf, uint sz, out int written);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr CreateRemoteThread(IntPtr h, IntPtr attr, uint stack, IntPtr start, IntPtr param, uint flags, out uint tid);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern uint WaitForSingleObject(IntPtr h, uint ms);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr LoadLibrary(string path);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool FreeLibrary(IntPtr h);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    static extern IntPtr GetProcAddress(IntPtr h, string name);
}
