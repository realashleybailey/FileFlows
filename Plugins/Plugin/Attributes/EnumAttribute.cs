namespace FileFlows.Plugin.Attributes
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
                Options.Add(new ListOption { Value = v, Label = $"objects.{enumType.Name}.{v}" });
            }
        }
        public EnumAttribute(int order, object value) : base(FormInputType.Select, order)
        {
            Options = new List<ListOption>();
            InitOptions(new object[] { value });
        }
        public EnumAttribute(int order, object value, object value2) : base(FormInputType.Select, order)
        {
            Options = new List<ListOption>();
            InitOptions(new object[] { value, value2 });
        }
        public EnumAttribute(int order, object value, object value2, object value3) : base(FormInputType.Select, order)
        {
            Options = new List<ListOption>();
            InitOptions(new object[] { value, value2, value3 });
        }
        public EnumAttribute(int order, object value, object value2, object value3, object value4) : base(FormInputType.Select, order)
        {
            Options = new List<ListOption>();
            InitOptions(new object[] { value, value2, value3, value4 });
        }
        public EnumAttribute(int order, object value, object value2, object value3, object value4, object value5) : base(FormInputType.Select, order)
        {
            Options = new List<ListOption>();
            InitOptions(new object[] { value, value2, value3, value4, value5 });
        }
        public EnumAttribute(int order, object value, object value2, object value3, object value4, object value5, object value6) : base(FormInputType.Select, order)
        {
            Options = new List<ListOption>();
            InitOptions(new object[] { value, value2, value3, value4, value5, value6 });
        }
        public EnumAttribute(int order, object value, object value2, object value3, object value4, object value5, object value6, object value7) : base(FormInputType.Select, order)
        {
            Options = new List<ListOption>();
            InitOptions(new object[] { value, value2, value3, value4, value5, value6, value7 });
        }
        public EnumAttribute(int order, object value, object value2, object value3, object value4, object value5, object value6, object value7, object value8) : base(FormInputType.Select, order)
        {
            Options = new List<ListOption>();
            InitOptions(new object[] { value, value2, value3, value4, value5, value6, value7, value8 });
        }
        public EnumAttribute(int order, object value, object value2, object value3, object value4, object value5, object value6, object value7, object value8, object value9) : base(FormInputType.Select, order)
        {
            Options = new List<ListOption>();
            InitOptions(new object[] { value, value2, value3, value4, value5, value6, value7, value8, value9 });
        }
        public EnumAttribute(int order, object value, object value2, object value3, object value4, object value5, object value6, object value7, object value8, object value9, object value10) : base(FormInputType.Select, order)
        {
            Options = new List<ListOption>();
            InitOptions(new object[] { value, value2, value3, value4, value5, value6, value7, value8, value9, value10 });
        }


        private void InitOptions(object[] values)
        {
            foreach (var v in values)
            {
                Options.Add(new ListOption { Value = v, Label = $"Enums.{v.GetType().Name}.{v}" });
            }

        }
    }
}