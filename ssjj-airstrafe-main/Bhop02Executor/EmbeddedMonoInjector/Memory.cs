using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace EmbeddedMonoInjector;

public class Memory : IDisposable
{
	private readonly IntPtr _handle;

	private readonly Dictionary<IntPtr, int> _allocations = new Dictionary<IntPtr, int>();

	public Memory(IntPtr processHandle)
	{
		_handle = processHandle;
	}

	public string ReadString(IntPtr address, int length, Encoding encoding)
	{
		List<byte> list = new List<byte>();
		for (int i = 0; i < length; i++)
		{
			byte b = ReadBytes(address + list.Count, 1)[0];
			if (b == 0)
			{
				break;
			}
			list.Add(b);
		}
		return encoding.GetString(list.ToArray());
	}

	public string ReadUnicodeString(IntPtr address, int length)
	{
		return Encoding.Unicode.GetString(ReadBytes(address, length));
	}

	public short ReadShort(IntPtr address)
	{
		return BitConverter.ToInt16(ReadBytes(address, 2), 0);
	}

	public int ReadInt(IntPtr address)
	{
		return BitConverter.ToInt32(ReadBytes(address, 4), 0);
	}

	public long ReadLong(IntPtr address)
	{
		return BitConverter.ToInt64(ReadBytes(address, 8), 0);
	}

	public byte[] ReadBytes(IntPtr address, int size)
	{
		byte[] array = new byte[size];
		if (!Native.ReadProcessMemory(_handle, address, array, size))
		{
			throw new InjectorException("Failed to read process memory", new Win32Exception(Marshal.GetLastWin32Error()));
		}
		return array;
	}

	public IntPtr AllocateAndWrite(byte[] data)
	{
		IntPtr intPtr = Allocate(data.Length);
		Write(intPtr, data);
		return intPtr;
	}

	public IntPtr AllocateAndWrite(string data)
	{
		return AllocateAndWrite(Encoding.UTF8.GetBytes(data));
	}

	public IntPtr AllocateAndWrite(int data)
	{
		return AllocateAndWrite(BitConverter.GetBytes(data));
	}

	public IntPtr AllocateAndWrite(long data)
	{
		return AllocateAndWrite(BitConverter.GetBytes(data));
	}

	public IntPtr Allocate(int size)
	{
		IntPtr intPtr = Native.VirtualAllocEx(_handle, IntPtr.Zero, size, AllocationType.MEM_COMMIT, MemoryProtection.PAGE_EXECUTE_READWRITE);
		if (intPtr == IntPtr.Zero)
		{
			throw new InjectorException("Failed to allocate process memory", new Win32Exception(Marshal.GetLastWin32Error()));
		}
		_allocations.Add(intPtr, size);
		return intPtr;
	}

	public void Write(IntPtr addr, byte[] data)
	{
		if (!Native.WriteProcessMemory(_handle, addr, data, data.Length))
		{
			throw new InjectorException("Failed to write process memory", new Win32Exception(Marshal.GetLastWin32Error()));
		}
	}

	public void Dispose()
	{
		foreach (KeyValuePair<IntPtr, int> allocation in _allocations)
		{
			Native.VirtualFreeEx(_handle, allocation.Key, allocation.Value, MemoryFreeType.MEM_DECOMMIT);
		}
	}
}

