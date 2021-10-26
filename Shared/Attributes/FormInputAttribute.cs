namespace ViWatcher.Shared.Attributes
{
    using System;
    using System.Collections.Generic;

    public class FormInputAttribute:Attribute
    {
        public FormInputType InputType{ get; set; }

        public int Order{ get; set; }

        public FormInputAttribute(FormInputType type, int order){
            this.InputType = type;
            this.Order = order;
        }
    }
}