namespace ViWatcher.Server.Controllers
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using ViWatcher.Shared.Models;
    using ViWatcher.Server.Helpers;
    using System.ComponentModel;
    using System.Dynamic;
    using ViWatcher.Plugins;
    using ViWatcher.Plugins.Attributes;

    [Route("/api/flow")]
    public class FlowController : Controller
    {

        [HttpGet("elements")]
        public IEnumerable<FlowElement> GetElements()
        {
            var nodeTypes = Helpers.NodeHelper.NodeTypes;
            List<FlowElement> elements = new List<FlowElement>();
            foreach (var x in nodeTypes)
            {

                FlowElement element = new FlowElement();
                element.Group = x.Namespace.Substring(x.Namespace.LastIndexOf(".") + 1);
                element.Name = x.Name;
                element.Uid = x.FullName;
                element.Fields = new();
                var instance = (Node)Activator.CreateInstance(x);
                element.Inputs = instance.Inputs;
                element.Outputs = instance.Outputs;

                // if(x.IsAssignableFrom(typeof(IConfigurableInputNode)) || x.IsAssignableFrom(typeof(IInputNode)))
                // {
                //    element.Inputs = dValue == null ? 1 : (int)dValue.Value;
                // }

                // if(x.IsAssignableFrom(typeof(IConfigurableOutputNode)) || x.IsAssignableFrom(typeof(IOutputNode)))
                // {
                //    var dValue = x.GetProperty(nameof(IOutputNode.Outputs)).GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute;
                //    element.Outputs = dValue == null ? 1 : (int)dValue.Value;
                // }
                var model = new ExpandoObject(); ;
                var dict = (IDictionary<string, object>)model;
                element.Model = model;

                foreach (var prop in x.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    var attribute = prop.GetCustomAttributes(typeof(FormInputAttribute), false).FirstOrDefault() as FormInputAttribute;
                    if (attribute != null)
                    {
                        element.Fields.Add(new ElementField
                        {
                            Name = prop.Name,
                            Order = attribute.Order,
                            InputType = attribute.InputType,
                            Type = prop.PropertyType.FullName
                        });
                        var dValue = prop.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute;
                        dict.Add(prop.Name, dValue != null ? dValue.Value : prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null);
                    }

                    if (prop.Name == nameof(element.Inputs))
                    {
                        var dValue = prop.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute;
                        if (dValue != null)
                            element.Inputs = (int)dValue.Value;
                    }
                    if (prop.Name == nameof(element.Outputs))
                    {
                        var dValue = prop.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute;
                        if (dValue != null)
                            element.Outputs = (int)dValue.Value;
                    }
                }

                //(ViWatcher.Shared.Nodes.Node)Activator.CreateInstance(x);
                elements.Add(element);
            }
            return elements;
        }

        [HttpGet("one")]
        public Flow Get() => DbHelper.Single<Flow>();

        [HttpPut]
        public Flow Save([FromBody] Flow model)
        {
            if (model == null)
                throw new Exception("No model");
            var flow = DbHelper.Update<Flow>(model);
            return flow;
        }

        [HttpPost("execute")]
        public async Task<string> Execute(string input)
        {
            var executor = new Helpers.FlowExecutor();
            executor.Flow = Get();
            var result = await executor.Run(input);
            return result.Logger.ToString();
        }
    }

}