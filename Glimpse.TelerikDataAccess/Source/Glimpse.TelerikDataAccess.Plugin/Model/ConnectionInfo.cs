using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glimpse.TelerikDataAccess.Plugin.Model
{
    class ConnectionInfo
    {
        public Dictionary<int, CommandInfo> Commands { get; set; }

        public Dictionary<int, TransactionInfo> Transactions { get; set; }

        public TimeSpan? Duration { get; set; }
    }
}
