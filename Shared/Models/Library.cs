namespace FileFlow.Shared.Models
{
    using System;

    public class Library : ViObject
    {
        public bool Enabled { get; set; }
        public string Path { get; set; }
        public string Filter { get; set; }
        public Guid Flow { get; set; }
    }
}