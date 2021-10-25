namespace ViWatcher.Shared.Models
{
    public class VideoFile:ViObject
    {
        public string ShortName { get; set; }
        public string Path { get; set; }
        public string Codec { get; set; }
        public string Audio { get; set; }
        public string Extension { get; set; }
    }
}