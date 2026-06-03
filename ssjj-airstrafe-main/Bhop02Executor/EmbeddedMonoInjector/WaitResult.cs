namespace EmbeddedMonoInjector;

public enum WaitResult : uint
{
	WAIT_ABANDONED = 128u,
	WAIT_OBJECT_0 = 0u,
	WAIT_TIMEOUT = 258u,
	WAIT_FAILED = uint.MaxValue
}

