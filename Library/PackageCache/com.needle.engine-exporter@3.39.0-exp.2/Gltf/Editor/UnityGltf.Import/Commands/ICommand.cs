using System.Threading.Tasks;

namespace Needle.Engine.Gltf.UnityGltf.Import
{
	public interface ICommand
	{
		Task Execute();
	}
}