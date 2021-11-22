namespace FileFlows.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;

    public class RegularExpressionAttribute : FormInputAttribute
    {
        public RegularExpressionAttribute(int order) : base(FormInputType.RegularExpression, order) { }
    }
}