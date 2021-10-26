namespace ViWatcher.Shared.Attributes
{
    using System;
    using System.Collections.Generic;

    public class TextAreaAttribute : FormInputAttribute
    {
        public TextAreaAttribute() : base(FormInputType.TextArea) { }
    }
}