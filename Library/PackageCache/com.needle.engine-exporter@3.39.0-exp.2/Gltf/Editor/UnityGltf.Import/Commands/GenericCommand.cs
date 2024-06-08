using System;
using System.Threading.Tasks;

namespace Needle.Engine.Gltf.UnityGltf.Import
{
	public class GenericCommand : ICommand
	{
		public Action Action;
		
		public GenericCommand(Action action)
		{
			Action = action;
		}
		
		public Task Execute()
		{
			Action?.Invoke();
			return Task.CompletedTask;
		}
	}
}