namespace FileFlows.Client.Components
{
    using FileFlows.Shared.Models;
    using Microsoft.AspNetCore.Components;
    using System.Collections.Generic;

    public partial class FlowPanel:ComponentBase
    {
        [Parameter] public List<ElementField> Fields { get; set; }

        [CascadingParameter] public Editor Editor { get; set; }

        protected void UpdateValue(ElementField field, object value) => Editor.UpdateValue(field, value);

        protected T GetParameter<T>(ElementField field, string parameter, T @default = default(T)) => Editor.GetParameter<T>(field, parameter, @default);

        protected T GetValue<T>(string field, T @default = default(T)) => Editor.GetValue<T>(field, @default);

        protected string TypeName => Editor.TypeName;

    }
}
