using System.Collections.Generic;
using System.Reflection;

namespace Bhop02;

internal class MethodHookPool
{
	private static readonly Dictionary<MethodBase, MethodHook> _hookers = new Dictionary<MethodBase, MethodHook>();

	public static void AddHooker(MethodBase method, MethodHook hooker)
	{
		if (_hookers.TryGetValue(method, out var value))
		{
			value.Uninstall();
			_hookers[method] = hooker;
		}
		else
		{
			_hookers.Add(method, hooker);
		}
	}

	public static void RemoveHooker(MethodBase method)
	{
		_hookers.Remove(method);
	}
}

