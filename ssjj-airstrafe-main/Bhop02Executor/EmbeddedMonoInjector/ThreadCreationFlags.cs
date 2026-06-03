using System;

namespace EmbeddedMonoInjector;

[Flags]
public enum ThreadCreationFlags
{
	None = 0,
	CREATE_SUSPENDED = 4,
	STACK_SIZE_PARAM_IS_A_RESERVATION = 0x10000
}

