namespace ViWatcher.Server.Controllers
{
    using System;
    using System.Diagnostics;    
    using Microsoft.AspNetCore.Mvc;
    using ViWatcher.Server;
    using ViWatcher.Shared;
    using ViWatcher.Shared.Models;
    using ViWatcher.Server.Helpers;

    [Route("/api/flow")]
    public class FlowController : Controller
    {

        [HttpGet("elements")]
        public IEnumerable<FlowElement> GetElements()
        {
            var tNode = typeof(ViWatcher.Shared.Nodes.Node);
            var assmebly = tNode.Assembly;
            return assmebly.GetTypes()
                           .Where(x => x.IsSubclassOf(tNode))
                           .Select(x => 
                           {
                               FlowElement element = new FlowElement();
                               element.Group = x.Namespace.Substring(x.Namespace.LastIndexOf(".") + 1);
                               element.Name = x.Name;
                               element.Uid = x.FullName;
                               element.Fields = new ();
                               foreach(var prop in x.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)){
                                   var attribute = prop.GetCustomAttributes(typeof(ViWatcher.Shared.Attributes.FormInputAttribute), false).FirstOrDefault() as ViWatcher.Shared.Attributes.FormInputAttribute;
                                   if(attribute != null){
                                       element.Fields.Add(new FlowElementField
                                       {
                                           Name = prop.Name,
                                           Order = attribute.Order,
                                           InputType = attribute.InputType,
                                           Type = prop.PropertyType.FullName
                                       });
                                   }
                               }

                               //(ViWatcher.Shared.Nodes.Node)Activator.CreateInstance(x);
                               return element;
                           });


            // var results = DbHelper.Select<FlowElement>().ToArray();

            // if(results.Length == 0){
            //     return new[] {
            //         new FlowElement { Uid = Guid.NewGuid(), Inputs = 0, Outputs = 1, Name = "Video File", Type = FlowElementType.Input },
            //         new FlowElement { Uid = Guid.NewGuid(), Inputs = 1, Outputs = 2, Name = "Function", Type = FlowElementType.Logic },
            //         new FlowElement { Uid = Guid.NewGuid(), Inputs = 1, Outputs = 1, Name = "h265", Type = FlowElementType.Process },
            //         new FlowElement { Uid = Guid.NewGuid(), Inputs = 1, Outputs = 1, Name = "AC3", Type = FlowElementType.Process },
            //         new FlowElement { Uid = Guid.NewGuid(), Inputs = 1, Outputs = 0, Name = "Output File", Type = FlowElementType.Output },
            //     };
            // }
            // return results;
        }

        [HttpGet("one")]
        public Flow Get() => DbHelper.Single<Flow>();

        [HttpPut]
        public Flow Save([FromBody] Flow model)
        {
            if(model == null)
                throw new Exception("No model");
            var flow = DbHelper.Update<Flow>(model);
            return flow;
        }
    }

}