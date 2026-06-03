using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace EmbeddedMonoInjector;

public static class ProcessUtils
{
	private static bool isTargetx64;

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsWow64Process2([In] IntPtr hProcess, out ushort processMachine, out ushort nativeMachine);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

	public static IEnumerable<ExportedFunction> GetExportedFunctions(IntPtr handle, IntPtr mod)
	{
		using Memory memory = new Memory(handle);
		int num = memory.ReadInt(mod + 60);
		IntPtr address = mod + num + 24 + (Is64BitProcess(handle) ? 112 : 96);
		IntPtr intPtr = mod + memory.ReadInt(address);
		IntPtr names = mod + memory.ReadInt(intPtr + 32);
		IntPtr ordinals = mod + memory.ReadInt(intPtr + 36);
		IntPtr functions = mod + memory.ReadInt(intPtr + 28);
		int count = memory.ReadInt(intPtr + 24);
		for (int i = 0; i < count; i++)
		{
			int num2 = memory.ReadInt(names + i * 4);
			string name = memory.ReadString(mod + num2, 32, Encoding.ASCII);
			short num3 = memory.ReadShort(ordinals + i * 2);
			IntPtr intPtr2 = mod + memory.ReadInt(functions + num3 * 4);
			if (intPtr2 != IntPtr.Zero)
			{
				yield return new ExportedFunction(name, intPtr2);
			}
		}
	}

	public static bool GetMonoModule(IntPtr handle, out IntPtr monoModule)
	{
		int num = (Is64BitProcess(handle) ? 8 : 4);
		IntPtr[] lphModule = new IntPtr[0];
		if (!Native.EnumProcessModulesEx(handle, lphModule, 0, out var lpcbNeeded, ModuleFilter.LIST_MODULES_ALL))
		{
			throw new InjectorException("Failed to enumerate process modules", new Win32Exception(Marshal.GetLastWin32Error()));
		}
		int num2 = lpcbNeeded / num;
		lphModule = new IntPtr[num2];
		if (!Native.EnumProcessModulesEx(handle, lphModule, lpcbNeeded, out lpcbNeeded, ModuleFilter.LIST_MODULES_ALL))
		{
			throw new InjectorException("Failed to enumerate process modules", new Win32Exception(Marshal.GetLastWin32Error()));
		}
		for (int i = 0; i < num2; i++)
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder(260);
				Native.GetModuleFileNameEx(handle, lphModule[i], stringBuilder, 260u);
				if (stringBuilder.ToString().IndexOf("mono", StringComparison.OrdinalIgnoreCase) > -1)
				{
					if (!Native.GetModuleInformation(handle, lphModule[i], out var lpmodinfo, (uint)(num * lphModule.Length)))
					{
						throw new InjectorException("Failed to get module information", new Win32Exception(Marshal.GetLastWin32Error()));
					}
					if (GetExportedFunctions(handle, lpmodinfo.lpBaseOfDll).Any((ExportedFunction f) => f.Name == "mono_get_root_domain"))
					{
						monoModule = lpmodinfo.lpBaseOfDll;
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "[ProcessUtils] GetMono - ERROR: " + ex.Message + "\r\n");
			}
		}
		monoModule = IntPtr.Zero;
		return false;
	}

	public static bool Is64BitProcess(IntPtr handle)
	{
		try
		{
			if (!Environment.Is64BitOperatingSystem)
			{
				return false;
			}
			if (((string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows NT\\CurrentVersion", "ProductName", null)).Contains("Windows 10"))
			{
				isTargetx64 = false;
				if (handle != IntPtr.Zero)
				{
					ushort processMachine = 0;
					ushort nativeMachine = 0;
					try
					{
						IsWow64Process2(handle, out processMachine, out nativeMachine);
						if (processMachine == 332)
						{
							isTargetx64 = false;
						}
						else
						{
							isTargetx64 = true;
						}
						return isTargetx64;
					}
					catch
					{
					}
				}
			}
			IsWow64Process(handle, out var wow64Process);
			if (wow64Process)
			{
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "[ProcessUtils] is64Bit - ERROR: " + ex.Message + "\r\n");
		}
		return true;
	}

	public static bool CheckForIL2Cpp(IntPtr handle, Process process)
	{
		int num = (Is64BitProcess(handle) ? 8 : 4);
		IntPtr[] lphModule = new IntPtr[0];
		if (!Native.EnumProcessModulesEx(handle, lphModule, 0, out var lpcbNeeded, ModuleFilter.LIST_MODULES_ALL))
		{
			throw new InjectorException("Failed to enumerate process modules", new Win32Exception(Marshal.GetLastWin32Error()));
		}
		int num2 = lpcbNeeded / num;
		lphModule = new IntPtr[num2];
		if (!Native.EnumProcessModulesEx(handle, lphModule, lpcbNeeded, out lpcbNeeded, ModuleFilter.LIST_MODULES_ALL))
		{
			throw new InjectorException("Failed to enumerate process modules", new Win32Exception(Marshal.GetLastWin32Error()));
		}
		for (int i = 0; i < num2; i++)
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder(260);
				Native.GetModuleFileNameEx(handle, lphModule[i], stringBuilder, 260u);
				if (stringBuilder.ToString().IndexOf("gameassembly", StringComparison.OrdinalIgnoreCase) > -1)
				{
					File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "\t\tIL2CPP Found in: " + process.ProcessName + ".exe\r\n");
					return true;
				}
			}
			catch (Exception ex)
			{
				File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "[ProcessUtils] IL2CPPCheck - ERROR: " + ex.Message + "\r\n");
			}
		}
		return false;
	}
}

