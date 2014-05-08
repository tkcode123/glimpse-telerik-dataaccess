using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glimpse.TelerikDataAccess.Plugin.Model;
using Glimpse.Core.Tab.Assist;

namespace Glimpse.TelerikDataAccess.Plugin.Tab
{
    public class DataAccessTabItem
    {        
        public DataAccessTabItem() { }

        public string Text { get; set; }

        public TimeSpan Duration { get; set; }

        public TimeSpan Offset { get; set; }

        public int Ordinal { get; set; }

        public int? Transaction { get; set; }

        public string Connection { get; set; }

        public string Action { get { return Kind.ToString(); } }

        internal Kind Kind { get; set; }

        public TimeSpan? FetchDuration { get; set; }

        public int? Rows { get; set; }

        public List<object> Details { get; set; }
    }
}
