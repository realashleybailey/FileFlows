using System.Reflection;
using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Components.Inputs;
using Microsoft.AspNetCore.Components.Rendering;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using Microsoft.JSInterop;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ffFlow = FileFlows.Shared.Models.Flow;

namespace FileFlows.Client.Pages;

public partial class Flows : ListPage<Guid, FlowListModel>
{
    [Inject] NavigationManager NavigationManager { get; set; }
    [Inject] public IJSRuntime jsRuntime { get; set; }

    private NewFlowEditor AddEditor;

    public override string ApiUrl => "/api/flow";

    private FlowSkyBox<FlowType> Skybox;

    private List<FlowListModel> DataStandard = new();
    private List<FlowListModel> DataFailure = new();
    private FlowType SelectedType = FlowType.Standard;

    private Dictionary<string, List<FlowTemplateModel>> Templates = new ();

    #if(DEBUG)
    private bool DEBUG = true;
    #else
    private bool DEBUG = false;
    #endif

    public override string FetchUrl => ApiUrl + "/list-all";


    async Task Enable(bool enabled, ffFlow flow)
    {
        Blocker.Show();
        try
        {
            await HttpHelper.Put<ffFlow>($"{ApiUrl}/state/{flow.Uid}?enable={enabled}");
        }
        finally
        {
            Blocker.Hide();
        }
    }

    private async void Add()
    {
        if (AddEditor == null)
        {
            NavigationManager.NavigateTo("flows/" + Guid.Empty);
            return;
        }
        var  newFlow = await AddEditor.Show();
        if (newFlow == null)
            return; // was canceled
        
        if (newFlow.Uid != Guid.Empty)
        {
            if ((App.Instance.FileFlowsSystem.ConfigurationStatus & ConfigurationStatus.Flows) != ConfigurationStatus.Flows)
            {
                // refresh the app configuration status
                await App.Instance.LoadAppInfo();
            }
            // was saved, refresh list
            await this.Refresh();
        }
        else
        {
            // edit it
            App.Instance.NewFlowTemplate = newFlow;
            NavigationManager.NavigateTo("flows/" + Guid.Empty);
        }
        return;
        
        Blocker.Show();
        List<Plugin.ListOption> templates = null;
        try
        {
            var flowResult = await HttpHelper.Get<Dictionary<string, List<FlowTemplateModel>>>("/api/flow/templates");
            if (flowResult.Success == false || flowResult.Data?.Any() != true)
            {
                // no templates, give them a blank
                NavigationManager.NavigateTo("flows/" + Guid.Empty);
                return;
            }

            templates = new();
            foreach (var group in flowResult.Data)
            {
                if (string.IsNullOrEmpty(group.Key) == false)
                {
                    templates.Add(new Plugin.ListOption
                    {
                        Value = Globals.LIST_OPTION_GROUP,
                        Label = group.Key
                    });
                }
                templates.AddRange(group.Value.Select(x => new Plugin.ListOption
                {
                    Label = x.Flow.Name,
                    Value = x
                }));
            }
        }
        finally
        {
            Blocker.Hide();
        }
        List<ElementField> fields = new List<ElementField>();
        // add the name to the fields, so a node can be renamed
        fields.Insert(0, new ElementField
        {
            Name = "PageDescription",
            InputType = Plugin.FormInputType.Label
        });
        var efTemplate = new ElementField
        {
            Name = "Template",
            InputType = Plugin.FormInputType.Select,
            Parameters = new Dictionary<string, object>
            {
                //{ nameof(InputSelect.HideLabel), true},
                { nameof(InputSelect.Options), templates },
                { nameof(InputSelect.AllowClear), false},
                { nameof(InputSelect.ShowDescription), true }
            }
        };

        efTemplate.ValueChanged += EfTemplate_ValueChanged;

        fields.Insert(1, efTemplate);

        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(ffFlow.Name),
            Validators = new List<FileFlows.Shared.Validators.Validator> {
                new FileFlows.Shared.Validators.Required()
            }
        });

        var newModelTask = Editor.Open("Pages.Flows.Template", "Pages.Flows.Template.Title", fields, new ExpandoObject(), lblSave: "Labels.Add");
        try
        {
            await newModelTask;
            if (newModelTask.IsCanceled || newModelTask.Result is IDictionary<string, object> == false)
                return;
        }
        catch (Exception)
        {
            return; // throws if canceled
        }
        efTemplate.ValueChanged -= EfTemplate_ValueChanged;

        var newFlowTemplate = await GetNewFlowTemplate(newModelTask.Result);
        if (newFlowTemplate != null && newFlowTemplate.Uid != Guid.Empty)
        {
            if ((App.Instance.FileFlowsSystem.ConfigurationStatus & ConfigurationStatus.Flows) !=
                ConfigurationStatus.Flows)
            {
                // refresh the app configuration status
                await App.Instance.LoadAppInfo();
            }
            // was saved, refresh list
            await this.Refresh();
        }
        else
        {
            // edit it
            App.Instance.NewFlowTemplate = newFlowTemplate;
            NavigationManager.NavigateTo("flows/" + Guid.Empty);
        }
    }

    private async Task<ffFlow> GetNewFlowTemplate(ExpandoObject newModel)
    {
        var dict = (IDictionary<string, object>)newModel;
        var newTemplate = dict.ContainsKey("Template") ? dict["Template"] as FlowTemplateModel : null;            

        var newFlowTemplate = newTemplate?.Flow;
        string name = (string)dict[nameof(ffFlow.Name)];

        if (newFlowTemplate != null)
        {
            // look for configured values
            foreach (var k in dict.Keys)
            {
                if (k == "Template" || k == nameof(ffFlow.Name))
                    continue;
                var ids = k.Split(';');
                string nodeId = ids[1];
                string fieldName = ids[2];
                var part = newFlowTemplate.Parts.Where(x => x.Uid.ToString() == nodeId).FirstOrDefault();
                if (part != null)
                {
                    // set the model incase its null
                    part.Model ??= new ExpandoObject();

                    var pmDict = part.Model as IDictionary<string, object>;
                    if (pmDict != null)
                    {
                        if (string.IsNullOrEmpty(fieldName) && dict[k] is IDictionary<string, object> pv)
                        {
                            // complex template type, eg we are setting more than one value to a node
                            foreach(var pvk in pv.Keys)
                            {
                                if (pmDict.ContainsKey(pvk))
                                    pmDict[pvk] = pv[pvk];
                                else
                                    pmDict.Add(pvk, pv[pvk]);
                            }
                        }
                        else
                        {
                            if (pmDict.ContainsKey(fieldName))
                                pmDict[fieldName] = dict[k];
                            else
                                pmDict.Add(fieldName, dict[k]);
                        }
                    }
                }
            }
        }

        newFlowTemplate.Name = name;
        newFlowTemplate.Type = newTemplate.Type;

        if (newTemplate?.Save == true)
        {
            Blocker.Show();
            try
            {
                var result = await HttpHelper.Put<ffFlow>("/api/flow?uniqueName=true", newFlowTemplate);
                if (result.Success)
                    newFlowTemplate = result.Data;
            }
            finally
            {
                Blocker.Hide();
            }
        }


        return newFlowTemplate;
    }

    private void EfTemplate_ValueChanged(object sender, object value)
    {
        var flowTemplate = value as FlowTemplateModel;
        var fields = flowTemplate?.Fields ?? new List<TemplateField>();
        var editor = sender as Editor;
        if (editor == null)
            return;

        editor.RemoveRegisteredInputs(nameof(ffFlow.Name), "Template");


        if (string.IsNullOrEmpty(flowTemplate?.Flow.Description))
        {
            editor.AdditionalFields = null;
            return;
        }

        if(editor.Model is IDictionary<string, object> dict)
        {
            dict[nameof(ffFlow.Name)] = flowTemplate.Flow.Name?.IndexOf("Blank ") >= 0 ? string.Empty : flowTemplate.Flow.Name;
            int hashCode = flowTemplate.GetHashCode();
            foreach(var key in dict.Keys.ToArray())
            {
                if (Regex.IsMatch(key, @"^[\d]+;[\w\d\-]{36};(.*?)$") == false)
                    continue;
                if (key.StartsWith(hashCode + ";") == false)
                    dict.Remove(key);
            }
        }
        editor.AdditionalFields = null;

        _ = Task.Run(async () =>
        {
            await WaitForRender();

            editor.AdditionalFields = builder =>
            {
                int count = 0;
                builder.OpenElement(++count, "div");
                builder.AddAttribute(1, "class", "input-label flow-template-description");
                builder.AddContent(2, flowTemplate.Flow.Description);
                builder.CloseComponent();


                foreach (var field in fields)
                {
                    if (field.Type == "Directory")
                    {
                        FlowTemplateEditor_AddDirectory(builder, field, count, editor, flowTemplate);
                    }
                    else if (field.Type == "Switch")
                    {
                        FlowTemplateEditor_AddSwitch(builder, field, count, editor, flowTemplate);
                    }
                    else if (field.Type == "Select")
                    {
                        FlowTemplateEditor_AddSelect(builder, field, count, editor, flowTemplate);
                    }
                }
            };
        });
    }

    public override async Task<bool> Edit(FlowListModel item)
    {
        if(item != null)
            NavigationManager.NavigateTo("flows/" + item.Uid);
        return await Task.FromResult(false);
    }

    private string GetFieldName(FlowTemplateModel flowTemplate, TemplateField field)
    {
        return flowTemplate.GetHashCode() + ";" + field.Uid + ";" + field.Name;
    }

    private delegate void FieldValueUpdate(TemplateField field, object value);

    private event FieldValueUpdate OnFieldValueUpdate;

    private void SetFieldValue(FlowTemplateModel flowTemplate, TemplateField field, Editor editor, object value)
    {
        Logger.Instance.ILog("Setting field value", field.Name);
        var em = editor.Model as IDictionary<string, object>;
        if (em == null)
            return;
        string key = GetFieldName(flowTemplate, field);

        if(value is JsonElement jElement)
        {
            if(jElement.ValueKind == JsonValueKind.Object)
            {
                var tv = jElement.Deserialize<Dictionary<string, object>>();
                if ((tv.ContainsKey("true") == false && tv.ContainsKey("false")) == false)
                {
                    // true/false are special cases for the swtich types 
                    // complex object, meaning we are setting properties
                    IDictionary<string, object> dict;
                    if (em.ContainsKey(key) == false)
                    {
                        em.Add(key, new ExpandoObject());
                    }
                    else if (em[key] is IDictionary<string, object> == false)
                    {
                        em[key] = new ExpandoObject();
                    }
                    dict = em[key] as IDictionary<string, object>;
                    
                    foreach (var kv in tv.Keys)
                    {
                        if (dict.ContainsKey(kv))
                            dict[kv] = tv[kv];
                        else
                            dict.Add(kv, tv[kv]);
                    }
                    return;
                }
            }
            else if(jElement.ValueKind == JsonValueKind.String)
            {
                value = jElement.GetString();
            }
            else if (jElement.ValueKind == JsonValueKind.Number)
            {
                value = jElement.GetDouble();
            }
        }

        if (em.ContainsKey(key))
            em[key] = value;
        else
            em.Add(key, value);
        
        OnFieldValueUpdate?.Invoke(field, value);
    }

    private Dictionary<string, bool> HiddenTemplateFields = new Dictionary<string, bool>();

    private void HideTemplateField(string name, bool visible)
    {
        if (HiddenTemplateFields.ContainsKey(name))
            HiddenTemplateFields[name] = visible;
        else
            HiddenTemplateFields.Add(name, visible);
    }

    private bool FieldIsVisbile(string name)
    {
        Logger.Instance.ILog("Checking is visible: " + name);
        if (HiddenTemplateFields.ContainsKey(name) == false)
            return true;
        return HiddenTemplateFields[name];
    }


    private void FlowTemplateEditor_AddDirectory(RenderTreeBuilder builder, TemplateField field, int count, Editor editor, FlowTemplateModel flowTemplate)
    {
        int fieldCount = 0;
        builder.OpenComponent<InputFile>(++count);
        builder.AddAttribute(++fieldCount, nameof(InputSwitch.Help), Translater.TranslateIfNeeded(field.Help));
        builder.AddAttribute(++fieldCount, nameof(InputSwitch.Label), field.Label);

        builder.AddAttribute(++fieldCount, nameof(InputFile.Directory), true);
        if (field.Required)
            builder.AddAttribute(++fieldCount, nameof(InputFile.Validators), new List<FileFlows.Shared.Validators.Validator>
                            {
                                new FileFlows.Shared.Validators.Required()
                            });
        object @default = field.Default;
        if (@default is System.Text.Json.JsonElement je)
            @default = je.GetString() ?? string.Empty;

        builder.AddAttribute(++fieldCount, nameof(InputFile.Value), @default as string ?? string.Empty);
        builder.AddAttribute(++fieldCount, nameof(InputFile.ValueChanged), EventCallback.Factory.Create<string>(this, arg =>
        {
            SetFieldValue(flowTemplate, field, editor, arg);
        }));
        if (field.Conditions?.Any() == true)
        {
            builder.AddAttribute(++fieldCount, nameof(InputFile.Visible), FieldIsVisbile(field.Uid.ToString()));
            OnFieldValueUpdate += (tf, val) =>
            {
                foreach (var condition in field.Conditions)
                {
                    Logger.Instance.ILog("Checking condition", condition.Property, tf);
                    if (condition.Property != tf.Uid.ToString())
                        continue;
                    bool isMatch = condition.Matches(val);
                    if (condition.IsMatch == isMatch)
                        continue;
                    condition.IsMatch = isMatch;
                    this.StateHasChanged();
                }
            };
        }
        builder.CloseComponent();
    }

    private void FlowTemplateEditor_AddSwitch(RenderTreeBuilder builder, TemplateField field, int count, Editor editor, FlowTemplateModel flowTemplate)
    {
        int fieldCount = 0;
        builder.OpenComponent<InputSwitch>(++count);
        builder.AddAttribute(++fieldCount, nameof(InputSwitch.Help), Translater.TranslateIfNeeded(field.Help));
        builder.AddAttribute(++fieldCount, nameof(InputSwitch.Label), field.Label);
        bool @default = ((JsonElement)field.Default).GetBoolean();

        object trueValue = true;
        object falseValue = false;
        if (field.Value != null)
        {
            if (field.Value is JsonElement je)
            {
                Dictionary<string, object> jeValue = JsonSerializer.Deserialize<Dictionary<string, object>>(je.ToJson());
                if (jeValue != null)
                {
                    if (jeValue.ContainsKey("true"))
                        trueValue = jeValue["true"];
                    if (jeValue.ContainsKey("false"))
                        falseValue = jeValue["false"];
                }
            }
        }

        builder.AddAttribute(++fieldCount, nameof(InputSwitch.Value), @default);
        builder.AddAttribute(++fieldCount, nameof(InputSwitch.ValueChanged), EventCallback.Factory.Create<bool>(this, arg =>
        {
            SetValue(arg);
        }));
        builder.CloseComponent();

        SetValue(@default);

        void SetValue(bool @checked)
        {
            SetFieldValue(flowTemplate, field, editor, @checked ? trueValue : falseValue);
        }
    }


    private void FlowTemplateEditor_AddSelect(RenderTreeBuilder builder, TemplateField field, int count, Editor editor, FlowTemplateModel flowTemplate)
    {
        int fieldCount = 0;
        builder.OpenComponent<InputSelect>(++count);
        builder.AddAttribute(++fieldCount, nameof(InputSelect.Help), Translater.TranslateIfNeeded(field.Help));
        builder.AddAttribute(++fieldCount, nameof(InputSelect.Label), field.Label);

        string jsonParameters = System.Text.Json.JsonSerializer.Serialize(field.Parameters);
        var parameters = System.Text.Json.JsonSerializer.Deserialize<TemplateSelectParameters>(System.Text.Json.JsonSerializer.Serialize(field.Parameters), new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        builder.AddAttribute(++fieldCount, nameof(InputSelect.Options), parameters.Options);

        builder.AddAttribute(++fieldCount, nameof(InputSelect.Validators), new List<FileFlows.Shared.Validators.Validator>
        {
            new FileFlows.Shared.Validators.Required()
        });

        builder.AddAttribute(++fieldCount, nameof(InputSelect.ValueChanged), EventCallback.Factory.Create<object>(this, arg =>
        {
            SetFieldValue(flowTemplate, field, editor, arg);
        }));
        builder.CloseComponent();
    }

    private async Task Export()
    {
#if (!DEMO)
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        string url = $"/api/flow/export/{item.Uid}";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        await jsRuntime.InvokeVoidAsync("ff.downloadFile", new object[] { url, item.Name + ".json" });
#endif
    }

    private async Task Import()
    {
#if (!DEMO)
        var idResult = await ImportDialog.Show();
        string json = idResult.content;
        if (string.IsNullOrEmpty(json))
            return;

        Blocker.Show();
        try
        {
            var newFlow = await HttpHelper.Post<ffFlow>("/api/flow/import", json);
            if (newFlow != null && newFlow.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Flows.Messages.FlowImported", new { name = newFlow.Data.Name }));
            }
        }
        finally
        {
            Blocker.Hide();
        }
#endif
    }

    private async Task Template()
    {
#if (DEBUG)

        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        string url = $"/api/flow/template/{item.Uid}";
        url = "http://localhost:6868" + url;
        await jsRuntime.InvokeVoidAsync("ff.downloadFile", new object[] { url, item.Name + ".json" });
#endif
    }

    private class TemplateSelectParameters
    {
        public List<Plugin.ListOption> Options { get; set; }
    }
    
    
    private async Task Duplicate()
    {
#if (!DEMO)
        Blocker.Show();
        try
        {
            var item = Table.GetSelected()?.FirstOrDefault();
            if (item == null)
                return;
            string url = $"/api/flow/duplicate/{item.Uid}";
#if (DEBUG)
            url = "http://localhost:6868" + url;
#endif
            var newItem = await HttpHelper.Get<Script>(url);
            if (newItem != null && newItem.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Flows.Messages.Duplicated",
                    new { name = newItem.Data.Name }));
            }
            else
            {
                Toast.ShowError(newItem.Body?.EmptyAsNull() ?? "Failed to duplicate");
            }
        }
        finally
        {
            Blocker.Hide();
        }
#endif
    }

    protected override Task PostDelete() => Refresh();

    public override Task PostLoad()
    {
        UpdateTypeData();
        return Task.CompletedTask;
    }
    
    private void UpdateTypeData()
    {
        this.DataFailure = this.Data.Where(x => x.Type == FlowType.Failure).ToList();
        this.DataStandard = this.Data.Where(x => x.Type == FlowType.Standard).ToList();
        this.Skybox.SetItems(new List<FlowSkyBoxItem<FlowType>>()
        {
            new ()
            {
                Name = "Standard Flows",
                Icon = "fas fa-sitemap",
                Count = this.DataStandard.Count,
                Value = FlowType.Standard
            },
            new ()
            {
                Name = "Failure Flows",
                Icon = "fas fa-exclamation-circle",
                Count = this.DataFailure.Count,
                Value = FlowType.Failure
            }
        }, this.SelectedType);
    }

    private void SetSelected(FlowSkyBoxItem<FlowType> item)
    {
        SelectedType = item.Value;
        // need to tell table to update so the "Default" column is shown correctly
        Table.TriggerStateHasChanged();
        this.StateHasChanged();
    }

    private async Task SetDefault()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        
        Blocker.Show();
        try
        {
            await HttpHelper.Put($"/api/flow/set-default/{item.Uid}?default={(!item.Default)}");
            await this.Refresh();
        }
        finally
        {
            Blocker.Hide();
        }
    }

    public override async Task Delete()
    {
        var used = Table.GetSelected()?.Any(x => x.UsedBy?.Any() == true) == true;
        if (used)
        {
            Toast.ShowError("Pages.Flows.Messages.DeleteUsed");
            return;
        }
        await base.Delete();
    }

    private async Task UsedBy()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item?.UsedBy?.Any() != true)
            return;
        await UsedByDialog.Show(item.UsedBy);
    }
}
