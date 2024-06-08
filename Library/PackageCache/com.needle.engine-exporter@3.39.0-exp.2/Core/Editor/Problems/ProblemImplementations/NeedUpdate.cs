namespace Needle.Engine.Problems
{
	public class NeedUpdate : Problem
	{
		public NeedUpdate(string message, string id, string directory, string packageName)
			: base(message, id, new NpmUpdate(directory, packageName))
		{
		}
	}
}