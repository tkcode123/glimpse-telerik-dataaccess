using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glimpse.TelerikDataAccess.Plugin.Tab
{
    class DataAccessTabStatistics
    {
        public int ConnectionCount { get; set; }
        public int QueryCount { get; set; }
        public int TransactionCount { get; set; }
        public int Rows { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public TimeSpan ConnectionOpenTime { get; set; }
        public int SecondLevelHits { get; set; }
        public int SecondLevelObjects { get; set; }
        public int SplittedCount { get; set; }

        public string GCCounts { get; set; }
    }
}
