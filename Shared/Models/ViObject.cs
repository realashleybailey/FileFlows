namespace FileFlows.Shared.Models
{
    using System;
    using System.Text.Json.Serialization;

    public class ViObject
    {
        public Guid Uid { get; set; }
        public string Name { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}