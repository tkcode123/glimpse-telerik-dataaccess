using System.Collections.Generic;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Resource;

namespace Glimpse.TelerikDataAccess.Plugin.Resource
{
    public class JsResource : FileResource, IDynamicClientScript
    {
        public JsResource() 
        {
            Name = GetResourceName();
        }

        public string GetResourceName()
        {
            return "dataaccess_js";
        }

        public ScriptOrder Order 
        {
            get { return ScriptOrder.IncludeAfterRequestDataScript; }
        }

        protected override EmbeddedResourceInfo GetEmbeddedResourceInfo(IResourceContext context)
        {
            return new EmbeddedResourceInfo(
               this.GetType().Assembly,
               "Glimpse.TelerikDataAccess.Plugin.Resource.dataaccess.js",
               "application/x-javascript");
        }

        public override IEnumerable<ResourceParameterMetadata> Parameters
        {
            get { return base.Parameters; }
        }
    }
}