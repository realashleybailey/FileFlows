namespace ViWatcher.BasicNodes
{
    using System.ComponentModel.DataAnnotations;
    using ViWatcher.Plugins.Attributes;

    public class Plugin : ViWatcher.Plugins.Plugin
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