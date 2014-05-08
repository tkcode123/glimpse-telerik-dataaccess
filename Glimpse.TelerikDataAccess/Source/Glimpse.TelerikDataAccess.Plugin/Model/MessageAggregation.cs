using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glimpse.TelerikDataAccess.Plugin.Model
{
    class MessageAggregation
    {
        public MessageAggregation()
        {
            Connections = new Dictionary<string, ConnectionInfo>();
            Commands = new Dictionary<string, CommandInfo>();
            Transactions = new Dictionary<string, TransactionInfo>(); 
        }
         
        public IDictionary<string, ConnectionInfo> Connections { get; private set; }

        public IDictionary<string, CommandInfo> Commands { get; private set; }

        public IDictionary<string, TransactionInfo> Transactions { get; private set; } 

        public IList<string> Warnings { get; private set; }
    }
}
