using System.Reflection;

namespace Bhop02;

internal static class ReflectionExtension
{
	public static T ReflectProperty<T>(this object obj, string name)
	{
		T result;
		if (obj == null)
		{
			result = default(T);
		}
		else
		{
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			PropertyInfo property = obj.GetType().GetProperty(name, bindingAttr);
			if (!(property == null))
			{
				return (T)property.GetValue(obj, null);
			}
			result = default(T);
		}
		return result;
	}

	public static T ReflectField<T>(this object obj, string name)
	{
		T result;
		if (obj == null)
		{
			result = default(T);
		}
		else
		{
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			FieldInfo field = obj.GetType().GetField(name, bindingAttr);
			if (!(field == null))
			{
				return (T)field.GetValue(obj);
			}
			result = default(T);
		}
		return result;
	}

	public static MethodInfo ReflectMethod(this object obj, string name)
	{
		if (obj == null)
		{
			return null;
		}
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		MethodInfo method = obj.GetType().GetMethod(name, bindingAttr);
		if (method == null)
		{
			return null;
		}
		return method;
	}

	public static void ReflectInvokeMethod(this object obj, string name, params object[] parameters)
	{
		if (obj != null)
		{
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			obj.GetType().GetMethod(name, bindingAttr)?.Invoke(obj, parameters);
		}
	}

	public static T ReflectInvokeMethod<T>(this object obj, string name, params object[] parameters)
	{
		T result;
		if (obj == null)
		{
			result = default(T);
		}
		else
		{
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			MethodInfo method = obj.GetType().GetMethod(name, bindingAttr);
			if (!(method == null))
			{
				return (T)method.Invoke(obj, parameters);
			}
			result = default(T);
		}
		return result;
	}
}

