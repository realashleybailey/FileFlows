namespace FileFlow.Plugin.Attributes
{
    using System;
    using System.Collections.Generic;

    public class EnumAttribute : FormInputAttribute
    {
        public List<ListOption> Options { get; set; }

        public EnumAttribute(Type enumType, int order) : base(FormInputType.Select, order)
        {
            Options = new List<ListOption>();
            foreach (var v in Enum.GetValues(enumType))
            {
                Options.Add(new ListOption { Value = v, Label = $"Enums.{enumType.Name}.{v}" });
            }
        }
    }
}