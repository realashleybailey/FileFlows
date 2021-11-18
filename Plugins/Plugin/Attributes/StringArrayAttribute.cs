namespace FileFlows.Plugin.Attributes
{
    public class StringArrayAttribute : FormInputAttribute
    {
        public StringArrayAttribute(int order) : base(FormInputType.StringArray, order) { }
    }
}