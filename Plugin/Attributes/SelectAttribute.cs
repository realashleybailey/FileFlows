namespace FileFlows.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;

    public class SelectAttribute : FormInputAttribute
    {
        public string OptionsProperty { get; set; }
        public SelectAttribute(string optionsProperty, int order) : base(FormInputType.Select, order)
        { 
            this.OptionsProperty = optionsProperty;
        }
    }
}