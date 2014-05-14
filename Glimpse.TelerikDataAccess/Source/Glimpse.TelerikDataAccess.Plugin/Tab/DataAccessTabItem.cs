using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glimpse.TelerikDataAccess.Plugin.Model;
using Glimpse.Core.Tab.Assist;
using Glimpse.Core.Message;

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
       
        public Guid Id { get; set; }

        public IEnumerable<object> Details { get; set; }

        public TimelineCategoryItem Category { get; set; }

        internal void ToTimeline(Glimpse.Core.Extensibility.IMessageBroker broker)
        {
            if ((this.Kind & Kind.Done) == 0)
            {
                string name = this.Action;
                if (this.Kind == Model.Kind.Scalar ||
                    this.Kind == Model.Kind.NonQuery ||
                    this.Kind == Model.Kind.Sql || 
                    this.Kind == Model.Kind.Batch || 
                    this.Kind == Model.Kind.None)
                    name = this.Text;
                broker.Publish(new DataAccessTimelineMessage(this.Id)
                {
                    Offset = this.Offset,
                    Duration = this.Duration,
                    EventCategory = this.Category,
                    EventName = name,
                    EventSubText = (Connection ?? "")
                });
            }
        }
    }
}
