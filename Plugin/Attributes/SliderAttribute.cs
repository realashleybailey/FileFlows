namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Attribute to add a slider to a form
/// </summary>
public class SliderAttribute : FormInputAttribute
{
    /// <summary>
    /// Constructs a slider for a form
    /// </summary>
    /// <param name="order">the order in the UI the input will appear</param>
    public SliderAttribute(int order) : base(FormInputType.Slider, order) { }
}