namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;
    using System.Collections.Generic;

    public partial class InputTextVariables : Input<string>
    {
        private Dictionary<string, object> _Variables = new Dictionary<string, object>();

        [Parameter]
        public Dictionary<string, object> Variables
        {
            get => _Variables;
            set { _Variables = value ?? new Dictionary<string, object>(); }
        }

        public new string Value
        {
            get => base.Value;
            set
            {
                if (base.Value == value)
                    return;

                base.Value = value ?? string.Empty;
                UpdatePreview();
            }
        }


        private string Preview = string.Empty;

        public override bool Focus() => FocusUid();
            
        protected override void OnInitialized()
        {
            base.OnInitialized();       
            UpdatePreview();
        }   

        private void UpdatePreview()
        {
            string preview = Plugin.VariablesHelper.ReplaceVariables(this.Value, Variables, false);
            this.Preview = preview;             
        }

        private void VariableOnSubmit()
        {
            _ = base.OnSubmit.InvokeAsync();
        }
    }
}