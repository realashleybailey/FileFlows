namespace FileFlows.Client.Components.Inputs
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Components;
    using System.Linq;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using System.Text.Json;
    using System.Threading.Tasks;
    using FileFlows.Plugin;
    using System;

    public partial class InputSelect : Input<object>
    {
        private Dictionary<string, List<ListOption>> Groups = new Dictionary<string, List<ListOption>>();
        private readonly List<ListOption> _Options = new List<ListOption>();

        [Parameter] public bool ShowDescription { get; set; }

        private string Description { get; set; }
        private bool UpdatingValue = false;

        [Parameter]
        public IEnumerable<ListOption> Options
        {
            get => _Options;
            set
            {
                _Options.Clear();
                if (value == null)
                    return;
                string group = string.Empty;
                Groups = new Dictionary<string, List<ListOption>>();
                Groups.Add(string.Empty, new List<ListOption>());
                foreach (var lo in value)
                {
                    if (lo.Value is JsonElement je && je.ValueKind == JsonValueKind.String)
                        lo.Value = je.GetString();  // this can happen from the Templates where the object is a JsonElement

                    if(lo.Value is string && (string)lo.Value == Globals.LIST_OPTION_GROUP)
                    {
                        group = lo.Label;
                        Groups.Add(group, new List<ListOption>());
                        continue;
                    }
                    if (Translater.NeedsTranslating(lo.Label))
                        lo.Label = Translater.Instant(lo.Label);
                    _Options.Add(lo);
                    Groups[group].Add(lo);
                }
            }
        }

        public override bool Focus() => FocusUid();

        private bool _AllowClear = true;

        private string lblSelectOne;
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
                UpdateDescription();
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            lblSelectOne = Translater.Instant("Labels.SelectOne");
            ValueUpdated();
        }

        protected override void ValueUpdated()
        {
            if (UpdatingValue)
                return;

            int startIndex = SelectedIndex;
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
            if(startIndex != SelectedIndex)
                SelectedIndex = startIndex;
        }

        private void SelectionChanged(ChangeEventArgs args)
        {
            UpdatingValue = true;
            try
            {
                if (int.TryParse(args?.Value?.ToString(), out int index))
                    SelectedIndex = index;
                else
                    Logger.Instance.DLog("Unable to find index of: ",  args?.Value);
                UpdateDescription();
            }
            finally
            {
                UpdatingValue = false;
            }
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

        private void UpdateDescription()
        {
            Description = string.Empty;
            if (this.ShowDescription == false)
                return;

            IDictionary<string, object> dict = Value as IDictionary<string, object>;

            if (dict == null)
            {
                try
                {
                    string json = JsonSerializer.Serialize(Value);
                    dict = (IDictionary<string, object>)JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(json);
                }
                catch (Exception) { }
            }

            if (dict?.ContainsKey("Description") == true)
                Description = dict["Description"]?.ToString() ?? string.Empty;
        }
    }
}