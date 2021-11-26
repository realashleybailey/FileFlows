namespace FileFlows.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;

    public class KeyValueAttribute : FormInputAttribute
    {
        public KeyValueAttribute(int order) : base(FormInputType.KeyValue, order) { }
    }
}
