namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.JSInterop;
    using System.Threading.Tasks;

    public partial class InputNumber : Input<int>
    {
        public override bool Focus() => FocusUid();

        private int _Min = 0, _Max = int.MaxValue;

        [Parameter]
        public int Min { get => _Min; set => _Min = value; }
        [Parameter]
        public int Max { get => _Max; set => _Max = value == 0 ? int.MaxValue : value; }

        private async Task ChangeValue(ChangeEventArgs e)
        {
            if (double.TryParse(e.Value?.ToString() ?? "", out double value))
            {
                if (value > int.MaxValue)
                    value = int.MaxValue;
                else if (value < int.MinValue)
                    value = int.MinValue;
                this.Value = (int)value;
                if (this.Value > Max)
                {
                    this.Value = Max;
                    await UpdateInputValue();
                }
                else if (this.Value < Min)
                {
                    this.Value = Min;
                    await UpdateInputValue();
                }
            }
            else
            {
                await UpdateInputValue();
            }
            this.ClearError();
        }

        async Task UpdateInputValue()
        {
            await jsRuntime.InvokeVoidAsync("eval", new object[] { $"document.getElementById('{Uid}').value = " + this.Value });
        }
        private async Task OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Code == "Enter")
                await OnSubmit.InvokeAsync();
            else if (e.Code == "Escape")
                await OnClose.InvokeAsync();
        }
    }
}