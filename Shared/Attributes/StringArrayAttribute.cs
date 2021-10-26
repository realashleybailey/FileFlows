namespace ViWatcher.Shared.Attributes
{
    public class StringArrayAttribute : FormInputAttribute
    {
        public StringArrayAttribute(int order) : base(FormInputType.StringArray, order) { }
    }
}