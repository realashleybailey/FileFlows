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
    using FileFlows.Plugin;
    using System.Text.Json;
    using Microsoft.JSInterop;

    public partial class Editor : ComponentBase
    {
        [Inject] IJSRuntime jsRuntime { get; set; }

        public bool Visible { get; set; }

        public string Title { get; set; }
        public string HelpUrl { get; set; }
        public string Icon { get; set; }

        /// <summary>
        /// Get the name of the type this editor is editing
        /// </summary>
        public string TypeName { get; set; }
        private bool IsSaving { get; set; }

        private string lblSave, lblSaving, lblCancel, lblClose, lblHelp;

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

        public delegate Task<bool> CancelDeletgate();
        public delegate Task ClosedDeletgate();
        public event CancelDeletgate OnCancel;
        public event ClosedDeletgate OnClosed;


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
            lblHelp = Translater.Instant("Labels.Help");
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

        internal void RemoveRegisteredInputs(params string[] except)
        {
            var listExcept = except?.ToList() ?? new();
            this.RegisteredInputs.RemoveAll(x => listExcept.Contains(x.Field?.Name ?? string.Empty) == false);
        }

        internal Inputs.IInput GetRegisteredInput(string name)
        {
            return this.RegisteredInputs.Where(x => x.Field.Name == name).FirstOrDefault();
        }

        internal Task<ExpandoObject> Open(string typeName, string title, List<ElementField> fields, object model, SaveDelegate saveCallback = null, bool readOnly = false, bool large = false, string lblSave = null, string lblCancel = null, RenderFragment additionalFields = null, Dictionary<string, List<ElementField>> tabs = null, string helpUrl = null, bool noTranslateTitle = false)
        {
            this.RegisteredInputs.Clear();
            var expandoModel = ConverToExando(model);
            this.Model = expandoModel;
            this.SaveCallback = saveCallback;
            this.TypeName = typeName;
            if (noTranslateTitle)
                this.Title = title;
            else
                this.Title = Translater.TranslateIfNeeded(title);
            this.Fields = fields;
            this.Tabs = tabs;
            this.ReadOnly = readOnly;
            this.Large = large;
            this.Visible = true;
            this.HelpUrl = helpUrl ?? string.Empty;
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

            this.EditorDescription = Translater.Instant(typeName + ".Description");
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


        private async Task OnSubmit()
        {
            await this.Save();
        }

        private async Task OnClose()
        {
            this.Cancel();
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
            this.OnClosed?.Invoke();
        }

        private async void Cancel()
        {
            if(OnCancel != null)
            {
                bool result = await OnCancel.Invoke();
                if (result == false)
                    return;
            }

            OpenTask.TrySetCanceled();
            this.Visible = false;
            if(this.Fields != null)
                this.Fields.Clear();
            if(this.Tabs != null)
                this.Tabs.Clear();

            await this.WaitForRender();
            this.OnClosed?.Invoke();
        }

        /// <summary>
        /// Finds a field by its name
        /// </summary>
        /// <param name="name">the name of the field</param>
        /// <returns>the field if found, otherwise null</returns>
        internal ElementField? FindField(string name)
        {
            var field = this.Fields?.Where(x => x.Name == name)?.FirstOrDefault();
            return field;
        }
        
        /// <summary>
        /// Updates a value
        /// </summary>
        /// <param name="field">the field whose value is being updated</param>
        /// <param name="value">the value of the field</param>
        internal void UpdateValue(ElementField field, object value)
        {
            if (field.UiOnly)
                return;
            if (Model == null)
                return;
            var dict = (IDictionary<string, object>)Model;
            if (dict.ContainsKey(field.Name))
                dict[field.Name] = value;
            else
                dict.Add(field.Name, value);
        }

        /// <summary>
        /// Gets a parameter value for a field
        /// </summary>
        /// <param name="field">the field to get the value for</param>
        /// <param name="parameter">the name of the parameter</param>
        /// <param name="default">the default value if not found</param>
        /// <typeparam name="T">the type of parameter</typeparam>
        /// <returns>the parameter value</returns>
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
                var converted = Converter.ConvertObject(typeof(T), val);
                T result = (T)converted;
                if(result is List<ListOption> options)
                {
                    foreach(var option in options)
                    {
                        if(option.Value is JsonElement je)
                        {
                            if (je.ValueKind == JsonValueKind.String)
                                option.Value = je.GetString();
                            else if (je.ValueKind == JsonValueKind.Number)
                                option.Value = je.GetInt32();
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Failed converted: " + parameter, val);
                return @default;
            }
        }
        
        /// <summary>
        /// Gets the minimum and maximum from a range validator (if exists)
        /// </summary>
        /// <param name="field">The field to get the range for</param>
        /// <returns>the range</returns>
        internal (int min, int max) GetRange(ElementField field)
        {
            var range = field?.Validators?.Where(x => x is FileFlows.Shared.Validators.Range)?.FirstOrDefault() as FileFlows.Shared.Validators.Range;
            return range == null ? (0, 0) : (range.Minimum, range.Maximum);
        }

        /// <summary>
        /// Gets the default of a specific type
        /// </summary>
        /// <param name="type">the type</param>
        /// <returns>the default value</returns>
        private object GetDefault(Type type)
        {
            if(type?.IsValueType == true)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        /// <summary>
        /// Gets a value for a field
        /// </summary>
        /// <param name="field">the field whose value to get</param>
        /// <param name="type">the type of value to get</param>
        /// <returns>the value</returns>
        internal object GetValue(string field, Type type)
        {
            if (Model == null)
                return GetDefault(type);
            
            var dict = (IDictionary<string, object>)Model;
            if (dict.ContainsKey(field) == false)
                return GetDefault(type);
            object value = dict[field];
            if (value == null)
                return GetDefault(type);

            if (value is JsonElement je)
            {
                if (type == typeof(string))
                    return je.GetString();
                if (type== typeof(int))
                    return je.GetInt32();
                if (type == typeof(bool))
                    return je.GetBoolean();
                if (type == typeof(float))
                    return (float)je.GetInt64();
            }

            if (value.GetType().IsAssignableTo(type))
            {
                return value;
            }

            try
            {
                return Converter.ConvertObject(type, value);
            }
            catch(Exception)
            {
                return GetDefault(type);
            }
        }
        
        /// <summary>
        /// Gets a value for a field
        /// </summary>
        /// <param name="field">the field whose value to get</param>
        /// <param name="default">the default value if none is found</param>
        /// <typeparam name="T">the type of value to get</typeparam>
        /// <returns>the value</returns>
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

            if (value is JsonElement je)
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)je.GetString();
                if (typeof(T) == typeof(int))
                    return (T)(object)je.GetInt32();
                if (typeof(T) == typeof(bool))
                    return (T)(object)je.GetBoolean();
                if (typeof(T) == typeof(float))
                {
                    try
                    {
                        return (T)(object)(float)je.GetInt64();
                    }
                    catch (Exception)
                    {
                        return (T)(object)(float.Parse(je.ToString()));
                    }
                }
            }

            if (value is T)
            {
                return (T)value;
            }

            try
            {
                return (T)Converter.ConvertObject(typeof(T), value);
            }
            catch(Exception)
            {
                return default;
            }
        }

        void OpenHelp()
        {
            if (string.IsNullOrWhiteSpace(HelpUrl))
                return;
            _ = jsRuntime.InvokeVoidAsync("open", HelpUrl, "_blank");
        }
    }
}