namespace FileFlows.Plugin.Attributes;

/// <summary>
/// A Input Number GUI element that will allow for floats to be entered
/// </summary>
public class NumberFloatAttribute : FormInputAttribute
{
    public NumberFloatAttribute(int order) : base(FormInputType.Float, order) { }
}
