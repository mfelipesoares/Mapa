using System.Collections.Generic;

namespace Needle.Engine
{
	/// <summary>
	/// Holds info about register_types.js files and is used to generate imports per project
	/// </summary>
	public struct TypeRegisterFileInfo
	{
		/// <summary>
		/// Either a relative file path or a package import path (like @needle-tools/engine/codegen/...
		/// </summary>
		public string RelativePath;
		/// <summary>
		/// Absolute path to the file to import (used to check if the file actually exists)
		/// </summary>
		public string AbsolutePath;
	}
	
	/// <summary>
	/// Used to generate register_types.js files
	/// </summary>
	public sealed class TypeRegisterInfo
	{
		/// <summary>
		/// Path to register_types.js that should be generated
		/// </summary>
		public readonly string RegisterTypesPath;
		public readonly List<ImportInfo> Types;

		public TypeRegisterInfo(string registerTypesPath, List<ImportInfo> types)
		{
			RegisterTypesPath = registerTypesPath;
			Types = types;
		}
	}

	public interface ITypeRegisterProvider
	{
		void RegisterTypes(List<TypeRegisterInfo> infos, IProjectInfo projectInfo);
		void GetTypeRegisterPaths(List<TypeRegisterFileInfo> paths, IProjectInfo projectInfo);
	}
}