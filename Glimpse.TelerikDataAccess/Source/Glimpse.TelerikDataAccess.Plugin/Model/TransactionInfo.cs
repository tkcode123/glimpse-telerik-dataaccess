using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Glimpse.TelerikDataAccess.Plugin.Model
{
    class TransactionInfo
    {
        public bool? Committed { get; set; }

        public IsolationLevel IsolationLevel { get; set; }
    }
}
