namespace FileFlows.Client.Pages
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Radzen;
    using Radzen.Blazor;
    using FileFlows.Client.Components;
    using FileFlows.Client.Components.Dialogs;
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using ffFlow = FileFlows.Shared.Models.Flow;
    using System;
    using FileFlows.Client.Components.Inputs;
    using System.Dynamic;
    using Microsoft.AspNetCore.Components.Rendering;
    using System.Text.RegularExpressions;

    public partial class Flows : ListPage<ffFlow>
    {
        [Inject] NavigationManager NavigationManager { get; set; }

        public override string ApiUrl => "/api/flow";



#if (DEMO)
        protected override Task<RequestResult<List<ffFlow>>> FetchData()
        {
            var results = Enumerable.Range(1, 10).Select(x => new ffFlow
            {
                Uid = Guid.NewGuid(),
                Name = "Demo Flow " + x,
                Enabled = x < 5
            }).ToList();
            return Task.FromResult(new RequestResult<List<ffFlow>> { Success = true, Data = results });
        }
#endif

        async Task Enable(bool enabled, ffFlow flow)
        {
#if (DEMO)
            return;
#else
            Blocker.Show();
            try
            {
                await HttpHelper.Put<ffFlow>($"{ApiUrl}/state/{flow.Uid}?enable={enabled}");
            }
            finally
            {
                Blocker.Hide();
            }
#endif
        }

        private async void Add()
        {
#if (DEMO)
            NavigationManager.NavigateTo("flows/" + Guid.Empty);
#else
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
            templates.Insert(0, new Plugin.ListOption
            {
                Label = Translater.Instant("Pages.Flows.Template.BlankTemplate"),
                Value = null
            });

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
                // was saved, refresh list
                await this.Refresh();
            }
            else
            {
                // edit it
                App.Instance.NewFlowTemplate = newFlowTemplate;
                NavigationManager.NavigateTo("flows/" + Guid.Empty);
            }
#endif
        }

        private async Task<ffFlow> GetNewFlowTemplate(ExpandoObject newModel)
        {
            var dict = (IDictionary<string, object>)newModel;
            var newTemplate = dict.ContainsKey("Template") ? dict["Template"] as FlowTemplateModel : null;            

            var newFlowTemplate = newTemplate?.Flow;

            if (newFlowTemplate != null)
            {
                // look for configured values
                foreach (var k in dict.Keys)
                {
                    if (k == "Template")
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
                            if (pmDict.ContainsKey(fieldName))
                                pmDict[fieldName] = dict[k];
                            else
                                pmDict.Add(fieldName, dict[k]);
                        }
                    }
                }
            }

            if (newTemplate?.Save == true)
            {
#if (DEMO == false)
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
#endif
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

            if (string.IsNullOrEmpty(flowTemplate?.Flow.Description))
            {
                editor.AdditionalFields = null;
                return;
            }

            if(editor.Model is IDictionary<string, object> dict)
            {
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

        public override async Task<bool> Edit(ffFlow item)
        {
            if(item != null)
                NavigationManager.NavigateTo("flows/" + item.Uid);
            return await Task.FromResult(false);
        }

        private string GetFieldName(FlowTemplateModel flowTemplate, TemplateField field)
        {
            return flowTemplate.GetHashCode() + ";" + field.Uid + ";" + field.Name;
        }

        private void SetFieldValue(FlowTemplateModel flowTemplate, TemplateField field, Editor editor, object value)
        {
            var em = editor.Model as IDictionary<string, object>;
            if (em == null)
                return;
            string key = GetFieldName(flowTemplate, field);
            if (em.ContainsKey(key))
                em[key] = value;
            else
                em.Add(key, value);
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
            builder.CloseComponent();
        }

        private void FlowTemplateEditor_AddSwitch(RenderTreeBuilder builder, TemplateField field, int count, Editor editor, FlowTemplateModel flowTemplate)
        {
            int fieldCount = 0;
            builder.OpenComponent<InputSwitch>(++count);
            builder.AddAttribute(++fieldCount, nameof(InputSwitch.Help), Translater.TranslateIfNeeded(field.Help));
            builder.AddAttribute(++fieldCount, nameof(InputSwitch.Label), field.Label);
            bool @default = ((System.Text.Json.JsonElement)field.Default).GetBoolean();

            object trueValue = true;
            object falseValue = false;
            if (field.Value != null)
            {
                if (field.Value is System.Text.Json.JsonElement je)
                {
                    Dictionary<string, object> jeValue = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(je.ToJson());
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


        private class TemplateSelectParameters
        {
            public List<Plugin.ListOption> Options { get; set; }
        }
    }

}