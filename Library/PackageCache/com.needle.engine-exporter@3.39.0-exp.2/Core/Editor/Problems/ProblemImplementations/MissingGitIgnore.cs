#nullable enable
namespace Needle.Engine.Problems
{
	public class MissingGitIgnore : Problem
	{
		public MissingGitIgnore(string message, string id, string gitignoreFilePath) : base(message, id, null)
		{
			Fix = new CreateGitIgnoreFix(gitignoreFilePath);
		}
	}
}