using UnityEngine.Playables;

namespace Needle.Engine.Timeline
{
	public class PlayableDirectorAdditionalData : AdditionalComponentData<PlayableDirector>
	{
		public bool waitForAudio = true;
	}
}