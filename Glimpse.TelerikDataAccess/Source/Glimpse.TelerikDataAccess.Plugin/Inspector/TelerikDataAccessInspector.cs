using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glimpse.Core.Extensibility;
using Glimpse.TelerikDataAccess.Plugin.Tracing;

namespace Glimpse.TelerikDataAccess.Plugin.Inspector
{
    public class TelerikDataAccessInspector : IInspector
    {
        public void Setup(IInspectorContext context)
        {   // called one time when GlimpseRuntime is initialized
            TelerikDataAccessExecutionBlock.Instance.Execute();

            TracingContextFactory.SetOperationContextFactory(new TracingContextFactory(context));
        }
    }
}