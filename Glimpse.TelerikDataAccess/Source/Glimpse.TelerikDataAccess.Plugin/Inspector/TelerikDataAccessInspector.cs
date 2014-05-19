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

            inspectorContext = context;
        }

        private static IInspectorContext inspectorContext;
        internal static IExecutionTimer Timer { get { return inspectorContext.TimerStrategy(); } }
        internal static IMessageBroker Broker { get { return inspectorContext.MessageBroker; } }
    }
}