namespace ViWatcher.Shared.Models
{
    using System;
    public class ViObject
    {
        public Guid Uid { get; set; }
        public string Name { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}