namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;

    [Route("/api/library")]
    public class LibraryController : ControllerStore<Library>
    {
        [HttpGet]
        public async Task<IEnumerable<Library>> GetAll() => (await GetDataList()).OrderBy(x => x.Name);

        [HttpGet("{uid}")]
        public Task<Library> Get(Guid uid) => GetByUid(uid);

        [HttpPost]
        public Task<Library> Save([FromBody] Library library) => base.Update(library, checkDuplicateName: true);

        [HttpPut("state/{uid}")]
        public async Task<Library> SetState([FromRoute] Guid uid, [FromQuery] bool enable)
        {
            var library = await GetByUid(uid);
            if (library == null)
                throw new Exception("Library not found.");
            if (library.Enabled != enable)
            {
                library.Enabled = enable;
                return await Update(library);
            }
            return library;
        }

        [HttpDelete]
        public Task Delete([FromBody] ReferenceModel model) => DeleteAll(model);

        internal async Task UpdateFlowName(Guid uid, string name)
        {
            var libraries = await GetDataList();
            foreach (var lib in libraries.Where(x => x.Flow?.Uid == uid))
            {
                lib.Flow.Name = name;
                await Update(lib);
            }
        }
    }
}