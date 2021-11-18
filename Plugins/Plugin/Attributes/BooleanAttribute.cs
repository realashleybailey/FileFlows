namespace FileFlows.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;

    public class BooleanAttribute : FormInputAttribute
    {
        public BooleanAttribute(int order) : base(FormInputType.Switch, order) { }
    }
}