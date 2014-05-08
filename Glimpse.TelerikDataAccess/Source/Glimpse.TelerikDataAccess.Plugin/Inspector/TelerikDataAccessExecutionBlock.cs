using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glimpse.Core.Framework.Support;
using Glimpse.TelerikDataAccess.Plugin.Tracing;

namespace Glimpse.TelerikDataAccess.Plugin.Inspector
{
    internal class TelerikDataAccessExecutionBlock : ExecutionBlockBase
    {
        public static readonly TelerikDataAccessExecutionBlock Instance = new TelerikDataAccessExecutionBlock();

        private TelerikDataAccessExecutionBlock()
        {
            Util.Interfacer.WireUp("OpenAccessRuntime.Intellitrace:tracerImpl", RuntimeTracer.Instance);
        }
    }
}
