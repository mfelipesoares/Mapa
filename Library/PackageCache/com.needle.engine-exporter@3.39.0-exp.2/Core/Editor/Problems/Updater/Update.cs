using System;

namespace Needle.Engine.Problems
{
	public abstract class Update
	{
		/// <summary>
		/// The date when this update was introduced - this defines the order in which updates are applied
		/// </summary>
		public abstract DateTime UpgradeDate { get; }
		
		/// <summary>
		/// Apply the update to the project, returns true when the update was applied successfully or nothing has to be done
		/// </summary>
		public abstract bool Apply(string fullProjectPath, CodeUpdateHelper codeUpdateHelper);

		public virtual bool RunOnce => true;
		public virtual bool DidRun { get; internal set; }
	}
}