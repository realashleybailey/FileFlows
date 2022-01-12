namespace FileFlows.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;

    public class PasswordAttribute : FormInputAttribute
    {
        public PasswordAttribute(int order) : base(FormInputType.Password, order) { }
    }
}