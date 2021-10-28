namespace FileFlow.BasicNodes
{
    using System.ComponentModel.DataAnnotations;
    using FileFlow.Plugins.Attributes;

    public class Plugin : FileFlow.Plugins.Plugin
    {
        public override string Name => "Basic Nodes";

        [Text(1)]
        [Required]
        public string HandBrakeCli { get; set; }

        [Text(2)]
        [Required]
        public string FFProbeExe { get; set; }
    }
}