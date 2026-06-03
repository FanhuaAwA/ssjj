using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MonoHookPatcher
{
    /// <summary>
    /// Pre-injection GG Hook patcher.
    /// Before loading our managed DLL, this tool:
    /// 1. Finds mono_image_open_from_data in the target process
    /// 2. Restores original bytes (temporarily disabling GG's hook)
    /// 3. Monitors until our DLL is loaded
    /// 4. Optionally restores GG's hook
    /// </summary>
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", SetLastError = true)]
        static extern bool EnumProcessModulesEx(IntPtr hProcess, IntPtr[] lphModule, uint cb, out uint lpcbNeeded, uint dwFilterFlag);

        [DllImport("psapi.dll", SetLastError = true)]
        static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, uint nSize);

        [DllImport("psapi.dll", SetLastError = true)]
        static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, uint cb);

        [StructLayout(LayoutKind.Sequential)]
        struct MODULEINFO
        {
            public IntPtr lpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }

        // PE export directory structures
        [StructLayout(LayoutKind.Sequential)]
        struct IMAGE_EXPORT_DIRECTORY
        {
            public uint Characteristics;
            public uint TimeDateStamp;
            public ushort MajorVersion;
            public ushort MinorVersion;
            public uint Name;
            public uint Base;
            public uint NumberOfFunctions;
            public uint NumberOfNames;
            public uint AddressOfFunctions;
            public uint AddressOfNames;
            public uint AddressOfNameOrdinals;
        }

        const uint PROCESS_VM_READ = 0x0010;
        const uint PROCESS_VM_WRITE = 0x0020;
        const uint PROCESS_VM_OPERATION = 0x0008;
        const uint PROCESS_QUERY_INFORMATION = 0x0400;
        const uint PAGE_EXECUTE_READWRITE = 0x40;
        const uint LIST_MODULES_ALL = 0x03;
        const int HOOK_PATCH_SIZE = 14;  // typical x64 JMP hook size

        static byte[] _originalHookBytes;  // GG's hook bytes to restore later
        static byte[] _originalCodeBytes;  // Original mono function bytes
        static IntPtr _hookAddress;
        static bool _wasHooked;

        static void Main(string[] args)
        {
            Console.WriteLine("=== SSJJ GG Hook Patcher ===");
            Console.WriteLine();

            // Check for restore mode
            bool restoreMode = false;
            string processName = "SSJJ_BattleClient_Unity";
            string dllPath = null;
            string monoDllPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-r") restoreMode = true;
                if (args[i] == "-p" && i + 1 < args.Length) processName = args[++i];
                if (args[i] == "-d" && i + 1 < args.Length) dllPath = args[++i];
                if (args[i] == "-m" && i + 1 < args.Length) monoDllPath = args[++i];
            }

            if (restoreMode)
            {
                RestoreHook(processName);
                return;
            }

            if (string.IsNullOrEmpty(dllPath))
            {
                Console.WriteLine("Usage: MonoHookPatcher.exe -d <dll_path> [-p <process_name>] [-m <mono_dll_path>]");
                Console.WriteLine();
                Console.WriteLine("  -d  Path to the managed DLL to inject");
                Console.WriteLine("  -p  Target process name (default: SSJJ_BattleClient_Unity)");
                Console.WriteLine("  -m  Path to clean mono-2.0-bdwgc.dll for original bytes");
                return;
            }

            if (!File.Exists(dllPath))
            {
                Console.WriteLine($"[!] DLL not found: {dllPath}");
                return;
            }

            // Auto-detect mono DLL path
            if (string.IsNullOrEmpty(monoDllPath))
            {
                var candidates = new[]
                {
                    @"D:\SSJJ-4399\battle\21_64\MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll",
                    Path.Combine(Path.GetDirectoryName(dllPath) ?? ".", "mono-2.0-bdwgc.dll")
                };
                foreach (var c in candidates)
                {
                    if (File.Exists(c)) { monoDllPath = c; break; }
                }
            }

            if (string.IsNullOrEmpty(monoDllPath) || !File.Exists(monoDllPath))
            {
                Console.WriteLine("[!] Cannot find clean mono-2.0-bdwgc.dll. Specify with -m");
                return;
            }

            // Step 1: Find target process
            Console.WriteLine($"[*] Looking for process: {processName}.exe");
            var procs = Process.GetProcessesByName(processName);
            if (procs.Length == 0)
            {
                Console.WriteLine("[!] Process not found. Start the game first.");
                return;
            }

            var proc = procs[0];
            Console.WriteLine($"[+] Found PID: {proc.Id}");
            Console.WriteLine();

            // Step 2: Open process
            IntPtr hProcess = OpenProcess(
                PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION | PROCESS_QUERY_INFORMATION,
                false, proc.Id);
            if (hProcess == IntPtr.Zero)
            {
                Console.WriteLine("[!] Failed to open process. Run as Administrator.");
                return;
            }
            Console.WriteLine("[+] Process opened");

            // Step 3: Find mono-2.0-bdwgc.dll base in target process
            IntPtr monoBase = IntPtr.Zero;
            var modules = new IntPtr[1024];
            if (EnumProcessModulesEx(hProcess, modules, (uint)(modules.Length * IntPtr.Size), out uint needed, LIST_MODULES_ALL))
            {
                int count = (int)(needed / IntPtr.Size);
                for (int i = 0; i < count && i < modules.Length; i++)
                {
                    var sb = new StringBuilder(260);
                    GetModuleFileNameEx(hProcess, modules[i], sb, 260);
                    if (sb.ToString().ToLower().Contains("mono-2.0-bdwgc"))
                    {
                        monoBase = modules[i];
                        Console.WriteLine($"[+] Found mono-2.0-bdwgc.dll at: 0x{monoBase.ToInt64():X}");
                        break;
                    }
                }
            }

            if (monoBase == IntPtr.Zero)
            {
                Console.WriteLine("[!] mono-2.0-bdwgc.dll not found in target process");
                CloseHandle(hProcess);
                return;
            }

            // Step 4: Resolve mono_image_open_from_data export
            IntPtr funcAddr = ResolveExportInTarget(hProcess, monoBase, "mono_image_open_from_data");
            if (funcAddr == IntPtr.Zero)
            {
                // Try with underscore prefix
                funcAddr = ResolveExportInTarget(hProcess, monoBase, "_mono_image_open_from_data");
            }
            if (funcAddr == IntPtr.Zero)
            {
                Console.WriteLine("[!] Could not resolve mono_image_open_from_data");
                CloseHandle(hProcess);
                return;
            }
            Console.WriteLine($"[+] mono_image_open_from_data at: 0x{funcAddr.ToInt64():X}");

            // Step 5: Read current bytes at the function
            byte[] currentBytes = new byte[HOOK_PATCH_SIZE];
            if (!ReadProcessMemory(hProcess, funcAddr, currentBytes, HOOK_PATCH_SIZE, out _))
            {
                Console.WriteLine("[!] Failed to read function bytes");
                CloseHandle(hProcess);
                return;
            }
            Console.WriteLine($"[*] Current bytes: {BitConverter.ToString(currentBytes).Replace("-", " ")}");

            // Step 6: Read original bytes from disk copy
            byte[] originalBytes = ReadOriginalBytes(monoDllPath, "mono_image_open_from_data");
            if (originalBytes == null || originalBytes.Length < HOOK_PATCH_SIZE)
            {
                Console.WriteLine("[!] Could not read original bytes from disk");
                CloseHandle(hProcess);
                return;
            }
            Console.WriteLine($"[*] Original bytes: {BitConverter.ToString(GetFirstBytes(originalBytes, HOOK_PATCH_SIZE)).Replace('-', ' ')}");

            // Step 7: Check if hooked (JMP = 0xE9 or 0xFF 0x25, CALL = 0xE8)
            _wasHooked = false;
            for (int i = 0; i < HOOK_PATCH_SIZE; i++)
            {
                if (currentBytes[i] != originalBytes[i])
                {
                    _wasHooked = true;
                    break;
                }
            }

            if (!_wasHooked)
            {
                Console.WriteLine("[*] No hook detected on mono_image_open_from_data");
                Console.WriteLine("[*] GG hook might be on a different function, or GG is not running");
                CloseHandle(hProcess);
                return;
            }

            Console.WriteLine("[!] GG HOOK DETECTED - patching...");

            // Step 8: Restore original bytes
            _hookAddress = funcAddr;
            _originalHookBytes = new byte[HOOK_PATCH_SIZE];
            Array.Copy(currentBytes, _originalHookBytes, HOOK_PATCH_SIZE);
            _originalCodeBytes = new byte[HOOK_PATCH_SIZE];
            Array.Copy(originalBytes, _originalCodeBytes, HOOK_PATCH_SIZE);

            // Make memory writable
            if (!VirtualProtectEx(hProcess, funcAddr, HOOK_PATCH_SIZE, PAGE_EXECUTE_READWRITE, out uint oldProtect))
            {
                Console.WriteLine("[!] VirtualProtectEx failed");
                CloseHandle(hProcess);
                return;
            }

            // Write original bytes
            if (!WriteProcessMemory(hProcess, funcAddr, originalBytes, HOOK_PATCH_SIZE, out _))
            {
                Console.WriteLine("[!] WriteProcessMemory failed");
                CloseHandle(hProcess);
                return;
            }

            // Restore protection
            VirtualProtectEx(hProcess, funcAddr, HOOK_PATCH_SIZE, oldProtect, out _);

            Console.WriteLine("[+] GG hook PATCHED - mono_image_open_from_data restored");
            Console.WriteLine();

            // Save state for later restore
            SaveState(funcAddr, currentBytes, originalBytes);

            Console.WriteLine("[*] Hook state saved. You can now inject the DLL.");
            Console.WriteLine("[*] After injection, run: MonoHookPatcher.exe -r to restore GG's hook");

            CloseHandle(hProcess);

            // Step 9: Restore GG's hook
            Console.WriteLine("[*] Restoring GG hook...");
            VirtualProtectEx(hProcess, funcAddr, HOOK_PATCH_SIZE, PAGE_EXECUTE_READWRITE, out oldProtect);
            WriteProcessMemory(hProcess, funcAddr, _originalHookBytes, HOOK_PATCH_SIZE, out _);
            VirtualProtectEx(hProcess, funcAddr, HOOK_PATCH_SIZE, oldProtect, out _);
            Console.WriteLine("[+] GG hook restored");

            CloseHandle(hProcess);
        }

        static IntPtr ResolveExportInTarget(IntPtr hProcess, IntPtr moduleBase, string exportName)
        {
            try
            {
                // Read DOS header
                byte[] dosHeader = new byte[0x40];
                if (!ReadProcessMemory(hProcess, moduleBase, dosHeader, 0x40, out _)) return IntPtr.Zero;

                int e_lfanew = BitConverter.ToInt32(dosHeader, 0x3C);

                // Read PE signature + full headers
                byte[] peHeader = new byte[0x18 + 0xF0]; // COFF (24) + optional header (240)
                if (!ReadProcessMemory(hProcess, moduleBase + e_lfanew, peHeader, (uint)peHeader.Length, out _))
                    return IntPtr.Zero;

                // Magic at offset 0x18 from PE sig (start of optional header)
                ushort magic = BitConverter.ToUInt16(peHeader, 0x18);
                uint exportRva, exportSize;

                if (magic == 0x20B) // PE32+
                {
                    // Data directories start at offset 0x88 from PE sig
                    exportRva = BitConverter.ToUInt32(peHeader, 0x88);
                    exportSize = BitConverter.ToUInt32(peHeader, 0x8C);
                }
                else
                {
                    // PE32: data directories at offset 0x78 from PE sig
                    exportRva = BitConverter.ToUInt32(peHeader, 0x78);
                    exportSize = BitConverter.ToUInt32(peHeader, 0x7C);
                }

                if (exportRva == 0 || exportSize == 0) return IntPtr.Zero;

                // Read export directory
                byte[] expDir = new byte[40]; // sizeof(IMAGE_EXPORT_DIRECTORY) = 40
                if (!ReadProcessMemory(hProcess, moduleBase + (int)exportRva, expDir, (uint)expDir.Length, out _))
                    return IntPtr.Zero;

                var dir = new IMAGE_EXPORT_DIRECTORY();
                dir.Characteristics = BitConverter.ToUInt32(expDir, 0);
                dir.NumberOfNames = BitConverter.ToUInt32(expDir, 24);
                dir.AddressOfFunctions = BitConverter.ToUInt32(expDir, 28);
                dir.AddressOfNames = BitConverter.ToUInt32(expDir, 32);
                dir.AddressOfNameOrdinals = BitConverter.ToUInt32(expDir, 36);

                // Read name pointers
                int nameCount = (int)dir.NumberOfNames;
                byte[] namePtrs = new byte[nameCount * 4];
                byte[] ordinals = new byte[nameCount * 2];

                if (!ReadProcessMemory(hProcess, moduleBase + (int)dir.AddressOfNames, namePtrs, (uint)namePtrs.Length, out _))
                    return IntPtr.Zero;
                if (!ReadProcessMemory(hProcess, moduleBase + (int)dir.AddressOfNameOrdinals, ordinals, (uint)ordinals.Length, out _))
                    return IntPtr.Zero;

                // Search for export by name
                byte[] funcPtrs = new byte[nameCount * 4];
                if (!ReadProcessMemory(hProcess, moduleBase + (int)dir.AddressOfFunctions, funcPtrs, (uint)funcPtrs.Length, out _))
                    return IntPtr.Zero;

                for (int i = 0; i < nameCount; i++)
                {
                    int nameRva = BitConverter.ToInt32(namePtrs, i * 4);
                    byte[] nameBytes = new byte[128];
                    if (ReadProcessMemory(hProcess, moduleBase + nameRva, nameBytes, 128, out _))
                    {
                        string name = Encoding.ASCII.GetString(nameBytes).Split('\0')[0];
                        if (name == exportName)
                        {
                            short ordinal = BitConverter.ToInt16(ordinals, i * 2);
                            int funcRva = BitConverter.ToInt32(funcPtrs, ordinal * 4);
                            return moduleBase + funcRva;
                        }
                    }
                }
            }
            catch { }
            return IntPtr.Zero;
        }

        static byte[] ReadOriginalBytes(string dllPath, string exportName)
        {
            try
            {
                // Parse PE file directly from disk to find export
                var data = File.ReadAllBytes(dllPath);
                int peOff = BitConverter.ToInt32(data, 0x3C);
                ushort machine = BitConverter.ToUInt16(data, peOff + 4);
                ushort secCount = BitConverter.ToUInt16(data, peOff + 6);
                ushort ohSize = BitConverter.ToUInt16(data, peOff + 20);
                int optOff = peOff + 24;

                uint exportRva, exportSize;
                ushort magic = BitConverter.ToUInt16(data, optOff);
                if (magic == 0x20B)
                {
                    // PE32+: data directories at optOff + 112
                    exportRva = BitConverter.ToUInt32(data, optOff + 112);
                    exportSize = BitConverter.ToUInt32(data, optOff + 116);
                }
                else
                {
                    // PE32: data directories at optOff + 96
                    exportRva = BitConverter.ToUInt32(data, optOff + 96);
                    exportSize = BitConverter.ToUInt32(data, optOff + 100);
                }

                if (exportRva == 0 || exportSize == 0) return null;

                // Parse section headers to convert RVA to file offset
                int secOff = optOff + ohSize;
                uint RvaToOffset(uint rva)
                {
                    for (int s = 0; s < secCount; s++)
                    {
                        int so = secOff + s * 40;
                        uint vr = BitConverter.ToUInt32(data, so + 12);
                        uint vs = BitConverter.ToUInt32(data, so + 8);
                        uint rp = BitConverter.ToUInt32(data, so + 20);
                        if (vr <= rva && rva < vr + vs)
                            return rp + (rva - vr);
                    }
                    return rva;
                }

                uint expOff = RvaToOffset(exportRva);
                uint numNames = BitConverter.ToUInt32(data, (int)expOff + 24);
                uint funcRva = BitConverter.ToUInt32(data, (int)expOff + 28);
                uint nameRva = BitConverter.ToUInt32(data, (int)expOff + 32);
                uint ordRva = BitConverter.ToUInt32(data, (int)expOff + 36);

                uint funcOff = RvaToOffset(funcRva);
                uint nameOff = RvaToOffset(nameRva);
                uint ordOff = RvaToOffset(ordRva);

                for (uint i = 0; i < numNames; i++)
                {
                    uint namePtrRva = BitConverter.ToUInt32(data, (int)nameOff + (int)i * 4);
                    uint namePtrOff = RvaToOffset(namePtrRva);
                    string name = System.Text.Encoding.ASCII.GetString(data, (int)namePtrOff, 128).Split('\0')[0];

                    if (name == exportName)
                    {
                        ushort ordinal = BitConverter.ToUInt16(data, (int)ordOff + (int)i * 2);
                        uint targetRva = BitConverter.ToUInt32(data, (int)funcOff + ordinal * 4);
                        uint targetOff = RvaToOffset(targetRva);

                        byte[] bytes = new byte[HOOK_PATCH_SIZE];
                        Array.Copy(data, (int)targetOff, bytes, 0, HOOK_PATCH_SIZE);
                        return bytes;
                    }
                }

                // Try with underscore prefix
                if (!exportName.StartsWith("_"))
                    return ReadOriginalBytes(dllPath, "_" + exportName);
            }
            catch { }
            return null;
        }

        static void RestoreHook(string processName)
        {
            string stateFile = Path.Combine(Path.GetTempPath(), "ssjj_hookstate.bin");
            if (!File.Exists(stateFile))
            {
                Console.WriteLine("[!] No saved hook state found. Run patcher first.");
                return;
            }

            IntPtr funcAddr;
            byte[] hookBytes, originalBytes;
            using (var fs = new FileStream(stateFile, FileMode.Open))
            using (var br = new BinaryReader(fs))
            {
                funcAddr = new IntPtr(br.ReadInt64());
                int hookLen = br.ReadInt32();
                hookBytes = br.ReadBytes(hookLen);
                int origLen = br.ReadInt32();
                originalBytes = br.ReadBytes(origLen);
            }

            var procs = Process.GetProcessesByName(processName);
            if (procs.Length == 0)
            {
                Console.WriteLine($"[!] Process '{processName}' not found");
                return;
            }

            var proc = procs[0];
            IntPtr hProcess = OpenProcess(
                PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION,
                false, proc.Id);

            if (hProcess == IntPtr.Zero)
            {
                Console.WriteLine("[!] Failed to open process. Run as Administrator.");
                return;
            }

            // Verify current bytes match original (still patched)
            byte[] currentBytes = new byte[hookBytes.Length];
            if (ReadProcessMemory(hProcess, funcAddr, currentBytes, (uint)hookBytes.Length, out _))
            {
                bool isPatched = true;
                for (int i = 0; i < hookBytes.Length; i++)
                {
                    if (currentBytes[i] != originalBytes[i])
                    {
                        isPatched = false;
                        break;
                    }
                }

                if (!isPatched)
                {
                    Console.WriteLine("[*] Hook already restored or bytes changed");
                    CloseHandle(hProcess);
                    File.Delete(stateFile);
                    return;
                }
            }

            // Restore GG's hook
            VirtualProtectEx(hProcess, funcAddr, (uint)hookBytes.Length, PAGE_EXECUTE_READWRITE, out uint oldProtect);
            WriteProcessMemory(hProcess, funcAddr, hookBytes, (uint)hookBytes.Length, out _);
            VirtualProtectEx(hProcess, funcAddr, (uint)hookBytes.Length, oldProtect, out _);

            Console.WriteLine("[+] GG hook RESTORED");
            File.Delete(stateFile);
            CloseHandle(hProcess);
        }

        static byte[] GetFirstBytes(byte[] arr, int count)
        {
            if (arr == null || arr.Length < count) return arr ?? new byte[0];
            var result = new byte[count];
            Array.Copy(arr, result, count);
            return result;
        }

        static void SaveState(IntPtr funcAddr, byte[] hookBytes, byte[] originalBytes)
        {
            string stateFile = Path.Combine(Path.GetTempPath(), "ssjj_hookstate.bin");
            using (var fs = new FileStream(stateFile, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(funcAddr.ToInt64());
                bw.Write(hookBytes.Length);
                bw.Write(hookBytes);
                bw.Write(originalBytes.Length);
                bw.Write(originalBytes);
            }
        }
    }
}
