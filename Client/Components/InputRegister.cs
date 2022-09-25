using FileFlows.Client.Components.Inputs;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

public abstract class InputRegister:ComponentBase
{
    protected readonly List<Inputs.IInput> RegisteredInputs = new List<Inputs.IInput>();


    internal void RegisterInput<T>(Input<T> input)
    {
        if (this.RegisteredInputs.Contains(input) == false)
            this.RegisteredInputs.Add(input);
    }

    // internal void RemoveRegisteredInputs(params string[] except)
    // {
    //     var listExcept = except?.ToList() ?? new();
    //     this.RegisteredInputs.RemoveAll(x => listExcept.Contains(x.Field?.Name ?? string.Empty) == false);
    // }

    // internal Inputs.IInput GetRegisteredInput(string name)
    // {
    //     return this.RegisteredInputs.Where(x => x.Field.Name == name).FirstOrDefault();
    // }

    /// <summary>
    /// Validates the registered inputs
    /// </summary>
    /// <returns>if all inputs are valid</returns>
    protected async Task<bool> Validate()
    {
        bool valid = true;
        foreach (var input in RegisteredInputs)
        {
            if (input.Disabled || input.Visible == false)
                continue; // don't validate hidden or disabled inputs
            bool iValid = await input.Validate();
            Logger.Instance.ILog("Validating: " + input.Label  + " = " + iValid);
            if (iValid == false)
            {
                Logger.Instance.DLog("Invalid input: " + input.Label);
                valid = false;
            }
        }

        return valid;
    }
}