using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Resource;

namespace Glimpse.TelerikDataAccess.Plugin.Resource
{
    public class HtmlResource : FileResource
    {
        public HtmlResource()
        {
            Name = "dataaccess_html";
        }
       
        protected override EmbeddedResourceInfo GetEmbeddedResourceInfo(IResourceContext context)
        {
            var sub = context.Parameters.ContainsKey("sub") ? context.Parameters["sub"] : "help";
            if ("store".Equals(sub) && context.Parameters.ContainsKey("name"))
            {
                var data = context.PersistenceStore.GetByRequestIdAndTabKey(Guid.Parse(context.Parameters["requestId"]), "glimpse_dataaccess");
                var serializer = new JsonNetSerializer(context.Logger);
                var jsonData = serializer.Serialize(data);
                File.WriteAllText(context.Parameters["name"], jsonData, Encoding.UTF8);

                return new EmbeddedResourceInfo(
                   this.GetType().Assembly,
                   "Glimpse.TelerikDataAccess.Plugin.Resource.store.html",
                   "text/html");
            }
            if ("glimpse1.png".Equals(sub))
            {
                return new EmbeddedResourceInfo(
                            this.GetType().Assembly,
                           "Glimpse.TelerikDataAccess.Plugin.Resource.glimpse1.png",
                           "text/html");
            }
            if ("glimpse2.png".Equals(sub))
            {
                return new EmbeddedResourceInfo(
                            this.GetType().Assembly,
                           "Glimpse.TelerikDataAccess.Plugin.Resource.glimpse2.png",
                           "text/html");
            }
            return new EmbeddedResourceInfo(
               this.GetType().Assembly,
               "Glimpse.TelerikDataAccess.Plugin.Resource.help.html",
               "text/html");
        }

        public override IEnumerable<ResourceParameterMetadata> Parameters
        {
            get
            {
                return new ResourceParameterMetadata[0];
            }
        }

        protected override int CacheDuration
        {
            get { return 0; }
        }
    }
}
