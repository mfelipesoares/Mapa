using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Needle.Engine.Gltf;
using Needle.Engine.Utils;
using UnityEngine.Timeline;

namespace Needle.Engine.Timeline
{
	public class SignalReceiverValueResolver : GltfExtensionHandlerBase, IValueResolver
	{
		public override void OnBeforeExport(GltfExportContext context)
		{
			base.OnBeforeExport(context);
			context.RegisterValueResolver(this);
		}

		public bool TryGetValue(IExportContext ctx, object instance, MemberInfo member, ref object value)
		{
			if (instance is SignalReceiver receiver)
			{
				switch (member.Name)
				{
					case "m_Events":
						var eventsList = new List<SignalReceiverModel>();
						value = eventsList;
						for (var i = 0; i < receiver.Count(); i++)
						{
							var sig = receiver.GetSignalAssetAtIndex(i);
							var evt = receiver.GetReaction(sig);
							var model = new SignalReceiverModel();
							model.signal = new SignalAssetModel(sig);
							eventsList.Add(model);
							if (evt.TryFindCalls(out var calls))
							{
								var reaction = new SignalReactionModel();
								model.reaction = reaction;
								reaction.calls = new List<SignalCall>();
								foreach (var call in calls)
								{
									var callModel = new SignalCall();
									callModel.target = call.Target.GetId();
									callModel.method = call.MethodName;
									callModel.argument = call.Argument;
									reaction.calls.Add(callModel);
								}
							}
						}
						return true;
				}
			}
			return false;
		}
	}
}