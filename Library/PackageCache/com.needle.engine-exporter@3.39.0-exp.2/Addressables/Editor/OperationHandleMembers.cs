#if HAS_ADDRESSABLES
using System;
using System.Collections;
using System.Reflection;
using Needle.Engine.Core.References;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Needle.Engine.Addressables
{
	public class OperationHandleMembers : ITypeMemberHandler
	{
		public bool ShouldIgnore(Type currentType, MemberInfo member)
		{
			return typeof(AsyncOperationHandle).IsAssignableFrom(currentType);
		}

		public bool ShouldRename(MemberInfo member, out string newName)
		{
			newName = null;
			return false;
		}
	}
}

#endif