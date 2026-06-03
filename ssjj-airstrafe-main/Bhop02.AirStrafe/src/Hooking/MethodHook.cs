using System;
using System.Reflection;
using Bhop02.DotNetDetour;

namespace Bhop02;

internal class MethodHook
{
	private readonly MethodBase _targetMethod;

	private readonly MethodBase _replacementMethod;

	private readonly MethodBase _proxyMethod;

	private readonly IntPtr _targetPtr;

	private readonly IntPtr _replacementPtr;

	private readonly IntPtr _proxyPtr;

	private static readonly byte[] s_jmpBuff;

	private static readonly byte[] s_jmpBuff_32;

	private static readonly byte[] s_jmpBuff_64;

	private static readonly int s_addrOffset;

	private readonly byte[] _jmpBuff;

	private byte[] _proxyBuff;

	public bool isHooked { get; private set; }

	static MethodHook()
	{
		s_jmpBuff_32 = new byte[6] { 104, 0, 0, 0, 0, 195 };
		s_jmpBuff_64 = new byte[14]
		{
			255, 37, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 0, 0
		};
		if (IntPtr.Size == 4)
		{
			s_jmpBuff = s_jmpBuff_32;
			s_addrOffset = 1;
		}
		else
		{
			s_jmpBuff = s_jmpBuff_64;
			s_addrOffset = 6;
		}
	}

	public MethodHook(MethodBase targetMethod, MethodBase replacementMethod, MethodBase proxyMethod = null)
	{
		_targetMethod = targetMethod;
		_replacementMethod = replacementMethod;
		_proxyMethod = proxyMethod;
		_targetPtr = GetFunctionAddr(_targetMethod);
		_replacementPtr = GetFunctionAddr(_replacementMethod);
		if (proxyMethod != null)
		{
			_proxyPtr = GetFunctionAddr(_proxyMethod);
		}
		_jmpBuff = new byte[s_jmpBuff.Length];
	}

	public void Install()
	{
		if (!isHooked)
		{
			MethodHookPool.AddHooker(_targetMethod, this);
			InitProxyBuff();
			BackupHeader();
			PatchTargetMethod();
			PatchProxyMethod();
			isHooked = true;
		}
	}

	public unsafe void Uninstall()
	{
		if (isHooked)
		{
			byte* ptr = (byte*)_targetPtr.ToPointer();
			for (int i = 0; i < _proxyBuff.Length; i++)
			{
				*(ptr++) = _proxyBuff[i];
			}
			isHooked = false;
			MethodHookPool.RemoveHooker(_targetMethod);
		}
	}

	private unsafe void InitProxyBuff()
	{
		byte* code = (byte*)_targetPtr.ToPointer();
		uint num = LDasm.SizeofMinNumByte(code, s_jmpBuff.Length);
		_proxyBuff = new byte[num];
	}

	private unsafe void BackupHeader()
	{
		byte* ptr = (byte*)_targetPtr.ToPointer();
		for (int i = 0; i < _proxyBuff.Length; i++)
		{
			_proxyBuff[i] = *(ptr++);
		}
	}

	private unsafe void PatchTargetMethod()
	{
		Array.Copy(s_jmpBuff, _jmpBuff, _jmpBuff.Length);
		fixed (byte* ptr = &_jmpBuff[s_addrOffset])
		{
			byte* ptr2 = ptr;
			if (IntPtr.Size == 4)
			{
				*(int*)ptr2 = _replacementPtr.ToInt32();
			}
			else
			{
				*(long*)ptr2 = _replacementPtr.ToInt64();
			}
		}
		byte* ptr3 = (byte*)_targetPtr.ToPointer();
		if (ptr3 != null)
		{
			int i = 0;
			for (int num = _jmpBuff.Length; i < num; i++)
			{
				*(ptr3++) = _jmpBuff[i];
			}
		}
	}

	private unsafe void PatchProxyMethod()
	{
		if (_proxyPtr == IntPtr.Zero)
		{
			return;
		}
		byte* ptr = (byte*)_proxyPtr.ToPointer();
		for (int i = 0; i < _proxyBuff.Length; i++)
		{
			*(ptr++) = _proxyBuff[i];
		}
		fixed (byte* ptr2 = &_jmpBuff[s_addrOffset])
		{
			byte* ptr3 = ptr2;
			if (IntPtr.Size == 4)
			{
				*(int*)ptr3 = _targetPtr.ToInt32() + _proxyBuff.Length;
			}
			else
			{
				*(long*)ptr3 = _targetPtr.ToInt64() + _proxyBuff.Length;
			}
		}
		for (int j = 0; j < _jmpBuff.Length; j++)
		{
			*(ptr++) = _jmpBuff[j];
		}
	}

	private IntPtr GetFunctionAddr(MethodBase method)
	{
		return method.MethodHandle.GetFunctionPointer();
	}
}

