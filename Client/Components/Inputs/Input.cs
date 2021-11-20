namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;
    using FileFlows.Shared;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.JSInterop;

    public interface IInput
    {
        string Label { get; set; }
        string Help { get; set; }
        string Placeholder { get; set; }
        string ErrorMessage { get; set; }

        Task<bool> Validate();

        bool Focus();
    }

    public abstract class Input<T> : ComponentBase, IInput
    {
        [CascadingParameter] protected Editor Editor { get; set; }

        [Inject] protected IJSRuntime jsRuntime { get; set; }
        protected string Uid = System.Guid.NewGuid().ToString();
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
        public string _Placeholder;

        [Parameter]
        public string Placeholder
        {
            get => _Placeholder;
            set { _Placeholder = value ?? ""; }
        }


        [Parameter] public List<FileFlows.Shared.Validators.Validator> Validators { get; set; }

        public string ErrorMessage { get; set; } = "";

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
                ErrorMessage = ""; // clear the error
                _Value = value;
                ValueChanged.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<T> ValueChanged { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Editor.RegisterInput(this);
        }
        protected void ClearError() => this.ErrorMessage = "";

        public virtual async Task<bool> Validate()
        {
            if (this.Validators?.Any() != true)
                return true;
            Logger.Instance.DLog("validating actual input: " + this.Label);
            foreach (var val in this.Validators)
            {
                if (await val.Validate(this.Value) == false)
                {
                    ErrorMessage = Translater.Instant($"Validators.{val.Type}", val);
                    return false;
                }
            }
            return true;
        }

        public virtual bool Focus() => false;

        protected bool FocusUid()
        {
            _ = jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{Uid}').focus()");
            return true;
        }
    }
}