namespace FileFlows.Client.Components.Common
{
    using Microsoft.AspNetCore.Components;
    using System;
    using System.Linq.Expressions;

    public partial class FlowSwitch: ComponentBase
    {
        private bool _Value;
        [Parameter]
        public bool Value
        {
            get => _Value;
            set
            {
                if(_Value != value)
                {
                    _Value = value;
                    if(ValueChanged.HasDelegate)
                        ValueChanged.InvokeAsync(value);
                }
            }
        }

        [Parameter]
        public EventCallback<bool> ValueChanged{get;set; }

        [Parameter]
        public Expression<Func<bool>> ValueExpression { get; set; }

        private void OnChange(ChangeEventArgs args)
        {
            this.Value = args.Value as bool? == true;
        }
    }
}
