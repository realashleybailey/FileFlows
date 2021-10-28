namespace ViWatcher.Plugins.Attributes
{
    using System;
    using System.Collections.Generic;

    public class TextAttribute : FormInputAttribute
    {
        public TextAttribute(int order) : base(FormInputType.Text, order) { }
    }
}