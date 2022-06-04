using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

public partial class InputTable:Input<object>
{
    [Parameter] public Type TableType { get; set; }
    [Parameter] public List<InputTableColumn> Columns { get; set; }

    private  Dictionary<string, PropertyInfo> Properties = new();

    public IList Data;

    protected override void OnInitialized()
    {
        Properties = TableType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(x => x.Name, x => x);
        
        
        
        Data = Value as IList;
        if (Data == null)
        {
            Type genericListType = typeof(List<>).MakeGenericType(TableType);
            Data = (IList)Activator.CreateInstance(genericListType);
        }
    }

    private string GetValueString(InputTableColumn column, object value)
    {
        if (value == null || string.IsNullOrWhiteSpace(column.Property) || Properties.ContainsKey(column.Property) == false)
            return string.Empty;
        var prop = Properties[column.Property];
        object pValue = prop.GetValue(value);
        if (pValue == null)
            return string.Empty;
        return pValue.ToString();
    }
}

public class InputTableColumn
{
    public string Name { get; set; }
    public string Width { get; set; }
    public string Property { get; set; }
}