using System;
using Glimpse.Core.Framework.Support;
using Glimpse.TelerikDataAccess.Plugin.Tracing;

namespace Glimpse.TelerikDataAccess.Plugin.Inspector
{
    internal class TelerikDataAccessExecutionBlock : ExecutionBlockBase
    {
        public static readonly TelerikDataAccessExecutionBlock Instance = new TelerikDataAccessExecutionBlock();

        private TelerikDataAccessExecutionBlock()
        {   // Executed only once: we wire up our runtime tracer instance to the one actually used by OpenAccess.
            try
            {
                Tracing.Interfacer.WireUp("Telerik.OpenAccess.SPI.TraceBase:Instance", RuntimeTracer.Instance);
                return;
            }
            catch (ArgumentException) { } // older runtime
            Tracing.Interfacer.WireUp("OpenAccessRuntime.Intellitrace:tracerImpl", RuntimeTracer.Instance);
        }
    }
}
