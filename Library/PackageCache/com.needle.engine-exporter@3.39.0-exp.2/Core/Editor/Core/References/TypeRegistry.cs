#nullable enable

using System;
using System.Collections.Generic;

namespace Needle.Engine.Core.References
{
	/// <summary>
	/// Used to register emitted js objects and paths + register fields
	/// </summary>
	public class TypeRegistry : ITypeRegistry
	{
		public bool IsKnownType(Type type)
		{
			OptimizeLookup();
			return knownTypesDict.ContainsKey(type.Name); // knownTypes.Any(t => t.TypeName == type.Name);
		}

		public IReadOnlyList<IImportedTypeInfo> KnownTypes { get; }


		public bool IsInstalled(Type type)
		{
			if (TryGetImportedTypeInfo(type, out var ti)) return ti.IsInstalled;
			return false;
		}

		public bool TryGetImportedTypeInfo(Type type, out IImportedTypeInfo info)
		{
			return knownTypesDict.TryGetValue(type.Name, out info);
		}
		
		private readonly Dictionary<string, IImportedTypeInfo> knownTypesDict;

		private void OptimizeLookup()
		{
			if(knownTypesDict.Count > 0) return;
			foreach (var t in KnownTypes)
			{
				if (!knownTypesDict.ContainsKey(t.TypeName))
					knownTypesDict.Add(t.TypeName, t);
			}
		}

		public TypeRegistry(IReadOnlyList<IImportedTypeInfo>? knownTypes)
		{
			this.KnownTypes = knownTypes ?? Array.Empty<IImportedTypeInfo>();
			knownTypesDict = new Dictionary<string, IImportedTypeInfo>();
		}
	}
}