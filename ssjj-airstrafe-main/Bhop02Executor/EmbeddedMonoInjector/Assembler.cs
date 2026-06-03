using System;
using System.Collections.Generic;

namespace EmbeddedMonoInjector;

public class Assembler
{
	private readonly List<byte> _asm = new List<byte>();

	public void MovRax(IntPtr arg)
	{
		_asm.AddRange(new byte[2] { 72, 184 });
		_asm.AddRange(BitConverter.GetBytes((long)arg));
	}

	public void MovRcx(IntPtr arg)
	{
		_asm.AddRange(new byte[2] { 72, 185 });
		_asm.AddRange(BitConverter.GetBytes((long)arg));
	}

	public void MovRdx(IntPtr arg)
	{
		_asm.AddRange(new byte[2] { 72, 186 });
		_asm.AddRange(BitConverter.GetBytes((long)arg));
	}

	public void MovR8(IntPtr arg)
	{
		_asm.AddRange(new byte[2] { 73, 184 });
		_asm.AddRange(BitConverter.GetBytes((long)arg));
	}

	public void MovR9(IntPtr arg)
	{
		_asm.AddRange(new byte[2] { 73, 185 });
		_asm.AddRange(BitConverter.GetBytes((long)arg));
	}

	public void SubRsp(byte arg)
	{
		_asm.AddRange(new byte[3] { 72, 131, 236 });
		_asm.Add(arg);
	}

	public void CallRax()
	{
		_asm.AddRange(new byte[2] { 255, 208 });
	}

	public void AddRsp(byte arg)
	{
		_asm.AddRange(new byte[3] { 72, 131, 196 });
		_asm.Add(arg);
	}

	public void MovRaxTo(IntPtr dest)
	{
		_asm.AddRange(new byte[2] { 72, 163 });
		_asm.AddRange(BitConverter.GetBytes((long)dest));
	}

	public void Push(IntPtr arg)
	{
		_asm.Add((byte)(((int)arg < 128) ? 106 : 104));
		_asm.AddRange(((int)arg > 255) ? BitConverter.GetBytes((int)arg) : new byte[1] { (byte)(int)arg });
	}

	public void MovEax(IntPtr arg)
	{
		_asm.Add(184);
		_asm.AddRange(BitConverter.GetBytes((int)arg));
	}

	public void CallEax()
	{
		_asm.AddRange(new byte[2] { 255, 208 });
	}

	public void AddEsp(byte arg)
	{
		_asm.AddRange(new byte[2] { 131, 196 });
		_asm.Add(arg);
	}

	public void MovEaxTo(IntPtr dest)
	{
		_asm.Add(163);
		_asm.AddRange(BitConverter.GetBytes((int)dest));
	}

	public void Return()
	{
		_asm.Add(195);
	}

	public byte[] ToByteArray()
	{
		return _asm.ToArray();
	}
}

