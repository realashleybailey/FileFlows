namespace FileFlows.Shared.Models
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Text.Json.Serialization;
    using FileFlows.Plugin;
    public class ElementField
    {
        public int Order { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets optional place holder text, this can be a translation key
        /// </summary>
        public string Placeholder { get; set; }
        public FormInputType InputType { get; set; }

        public bool UiOnly { get; set; }

        public Dictionary<string, object> Variables { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        public List<Validators.Validator> Validators { get; set; }

        public delegate void ValueChangedEvent(object sender, object value);
        public event ValueChangedEvent ValueChanged;

        public delegate void DisabledChangeEvent(bool state);
        public event DisabledChangeEvent DisabledChange;

        public delegate void ConditionsChangeEvent(bool state);
        public event ConditionsChangeEvent ConditionsChange;

        public void InvokeValueChanged(object sender, object value) => this.ValueChanged?.Invoke(sender, value);

        private List<Condition> _DisabledConditions;
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

        internal void InvokeChange(Condition condition, bool state)
        {
            if(this.DisabledConditions?.Any(x => x == condition) == true)
                this.DisabledChange?.Invoke(state);
            if (this.Conditions?.Any(x => x == condition) == true)
                this.ConditionsChange?.Invoke(state == false); // state is the "disabled" state, for conditions we want the inverse
        }
    }

    public class Condition
    {
        [JsonIgnore]
        public ElementField Field { get; private set; }
        public string Property { get; set; }
        public object Value { get; set; }
        public bool IsNot { get; set; }

        public bool IsMatch { get; set; }

        [JsonIgnore]
        public ElementField Owner { get; set; }

        public Condition()
        {

        }

        public Condition(ElementField field, object initialValue)
        {
            this.Property = field.Name;
            this.SetField(field, initialValue);
        }

        public void SetField(ElementField field, object initialValue)
        {
            this.Field = field;
            this.Field.ValueChanged += Field_ValueChanged;
            Field_ValueChanged(this, initialValue);
        }

        private void Field_ValueChanged(object sender, object value)
        {
            bool matches = this.Matches(value);
            matches = !matches; // reverse this as we matches mean enabled, so we want disabled
            this.IsMatch = matches;
            this.Owner?.InvokeChange(this, matches);
        }

        public virtual bool Matches(object value)
        {
            bool matches = Helpers.ObjectHelper.ObjectsAreSame(value, this.Value);
            if (IsNot)
                matches = !matches;
            return matches;
        }
    }

    public class EmptyCondition:Condition
    {
        public EmptyCondition(ElementField field, object initialValue):base(field, initialValue)
        {

        }

        public override bool Matches(object value)
        {
            if(value == null)
            {
                return IsNot ? false : true;
            }
            else if(value is string str)
            {
                bool empty = string.IsNullOrWhiteSpace(str);
                if(IsNot)
                    empty = !empty;
                return empty;
            }
            else if(value is IList list)
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
            else if(value is int iValue)
            {
                return IsNot ? iValue > 0 : iValue == 0;
            }
            else if (value is Int64 iValue64)
            {
                return IsNot ? iValue64 > 0 : iValue64 == 0;
            }
            else if (value is bool bValue)
            {
                if(IsNot)
                    bValue = !bValue;
                return bValue;
            }
            return base.Matches(value);
        }
    }

}