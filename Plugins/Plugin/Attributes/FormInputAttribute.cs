namespace FileFlow.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;
    using FileFlow.Plugin;

    public class FormInputAttribute : Attribute
    {
        public FormInputType InputType { get; set; }

        public int Order { get; set; }

        public FormInputAttribute(FormInputType type, int order)
        {
            this.InputType = type;
            this.Order = order;
        }
    }
}