namespace Needle.Engine.Gltf
{
	public readonly struct NEEDLE_progressive_texture_settings
	{
		public readonly bool generateLods;
		public readonly string guid;
		public readonly int maxSize;

		public NEEDLE_progressive_texture_settings(string guid, int maxSize, bool generateLods = true)
		{
			this.generateLods = generateLods;
			this.maxSize = maxSize;
			this.guid = guid;
		}
	}
}