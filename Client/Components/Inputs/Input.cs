namespace FileFlow.Client.Components.Inputs 
{
    using Microsoft.AspNetCore.Components;
    using FileFlow.Shared;

    public interface IInput
    {
        string Label { get; set; }
        string Help { get; set; }
        string Placeholder { get; set; }
    }

    public abstract class Input<T> : ComponentBase, IInput
    {
        private string _Label;
        private string _LabelOriginal;
        [Parameter]
        public string Label
        {
            get => _Label;
            set
            {
                if (_LabelOriginal == value)
                    return;
                _LabelOriginal = value;
                if (Translater.NeedsTranslating(_LabelOriginal))
                {
                    _Label = Translater.Instant(_LabelOriginal);
                    Help = Translater.Instant(_LabelOriginal + "-Help");
                    Placeholder = Translater.Instant(_LabelOriginal + "-Placeholder").EmptyAsNull() ?? _Label;
                }
                else
                {
                    _Label = value;
                }
            }
        }

        public string Help { get; set; }
        public string Placeholder { get; set; }


        private T _Value;

        [Parameter]
        public T Value
        {
            get => _Value;
            set
            {
                if (_Value == null && value == null)
                    return;
                if (_Value != null && value != null && _Value.Equals(value)) return;
                _Value = value;
                ValueChanged.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<T> ValueChanged { get; set; }
    }
}