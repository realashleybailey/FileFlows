namespace FileFlows.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;
    using FileFlows.Plugin;

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