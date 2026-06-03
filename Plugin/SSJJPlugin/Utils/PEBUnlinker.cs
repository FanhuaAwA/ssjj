using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Components
{
    /// <summary>
    /// PEB (Process Environment Block) module unlinking.
    /// Hides our DLL from CreateToolhelp32Snapshot enumeration used by ggscan.
    /// Based on IDA reverse engineering of nProtect GameGuard module scanning logic.
    /// </summary>
    internal static unsafe class PEBUnlinker
    {
        // PEB access via x64 GS segment
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize,
            uint flNewProtect, out uint lpflOldProtect);

        private const uint PAGE_READWRITE = 0x04;

        // PEB_LDR_DATA offsets (x64)
        private const int LDR_OFFSET_IN_LOAD_ORDER = 0x10;
        private const int LDR_OFFSET_IN_MEMORY_ORDER = 0x20;
        private const int LDR_OFFSET_IN_INIT_ORDER = 0x30;
        // LDR_DATA_TABLE_ENTRY offsets (x64)
        private const int LDR_LOAD_ORDER_LINKS = 0x00;
        private const int LDR_MEMORY_ORDER_LINKS = 0x10;
        private const int LDR_INIT_ORDER_LINKS = 0x20;
        private const int LDR_DLL_BASE = 0x30;

        public static bool HideModule(string moduleName)
        {
            try
            {
                IntPtr hModule = GetModuleHandle(moduleName);
                if (hModule == IntPtr.Zero) return false;

                // On x64: PEB at GS:[0x60]
                // Get PEB pointer via NtQueryInformationProcess
                var pbi = new PROCESS_BASIC_INFORMATION();
                uint returnLength;
                int status = NtQueryInformationProcess(
                    GetCurrentProcess(),
                    0, // ProcessBasicInformation
                    ref pbi,
                    (uint)Marshal.SizeOf(pbi),
                    out returnLength);

                if (status != 0) return false;
                IntPtr pebPtr = pbi.PebBaseAddress;
                if (pebPtr == IntPtr.Zero) return false;

                // Read PEB_LDR_DATA pointer at PEB+0x18
                IntPtr ldrPtr = Marshal.ReadIntPtr(pebPtr, 0x18);
                if (ldrPtr == IntPtr.Zero) return false;

                // Walk modules
                UnlinkFromList(ldrPtr, LDR_OFFSET_IN_LOAD_ORDER, LDR_LOAD_ORDER_LINKS, hModule);
                UnlinkFromList(ldrPtr, LDR_OFFSET_IN_MEMORY_ORDER, LDR_MEMORY_ORDER_LINKS, hModule);
                UnlinkFromList(ldrPtr, LDR_OFFSET_IN_INIT_ORDER, LDR_INIT_ORDER_LINKS, hModule);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool UnlinkFromList(IntPtr ldrPtr, int listOffset, int entryLinkOffset, IntPtr targetBase)
        {
            // Get list head
            IntPtr headEntry = ldrPtr + listOffset;
            IntPtr current = Marshal.ReadIntPtr(headEntry); // Flink of head

            while (current != IntPtr.Zero && current != headEntry)
            {
                // Read DllBase from entry
                IntPtr dllBase = Marshal.ReadIntPtr(current, LDR_DLL_BASE - entryLinkOffset + LDR_LOAD_ORDER_LINKS);
                // Actually the LDR_DATA_TABLE_ENTRY starts at (current - entryLinkOffset)
                IntPtr entry = current - entryLinkOffset;
                dllBase = Marshal.ReadIntPtr(entry, LDR_DLL_BASE);

                if (dllBase == targetBase)
                {
                    // Found our module - unlink it
                    IntPtr flink = Marshal.ReadIntPtr(current);
                    IntPtr blink = Marshal.ReadIntPtr(current, 8);

                    // Make memory writable
                    uint oldProtect;
                    VirtualProtect(current - entryLinkOffset, 0x100, PAGE_READWRITE, out oldProtect);

                    // prev->Flink = next
                    Marshal.WriteIntPtr(blink, flink);
                    // next->Blink = prev
                    Marshal.WriteIntPtr(flink + 8, blink);

                    // Clear our own links to avoid traversing back
                    Marshal.WriteIntPtr(current, IntPtr.Zero);
                    Marshal.WriteIntPtr(current + 8, IntPtr.Zero);

                    return true;
                }

                current = Marshal.ReadIntPtr(current);
            }
            return false;
        }

        // NtQueryInformationProcess
        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            ref PROCESS_BASIC_INFORMATION processInformation,
            uint processInformationLength,
            out uint returnLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr Reserved3;
        }
    }
}
