namespace FileFlows.Shared.Models
{
    using System;
    using System.Text.Json.Serialization;

    public class FileFlowObject: IUniqueObject
    {
        public Guid Uid { get; set; }
        public string Name { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }

    public interface IUniqueObject
    {
        Guid Uid { get; set; }
    }
}