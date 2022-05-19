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
    /// <param name="inverse">if the range should be inversed with the maximum on the left and the minimum on the right</param>
    public SliderAttribute(int order, bool inverse = false) : base(FormInputType.Slider, order)
    {
        this.Inverse = inverse;
    }
    
    /// <summary>
    /// Gets or sets if the range should be inversed with the maximum on the left and the minimum on the right
    /// </summary>
    public bool Inverse { get; set; }
}