namespace FileFlow.Client.Components
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using FileFlow.Shared;
    using FileFlow.Shared.Models;
    using ffElement = FileFlow.Shared.Models.FlowElement;
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Reflection;
    using FileFlow.Plugin.Attributes;
    using System.Linq;
    using System.ComponentModel;
    using FileFlow.Client.Components.Inputs;

    public partial class Editor : ComponentBase
    {

        public bool Visible { get; set; }

        public string Title { get; set; }
        public string Icon { get; set; }

        private string TypeName { get; set; }
        private bool IsSaving { get; set; }

        private string lblSave, lblSaving, lblCancel, lblClose;

        private List<ElementField> Fields { get; set; }

        private ExpandoObject Model { get; set; }

        TaskCompletionSource<ExpandoObject> OpenTask;

        public delegate Task<bool> SaveDelegate(ExpandoObject model);
        private SaveDelegate SaveCallback;

        private bool ReadOnly { get; set; }
        public bool Large { get; set; }

        public string EditorDescription { get; set; }

        private readonly List<Inputs.IInput> RegisteredInputs = new List<Inputs.IInput>();

        private bool FocusFirst = false;

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

        internal Task<ExpandoObject> Open(string typeName, string title, List<ElementField> fields, object model, SaveDelegate saveCallback = null, bool readOnly = false, bool large = false)
        {
            this.SaveCallback = saveCallback;
            this.TypeName = typeName;
            this.Title = title;
            this.Fields = fields;
            this.ReadOnly = readOnly;
            this.Large = large;
            this.Visible = true;
            Logger.Instance.DLog("getting description for: " + typeName);
            this.EditorDescription = Translater.Instant(typeName + ".Description");
            Logger.Instance.DLog("getting description for: " + typeName, this.EditorDescription);
            var expandoModel = ConverToExando(model);
            this.Model = expandoModel;
            OpenTask = new TaskCompletionSource<ExpandoObject>();
            this.StateHasChanged();
            this.FocusFirst = true;
            return OpenTask.Task;
        }

        private async Task Save()
        {
            bool valid = true;
            foreach (var input in RegisteredInputs)
            {
                Logger.Instance.DLog("Validating input: " + input.Label);
                bool iValid = await input.Validate();
                if (iValid == false)
                {
                    Logger.Instance.DLog("Invalid input:" + input.Label);
                    valid = false;
                }
            }
            if (valid == false)
                return;

            Logger.Instance.DLog("editor is valid!");

            if (SaveCallback != null)
            {
                bool saved = await SaveCallback(this.Model);
                if (saved == false)
                    return;
            }
            OpenTask.TrySetResult(this.Model);

            this.Visible = false;
            this.Fields.Clear();
        }

        private void Cancel()
        {
            OpenTask.TrySetCanceled();
            this.Visible = false;
            this.Fields.Clear();

        }

        private void UpdateValue(string field, object value)
        {
            var dict = (IDictionary<string, object>)Model;
            if (dict.ContainsKey(field))
                dict[field] = value;
            else
                dict.Add(field, value);
        }

        private T GetParameter<T>(ElementField field, string parameter)
        {
            var dict = field?.Parameters as IDictionary<string, object>;
            if (dict?.ContainsKey(parameter) != true)
                return default(T);
            var val = dict[parameter];
            if (val == null)
                return default(T);
            try
            {
                return (T)FileFlow.Shared.Converter.ConvertObject(typeof(T), val);
            }
            catch (Exception)
            {
                Logger.Instance.ELog("Failed converted: " + parameter, val);
                return default(T);
            }
        }

        private T GetValue<T>(string field, T @default = default(T))
        {
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

            return (T)FileFlow.Shared.Converter.ConvertObject(typeof(T), value);
            // var valueType = value.GetType();
            // try
            // {
            //     if (typeof(T).IsArray && typeof(IEnumerable).IsAssignableFrom(valueType))
            //     {

            //         // we have a list, we want to make it an array
            //         var converted = FileFlow.Shared.Converter.ChangeListToArray<T>((IEnumerable)value, valueType);
            //         return (T)converted;
            //     }

            //     return (T)Convert.ChangeType(value, typeof(T));
            // }
            // catch (Exception ex)
            // {
            //     Logger.Instance.DLog("Not of type: " + field + ", " + value.GetType());
            //     Logger.Instance.WLog("error: " + ex.Message + "\n" + ex.StackTrace);
            //     return @default;
            // }
        }
    }
}