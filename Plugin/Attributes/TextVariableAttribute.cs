namespace FileFlows.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;

    public class TextVariableAttribute : FormInputAttribute
    {
        public TextVariableAttribute(int order) : base(FormInputType.TextVariable, order) { }
    }
}