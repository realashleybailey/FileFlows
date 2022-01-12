namespace FileFlows.Plugin
{
    public interface IPlugin
    {
        string Name { get; }

        string MinimumVersion { get; }

        void Init();
    }

    public interface IPluginSettings
    {

    }
}