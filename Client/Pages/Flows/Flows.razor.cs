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
                var flowResult = await HttpHelper.Get<Dictionary<string, List<ffFlow>>>("/api/flow/templates");
                if(flowResult.Success == false || flowResult.Data?.Any() != true)
                {
                    // no templates, give them a blank
                    NavigationManager.NavigateTo("flows/" + Guid.Empty);
                    return;
                }

                templates = new();
                foreach(var  group in flowResult.Data)
                {
                    if(string.IsNullOrEmpty(group.Key) == false)
                    {
                        templates.Add(new Plugin.ListOption
                        {
                            Value = Globals.LIST_OPTION_GROUP,
                            Label = group.Key
                        });
                    }
                    templates.AddRange(group.Value.Select(x => new Plugin.ListOption
                    {
                        Label = x.Name,
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
            fields.Insert(1, new ElementField
            {
                Name = "Template",
                InputType = Plugin.FormInputType.Select,
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputSelect.HideLabel), true},
                    { nameof(InputSelect.Options), templates },
                    { nameof(InputSelect.AllowClear), false},
                    { nameof(InputSelect.ShowDescription), true }
                }
            });

            var newModelTask = Editor.Open("Pages.Flows.Template", "Pages.Flows.Template.Title", fields, new System.Dynamic.ExpandoObject(), lblSave: "Labels.Add");
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

            var newTemplate = ((IDictionary<string, object>)newModelTask.Result)["Template"] as ffFlow;

            App.Instance.NewFlowTemplate = newTemplate;
            NavigationManager.NavigateTo("flows/" + Guid.Empty);
#endif
        }


        public override async Task<bool> Edit(ffFlow item)
        {
            if(item != null)
                NavigationManager.NavigateTo("flows/" + item.Uid);
            return await Task.FromResult(false);
        }
    }

}