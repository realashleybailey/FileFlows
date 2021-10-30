namespace FileFlow.Plugin
{
    public interface IPlugin
    {
        string Name { get; }

        void Init();
    }
}