namespace FileFlows.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;

    public class ChecklistAttribute : FormInputAttribute
    {
        public string OptionsProperty { get; set; }
        public ChecklistAttribute(string optionsProperty, int order) : base(FormInputType.Checklist, order)
        { 
            this.OptionsProperty = optionsProperty;
        }
    }
}