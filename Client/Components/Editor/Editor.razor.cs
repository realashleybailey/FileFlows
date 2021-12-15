namespace FileFlows.Client.Components
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using ffElement = FileFlows.Shared.Models.FlowElement;
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Reflection;
    using FileFlows.Plugin.Attributes;
    using System.Linq;
    using System.ComponentModel;
    using FileFlows.Client.Components.Inputs;

    public partial class Editor : ComponentBase
    {

        public bool Visible { get; set; }

        public string Title { get; set; }
        public string Icon { get; set; }

        public string TypeName { get; set; }
        private bool IsSaving { get; set; }

        private string lblSave, lblSaving, lblCancel, lblClose;

        private List<ElementField> Fields { get; set; }

        private Dictionary<string, List<ElementField>> Tabs { get; set; }

        public ExpandoObject Model { get; set; }

        TaskCompletionSource<ExpandoObject> OpenTask;

        public delegate Task<bool> SaveDelegate(ExpandoObject model);
        private SaveDelegate SaveCallback;

        private bool ReadOnly { get; set; }
        public bool Large { get; set; }

        public string EditorDescription { get; set; }

        private readonly List<Inputs.IInput> RegisteredInputs = new List<Inputs.IInput>();

        private bool FocusFirst = false;
        private bool _needsRendering = false;


        private RenderFragment _AdditionalFields;
        public RenderFragment AdditionalFields
        {
            get => _AdditionalFields;
            set
            {
                _AdditionalFields = value;
                this.StateHasChanged();
            }
        }

        protected override void OnInitialized()
        {
            lblSave = Translater.Instant("Labels.Save");
            lblSaving = Translater.Instant("Labels.Saving");
            lblCancel = Translater.Instant("Labels.Cancel");
            lblClose = Translater.Instant("Labels.Close");
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (FocusFirst)
            {
                foreach (var input in RegisteredInputs)
                {
                    if (input.Focus())
                        break;
                }
                FocusFirst = false;
            }
        }

        private ExpandoObject ConverToExando(object model)
        {
            if (model == null)
                return new ExpandoObject();
            if (model is ExpandoObject eo)
                return eo;

            var expando = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)expando;

            foreach (var property in model.GetType().GetProperties())
                dictionary.Add(property.Name, property.GetValue(model));
            return expando;
        }


        internal void RegisterInput<T>(Input<T> input)
        {
            if (this.RegisteredInputs.Contains(input) == false)
                this.RegisteredInputs.Add(input);
        }

        internal Inputs.IInput GetRegisteredInput(string name)
        {
            return this.RegisteredInputs.Where(x => x.Field.Name == name).FirstOrDefault();
        }

        internal Task<ExpandoObject> Open(string typeName, string title, List<ElementField> fields, object model, SaveDelegate saveCallback = null, bool readOnly = false, bool large = false, string lblSave = null, string lblCancel = null, RenderFragment additionalFields = null, Dictionary<string, List<ElementField>> tabs = null)
        {
            this.RegisteredInputs.Clear();
            var expandoModel = ConverToExando(model);
            this.Model = expandoModel;
            this.SaveCallback = saveCallback;
            this.TypeName = typeName;
            this.Title = Translater.TranslateIfNeeded(title);
            this.Fields = fields;
            this.Tabs = tabs;
            this.ReadOnly = readOnly;
            this.Large = large;
            this.Visible = true;
            this.AdditionalFields = additionalFields;

            lblSave = lblSave.EmptyAsNull() ?? "Labels.Save";
            this.lblCancel = Translater.TranslateIfNeeded(lblCancel.EmptyAsNull() ?? "Labels.Cancel");

            if (lblSave == "Labels.Save") {
                this.lblSaving = Translater.Instant("Labels.Saving");
                this.lblSave = Translater.Instant(lblSave);
            }
            else
            {
                this.lblSave = Translater.Instant(lblSave);
                this.lblSaving = lblSave;
            }

            Logger.Instance.DLog("getting description for: " + typeName);
            this.EditorDescription = Translater.Instant(typeName + ".Description");
            Logger.Instance.DLog("getting description for: " + typeName, this.EditorDescription);
            OpenTask = new TaskCompletionSource<ExpandoObject>();
            this.FocusFirst = true;
            this.StateHasChanged();
            return OpenTask.Task;
        }

        private async Task WaitForRender()
        {
            _needsRendering = true;
            StateHasChanged();
            while (_needsRendering)
            {
                await Task.Delay(50);
            }
        }


        private async Task Save()
        {
            bool valid = true;
            foreach (var input in RegisteredInputs)
            {
                bool iValid = await input.Validate();
                if (iValid == false)
                {
                    Logger.Instance.DLog("Invalid input:" + input.Label);
                    valid = false;
                }
            }
            if (valid == false)
                return;

            if (SaveCallback != null)
            {
                bool saved = await SaveCallback(this.Model);
                if (saved == false)
                    return;
            }
            OpenTask.TrySetResult(this.Model);

            this.Visible = false;
            this.Fields?.Clear();
            this.Tabs?.Clear();
        }

        private void Cancel()
        {
            OpenTask.TrySetCanceled();
            this.Visible = false;
            if(this.Fields != null)
                this.Fields.Clear();
            if(this.Tabs != null)
                this.Tabs.Clear();

        }

        internal void UpdateValue(ElementField field, object value)
        {
            if (Model == null)
                return;
            var dict = (IDictionary<string, object>)Model;
            if (dict.ContainsKey(field.Name))
                dict[field.Name] = value;
            else
                dict.Add(field.Name, value);
        }

        internal T GetParameter<T>(ElementField field, string parameter, T @default = default(T))
        {
            var dict = field?.Parameters as IDictionary<string, object>;
            if (dict?.ContainsKey(parameter) != true)
                return @default;
            var val = dict[parameter];
            if (val == null)
                return @default;
            try
            {
                return (T)FileFlows.Shared.Converter.ConvertObject(typeof(T), val);
            }
            catch (Exception)
            {
                Logger.Instance.ELog("Failed converted: " + parameter, val);
                return @default;
            }
        }

        internal T GetValue<T>(string field, T @default = default(T))
        {
            if (Model == null)
                return @default;
            var dict = (IDictionary<string, object>)Model;
            if (dict.ContainsKey(field) == false)
            {
                return @default;
            }
            object value = dict[field];
            if (value == null)
            {
                return @default;
            }

            if (value is System.Text.Json.JsonElement je)
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)je.GetString();
                if (typeof(T) == typeof(int))
                    return (T)(object)je.GetInt32();
                if (typeof(T) == typeof(bool))
                    return (T)(object)je.GetBoolean();
                if (typeof(T) == typeof(float))
                    return (T)(object)(float)je.GetInt64();
            }

            if (value is T)
            {
                return (T)value;
            }

            return (T)FileFlows.Shared.Converter.ConvertObject(typeof(T), value);
        }
    }
}