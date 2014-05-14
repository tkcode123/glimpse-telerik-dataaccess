using Glimpse.Core.Extensibility;

namespace Glimpse.TelerikDataAccess.Plugin.Tracing
{
    class TracingContextFactory
    {        
        private static readonly object locker = new object();

        private IInspectorContext InspectorContext { get; set; }
        public IExecutionTimer Timer { get { return InspectorContext.TimerStrategy(); } }
        public IMessageBroker Broker { get { return InspectorContext.MessageBroker; } }

        private TracingContextFactory(IInspectorContext context)
        {
            InspectorContext = context;
        }

        public static void SetOperationContextFactory(IInspectorContext ctx)
        {
            lock (locker)
            {
                Current = new TracingContextFactory(ctx);
            }
        }

        public static TracingContextFactory Current { get; private set; }
    }
}
