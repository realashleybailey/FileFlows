namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;

    [Route("/api/tool")]
    public class ToolController : Controller
    {
        [HttpGet]
        public IEnumerable<Tool> GetAll()
        {
            return DbHelper.Select<Tool>();
        }

        [HttpGet("{uid}")]
        public Tool Get(Guid uid)
        {
            return DbHelper.Single<Tool>(uid);
        }

        [HttpGet("{uid}")]
        public Tool GetByName(string name)
        {
            return DbHelper.SingleByName<Tool>(name);
        }

        [HttpPost]
        public Tool Save([FromBody] Tool library)
        {
            var duplicate = DbHelper.Single<Tool>("lower(name) = lower(@1) and uid <> @2", library.Name, library.Uid.ToString());
            if (duplicate != null && duplicate.Uid != Guid.Empty)
                throw new Exception("ErrorMessages.NameInUse");

            return DbHelper.Update(library);
        }

        [HttpDelete]
        public void Delete([FromBody] ReferenceModel model)
        {
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete
            DbHelper.Delete<Tool>(model.Uids);
        }
    }
}