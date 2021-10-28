namespace FileFlow.Shared.Models
{
    public class Settings : ViObject
    {
        public string Container { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }

        public bool TestMode { get; set; }

        public string[] Extensions { get; set; }

        public string HandBrakeCli { get; set; }
        public string FFProbeExe { get; set; }
    }

}