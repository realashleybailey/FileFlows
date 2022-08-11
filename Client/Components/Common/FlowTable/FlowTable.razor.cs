using System.ComponentModel.Design;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileFlows.Client.Components.Common;

public partial class FlowTable<TItem>: ComponentBase,IDisposable
{
    private readonly string Uid = Guid.NewGuid().ToString();
    private Dictionary<TItem, string> DataDictionary;
    private List<TItem> _Data;
    [Parameter]
    public List<TItem> Data // the original data, not filtered
    {
        get => this._Data;
        set
        {
            if (this._Data == value)
                return;
            SetData(value);
        }
    }

    /// <summary>
    /// Gets or sets a table identifier for saving/loading the column information
    /// </summary>
    [Parameter] public string TableIdentifier { get; set; }

    public void SetData(List<TItem> value, bool clearSelected = true)
    {
        this._FilterText = string.Empty;
        this._Data = value ?? new();
        var jsonOptions = new System.Text.Json.JsonSerializerOptions()
        {   
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        this.DataDictionary = this._Data.ToDictionary(x => x, x =>
        {
            string result = JsonSerializer.Serialize(x, jsonOptions);  
            return result.ToLowerExplicit();
        });
        FilterData(clearSelected : clearSelected);
        
        // need to reselect the items here!
        var keys = this.SelectedItems.Select(x =>
        {
            if (x is IUniqueObject<Guid> unique)
                return unique.Uid as Guid?;
            return null;
        }).Where(x => x != null).Select(x => x.Value).ToList();
        
        var selection = this.Data.Where(x =>
        {
            if (x is IUniqueObject<Guid> unique && keys.Contains(unique.Uid))
                return true;
            return false;
        }).ToList();
        this.SelectedItems.AddRange(selection);
    }

    [Parameter]
    public string MinWidth { get; set; }

    private ElementReference eleFilter { get; set; }

    [Parameter] public EventCallback<TItem> DoubleClick { get; set; }

    private Dictionary<TItem, string> DisplayData = new ();
    private readonly List<TItem> SelectedItems = new ();

    private string CurrentFilter = string.Empty;

    private string _FilterText = string.Empty;
    private string FilterText
    {
        get => _FilterText;
        set
        {
            _FilterText = value ?? string.Empty;
            FilterData();
        }
    }

    [Parameter] public SelectionMode Selection { get; set; }

    [Parameter]
    public RenderFragment ToolBar { get; set; }
    [Parameter]
    public RenderFragment Columns { get; set; }

    [Inject] IJSRuntime jsRuntime{ get; set; }
    [Inject] IHotKeysService HotKeyService { get; set; }

    List<FlowTableColumn<TItem>> ColumnList = new ();
    List<FlowTableButton<TItem>> Buttons = new();

    private TItem LastSelected;

    public delegate void SelectionChangedEvent(List<TItem> items);
    public event SelectionChangedEvent SelectionChanged;

    private string lblFilter;

    public IEnumerable<TItem> GetSelected() => new List<TItem>(this.SelectedItems); // clone the list, dont give them the actual one

    private string FlowTableHotkey;

    protected override void OnInitialized()
    {
        FlowTableHotkey = Guid.NewGuid().ToString();
        lblFilter = Translater.Instant("Labels.FilterPlaceholder");
        HotKeyService.RegisterHotkey(FlowTableHotkey, "/", callback: () =>
        {
            Task.Run(async () =>
            {
                bool editorOpen = await jsRuntime.InvokeAsync<int>("eval", "document.querySelectorAll('.editor-wrapper').length") > 0;
                if (editorOpen)
                    return;
                await eleFilter.FocusAsync();
            });
        });
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        //if (firstRender)
        {
            Task.Run(async () =>
            {
                await Task.Delay(500);
                await jsRuntime.InvokeVoidAsync("ff.resizableTable", this.Uid, this.TableIdentifier);
            });
        }

        return base.OnAfterRenderAsync(firstRender);
    }

    private async Task ResetLayout()
    {
        await jsRuntime.InvokeVoidAsync("ff.resetTable", this.Uid, this.TableIdentifier);
    }

    public void Dispose()
    {
        HotKeyService.DeregisterHotkey(FlowTableHotkey);
    }

    internal void AddColumn(FlowTableColumn<TItem> col)
    {
        if (ColumnList.Contains(col) == false)
            ColumnList.Add(col);
    }
    internal void AddButton(FlowTableButton<TItem> button)
    {
        if (Buttons.Contains(button) == false)
            Buttons.Add(button);
    }


    public virtual void SelectAll(ChangeEventArgs e)
    {
        bool @checked = e.Value as bool? == true;
        this.SelectedItems.Clear();
        if (@checked && this.DisplayData?.Any() == true)
            this.SelectedItems.AddRange(this.DisplayData.Keys);
        this.NotifySelectionChanged();
    }

    internal async Task SetSelectedIndex(int index)
    {
        if (index < 0 || index > this.Data.Count - 1)
            return;
        var item = this.Data[index];
        if (SelectedItems.Contains(item) == false)
        {
            this.SelectedItems.Add(item);
            this.NotifySelectionChanged();
        }
    }

    /// <summary>
    /// Selects a single item
    /// </summary>
    /// <param name="item">the item to select</param>
    internal void SelectItem(TItem item)
    {
        this.SelectedItems.Clear();
        this.SelectedItems.Add(item);
        this.StateHasChanged();
    }

    /// <summary>
    /// Calls StateHasChanged on the component
    /// </summary>
    public void TriggerStateHasChanged() => this.StateHasChanged();

    private void CheckItem(ChangeEventArgs e, TItem item)
    {
        bool @checked = e.Value as bool? == true;
        if (@checked)
        {
            if (this.SelectedItems.Contains(item) == false)
            {
                this.SelectedItems.Add(item);
                this.NotifySelectionChanged();
            }
        }
        else if (this.SelectedItems.Contains(item))
        {
            this.SelectedItems.Remove(item);
            this.NotifySelectionChanged();
        }
    }

    private void FilterData(bool clearSelected = true)
    {
        if (clearSelected && this.SelectedItems.Any())
        {
            this.SelectedItems.Clear();
            this.NotifySelectionChanged();
        }
        string filter = this.FilterText.ToLowerExplicit();
        if (filter == string.Empty)
            this.DisplayData = this.DataDictionary;
        else if (filter.StartsWith(CurrentFilter))
        {
            // filtering the same set
            this.DisplayData = this.DisplayData.Where(x => x.Value.Contains(filter))
                                   .ToDictionary(x => x.Key, x => x.Value);
        }
        else
        {
            this.DisplayData = this.DataDictionary.Where(x => x.Value.Contains(filter))
                                   .ToDictionary(x => x.Key, x => x.Value);
        }
        CurrentFilter = filter;
    }

    private async Task OnClick(MouseEventArgs e, TItem item)
    {
        bool changed = false;
        bool wasSelected = this.SelectedItems.Contains(item);
        if (e.CtrlKey == false && e.ShiftKey == false)
        {
            // just select/unselect this one
            if(wasSelected && this.SelectedItems.Count == 1)
                this.SelectedItems.Clear();
            else
            {
                this.SelectedItems.Clear();
                this.SelectedItems.Add(item);
            }
            changed = true;
        }
        else if (e.CtrlKey)
        {
            // multiselect changing one item
            if (wasSelected)
            {
                this.SelectedItems.Remove(item);
            }
            else
            {
                this.SelectedItems.Add(item);
            }
            changed = true;
        }
        else
        {

            if (wasSelected == false)
            {
                this.SelectedItems.Add(item);
                changed = true;
            }

            if (this.LastSelected != null && e.ShiftKey)
            {
                // select everything in between
                int last = this.Data.IndexOf(this.LastSelected);
                int current = this.Data.IndexOf(item);
                int start = last > current ? current : last;
                int end = last > current ? last : current;

                bool unselecting = e.CtrlKey;
                for (int i = start; i <= end && i < Data.Count - 1; i++)
                {
                    var tItem = this.Data[i];
                    if (unselecting)
                    {
                        if (this.SelectedItems.Contains(tItem))
                        {
                            this.SelectedItems.Remove(tItem);
                            changed = true;
                        }
                    }
                    else if (this.SelectedItems.Contains(tItem) == false)
                    {
                        this.SelectedItems.Add(tItem);
                        changed = true;
                    }
                }
            }
        }

        if(changed)
            this.NotifySelectionChanged();

        this.LastSelected = item;
    }

    private async Task OnDoubleClick(TItem item)
    {
        this.SelectedItems.Clear();
        this.SelectedItems.Add(item);
        this.NotifySelectionChanged();
        await this.DoubleClick.InvokeAsync(item);
    }

    private void NotifySelectionChanged()
    {
        if (SelectionChanged != null)
            SelectionChanged(new (SelectedItems)); // we want a clone of the list, not one they can modify 
    }

    private async Task FilterKeyDown(KeyboardEventArgs args)
    {
        if(args.Key == "Escape")
        {
            this.FilterText = String.Empty;
        }
    }
}
public enum SelectionMode
{
    None,
    Single,
    Multiple
}