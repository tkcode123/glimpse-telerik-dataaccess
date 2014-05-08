using Glimpse.Core.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glimpse.TelerikDataAccess.Plugin.Tracing
{
    class TracingContext
    {
        public IInspectorContext InspectorContext { get; private set; }
        internal TracingContext(IInspectorContext context)
        {
            InspectorContext = context;
        }
    }
}
