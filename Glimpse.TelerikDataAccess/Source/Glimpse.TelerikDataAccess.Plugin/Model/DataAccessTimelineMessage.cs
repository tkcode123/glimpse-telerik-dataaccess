using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glimpse.Core.Message;

namespace Glimpse.TelerikDataAccess.Plugin.Model
{
    class DataAccessTimelineMessage : ITimelineMessage
    {
        internal DataAccessTimelineMessage(Guid id)
        {
            Id = id;
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
