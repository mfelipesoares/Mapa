namespace Needle.Engine.EditorSync
{
	public static class MaterialPropertyNameConverter
	{
		public static void ToGltfName(ref string propertyName)
		{
			switch (propertyName)
			{
				case "_Color":
				case "_BaseColor":
					propertyName = "baseColorFactor";
					break;
				case "_BaseMap":
				case "_MainTex":
					propertyName = "baseColorTexture";
					break;

				case "_Metallic":
					propertyName = "metallicFactor";
					break;
				case "_Smoothness":
				case "_Glossiness":
				case "_GlossMapScale":
					propertyName = "roughnessFactor";
					break;
				case "_GlossMap":
					propertyName = "roughnessTexture";
					break;
				
				case "_BumpMap":
					propertyName = "normalTexture";
					break;
				case "_BumpScale":
					propertyName = "normalTextureScale";
					break;
				
				case "_EmissionColor":
					propertyName = "emissiveFactor";
					break;
			}
		}
	}
}