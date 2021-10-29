namespace FileFlow.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;

    public class NumberIntAttribute : FormInputAttribute
    {
        public NumberIntAttribute(int order) : base(FormInputType.Int, order) { }
    }
}