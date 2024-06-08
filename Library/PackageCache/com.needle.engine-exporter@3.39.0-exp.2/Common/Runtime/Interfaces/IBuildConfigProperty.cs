namespace Needle.Engine
{
    public interface IBuildConfigProperty
    {
        string Key { get; }
        object GetValue(string projectDirectory);
    }
}