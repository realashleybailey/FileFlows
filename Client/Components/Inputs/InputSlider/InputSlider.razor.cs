namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using System.Threading.Tasks;

    public partial class InputSlider : Input<int>
    {
        public override bool Focus() => FocusUid();

        private int _Min = 0, _Max = int.MaxValue;

        [Parameter]
        public int Min { get => _Min; set => _Min = value; }
        [Parameter]
        public int Max { get => _Max; set => _Max = value == 0 ? int.MaxValue : value; }

    }
}