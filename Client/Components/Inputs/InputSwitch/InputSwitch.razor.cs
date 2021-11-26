namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;

    public partial class InputSwitch : Input<bool>
    {
        [Parameter]
        public bool Inverse { get; set; }

        public new bool Value
        {
            get
            {
                if (Inverse) return !base.Value;
                return base.Value;  
            }
            set
            {
                if (Inverse) base.Value = !value;
                else base.Value = value;
            }
        }
    }
}