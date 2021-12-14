namespace FileFlows.Client.Components.Inputs
{
    using FileFlows.Shared;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class InputKeyValue : Input<List<KeyValuePair<string, string>>>
    {
        private string InputText = "";
        private string PreviousInputText = "";
        private string NewKey = string.Empty;
        private string NewValue = string.Empty;
        public override bool Focus() => FocusUid();

        private List<KeyValue> Data = new List<KeyValue>();

        private string DuplicateKey = null; // one time we do want null....

        private string lblKey, lblValue;

        protected override void OnInitialized()
        {
            lblKey = Translater.Instant(this.LabelOriginal + "Key");
            lblValue = Translater.Instant(this.LabelOriginal + "Value");
            base.OnInitialized();
            if (Value == null)
                Value = new List<KeyValuePair<string, string>>();

            this.Data = Value.Select(x => new KeyValue {  Key = x.Key, Value = x.Value }).ToList();  
        }


        void Remove(KeyValue kv)
        {
            this.Data.Remove(kv);
            CheckForDuplicates();
        }

        void Add()
        {
            string key = NewKey;
            string value = NewValue ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key))
                return;
            key = key.Trim();

            this.Data.Add(new KeyValue {  Key = key, Value = value });

            CheckForDuplicates();

            NewKey = string.Empty;
            NewValue = string.Empty;
            FocusUid();
        }

        void OnBlur()
        {
            CheckForDuplicates();
        }

        public override async Task<bool> Validate()
        {
            this.Data ??= new();

            if (CheckForDuplicates() == false)
                return false;


            if (this.Data.Any() == false && Validators?.Any(x => x.Type == "Required") == true)
            {
                this.ErrorMessage = Translater.Instant("Validators.Required");
                return false;
            }


            this.Value = this.Data.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)).ToList();

            return await base.Validate();
        }

        private bool CheckForDuplicates()
        {
            DuplicateKey = this.Data?.GroupBy(x => x.Key, x => x)?.FirstOrDefault(x => x.Count() > 1)?.Select(x => x.Key)?.FirstOrDefault();
            if (DuplicateKey != null)
            {
                Logger.Instance.WLog("Duplicates found, " + DuplicateKey);
                ErrorMessage = Translater.Instant("ErrorMessages.DuplicatesFound");
                this.StateHasChanged();
                return false;
            }
            ErrorMessage = string.Empty;
            return true;
        }

        class KeyValue
        {
            public string Key { get; set; } 
            public string Value { get; set; }   
        }
    }
}