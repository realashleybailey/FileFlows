using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;

namespace FileFlows.Client.Shared;

public partial class MainLayout : LayoutComponentBase
{
    public NavMenu Menu { get; set; }
    public Blocker Blocker { get; set; }
    public Editor Editor { get; set; }
    [Inject] private Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; }

    public static MainLayout Instance { get; private set; }

    private bool SearchVisible = false;

    public MainLayout()
    {
        Instance = this;
    }

    protected override async Task OnInitializedAsync()
    {
        App.Instance.NavMenuCollapsed = await LocalStorage.GetItemAsync<bool>("NavMenuCollapsed");
    }

    private void ToggleExpand()
    {
        App.Instance.NavMenuCollapsed = !App.Instance.NavMenuCollapsed;
        LocalStorage.SetItemAsync("NavMenuCollapsed", App.Instance.NavMenuCollapsed);
    }

    public void ShowSearch()
    {
        SearchVisible = true;
        this.StateHasChanged();
    }

    public void HideSearch()
    {
        SearchVisible = false;
        this.StateHasChanged();
        
    }
}