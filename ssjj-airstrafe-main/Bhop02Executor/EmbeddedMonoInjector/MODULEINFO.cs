using System;

namespace EmbeddedMonoInjector;

public struct MODULEINFO
{
	public IntPtr lpBaseOfDll;

	public int SizeOfImage;

	public IntPtr EntryPoint;
}

