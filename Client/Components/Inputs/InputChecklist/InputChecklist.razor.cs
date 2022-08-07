using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileFlows.Client.Components.Inputs
{
    using FileFlows.Plugin;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class InputChecklist: Input<List<object>>
    {

        [Parameter]
        public List<ListOption> Options { get; set; }

        [Parameter] public bool ListOnly { get; set; }
        //public override bool Focus() => FocusUid();

        protected override void OnInitialized()
        {
            base.OnInitialized();
            if (Value == null)
                Value = new List<object>();
            else if(Options != null)
            {
                this.Value = this.Value.Select(x =>
                {
                    foreach (var opt in this.Options)
                    {
                        if (opt.Value == x)
                            return x;
                        if (opt.Value is string && x is string)
                            continue;
                        if (x.GetType().IsPrimitive)
                            continue;
                        if (opt.Value == null)
                            continue;
                        string xJson = System.Text.Json.JsonSerializer.Serialize(x);
                        string optJson = System.Text.Json.JsonSerializer.Serialize(opt.Value);
                        if (xJson == optJson)
                            return opt.Value;
                    }
                    return x;
                }).ToList();
            }
        }

        private void OnChange(ChangeEventArgs args, ListOption opt)
        {
            if (ReadOnly)
            {
                this.StateHasChanged();
                return;
            }
            bool @checked = args.Value as bool? == true;
            if (@checked && this.Value.Contains(opt.Value) == false)
                this.Value.Add(opt.Value);
            else if (@checked == false && this.Value.Contains(opt.Value))
                this.Value.Remove(opt.Value);
        }
    }
}