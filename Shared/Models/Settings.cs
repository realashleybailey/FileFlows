namespace FileFlow.Shared.Models
{
    using System.ComponentModel.DataAnnotations;
    using FileFlow.Plugin.Attributes;

    public class Settings : ViObject
    {
        [Folder(1)]
        [Required]
        public string TempPath { get; set; }

        [Folder(1)]
        [Required]
        public string LoggingPath { get; set; }

        [NumberInt(3)]
        public int Workers { get; set; }

        [Boolean(4)]
        public bool WorkerScanner { get; set; }
        [Boolean(5)]
        public bool WorkerFlowExecutor { get; set; }

        public string GetLogFile(System.Guid uid)
        {
            if (string.IsNullOrEmpty(LoggingPath))
                return string.Empty;
            return System.IO.Path.Combine(LoggingPath, uid + ".log");
        }
    }

}