namespace FileFlows.Server.Services;

using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// Service for communicating with FileFlows server for variables
/// </summary>
public class VariableService : IVariableService
{
    /// <summary>
    /// Gets all variables in the system
    /// </summary>
    /// <returns>all variables in the system</returns>
    public Task<IEnumerable<Variable>> GetAll() => new VariableController().GetAll();
}