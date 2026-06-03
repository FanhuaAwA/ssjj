using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Plugins.Hacks
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class MethodHookManager
    {
        public MethodHookManager()
        {
            Assembly.GetExecutingAssembly().ManifestModule.GetPEKind(out _, out ImageFileMachine imageFileMachine);
            if (imageFileMachine == ImageFileMachine.AMD64 || imageFileMachine == ImageFileMachine.I386)
            {
                this.Is64Bit = imageFileMachine == ImageFileMachine.AMD64;
                this.Hooks = new Dictionary<MethodInfo, byte[]>();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public void HookMethod(MethodInfo originalMethod, MethodInfo replacementMethod)
        {
            if (originalMethod == null)
            {
                throw new ArgumentNullException(nameof(originalMethod));
            }
            if (replacementMethod == null)
            {
                throw new ArgumentNullException(nameof(replacementMethod));
            }
            if (originalMethod == replacementMethod)
            {
                throw new ArgumentException();
            }
            if (originalMethod.IsGenericMethod)
            {
                throw new ArgumentException();
            }
            if (replacementMethod.IsGenericMethod || !replacementMethod.IsStatic)
            {
                throw new ArgumentException();
            }
            if (this.Hooks.ContainsKey(originalMethod))
            {
                throw new ArgumentException();
            }
            byte[] originalBytes = this.PatchJmpToMethod(originalMethod, replacementMethod);
            this.Hooks.Add(originalMethod, originalBytes);
        }

        public void UnhookMethod(MethodInfo originalMethod)
        {
            if (originalMethod == null)
            {
                throw new ArgumentNullException(nameof(originalMethod));
            }
            if (!this.Hooks.ContainsKey(originalMethod))
            {
                throw new ArgumentException();
            }
            byte[] originalBytes = this.Hooks[originalMethod];
            this.UnpatchJmpToMethod(originalMethod, originalBytes);
            _ = this.Hooks.Remove(originalMethod);
        }

        private unsafe byte[] PatchJmpToMethod(MethodInfo originalMethod, MethodInfo replacementMethod)
        {
            RuntimeHelpers.PrepareMethod(originalMethod.MethodHandle);
            RuntimeHelpers.PrepareMethod(replacementMethod.MethodHandle);
            IntPtr originalMethodPtr = originalMethod.MethodHandle.GetFunctionPointer();
            IntPtr replacementMethodPtr = replacementMethod.MethodHandle.GetFunctionPointer();
            uint jmpSize = this.Is64Bit ? 13U : 6U;
            byte[] originalBytes = new byte[jmpSize];
            uint oldProtection = this.VirtualProtect(originalMethodPtr, jmpSize, 64U);
            byte* ptr = (byte*)originalMethodPtr.ToPointer();
            for (int i = 0; i < jmpSize; i++)
            {
                originalBytes[i] = ptr[i];
            }
            if (this.Is64Bit)
            {
                ptr[0] = 73; ptr[1] = 187;
                *(long*)(ptr + 2) = replacementMethodPtr.ToInt64();
                ptr[10] = 65; ptr[11] = byte.MaxValue;
                ptr[12] = 227;
            }
            else
            {
                ptr[0] = 104; *(int*)(ptr + 1) = replacementMethodPtr.ToInt32();
                ptr[5] = 195;
            }
            this.FlushInstructionCache(originalMethodPtr, jmpSize);
            _ = this.VirtualProtect(originalMethodPtr, jmpSize, oldProtection);
            return originalBytes;
        }

        private unsafe void UnpatchJmpToMethod(MethodInfo originalMethod, byte[] originalBytes)
        {
            IntPtr originalMethodPtr = originalMethod.MethodHandle.GetFunctionPointer();
            uint oldProtection = this.VirtualProtect(originalMethodPtr, (uint)originalBytes.Length, 64U);
            byte* ptr = (byte*)originalMethodPtr.ToPointer();
            for (int i = 0; i < originalBytes.Length; i++)
            {
                ptr[i] = originalBytes[i];
            }
            this.FlushInstructionCache(originalMethodPtr, (uint)originalBytes.Length);
            _ = this.VirtualProtect(originalMethodPtr, (uint)originalBytes.Length, oldProtection);
        }

        private uint VirtualProtect(IntPtr address, uint size, uint newProtection)
        {
            return !VirtualProtect(address, (UIntPtr)size, newProtection, out uint oldProtection) ? throw new Win32Exception() : oldProtection;
        }

        private void FlushInstructionCache(IntPtr address, uint size)
        {
            if (!FlushInstructionCache(GetCurrentProcess(), address, (UIntPtr)size))
            {
                throw new Win32Exception();
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, UIntPtr dwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        private readonly bool Is64Bit;
        private readonly Dictionary<MethodInfo, byte[]> Hooks;
    }
}