namespace FileFlow.BasicNodes
{
    using System.ComponentModel.DataAnnotations;

    public class Plugin : FileFlow.Plugin.IPlugin
    {
        public string Name => "Basic Nodes";
    }
}