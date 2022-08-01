namespace FileFlows.Shared.Models;

using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json.Serialization;
using FileFlows.Plugin;

/// <summary>
/// An element field is a UI component that is displayed in the web browser
/// </summary>
public class ElementField
{
    /// <summary>
    /// Gets or sets the order of which to display this filed
    /// </summary>
    public int Order { get; set; }
    /// <summary>
    /// Gets or sets the type of this field 
    /// </summary>
    public string Type { get; set; }
    /// <summary>
    /// Gets or sets the name of this field
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets an optional label that should be used
    /// If set, this field won't be translated
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets optional help text to show
    /// If set, then translated help will not be looked for
    /// </summary>
    public string HelpText { get; set; }

    /// <summary>
    /// Gets or sets optional place holder text, this can be a translation key
    /// </summary>
    public string Placeholder { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the element, if this is set, this will be used instead of the HelpHint
    /// Note: this is used by the Script which the user defines the description for
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the input type of this field
    /// </summary>
    public FormInputType InputType { get; set; }

    /// <summary>
    /// Gets or sets if this field is only only a UI field
    /// and value will not be saved
    /// </summary>
    public bool UiOnly { get; set; }

    /// <summary>
    /// Gets or sets the variables {} available to this field
    /// </summary>
    public Dictionary<string, object> Variables { get; set; }

    /// <summary>
    /// Gets or sets the parameters of the field
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; }

    /// <summary>
    /// Gets a list of change values for this field
    /// </summary>
    public List<ChangeValue> ChangeValues { get; set; }

    /// <summary>
    /// Gets or sets the validators for the field
    /// </summary>
    public List<Validators.Validator> Validators { get; set; }

    /// <summary>
    /// A delegate used when a value change event
    /// </summary>
    public delegate void ValueChangedEvent(object sender, object value);
    /// <summary>
    /// A event that is raised when the value changes
    /// </summary>
    public event ValueChangedEvent ValueChanged;

    /// <summary>
    /// A delegate used for the disabled change event
    /// </summary>
    public delegate void DisabledChangeEvent(bool state);
    /// <summary>
    /// An event that is raised when the disable state of the field is changed
    /// </summary>
    public event DisabledChangeEvent DisabledChange;

    /// <summary>
    /// A delegate for when the conditions of the field changes
    /// </summary>
    public delegate void ConditionsChangeEvent(bool state);
    /// <summary>
    /// An event that is raised when the conditions of the field changes
    /// </summary>
    public event ConditionsChangeEvent ConditionsChange;

    /// <summary>
    /// Invokes the value changed event
    /// </summary>
    /// <param name="sender">The sender of the invoker</param>
    /// <param name="value">The value to invoke</param>
    public void InvokeValueChanged(object sender, object value) => this.ValueChanged?.Invoke(sender, value);

    private List<Condition> _DisabledConditions;
    /// <summary>
    /// Gets or sets the conditions used to disable this field
    /// </summary>
    public List<Condition> DisabledConditions
    {
        get => _DisabledConditions;
        set
        {
            _DisabledConditions = value ?? new List<Condition>();
            foreach (var condition in _DisabledConditions)
                condition.Owner = this;
        }
    }

    private List<Condition> _Conditions;
    /// <summary>
    /// Gets or sets the conditions used to show this field
    /// </summary>
    public List<Condition> Conditions
    {
        get => _Conditions;
        set
        {
            _Conditions = value ?? new List<Condition>();
            foreach (var condition in _Conditions)
                condition.Owner = this;
        }
    }

    /// <summary>
    /// Invokes a condition
    /// </summary>
    /// <param name="condition">the condition to invokte</param>
    /// <param name="state">the condition state</param>
    internal void InvokeChange(Condition condition, bool state)
    {
        if(this.DisabledConditions?.Any(x => x == condition) == true)
            this.DisabledChange?.Invoke(state);
        if (this.Conditions?.Any(x => x == condition) == true)
        {
            this.ConditionsChange?.Invoke(state == false); // state is the "disabled" state, for conditions we want the inverse
        }
    }

    /// <summary>
    /// Tests if all the conditions on this field match
    /// </summary>
    /// <returns>if all the conditions on this field match</returns>
    public bool ConditionsAllMatch()
    {
        if (this.Conditions?.Any() != true)
            return true;
        bool matches = true;
        foreach (var condition in this.Conditions)
        {
            if (condition.IsMatch == false)
            {
                matches = false;
            }
        }

        return matches;
    }
}

/// <summary>
/// A condition that determines if a element field is shown or disabled
/// </summary>
public class Condition
{
    /// <summary>
    /// Get or sets the Field this condition is attached to
    /// </summary>
    [JsonIgnore]
    public ElementField Field { get; private set; }
    /// <summary>
    /// Gets or sets the property this condition evaluates
    /// </summary>
    public string Property { get; set; }
    /// <summary>
    /// Gets or sets the value used to when evaluating the condition
    /// </summary>
    public object Value { get; set; }
    /// <summary>
    /// Gets or sets if the match is inversed, ie is not the value
    /// </summary>
    public bool IsNot { get; set; }

    /// <summary>
    /// Gets or sets if this condition is a match
    /// </summary>
    public bool IsMatch { get; set; }

    /// <summary>
    /// Gets or sets the owner who owns this condition
    /// </summary>
    [JsonIgnore]
    public ElementField Owner { get; set; }

    /// <summary>
    /// Constructs a condition
    /// </summary>
    public Condition()
    {

    }

    /// <summary>
    /// Constructs a condition
    /// </summary>
    /// <param name="field">the field the condition is attached to</param>
    /// <param name="initialValue">the initial value of the field</param>
    /// <param name="value">the value to evaluate for</param>
    /// <param name="isNot">if the condition should NOT match the value</param>
    public Condition(ElementField field, object initialValue, object value = null, bool isNot = false)
    {
        this.Property = field.Name;
        this.Value = value;
        this.IsNot = isNot;
        this.SetField(field, initialValue);
    }

    /// <summary>
    /// Sets the field 
    /// </summary>
    /// <param name="field">the field</param>
    /// <param name="initialValue">the fields initial value</param>
    public void SetField(ElementField field, object initialValue)
    {
        this.Field = field;
        this.Field.ValueChanged += Field_ValueChanged;
        Field_ValueChanged(this, initialValue);
    }

    /// <summary>
    /// Fired when the field value changes
    /// </summary>
    /// <param name="sender">the sender object</param>
    /// <param name="value">the new field value</param>
    private void Field_ValueChanged(object sender, object value)
    {
        bool matches = this.Matches(value);
        matches = !matches; // reverse this as we matches mean enabled, so we want disabled
        this.IsMatch = matches;
        this.Owner?.InvokeChange(this, matches);
    }

    /// <summary>
    /// Test if the condition matches the given object value
    /// </summary>
    /// <param name="value">the value to test the condition against</param>
    /// <returns>true if the condition is matches</returns>
    public virtual bool Matches(object value)
    {
        bool matches = Helpers.ObjectHelper.ObjectsAreSame(value, this.Value);
        if (IsNot)
            matches = !matches;
        return matches;
    }
}

/// <summary>
/// Condition to test if a field is empty
/// </summary>
public class EmptyCondition : Condition
{
    /// <summary>
    /// Constructs a empty condition
    /// </summary>
    /// <param name="field">the field this condition is attached to</param>
    /// <param name="initialValue">the initial value of the field</param>
    public EmptyCondition(ElementField field, object initialValue) : base(field, initialValue)
    {

    }

    /// <summary>
    /// Test if the condition matches the given object value
    /// </summary>
    /// <param name="value">the value to test the condition against</param>
    /// <returns>true if the condition is matches</returns>
    public override bool Matches(object value)
    {
        if (value == null)
        {
            return IsNot ? false : true;
        }
        else if (value is string str)
        {
            bool empty = string.IsNullOrWhiteSpace(str);
            if (IsNot)
                empty = !empty;
            return empty;
        }
        else if (value is IList list)
        {
            bool empty = list.Count == 0;
            if (IsNot)
                empty = !empty;
            return empty;
        }
        else if (value.GetType().IsArray)
        {
            bool empty = ((Array)value).Length == 0;
            if (IsNot)
                empty = !empty;
            return empty;
        }
        else if (value is int iValue)
        {
            return IsNot ? iValue > 0 : iValue == 0;
        }
        else if (value is Int64 iValue64)
        {
            return IsNot ? iValue64 > 0 : iValue64 == 0;
        }
        else if (value is bool bValue)
        {
            if (IsNot)
                bValue = !bValue;
            return bValue;
        }

        return base.Matches(value);
    }
}




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
