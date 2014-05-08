using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glimpse.Core.Message;

namespace Glimpse.TelerikDataAccess.Plugin.Model
{
    class DataAccessTimelineMessage : ITimelineMessage
    {
        internal DataAccessTimelineMessage(DataAccessMessage m, TimelineCategoryItem i)
        {
            EventCategory = i;
            EventName = m.EventName ?? m.Kind.ToString();
            EventSubText = m.EventSubText ?? "";
            Duration = m.Duration;
            Offset = m.Offset;
            StartTime = m.StartTime;
            Id = m.Id;
        }

        public TimelineCategoryItem EventCategory
        {
            get;
            set;
        }

        public string EventName
        {
            get;
            set;
        }

        public string EventSubText
        {
            get;
            set;
        }

        public TimeSpan Duration
        {
            get;
            set;
        }

        public TimeSpan Offset
        {
            get;
            set;
        }

        public DateTime StartTime
        {
            get;
            set;
        }

        public Guid Id
        {
            get;
            private set;
        }
    }
}
