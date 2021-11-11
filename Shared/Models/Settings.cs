namespace FileFlow.Shared.Models
{
    using System.ComponentModel.DataAnnotations;
    using FileFlow.Plugin.Attributes;

    public class Settings : ViObject
    {
        [Folder(1)]
        [Required]
        public string TempPath { get; set; }
    }

}