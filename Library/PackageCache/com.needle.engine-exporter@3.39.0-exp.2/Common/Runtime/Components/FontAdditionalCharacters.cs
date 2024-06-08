using UnityEngine;

namespace Needle.Engine
{
	public class FontAdditionalCharacters : MonoBehaviour, IAdditionalFontCharacters
	{
		[Info("Add characters that you want to add to every font character atlas"), Multiline(10)] 
		public string AdditionalCharacters;
		
		public string GetAdditionalCharacters() => AdditionalCharacters;
	}
}