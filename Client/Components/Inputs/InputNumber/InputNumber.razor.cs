namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using System.Threading.Tasks;

    public partial class InputNumber : Input<int>
    {
        public override bool Focus() => FocusUid();

        private int _Min = 0, _Max = 10_000;

        [Parameter]
        public int Min { get => _Min; set => _Min = value; }
        [Parameter]
        public int Max { get => _Max; set => _Max= value; }

        private async Task ChangeValue(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString() ?? "", out int value))
            {
                this.Value = value;
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
    }
}