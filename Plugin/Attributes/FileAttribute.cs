namespace FileFlows.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;

    public class FileAttribute : FormInputAttribute
    {
        public string[] Extensions { get; set; }
        public FileAttribute(int order, params string[] extensions) : base(FormInputType.File, order)
        {
            this.Extensions = extensions;
        }
    }
}