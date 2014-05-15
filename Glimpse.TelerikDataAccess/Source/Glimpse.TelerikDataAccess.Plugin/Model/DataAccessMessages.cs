using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glimpse.Core.Message;

namespace Glimpse.TelerikDataAccess.Plugin.Model
{
    public class DataAccessMessage : ITimedMessage
    {     
        public DataAccessMessage()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }

        public TimeSpan Offset { get; set; }

        public TimeSpan Duration { get; set; }

        public DateTime StartTime { get; set; }

        public TimelineCategoryItem EventCategory { get; set; }

        public string EventName { get; set; }

        public string Text { get; set; }

        public Exception Failure { get; set; }

        public TimeSpan? FetchDuration { get; set; }

        public Kind Kind { get; set; }

        public override string ToString()
        {
            return Kind.ToString();
        }

        public override bool Equals(object obj)
        {
            var other = obj as DataAccessMessage;
            if (other != null)
            {
                return this.Id.Equals(other.Id);
            }
            if (obj is Guid)
            {
                return this.Id.Equals((Guid)obj);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }

    public class ConnectionMessage : DataAccessMessage
    {
        public string Connection { get; set; }
    }

    public class TransactionMessage : ConnectionMessage
    {
        public int? Transaction { get; set; }
    }

    public class CommandMessage : TransactionMessage
    {
        public int? Rows { get; set; }

        public ParameterInfo[] Parameters { get; set; } 
    }

    public class EvictMessage : ConnectionMessage
    {
        public bool All { get; set; }
        public string[] Classes { get; set; }
        public int OIDs { get; set; }
        public bool Remote { get; set; }
    }

    public class CacheMessage : ConnectionMessage
    {
        public int Objects { get; set; }
    }

    public struct ParameterInfo
    {
        public int? Row { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
