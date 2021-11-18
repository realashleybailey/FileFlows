namespace FileFlows.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;

    public class CodeAttribute : FormInputAttribute
    {
        public CodeAttribute(int order) : base(FormInputType.Code, order) { }
    }
}