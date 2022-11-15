namespace FileFlows.Shared.Models;


/// <summary>
/// Changes a value when the conditions are met
/// </summary>
public class ChangeValue
{
    /// <summary>
    /// Get or sets the Field this condition is attached to
    /// </summary>
    [JsonIgnore]
    public ElementField Field { get; set; }
    
    /// <summary>
    /// Gets or sets the property to change
    /// </summary>
    public string Property { get; set; }
    
    /// <summary>
    /// Gets or sets the value to set in the property
    /// </summary>
    public object Value { get; set; }
    
    /// <summary>
    /// Gets or sets when to change the value, when this value equals a value
    /// </summary>
    public object WhenValue { get; set; }
    
    /// <summary>
    /// Gets or sets if the value should be set when this value does not equal the WhenValue
    /// </summary>
    public bool WhenValueIsNot { get; set; }

    /// <summary>
    /// Gets or sets the owner who owns this condition
    /// </summary>
    [JsonIgnore]
    public ElementField Owner { get; set; }

    /// <summary>
    /// Constructs a condition
    /// </summary>
    public ChangeValue()
    {

    }

    /// <summary>
    /// Constructs a change value
    /// </summary>
    /// <param name="property">the property to change</param>
    /// <param name="value">the value to set in the property</param>
    /// <param name="whenValue">when to change the value, when this value equals a value</param>
    /// <param name="whenValueIsNot">the value should be set when this value does not equal the WhenValue</param>
    public ChangeValue(string property, object value, object whenValue, bool whenValueIsNot = false)
    {
        this.Property = property;
        this.Value = value;
        this.WhenValue = whenValue;
        this.WhenValueIsNot = whenValueIsNot;
    }


    /// <summary>
    /// Test if the change value condition matches the given object value
    /// </summary>
    /// <param name="value">the value of the field</param>
    /// <returns>true if the condition is matches</returns>
    public virtual bool Matches(object value)
    {
        bool matches = Helpers.ObjectHelper.ObjectsAreSame(this.WhenValue, value);
        if (WhenValueIsNot)
            matches = !matches;
        return matches;
    }
}
