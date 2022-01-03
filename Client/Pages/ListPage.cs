namespace FileFlows.Client.Pages
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FileFlows.Client.Components;
    using FileFlows.Client.Components.Dialogs;
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using Microsoft.AspNetCore.Components;
    using FileFlows.Client.Components.Common;
    using System;

    public abstract class ListPage<T> : ComponentBase where T : FileFlowObject
    {
        protected FlowTable<T> Table { get; set; }
        [CascadingParameter] public Blocker Blocker { get; set; }
        [CascadingParameter] public Editor Editor { get; set; }
        public string lblAdd, lblEdit, lblDelete, lblDeleting, lblRefresh;

        public abstract string ApiUrl { get; }
        private bool _needsRendering = false;

        protected bool Loaded { get; set; }
        protected bool HasData { get; set; }



        public List<T> _Data = new List<T>();
        public List<T> Data
        {
            get => _Data;
            set
            {
                _Data = value ?? new List<T>();
            }
        }

        protected override void OnInitialized()
        {
            lblAdd = Translater.Instant("Labels.Add");
            lblEdit = Translater.Instant("Labels.Edit");
            lblDelete = Translater.Instant("Labels.Delete");
            lblDeleting = Translater.Instant("Labels.Deleting");
            lblRefresh = Translater.Instant("Labels.Refresh");

            _ = Load();
        }

        public virtual async Task Refresh() => await Load();

        public virtual string FetchUrl => ApiUrl;

        public async virtual Task PostLoad()
        {
            await Task.CompletedTask;
        }
        protected async Task WaitForRender()
        {
            _needsRendering = true;
            StateHasChanged();
            while (_needsRendering)
            {
                await Task.Delay(50);
            }
        }
        protected override void OnAfterRender(bool firstRender)
        {
            _needsRendering = false;
        }


        public virtual async Task Load(Guid? selectedUid = null)
        {
            Blocker.Show();
            await this.WaitForRender();
            try
            {
                var result = await FetchData();
                if (result.Success)
                {
                    this.Data = result.Data;
                    if (Table != null)
                    {
                        Table.Data = this.Data;
                        if (selectedUid != null && selectedUid.Value != Guid.Empty)
                        {
                            int index = result.Data.FindIndex(x => x.Uid == selectedUid);
                            if (index >= 0)
                            {
                                this.Table.SetSelectedIndex(index);
                            }
                        }
                    }
                }
                await PostLoad();
            }
            finally
            {
                HasData = this.Data?.Any() == true;
                this.Loaded = true;
                Blocker.Hide();
                await this.WaitForRender();
            }
        }

        protected virtual Task<RequestResult<List<T>>> FetchData()
        {
            return HttpHelper.Get<List<T>>(FetchUrl);
        }


        protected async Task OnDoubleClick(T item)
        {
            await Edit(item);
        }


        public async Task Edit()
        {
            var selected = Table.GetSelected()?.FirstOrDefault();
            if (selected == null)
                return;
            var changed = await Edit(selected);
            if (changed)
                await this.Load(selected.Uid);
        }

        public abstract Task<bool> Edit(T item);

        public void ShowEditHttpError<U>(RequestResult<U> result, string defaultMessage = "ErrorMessage.NotFound")
        {
            Toast.ShowError(
                result.Success || string.IsNullOrEmpty(result.Body) ? Translater.Instant(defaultMessage) : Translater.TranslateIfNeeded(result.Body),
                duration: 60_000
            );
        }


        public async Task Enable(bool enabled, T item)
        {
#if (DEMO)
            return;
#else
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();
            try
            {
                await HttpHelper.Put<T>($"{ApiUrl}/state/{item.Uid}?enable={enabled}");
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
#endif
        }

        protected virtual string DeleteMessage => "Labels.DeleteItems";

        public async Task Delete()
        {
            var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
            if (uids.Length == 0)
                return; // nothing to delete
            if (await Confirm.Show("Labels.Delete",
                Translater.Instant(DeleteMessage, new { count = uids.Length })) == false)
                return; // rejected the confirm

            Blocker.Show();
            this.StateHasChanged();

            try
            {
#if (!DEMO)
                var deleteResult = await HttpHelper.Delete($"{ApiUrl}", new ReferenceModel { Uids = uids });
                if (deleteResult.Success == false)
                {
                    if(Translater.NeedsTranslating(deleteResult.Body))
                        Toast.ShowError( Translater.Instant(deleteResult.Body));
                    else
                        Toast.ShowError( Translater.Instant("ErrorMessages.DeleteFailed"));
                    return;
                }
#endif

                this.Data = this.Data.Where(x => uids.Contains(x.Uid) == false).ToList();

                await PostDelete();
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
        }

        protected async virtual Task PostDelete()
        {
            await Task.CompletedTask;
        }
    }
}