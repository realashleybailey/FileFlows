using FileFlows.Client.Components;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

using FileFlows.Shared.Models;

public partial class StatisticsPage : ComponentBase
{
    [CascadingParameter] Blocker Blocker { get; set; }

    protected Statistics Model { get; set; }

    private readonly List<NodeDataModel> NodeData = new ();

    private bool TelemetryDisabled { get; set; }

    protected override async Task OnInitializedAsync()
    {
#if (!DEMO)
        var response = await HttpHelper.Get<Statistics>("/api/statistics");
        if (response.Success)
        {
            this.Model = response.Data;
            if(this.Model.Uid == Guid.Empty)
            {
                TelemetryDisabled = true;
            }
        }
        this.Model ??= new Statistics();
        InitNodeData();
#endif
        Blocker.Hide();
    }

    private void InitNodeData()
    {
        foreach (var node in this.Model.ExecutedNodes)
        {
            string type = node.Value.Uid.Substring(node.Value.Uid.LastIndexOf(".") + 1);
            string name = Translater.Instant($"Flow.Parts.{type}.Label", supressWarnings: true);
            if (name == "Label")
                name = Helpers.FlowHelper.FormatLabel(type);

            foreach (var output in node.Value.Outputs.GroupBy(x => x.Output))
            {
                NodeData.Add(new NodeDataModel
                {
                    Name = name,
                    Output = output.Key,
                    Total = output.Count(),
                    Duration = new TimeSpan(output.Sum(x => x.Duration.Ticks))
                });
            }
        }
    }


    class NodeDataModel
    {
        public string Name { get; set; }
        public int Output { get; set; }
        public int Total { get; set; }

        public TimeSpan Duration { get; set; }
    }
}
