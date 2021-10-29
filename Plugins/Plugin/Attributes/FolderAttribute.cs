namespace FileFlow.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;

    public class FolderAttribute : FormInputAttribute
    {
        public FolderAttribute(int order) : base(FormInputType.Folder, order) { }
    }
}