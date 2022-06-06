namespace FileFlows.Plugin.Attributes;

/// <summary>
/// A Input Number GUI element that will allow for integers to be entered
/// </summary>
public class NumberIntAttribute : FormInputAttribute
{
    public NumberIntAttribute(int order) : base(FormInputType.Int, order) { }
}
