namespace ViWatcher.Client.Components.Inputs 
{
    using System.Collections.Generic;
    using Models;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using System.Linq;

    public partial class InputArray : Input<string[]>
    {

        [Parameter]
        public bool AllowDuplicates { get; set; }
        [Parameter]
        public bool EnterOnSpace{ get; set; }
        private string InputText = "";
        private string PreviousInputText = "";
        private void OnKeyDown(KeyboardEventArgs e)
        {
            if(e.ShiftKey == false && e.AltKey == false && e.CtrlKey == false)
            {
                if (e.Code == "Enter" || (EnterOnSpace && e.Code == "Space"))
                {
                    if (Add(InputText))
                        this.InputText = "";
                }
                else if(e.Code == "Backspace" && PreviousInputText == string.Empty){
                    if(this.Value?.Any() == true){
                        this.Value = this.Value.Take(this.Value.Length - 1).ToArray();
                    }
                }
            }
            PreviousInputText = InputText;
        }

        void Remove(string str)
        {
            this.Value = this.Value.Except(new[] { str }).ToArray();
        }

        bool Add(string str)
        {
            if(string.IsNullOrEmpty(str))
                return false;
            str = str.Trim();
            if(AllowDuplicates == false)
            {
                if(this.Value.Contains(str))
                    return false;
            }
            this.Value = this.Value.Union(new[] { str }).ToArray();
            return true;
        }
    }
}