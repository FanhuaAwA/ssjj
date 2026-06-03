using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace EmbeddedMonoInjector;

public class Injector : IDisposable
{
	private const string mono_get_root_domain = "mono_get_root_domain";

	private const string mono_thread_attach = "mono_thread_attach";

	private const string mono_image_open_from_data = "mono_image_open_from_data";

	private const string mono_assembly_load_from_full = "mono_assembly_load_from_full";

	private const string mono_assembly_get_image = "mono_assembly_get_image";

	private const string mono_class_from_name = "mono_class_from_name";

	private const string mono_class_get_method_from_name = "mono_class_get_method_from_name";

	private const string mono_runtime_invoke = "mono_runtime_invoke";

	private const string mono_assembly_close = "mono_assembly_close";

	private const string mono_image_strerror = "mono_image_strerror";

	private const string mono_object_get_class = "mono_object_get_class";

	private const string mono_class_get_name = "mono_class_get_name";

	private readonly Dictionary<string, IntPtr> Exports = new Dictionary<string, IntPtr>
	{
		{
			"mono_get_root_domain",
			IntPtr.Zero
		},
		{
			"mono_thread_attach",
			IntPtr.Zero
		},
		{
			"mono_image_open_from_data",
			IntPtr.Zero
		},
		{
			"mono_assembly_load_from_full",
			IntPtr.Zero
		},
		{
			"mono_assembly_get_image",
			IntPtr.Zero
		},
		{
			"mono_class_from_name",
			IntPtr.Zero
		},
		{
			"mono_class_get_method_from_name",
			IntPtr.Zero
		},
		{
			"mono_runtime_invoke",
			IntPtr.Zero
		},
		{
			"mono_assembly_close",
			IntPtr.Zero
		},
		{
			"mono_image_strerror",
			IntPtr.Zero
		},
		{
			"mono_object_get_class",
			IntPtr.Zero
		},
		{
			"mono_class_get_name",
			IntPtr.Zero
		}
	};

	private Memory _memory;

	private IntPtr _rootDomain;

	private bool _attach;

	private readonly IntPtr _handle;

	private IntPtr _mono;

	public bool Is64Bit { get; private set; }

	public bool IsIL2Cpp { get; private set; }

	public Injector(string processName)
	{
		IsIL2Cpp = false;
		if (processName.EndsWith(".exe"))
		{
			processName.Replace(".exe", "");
		}
		Process process = Process.GetProcesses().FirstOrDefault((Process p) => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
		if (process == null)
		{
			throw new InjectorException("Could not find a process with the name " + processName);
		}
		if ((_handle = Native.OpenProcess(ProcessAccessRights.PROCESS_ALL_ACCESS, bInheritHandle: false, process.Id)) == IntPtr.Zero)
		{
			throw new InjectorException("Failed to open process", new Win32Exception(Marshal.GetLastWin32Error()));
		}
		Is64Bit = ProcessUtils.Is64BitProcess(_handle);
		if (ProcessUtils.CheckForIL2Cpp(_handle, process))
		{
			IsIL2Cpp = true;
		}
		if (!ProcessUtils.GetMonoModule(_handle, out _mono))
		{
			throw new InjectorException("Failed to find mono.dll in the target process");
		}
		_memory = new Memory(_handle);
	}

	public Injector(int processId)
	{
		IsIL2Cpp = false;
		Process process = Process.GetProcesses().FirstOrDefault((Process p) => p.Id == processId);
		if (process == null)
		{
			throw new InjectorException($"Could not find a process with the id {processId}");
		}
		if ((_handle = Native.OpenProcess(ProcessAccessRights.PROCESS_ALL_ACCESS, bInheritHandle: false, process.Id)) == IntPtr.Zero)
		{
			throw new InjectorException("Failed to open process", new Win32Exception(Marshal.GetLastWin32Error()));
		}
		Is64Bit = ProcessUtils.Is64BitProcess(_handle);
		if (ProcessUtils.CheckForIL2Cpp(_handle, process))
		{
			IsIL2Cpp = true;
		}
		if (!ProcessUtils.GetMonoModule(_handle, out _mono))
		{
			throw new InjectorException("Failed to find mono.dll in the target process");
		}
		_memory = new Memory(_handle);
	}

	public Injector(IntPtr processHandle, IntPtr monoModule)
	{
		if ((_handle = processHandle) == IntPtr.Zero)
		{
			throw new ArgumentException("Argument cannot be zero", "processHandle");
		}
		if ((_mono = monoModule) == IntPtr.Zero)
		{
			throw new ArgumentException("Argument cannot be zero", "monoModule");
		}
		Is64Bit = ProcessUtils.Is64BitProcess(_handle);
		_memory = new Memory(_handle);
	}

	public void Dispose()
	{
		_memory.Dispose();
		Native.CloseHandle(_handle);
	}

	private void ObtainMonoExports()
	{
		foreach (ExportedFunction exportedFunction in ProcessUtils.GetExportedFunctions(_handle, _mono))
		{
			if (Exports.ContainsKey(exportedFunction.Name))
			{
				Exports[exportedFunction.Name] = exportedFunction.Address;
			}
		}
		foreach (KeyValuePair<string, IntPtr> export in Exports)
		{
			if (export.Value == IntPtr.Zero)
			{
				throw new InjectorException("Failed to obtain the address of " + export.Key + "()");
			}
		}
	}

	public IntPtr Inject(byte[] rawAssembly, string @namespace, string className, string methodName)
	{
		if (rawAssembly == null)
		{
			throw new ArgumentNullException("rawAssembly");
		}
		if (rawAssembly.Length == 0)
		{
			throw new ArgumentException("rawAssembly cannot be empty", "rawAssembly");
		}
		if (className == null)
		{
			throw new ArgumentNullException("className");
		}
		if (methodName == null)
		{
			throw new ArgumentNullException("methodName");
		}
		ObtainMonoExports();
		_rootDomain = GetRootDomain();
		IntPtr image = OpenImageFromData(rawAssembly);
		_attach = true;
		IntPtr intPtr = OpenAssemblyFromImage(image);
		IntPtr imageFromAssembly = GetImageFromAssembly(intPtr);
		IntPtr classFromName = GetClassFromName(imageFromAssembly, @namespace, className);
		IntPtr methodFromName = GetMethodFromName(classFromName, methodName);
		RuntimeInvoke(methodFromName);
		return intPtr;
	}

	public void Eject(IntPtr assembly, string @namespace, string className, string methodName)
	{
		if (assembly == IntPtr.Zero)
		{
			throw new ArgumentException("assembly cannot be zero", "assembly");
		}
		if (className == null)
		{
			throw new ArgumentNullException("className");
		}
		if (methodName == null)
		{
			throw new ArgumentNullException("methodName");
		}
		ObtainMonoExports();
		_rootDomain = GetRootDomain();
		_attach = true;
		IntPtr imageFromAssembly = GetImageFromAssembly(assembly);
		IntPtr classFromName = GetClassFromName(imageFromAssembly, @namespace, className);
		IntPtr methodFromName = GetMethodFromName(classFromName, methodName);
		RuntimeInvoke(methodFromName);
		CloseAssembly(assembly);
	}

	private static void ThrowIfNull(IntPtr ptr, string methodName)
	{
		if (ptr == IntPtr.Zero)
		{
			throw new InjectorException(methodName + "() returned NULL");
		}
	}

	private IntPtr GetRootDomain()
	{
		IntPtr intPtr = Execute(Exports["mono_get_root_domain"]);
		ThrowIfNull(intPtr, "mono_get_root_domain");
		return intPtr;
	}

	private IntPtr OpenImageFromData(byte[] assembly)
	{
		IntPtr intPtr = _memory.Allocate(4);
		IntPtr result = Execute(Exports["mono_image_open_from_data"], _memory.AllocateAndWrite(assembly), (IntPtr)assembly.Length, (IntPtr)1, intPtr);
		MonoImageOpenStatus monoImageOpenStatus = (MonoImageOpenStatus)_memory.ReadInt(intPtr);
		if (monoImageOpenStatus != MonoImageOpenStatus.MONO_IMAGE_OK)
		{
			IntPtr address = Execute(Exports["mono_image_strerror"], (IntPtr)(int)monoImageOpenStatus);
			string text = _memory.ReadString(address, 256, Encoding.UTF8);
			throw new InjectorException("mono_image_open_from_data() failed: " + text);
		}
		return result;
	}

	private IntPtr OpenAssemblyFromImage(IntPtr image)
	{
		IntPtr intPtr = _memory.Allocate(4);
		IntPtr result = Execute(Exports["mono_assembly_load_from_full"], image, _memory.AllocateAndWrite(new byte[1]), intPtr, IntPtr.Zero);
		MonoImageOpenStatus monoImageOpenStatus = (MonoImageOpenStatus)_memory.ReadInt(intPtr);
		if (monoImageOpenStatus != MonoImageOpenStatus.MONO_IMAGE_OK)
		{
			IntPtr address = Execute(Exports["mono_image_strerror"], (IntPtr)(int)monoImageOpenStatus);
			string text = _memory.ReadString(address, 256, Encoding.UTF8);
			throw new InjectorException("mono_assembly_load_from_full() failed: " + text);
		}
		return result;
	}

	private IntPtr GetImageFromAssembly(IntPtr assembly)
	{
		IntPtr intPtr = Execute(Exports["mono_assembly_get_image"], assembly);
		ThrowIfNull(intPtr, "mono_assembly_get_image");
		return intPtr;
	}

	private IntPtr GetClassFromName(IntPtr image, string @namespace, string className)
	{
		IntPtr intPtr = Execute(Exports["mono_class_from_name"], image, _memory.AllocateAndWrite(@namespace), _memory.AllocateAndWrite(className));
		ThrowIfNull(intPtr, "mono_class_from_name");
		return intPtr;
	}

	private IntPtr GetMethodFromName(IntPtr @class, string methodName)
	{
		IntPtr intPtr = Execute(Exports["mono_class_get_method_from_name"], @class, _memory.AllocateAndWrite(methodName), IntPtr.Zero);
		ThrowIfNull(intPtr, "mono_class_get_method_from_name");
		return intPtr;
	}

	private string GetClassName(IntPtr monoObject)
	{
		IntPtr intPtr = Execute(Exports["mono_object_get_class"], monoObject);
		ThrowIfNull(intPtr, "mono_object_get_class");
		IntPtr intPtr2 = Execute(Exports["mono_class_get_name"], intPtr);
		ThrowIfNull(intPtr2, "mono_class_get_name");
		return _memory.ReadString(intPtr2, 256, Encoding.UTF8);
	}

	private string ReadMonoString(IntPtr monoString)
	{
		int num = _memory.ReadInt(monoString + (Is64Bit ? 16 : 8));
		return _memory.ReadUnicodeString(monoString + (Is64Bit ? 20 : 12), num * 2);
	}

	private void RuntimeInvoke(IntPtr method)
	{
		IntPtr intPtr = (Is64Bit ? _memory.AllocateAndWrite(0L) : _memory.AllocateAndWrite(0));
		Execute(Exports["mono_runtime_invoke"], method, IntPtr.Zero, IntPtr.Zero, intPtr);
		IntPtr intPtr2 = (IntPtr)_memory.ReadInt(intPtr);
		if (intPtr2 != IntPtr.Zero)
		{
			string className = GetClassName(intPtr2);
			string text = ReadMonoString((IntPtr)_memory.ReadInt(intPtr2 + (Is64Bit ? 32 : 16)));
			throw new InjectorException("The managed method threw an exception: (" + className + ") " + text);
		}
	}

	private void CloseAssembly(IntPtr assembly)
	{
		ThrowIfNull(Execute(Exports["mono_assembly_close"], assembly), "mono_assembly_close");
	}

	private IntPtr Execute(IntPtr address, params IntPtr[] args)
	{
		IntPtr intPtr = (Is64Bit ? _memory.AllocateAndWrite(0L) : _memory.AllocateAndWrite(0));
		byte[] data = Assemble(address, intPtr, args);
		IntPtr lpStartAddress = _memory.AllocateAndWrite(data);
		int lpThreadId;
		IntPtr intPtr2 = Native.CreateRemoteThread(_handle, IntPtr.Zero, 0, lpStartAddress, IntPtr.Zero, ThreadCreationFlags.None, out lpThreadId);
		if (intPtr2 == IntPtr.Zero)
		{
			throw new InjectorException("Failed to create a remote thread", new Win32Exception(Marshal.GetLastWin32Error()));
		}
		if (Native.WaitForSingleObject(intPtr2, -1) == WaitResult.WAIT_FAILED)
		{
			throw new InjectorException("Failed to wait for a remote thread", new Win32Exception(Marshal.GetLastWin32Error()));
		}
		IntPtr intPtr3 = (Is64Bit ? ((IntPtr)_memory.ReadLong(intPtr)) : ((IntPtr)_memory.ReadInt(intPtr)));
		if ((long)intPtr3 == 3221225477u)
		{
			throw new InjectorException("An access violation occurred while executing " + Exports.First((KeyValuePair<string, IntPtr> e) => e.Value == address).Key + "()");
		}
		return intPtr3;
	}

	private byte[] Assemble(IntPtr functionPtr, IntPtr retValPtr, IntPtr[] args)
	{
		if (!Is64Bit)
		{
			return Assemble86(functionPtr, retValPtr, args);
		}
		return Assemble64(functionPtr, retValPtr, args);
	}

	private byte[] Assemble86(IntPtr functionPtr, IntPtr retValPtr, IntPtr[] args)
	{
		Assembler assembler = new Assembler();
		if (_attach)
		{
			assembler.Push(_rootDomain);
			assembler.MovEax(Exports["mono_thread_attach"]);
			assembler.CallEax();
			assembler.AddEsp(4);
		}
		for (int num = args.Length - 1; num >= 0; num--)
		{
			assembler.Push(args[num]);
		}
		assembler.MovEax(functionPtr);
		assembler.CallEax();
		assembler.AddEsp((byte)(args.Length * 4));
		assembler.MovEaxTo(retValPtr);
		assembler.Return();
		return assembler.ToByteArray();
	}

	private byte[] Assemble64(IntPtr functionPtr, IntPtr retValPtr, IntPtr[] args)
	{
		Assembler assembler = new Assembler();
		assembler.SubRsp(40);
		if (_attach)
		{
			assembler.MovRax(Exports["mono_thread_attach"]);
			assembler.MovRcx(_rootDomain);
			assembler.CallRax();
		}
		assembler.MovRax(functionPtr);
		for (int i = 0; i < args.Length; i++)
		{
			switch (i)
			{
			case 0:
				assembler.MovRcx(args[i]);
				break;
			case 1:
				assembler.MovRdx(args[i]);
				break;
			case 2:
				assembler.MovR8(args[i]);
				break;
			case 3:
				assembler.MovR9(args[i]);
				break;
			}
		}
		assembler.CallRax();
		assembler.AddRsp(40);
		assembler.MovRaxTo(retValPtr);
		assembler.Return();
		return assembler.ToByteArray();
	}
}

