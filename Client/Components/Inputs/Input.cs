namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;
    using FileFlows.Shared;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.JSInterop;
    using System;

    public interface IInput
    {
        string Label { get; set; }
        string Help { get; set; }
        string Placeholder { get; set; }
        string ErrorMessage { get; set; }
        bool HideLabel { get; set; }

        FileFlows.Shared.Models.ElementField Field { get; set; }

        Task<bool> Validate();

        EventHandler<bool> ValidStateChanged { get; set; }

        bool Focus();
    }

    public abstract class Input<T> : ComponentBase, IInput
    {
        [CascadingParameter] protected Editor Editor { get; set; }

        [Inject] protected IJSRuntime jsRuntime { get; set; }
        protected string Uid = System.Guid.NewGuid().ToString();
        private string _Label;
        private string _LabelOriginal;
        public EventHandler<bool> ValidStateChanged { get; set; }

        public string Suffix { get; set; }
        public string Prefix { get; set; }

        protected string LabelOriginal => _LabelOriginal;

        [Parameter]
        public bool HideLabel { get; set; }

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
                    Suffix = Translater.Instant(_LabelOriginal + "-Suffix");
                    Prefix = Translater.Instant(_LabelOriginal + "-Prefix");
                    Placeholder = Translater.Instant(_LabelOriginal + "-Placeholder").EmptyAsNull() ?? _Label;
                }
                else
                {
                    _Label = value;
                }
            }
        }

        [Parameter]
        public bool ReadOnly { get; set; }
        [Parameter]
        public FileFlows.Shared.Models.ElementField Field { get; set; }

        public string Help { get; set; }
        public string _Placeholder;

        [Parameter]
        public string Placeholder
        {
            get => _Placeholder;
            set { _Placeholder = value ?? ""; }
        }


        [Parameter] public List<FileFlows.Shared.Validators.Validator> Validators { get; set; }


        private string _ErrorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _ErrorMessage;
            set
            {
                _ErrorMessage = value;
            }
        }

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

                bool areEqual = System.Text.Json.JsonSerializer.Serialize(_Value) == System.Text.Json.JsonSerializer.Serialize(value);
                if (areEqual == false) // for lists/arrays if they havent really changed, empty to empty, dont clear validation
                    ErrorMessage = ""; // clear the error

                _Value = value;
                ValueUpdated();
                ValueChanged.InvokeAsync(value);
                Field?.InvokeValueChanged(this.Editor, value);
            }
        }

        protected virtual void ValueUpdated() { }

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
            bool isValid = string.IsNullOrEmpty(ErrorMessage);
            Logger.Instance.DLog("validating actual input: " + this.Label);
            foreach (var val in this.Validators)
            {
                if (await val.Validate(this.Value) == false)
                {
                    ErrorMessage = Translater.Instant($"Validators.{val.Type}", val);
                    this.StateHasChanged();
                    if (isValid)
                        ValidStateChanged?.Invoke(this, false);
                    return false;
                }
            }
            if(isValid == false)
                ValidStateChanged?.Invoke(this, true);
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