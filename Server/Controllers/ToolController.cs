namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;

    [Route("/api/tool")]
    public class ToolController : ControllerStore<Tool>
    {
        [HttpGet]
        public async Task<IEnumerable<Tool>> GetAll() => (await GetDataList()).OrderBy(x => x.Name);

        [HttpGet("{uid}")]
        public Task<Tool> Get(Guid uid) => GetByUid(uid);

        [HttpGet("name/{name}")]
        public async Task<Tool?> GetByName(string name)
        {
            var list = await GetData();
            name = name.ToLower().Trim();
            return list.Values.FirstOrDefault(x => x.Name.ToLower() == name);
        }

        [HttpPost]
        public Task<Tool> Save([FromBody] Tool tool) => Update(tool, checkDuplicateName: true);

        [HttpDelete]
        public Task Delete([FromBody] ReferenceModel model) => DeleteAll(model);
    }
}