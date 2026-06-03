using System;

namespace EmbeddedMonoInjector;

public struct ExportedFunction(string name, IntPtr address)
{
	public string Name = name;

	public IntPtr Address = address;
}

