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

        private void Add()
        {
            NavigationManager.NavigateTo("flows/" + Guid.Empty);
        }


        public override async Task<bool> Edit(ffFlow item)
        {
            if(item != null)
                NavigationManager.NavigateTo("flows/" + item.Uid);
            return await Task.FromResult(false);
        }
    }

}