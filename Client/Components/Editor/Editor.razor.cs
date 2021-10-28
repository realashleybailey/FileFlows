namespace FileFlow.Client.Components
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using FileFlow.Shared;
    using FileFlow.Shared.Models;
    using Newtonsoft.Json;
    using ffElement = FileFlow.Shared.Models.FlowElement;
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Reflection;
    using FileFlow.Plugins.Attributes;
    using System.Linq;
    using System.ComponentModel;

    public partial class Editor : ComponentBase
    {

        public bool Visible { get; set; }

        public string Title { get; set; }
        public string Icon { get; set; }

        private string TypeName { get; set; }
        private bool IsSaving { get; set; }

        private string lblSave, lblSaving, lblCancel;

        private List<ElementField> Fields { get; set; }

        private ExpandoObject Model { get; set; }

        TaskCompletionSource<ExpandoObject> OpenTask;

        public delegate Task<bool> SaveDelegate(ExpandoObject model);
        private SaveDelegate SaveCallback;

        protected override void OnInitialized()
        {
            lblSave = Translater.Instant("Labels.Save");
            lblSaving = Translater.Instant("Labels.Saving");
            lblCancel = Translater.Instant("Labels.Cancel");
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

        internal Task<ExpandoObject> Open(string title, object obj, object model, SaveDelegate saveCallback = null)
        {
            Logger.Instance.DLog("opening editor!");
            var expandoModel = ConverToExando(model);
            var dict = (IDictionary<string, object>)expandoModel;
            Logger.Instance.DLog("Part: ", obj);
            var fields = new List<ElementField>();
            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var attribute = prop.GetCustomAttributes(typeof(FormInputAttribute), false).FirstOrDefault() as FormInputAttribute;
                if (attribute != null)
                {
                    fields.Add(new ElementField
                    {
                        Name = prop.Name,
                        Order = attribute.Order,
                        InputType = attribute.InputType,
                        Type = prop.PropertyType.FullName
                    });
                    if (dict.ContainsKey(prop.Name) == false)
                    {
                        var dValue = prop.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute;
                        dict.Add(prop.Name, dValue != null ? dValue.Value : prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null);
                    }
                }
            }

            return Open(obj.GetType().Name, title, fields, expandoModel, saveCallback: saveCallback);
        }


        internal Task<ExpandoObject> Open(string typeName, string title, List<ElementField> fields, object model, SaveDelegate saveCallback = null)
        {
            this.SaveCallback = saveCallback;
            this.TypeName = typeName;
            this.Title = title;
            this.Fields = fields;
            this.Visible = true;
            var expandoModel = ConverToExando(model);
            this.Model = expandoModel;
            Logger.Instance.DLog("model", this.Model);
            OpenTask = new TaskCompletionSource<ExpandoObject>();
            this.StateHasChanged();
            return OpenTask.Task;
        }

        private async Task Save()
        {

            if (SaveCallback != null)
            {
                Logger.Instance.DLog("About to call save callbakc");
                bool saved = await SaveCallback(this.Model);
                Logger.Instance.DLog("saved:" + saved);
                if (saved == false)
                    return;
            }
            else
            {
                Logger.Instance.DLog("Save callback was null");
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

        private T GetValue<T>(string field, T @default = default(T))
        {
            var dict = (IDictionary<string, object>)Model;
            Logger.Instance.DLog("Getting value for: " + field);
            if (dict.ContainsKey(field) == false)
            {
                Logger.Instance.DLog("Not in model");
                return @default;
            }
            object value = dict[field];
            if (value == null)
            {
                Logger.Instance.DLog("value is null");
                return @default;
            }
            if (value is T)
            {
                return (T)value;
            }
            var valueType = value.GetType();
            try
            {
                if (typeof(T).IsArray && typeof(IEnumerable).IsAssignableFrom(valueType))
                {

                    // we have a list, we want to make it an array
                    var converted = FileFlow.Shared.Converter.ChangeListToArray<T>((IEnumerable)value, valueType);
                    return (T)converted;
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                Logger.Instance.DLog("Not of type: " + value.GetType());
                Logger.Instance.WLog("error: " + ex.Message + "\n" + ex.StackTrace);
                return @default;

            }
        }
    }
}