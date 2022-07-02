using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Shared.Portlets;

namespace FileFlows.Client.Pages;


public partial class Dashboard 
{
    private async Task AddPortletDialog()
    {
        List<ElementField> fields = new List<ElementField>();
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.CpuUsage),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.TimeSeries } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.MemoryUsage),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.TimeSeries } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.LogStorage),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.TimeSeries } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.TempStorage),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.TimeSeries } 
            }
        });
        
        
        var result = await Editor.Open("Pages.Portlet", "Pages.Portlet.Title", fields, null);
        
        
    }
}