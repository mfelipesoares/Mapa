using System.IO;
using UnityEditor;
using UnityEngine.Video;

namespace Needle.Engine.Core.References.ReferenceResolvers
{
	public class VideoResolver
	{
		// public bool TryResolve(ReferenceResolver resolver, ReferenceCollection references, string path1, object value, out string result)
		// {
		// 	if (value is VideoClip clip)
		// 	{
		// 		result = "\"" + ExportVideoClip(clip, resolver.ExportContext!.AssetsDirectory) + "\"";
		// 		return true;
		// 	}
		// 	// if (resolver.CurrentField?.Owner is VideoPlayer && resolver.CurrentField.Name == "url")
		// 	// {
		// 	// 	
		// 	// }
		// 	
		// 	result = null;
		// 	return false;
		// }

		public static string ExportVideoClip(VideoClip clip, IExportContext context)
		{
			var path = AssetDatabase.GetAssetPath(clip);
			var name = Path.GetFileName(path);
			var outputPath = Path.GetDirectoryName(context.Path) + "/" + name;
			File.Copy(path, outputPath, true);
			return name.AsRelativeUri();
		}
	}
}