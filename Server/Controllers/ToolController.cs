namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;

    /// <summary>
    /// Tool Controller
    /// </summary>
    [Route("/api/tool")]
    public class ToolController : ControllerStore<Tool>
    {
        /// <summary>
        /// Get all tools configured in the system
        /// </summary>
        /// <returns>A list of all configured tools</returns>
        [HttpGet]
        public async Task<IEnumerable<Tool>> GetAll() => (await GetDataList()).OrderBy(x => x.Name);

        /// <summary>
        /// Get tool
        /// </summary>
        /// <param name="uid">The UID of the tool to get</param>
        /// <returns>The tool instance</returns>
        [HttpGet("{uid}")]
        public Task<Tool> Get(Guid uid) => GetByUid(uid);

        /// <summary>
        /// Get a tool by its name, case insensitive
        /// </summary>
        /// <param name="name">The name of the tool</param>
        /// <returns>The tool instance if found</returns>
        [HttpGet("name/{name}")]
        public async Task<Tool?> GetByName(string name)
        {
            var list = await GetData();
            name = name.ToLower().Trim();
            return list.Values.FirstOrDefault(x => x.Name.ToLower() == name);
        }

        /// <summary>
        /// Saves a tool
        /// </summary>
        /// <param name="tool">The tool to save</param>
        /// <returns>The saved instance</returns>
        [HttpPost]
        public Task<Tool> Save([FromBody] Tool tool) => Update(tool, checkDuplicateName: true);

        /// <summary>
        /// Delete tools from the system
        /// </summary>
        /// <param name="model">A reference model containing UIDs to delete</param>
        /// <returns>an awaited task</returns>
        [HttpDelete]
        public Task Delete([FromBody] ReferenceModel model) => DeleteAll(model);
    }
}