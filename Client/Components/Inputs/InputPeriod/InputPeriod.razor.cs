using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input for a period, time in minutes
/// </summary>
public partial class InputPeriod : Input<int>
{
    public override bool Focus() => FocusUid();

    private bool UpdatingValue = false;

    private int Period { get; set; }

    private int Number { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        this.Period = 1440;

        if (Value > 0)
        {
            foreach (int p in new [] { 10080, 1440, 60, 1})
            {
                if (Value % p == 0)
                {
                    // weeks
                    Number = Value / p;
                    Period = p;
                    break;
                }
            }
        }
        else
        {
            Number = 3;
            Period = 1440;
            Value = Number * Period;
        }
    }
    

    private async Task ChangeValue(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString() ?? "", out int value) == false)
            return;
        
        int max = Period switch
        {
            1 => 100_000,
            60 => 10_000,
            1440 => 10_000,
            _ => 1_000
        };
        if (value > max)
            value = max;
        else if (value < 1)
            value = 1;
        this.Number = value;
        UpdatingValue = true;
        this.Value = this.Number * this.Period;
        this.ClearError();
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == "Enter")
            await OnSubmit.InvokeAsync();
        else if (e.Code == "Escape")
            await OnClose.InvokeAsync();
    }
    
    
    private void PeriodSelectionChanged(ChangeEventArgs args)
    {
        UpdatingValue = true;
        try
        {
            if (int.TryParse(args?.Value?.ToString(), out int index))
            {
                Period = index;
                Value = Number * Period;
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