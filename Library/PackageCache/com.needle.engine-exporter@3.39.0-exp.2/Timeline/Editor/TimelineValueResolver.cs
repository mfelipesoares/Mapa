using System.Collections.Generic;
using System.Reflection;
using Needle.Engine.Core;
using Needle.Engine.Utils;
using UnityEngine.Timeline;

namespace Needle.Engine.Timeline
{
	internal class TimelineValueResolver : IValueResolver
	{
		public readonly PlayableDirectorExportContext context;

		public TimelineValueResolver(PlayableDirectorExportContext directorExport)
		{
			this.context = directorExport;
		}

		public bool TryGetValue(IExportContext ctx, object instance, MemberInfo member, ref object value)
		{
			if (value is TimelineAsset asset)
			{
				if (context.Director.playableAsset == asset)
				{
					if (TimelineSerializer.TryExportPlayableAsset(context, asset, out var res))
					{
						value = res;
						return true;
					}
				}
			}
				
			if (value is SignalAsset signal)
			{
				value = new SignalAssetModel(signal);
				return true;
			}
				
			return false;
		}
	}
}