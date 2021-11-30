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
    using Radzen;
    using Radzen.Blazor;

    public abstract class ListPage<T> : ComponentBase where T : ViObject
    {
        public string FilterText { get; set; }
        protected void UpdateFilter()
        {
            //this.DataGrid.LoadData.InvokeAsync();
            LoadData();
        }
        [CascadingParameter] public Blocker Blocker { get; set; }
        [CascadingParameter] public Editor Editor { get; set; }
        [Inject] public NotificationService NotificationService { get; set; }
        public string lblAdd, lblEdit, lblDelete, lblDeleting, lblRefresh;

        public abstract string ApIUrl { get; }


        public List<T> _Data = new List<T>();
        public List<T> Data
        {
            get => _Data;
            set
            {
                _Data = value ?? new List<T>();
                LoadData();
            }
        }

        public IEnumerable<T> DisplayData;
        private RadzenDataGrid<T> _DataGrid;
        public IEnumerable<int> PageSizeOptions = new int[] { 50, 100, 250, 500 };
        public RadzenDataGrid<T> DataGrid
        {
            get => _DataGrid;
            set
            {
                if (_DataGrid == null && value != null)
                {
#pragma warning disable BL0005
                    value.PageSize = 250;
#pragma warning restore BL0005
                }
                _DataGrid = value;
            }
        }


        public IList<T> SelectedItems;


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

        public virtual string FetchUrl => ApIUrl;

        public virtual void SelectAll()
        {
            if (this.SelectedItems?.Any() == true)
                this.SelectedItems = new List<T>();
            else
                this.SelectedItems = this.DisplayData?.ToList() ?? new List<T>();
        }

        public async virtual Task PostLoad()
        {
            await Task.CompletedTask;
        }

        public virtual async Task Load()
        {
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();
            try
            {
                var result = await HttpHelper.Get<List<T>>(FetchUrl);
                if (result.Success)
                {
                    this.Data = result.Data;
                }
                await PostLoad();
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
        }

        protected virtual void LoadData()//LoadDataArgs args)
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                DisplayData = Data;
                return;
            }
            string ft = FilterText.Trim().ToLower();
            DisplayData = Data.Where(x => System.Text.Json.JsonSerializer.Serialize(x).ToLower().Contains(ft));
        }

        public async Task RowDoubleClicked(DataGridRowMouseEventArgs<T> item)
        {
            this.SelectedItems.Clear();
            this.SelectedItems.Add(item.Data);
            await Edit(item.Data);
        }



        public async Task Edit()
        {
            var selected = this.SelectedItems?.FirstOrDefault();
            if (selected == null)
                return;
            await Edit(selected);
        }

        public abstract Task Edit(T item);

        public void ShowEditHttpError<U>(RequestResult<U> result, string defaultMessage = "ErrorMessage.NotFound")
        {
            NotificationService.Notify(NotificationSeverity.Error,
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
                await HttpHelper.Put<T>($"{ApIUrl}/state/{item.Uid}?enable={enabled}");
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
#endif
        }

        public async Task Delete()
        {
            var uids = this.SelectedItems?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
            if (uids.Length == 0)
                return; // nothing to delete
            if (await Confirm.Show("Labels.Delete",
                Translater.Instant("Labels.DeleteItems", new { count = uids.Length })) == false)
                return; // rejected the confirm

            Blocker.Show();
            this.StateHasChanged();

            try
            {
#if (!DEMO)
                var deleteResult = await HttpHelper.Delete($"{ApIUrl}", new ReferenceModel { Uids = uids });
                if (deleteResult.Success == false)
                {
                    NotificationService.Notify(NotificationSeverity.Error, Translater.Instant("ErrorMessages.DeleteFailed"));
                    return;
                }
#endif

                this.SelectedItems.Clear();
                this.Data.RemoveAll(x => uids.Contains(x.Uid));
                await DataGrid.Reload();

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