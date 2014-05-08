using Glimpse.Core.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glimpse.TelerikDataAccess.Plugin.Tracing
{
    class TracingContextFactory
    {        
        private static readonly object locker = new object();

        private IInspectorContext InspectorContext { get; set; }
        public IExecutionTimer Timer { get { return InspectorContext.TimerStrategy(); } }
        public IMessageBroker Broker { get { return InspectorContext.MessageBroker; } }

        public TracingContextFactory(IInspectorContext context)
        {
            InspectorContext = context;
        }
        
        public static void SetOperationContextFactory(TracingContextFactory factory)
        {
            lock (locker)
            {
                Current = factory;
            }
        }

        public static TracingContextFactory Current { get; private set; }

        public TracingContext Create()
        {
            return new TracingContext(InspectorContext);
        }
    }
}
