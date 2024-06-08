using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Needle.Engine.Gltf.UnityGltf.Import
{
	public static class ReflectionUtils
	{
		public static bool TrySet(object instance, string name, object value)
		{
			var type = instance.GetType();
			var field = GetFieldOrProperty(type, name);
			return TrySet(instance, field, value);
		}

		public static Type TryGetType(object instance, string name)
		{
			var type = instance.GetType();
			var field = GetFieldOrProperty(type, name);
			if (field is FieldInfo fi) return fi.FieldType;
			if (field is PropertyInfo pi) return pi.PropertyType;
			return null;
		}

		public static bool TryGetMatchingType(ref object value, Type type)
		{
			if (value == null)  
				return true;

			var currentType = value.GetType();
			
			if (type == currentType) 
				return true;
			
			if (value is GameObject go && type == typeof(Transform))
			{ 
				value = go.transform;
				return true;
			}
			if (typeof(Enum).IsAssignableFrom(type))
			{
				value = Convert.ChangeType(value, typeof(int));
				return true;
			}
			if(value is IConvertible)
			{
				value = Convert.ChangeType(value, type);
				return true;
			}
			if (type.IsAssignableFrom(currentType))
			{
				return true;
			}

			return false;
		}

		private static bool TrySet(object instance, MemberInfo member, object value)
		{
			try
			{
				if (member is FieldInfo field)
				{
					if (TryGetMatchingType(ref value, field.FieldType))
					{
						field.SetValue(instance, value);
						return true;
					}
				}
				if (member is PropertyInfo property)
				{
					if (property.CanWrite)
					{
						if (TryGetMatchingType(ref value, property.PropertyType))
						{
							property.SetValue(instance, value);
							return true;
						}
					}
				}
			}
			catch (InvalidCastException invalidCast)
			{
				Debug.LogError(
					$"Failed to set value of {member.Name} on {instance.GetType().Name} → \"{value}\": {invalidCast.Message}");
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
			return false;
		}

		private static readonly Dictionary<Type, Dictionary<string, MemberInfo>> membersCache =
			new Dictionary<Type, Dictionary<string, MemberInfo>>();

		private static MemberInfo GetFieldOrProperty(Type type, string name)
		{
			if (membersCache.TryGetValue(type, out var list))
			{
				if (list.TryGetValue(name, out var memberInfo)) return memberInfo;
			}

			var reflected = GetFieldOrPropertyReflected(type, name);
			if (membersCache.ContainsKey(type) == false) membersCache.Add(type, new Dictionary<string, MemberInfo>());
			membersCache[type].Add(name, reflected);
			return reflected;
		}

		private static MemberInfo GetFieldOrPropertyReflected(Type type, string name)
		{
			var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null) return field;
			var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null) return property;
			if (type.BaseType != null)
			{
				var baseMember = GetFieldOrPropertyReflected(type.BaseType, name);
				if (baseMember != null) return baseMember;
			}
			return null;
		}
	}
}