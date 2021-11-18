namespace FileFlows.Plugin
{
    public interface IPlugin
    {
        string Name { get; }

        void Init();
    }
}