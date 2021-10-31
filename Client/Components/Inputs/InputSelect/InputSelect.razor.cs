namespace FileFlow.Client.Components.Inputs
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Components;
    using System.Linq;
    using FileFlow.Shared;
    using FileFlow.Shared.Models;
    using System.Text.Json;
    using System.Threading.Tasks;
    using FileFlow.Plugin;

    public partial class InputSelect : Input<object>
    {
        private readonly List<ListOption> _Options = new List<ListOption>();
        [Parameter]
        public IEnumerable<ListOption> Options
        {
            get => _Options;
            set
            {
                _Options.Clear();
                if (value == null)
                    return;
                foreach (var lo in value)
                {
                    if (Translater.NeedsTranslating(lo.Label))
                        lo.Label = Translater.Instant(lo.Label);
                    _Options.Add(lo);
                }
            }
        }

        public override bool Focus() => FocusUid();

        private bool _AllowClear = true;
        [Parameter]
        public bool AllowClear { get => _AllowClear; set { _AllowClear = value; } }

        private int _SelectedIndex = -1;
        public int SelectedIndex
        {
            get => _SelectedIndex;
            set
            {
                _SelectedIndex = value;
                if (value == -1)
                    this.Value = null;
                else
                    this.Value = Options.ToArray()[value].Value;
            }
        }

        private string lblSelectOne;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            lblSelectOne = Translater.Instant("Labels.SelectOne");
            int startIndex = -1;
            if (Value != null)
            {
                var opt = Options.ToArray();
                var valueJson = JsonSerializer.Serialize(Value);
                for (int i = 0; i < opt.Length; i++)
                {
                    if (opt[i].Value == Value)
                    {
                        startIndex = i;
                        break;
                    }
                    if (JsonSerializer.Serialize(opt[i].Value) == valueJson)
                    {
                        startIndex = i;
                        break;
                    }
                }
            }

            if (startIndex == -1)
            {
                if (AllowClear)
                {
                    startIndex = -1;
                }
                else
                {
                    startIndex = 0;
                    Value = Options.FirstOrDefault()?.Value;
                }
            }
            SelectedIndex = startIndex;
        }

        private void SelectionChanged(ChangeEventArgs args)
        {
            if (int.TryParse(args?.Value?.ToString(), out int index))
                SelectedIndex = index;
        }

        public override async Task<bool> Validate()
        {
            if (this.SelectedIndex == -1)
            {
                ErrorMessage = Translater.Instant($"Validators.Required");
                return false;
            }
            return await base.Validate();
        }
    }
}