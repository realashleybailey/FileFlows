namespace FileFlows.Plugin
{
    using System;

    public class ObjectReference
    {
        public string Name { get; set; }
        public Guid Uid { get; set; }
        public string Type { get; set; }

        public override string ToString() => Name ?? string.Empty;
    }
}
