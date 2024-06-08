using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Gltf.UnityGltf.Import
{
	public struct ResolveReference : ICommand
	{
		public IImportContext context;
		private readonly string id;
		public object target;
		public string memberName;
		public int index;

		public ResolveReference(IImportContext context,
			object target,
			string memberName,
			string id,
			int index = -1)
		{
			this.context = context;
			this.target = target;
			this.memberName = memberName;
			this.id = id;
			this.index = index;
			if(target is IList && index < 0) Debug.LogError("ResolveReference: index is required for list targets");
		}

		public Task Execute()
		{
			if (context.TryResolve(id, out var value))
			{
				if (target is IList list)
				{
					list[index] = value;
				}
				else
				{
					ReflectionUtils.TrySet(target, memberName, value);
				}
			}

			return Task.CompletedTask;
		}
	}
}