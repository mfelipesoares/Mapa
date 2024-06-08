namespace Needle.Engine
{
    /// <summary>
    /// MonoBehaviours implementing this interface can add additional characters to generated font atlases.
    /// This ensures that they are available to be displayed in UI components at runtime.
    /// </summary>
    public interface IAdditionalFontCharacters
    {
        public string GetAdditionalCharacters();
    }
}