namespace ViWatcher.Shared.Attributes
{
    using System;
    using System.Collections.Generic;

    public class FormInputAttribute:Attribute
    {
        public FormInputType InputType{ get; set; }

        public FormInputAttribute(FormInputType type){
            this.InputType = type;
        }
    }
}