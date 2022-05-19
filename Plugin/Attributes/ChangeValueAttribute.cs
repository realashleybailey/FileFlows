using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Updates a value when this value matches
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ChangeValueAttribute:Attribute
{
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
    /// Constructs a change value
    /// </summary>
    /// <param name="property">the property to change</param>
    /// <param name="value">the value to set in the property</param>
    /// <param name="whenValue">when to change the value, when this value equals a value</param>
    /// <param name="whenValueIsNot">the value should be set when this value does not equal the WhenValue</param>
    public ChangeValueAttribute(string property, string value, int whenValue, bool whenValueIsNot = false)
    {
        this.Property = property;
        this.Value = value;
        this.WhenValue = whenValue;
        this.WhenValueIsNot = whenValueIsNot; 
    }
    /// <summary>
    /// Constructs a change value
    /// </summary>
    /// <param name="property">the property to change</param>
    /// <param name="value">the value to set in the property</param>
    /// <param name="whenValue">when to change the value, when this value equals a value</param>
    /// <param name="whenValueIsNot">the value should be set when this value does not equal the WhenValue</param>
    public ChangeValueAttribute(string property, int value, int whenValue, bool whenValueIsNot = false)
    {
        this.Property = property;
        this.Value = value;
        this.WhenValue = whenValue;
        this.WhenValueIsNot = whenValueIsNot; 
    }
    /// <summary>
    /// Constructs a change value
    /// </summary>
    /// <param name="property">the property to change</param>
    /// <param name="value">the value to set in the property</param>
    /// <param name="whenValue">when to change the value, when this value equals a value</param>
    /// <param name="whenValueIsNot">the value should be set when this value does not equal the WhenValue</param>
    public ChangeValueAttribute(string property, bool value, int whenValue, bool whenValueIsNot = false)
    {
        this.Property = property;
        this.Value = value;
        this.WhenValue = whenValue;
        this.WhenValueIsNot = whenValueIsNot; 
    }
    /// <summary>
    /// Constructs a change value
    /// </summary>
    /// <param name="property">the property to change</param>
    /// <param name="value">the value to set in the property</param>
    /// <param name="whenValue">when to change the value, when this value equals a value</param>
    /// <param name="whenValueIsNot">the value should be set when this value does not equal the WhenValue</param>
    public ChangeValueAttribute(string property, string value, string whenValue, bool whenValueIsNot = false)
    {
        this.Property = property;
        this.Value = value;
        this.WhenValue = whenValue;
        this.WhenValueIsNot = whenValueIsNot; 
    }
    /// <summary>
    /// Constructs a change value
    /// </summary>
    /// <param name="property">the property to change</param>
    /// <param name="value">the value to set in the property</param>
    /// <param name="whenValue">when to change the value, when this value equals a value</param>
    /// <param name="whenValueIsNot">the value should be set when this value does not equal the WhenValue</param>
    public ChangeValueAttribute(string property, int value, string whenValue, bool whenValueIsNot = false)
    {
        this.Property = property;
        this.Value = value;
        this.WhenValue = whenValue;
        this.WhenValueIsNot = whenValueIsNot; 
    }
    /// <summary>
    /// Constructs a change value
    /// </summary>
    /// <param name="property">the property to change</param>
    /// <param name="value">the value to set in the property</param>
    /// <param name="whenValue">when to change the value, when this value equals a value</param>
    /// <param name="whenValueIsNot">the value should be set when this value does not equal the WhenValue</param>
    public ChangeValueAttribute(string property, bool value, string whenValue, bool whenValueIsNot = false)
    {
        this.Property = property;
        this.Value = value;
        this.WhenValue = whenValue;
        this.WhenValueIsNot = whenValueIsNot; 
    }
}
