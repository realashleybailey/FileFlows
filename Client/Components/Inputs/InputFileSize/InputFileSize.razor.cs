using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input for a file size
/// </summary>
public partial class InputFileSize : Input<int>
{
    public override bool Focus() => FocusUid();

    private bool UpdatingValue = false;

    private int Unit { get; set; }

    private int Number { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        this.Unit = 1440;

        if (Value > 0)
        {
            foreach (int p in new [] { 1000000000, 1000000, 1000, 1})
            {
                if (Value % p == 0)
                {
                    // weeks
                    Number = Value / p;
                    Unit = p;
                    break;
                }
            }
        }
        else
        {
            Number = 10;
            Unit = 1000000;
            Value = Number * Unit;
        }
    }
    

    private async Task ChangeValue(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString() ?? "", out int value) == false)
            return;
        
        int max = Unit switch
        {
            1 => 1_000_000,
            1_000 => 100_000,
            1_000_000 => 10_000,
            _ => 1000
        };
        if (value > max)
            value = max;
        else if (value < 1)
            value = 1;
        this.Number = value;
        UpdatingValue = true;
        this.Value = this.Number * this.Unit;
        this.ClearError();
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == "Enter")
            await OnSubmit.InvokeAsync();
        else if (e.Code == "Escape")
            await OnClose.InvokeAsync();
    }
    
    
    private void UnitSelectionChanged(ChangeEventArgs args)
    {
        UpdatingValue = true;
        try
        {
            if (int.TryParse(args?.Value?.ToString(), out int index))
            {
                Unit = index;
                Value = Number * Unit;
            }
            else
                Logger.Instance.DLog("Unable to find index of: ",  args?.Value);
        }
        finally
        {
            UpdatingValue = false;
        }
    }

}