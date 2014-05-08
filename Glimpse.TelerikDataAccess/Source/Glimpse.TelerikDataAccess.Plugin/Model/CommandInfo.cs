using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glimpse.TelerikDataAccess.Plugin.Model
{
    class CommandInfo
    {
        public string Command { get; set; }
        public Exception Exception { get; set; }
        public int? RecordsAffected { get; set; }
        public int? TotalRecords { get; set; }
        public bool IsDuplicate { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool HasTransaction { get; set; }
        public TransactionInfo HeadTransaction { get; set; }
        public TransactionInfo TailTransaction { get; set; }
        public bool IsAsync { get; set; }
        public DateTime Offset { get; set; }
        public List<ParameterInfo> Parameters { get; set; }
    }
}
