using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Extensions;
using Glimpse.TelerikDataAccess.Plugin.Model;

namespace Glimpse.TelerikDataAccess.Plugin.Tab
{
    class DataAccessAggregator
    {
        private readonly List<DataAccessMessage> rawMessages;
        private readonly List<DataAccessTabItem> aggregatedMessages;

        internal DataAccessAggregator(ITabContext context)
        {
            var raw = context.GetMessages<DataAccessMessage>();
            rawMessages = new List<DataAccessMessage>(raw);
            aggregatedMessages = new List<DataAccessTabItem>(rawMessages.Count);
            Aggregate();
        }

        internal bool HasMessages { get { return aggregatedMessages.Count > 0; } }

        private void Aggregate()
        {
            var open = new Dictionary<string, int>();
            for (int i = 0; i < rawMessages.Count; i++)
            {
                var raw = rawMessages[i];
                var m = raw as CommandMessage;
                if (m != null)
                {
                    int start;
                    bool found = open.TryGetValue(m.Connection, out start);
                    if (m.Kind == Kind.Sql)
                    {
                        if (found)
                        {   // detect second sql on the same connection
                            if (start >= 0)
                                open[m.Connection] = -start;
                        }
                        else
                            open.Add(m.Connection, i);
                        continue;
                    }
                    else if (m.Kind == Kind.Done || m.Kind == Kind.NonQuery || m.Kind == Kind.Scalar || m.Kind == Kind.Reader)
                    {
                        if (found)
                        {
                            var started = (CommandMessage)rawMessages[start];
                            started.Duration = (m.Offset - started.Offset);
                            if (m.Kind == Kind.Reader)
                            {
                                started.FetchDuration = m.Offset;
                                continue;
                            }
                            if (m.Kind == Kind.Done)
                            {
                                var fetch = (m.Offset - started.FetchDuration);
                                started.FetchDuration = fetch;                                
                            }
                            else
                            {
                                started.Rows = m.Kind == Kind.NonQuery ? m.Affected : m.Rows;
                            }
                            open.Remove(m.Connection);
                            raw = rawMessages[start];
                        }
                        else
                        {
                            // unstarted sql ?
                            continue;
                        }
                    }
                }
                else if (raw.Kind == Kind.None)
                {
                    if (raw.EventName == "OpenDone")
                    {
                        for (int r = aggregatedMessages.Count - 1; r >= 0; r--)
                        {
                            if (aggregatedMessages[r].Kind == Kind.Open && aggregatedMessages[r].Connection == ((ConnectionMessage)raw).Connection)
                            {
                                aggregatedMessages[r].Duration = (raw.Offset - aggregatedMessages[r].Offset);
                                break;
                            }
                        }
                        continue;
                    }
                }
                int? rs = (raw is CommandMessage) ? ((CommandMessage)raw).Rows : null;
                if (rs.HasValue && rs.Value < 0)
                    rs = null;
                aggregatedMessages.Add(new DataAccessTabItem()
                {
                    Ordinal = aggregatedMessages.Count,
                    Kind = raw.Kind,
                    Rows = rs,
                    Transaction = (raw is TransactionMessage) ? ((TransactionMessage)raw).Transaction : null,
                    Connection = (raw is ConnectionMessage) ? ((ConnectionMessage)raw).Connection : "",
                    FetchDuration = (raw is CommandMessage) ? ((CommandMessage)raw).FetchDuration : null,
                    Duration = raw.Duration,
                    Offset = raw.Offset,
                    Text = raw.EventName ?? "",
                });
            }
        }

        internal DataAccessTabStatistics GetStatistics()
        {
            var conns = new Dictionary<string, bool>();
            var txns = new Dictionary<int, bool>();
            var qrys = 0;

            foreach (var m in rawMessages)
            {
                var cm = (m as ConnectionMessage);
                if (cm != null)
                    conns[cm.Connection] = true;
                var tm = (m as TransactionMessage);
                if (tm != null && tm.Transaction.HasValue)
                    txns[tm.Transaction.Value] = true;
                if (m.Kind == Kind.Sql)
                    qrys++;
            }
            return new DataAccessTabStatistics() { ConnectionCount = conns.Count, TransactionCount = txns.Count, QueryCount = qrys };
        }

        internal IEnumerable<DataAccessTabItem> GetItems()
        {
            return aggregatedMessages;
        }
    }
}
