namespace ViWatcher.Plugins.Attributes
{
    using System;
    using System.Collections.Generic;
    using ViWatcher.Plugins;

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