namespace ViWatcher.Shared.Attributes
{
    using System;
    using System.Collections.Generic;

    public class CodeAttribute : FormInputAttribute
    {
        public CodeAttribute() : base(FormInputType.Code) { }
    }
}