namespace ViWatcher.Shared.Attributes
{
    using System;
    using System.Collections.Generic;

    public class TextAreaAttribute : FormInputAttribute
    {
        public TextAreaAttribute(int order) : base(FormInputType.TextArea, order) { }
    }
}