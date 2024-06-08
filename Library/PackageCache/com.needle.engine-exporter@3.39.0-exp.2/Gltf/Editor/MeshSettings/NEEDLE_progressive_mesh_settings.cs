namespace Needle.Engine.Gltf
{
	public readonly struct NEEDLE_progressive_mesh_settings
	{
		/// <summary>
		/// the guid of the mesh
		/// </summary>
		public readonly string guid;
		/// <summary>
		/// Will generate lods when enabled
		/// </summary>
		public readonly bool generateLods;

		public NEEDLE_progressive_mesh_settings(string guid, bool generateLods)
		{
			this.guid = guid;
			this.generateLods = generateLods;
		}
	}
}