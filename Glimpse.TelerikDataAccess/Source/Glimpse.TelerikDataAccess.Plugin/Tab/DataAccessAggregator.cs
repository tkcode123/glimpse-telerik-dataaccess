using System;
using System.Collections.Generic;
using System.Linq;
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
            rawMessages = new List<DataAccessMessage>(context.GetMessages<DataAccessMessage>());
            aggregatedMessages = new List<DataAccessTabItem>(rawMessages.Count);
            Aggregate();
            var broker = context.MessageBroker;
            foreach (var e in aggregatedMessages)
                e.ToTimeline(broker);
        }

        internal bool HasMessages { get { return aggregatedMessages.Count > 0; } }

        private DataAccessTabItem FindReverseWithSameConnection(Kind kind, string connectionId)
        {
            for (int r = aggregatedMessages.Count-1; r >= 0; r--)
            {
                if (aggregatedMessages[r].Kind == kind && aggregatedMessages[r].Connection == connectionId)
                {
                    return aggregatedMessages[r];
                }
            }
            return null;
        }

        private void Aggregate()
        {
            bool v2 = rawMessages.Any(x => (x.Kind & Kind.V2) != 0);
            if (v2)
            {
                Aggregate2();
                return;
            }
            var open = new Dictionary<string, int>();
            for (int i = 0; i < rawMessages.Count; i++)
            {
                IEnumerable<object> details = null;
                int? rows = null;
                var raw = rawMessages[i];
                var m = raw as CommandMessage;
                if (m != null)
                {
                    int start;
                    bool found = open.TryGetValue(m.Connection, out start);
                    if (m.Kind == Kind.Sql || m.Kind == Kind.Batch)
                    {
                        if (found)
                        {   // detect second sql on the same connection
                        }
                        else
                            open.Add(m.Connection, i);
                        continue;
                    }
                    else if (m.Kind == (Kind.Sql|Kind.Done) || m.Kind == Kind.NonQuery || m.Kind == Kind.Scalar || m.Kind == Kind.Reader || m.Kind == (Kind.Batch|Kind.Done))
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
                            if (m.Kind == (Kind.Sql|Kind.Done) || m.Kind == (Kind.Batch|Kind.Done))
                            {
                                var fetch = (m.Offset - started.FetchDuration);
                                started.FetchDuration = fetch;
                                started.Rows = m.Rows;
                            }
                            else
                            {
                                started.Rows = m.Rows;
                            }
                            open.Remove(m.Connection);
                            raw = rawMessages[start];
                            details = CreateParameterDetails(started.Parameters);
                            if (started.Rows.HasValue && started.Rows.Value < 0)
                                started.Rows = null;
                            rows = started.Rows;
                        }
                        else
                        {
                            // unstarted sql ?
                            continue;
                        }
                    }
                }
                else if (raw.Kind == Kind.Open)
                {
                    raw.EventName = raw.Text;
                }
                else if (raw.Kind == (Kind.Open | Kind.Done))
                {
                    var started = FindReverseWithSameConnection(Kind.Open, ((ConnectionMessage)raw).Connection);
                    if (started != null)
                        started.Duration = (raw.Offset - started.Offset);
                    continue;
                }
                else if (raw.Kind == (Kind.Begin | Kind.Done))
                {
                    var started = FindReverseWithSameConnection(Kind.Begin, ((ConnectionMessage)raw).Connection);
                    if (started != null)
                    {
                        started.Duration = (raw.Offset - started.Offset);
                        started.Transaction = ((TransactionMessage)raw).Transaction;
                        started.Text = raw.Text;
                    }
                    continue;
                }
                else if (raw.Kind == (Kind.Commit | Kind.Done))
                {
                    var started = FindReverseWithSameConnection(Kind.Commit, ((ConnectionMessage)raw).Connection);
                    if (started != null)
                        started.Duration = (raw.Offset - started.Offset);
                    continue;
                }
                else if (raw.Kind == (Kind.Rollback | Kind.Done))
                {
                    var started = FindReverseWithSameConnection(Kind.Rollback, ((ConnectionMessage)raw).Connection);
                    if (started != null)
                        started.Duration = (raw.Offset - started.Offset);
                    continue;
                }
                else if (raw.Kind == (Kind.Enlist | Kind.Done))
                {
                    var started = FindReverseWithSameConnection(Kind.Rollback, ((ConnectionMessage)raw).Connection);
                    if (started != null)
                        started.Duration = (raw.Offset - started.Offset);
                    continue;
                }

                aggregatedMessages.Add(new DataAccessTabItem()
                {
                    Id = raw.Id,
                    Ordinal = aggregatedMessages.Count,
                    Kind = raw.Kind,
                    Rows = rows,
                    Transaction = (raw is TransactionMessage) ? ((TransactionMessage)raw).Transaction : null,
                    Connection = (raw is ConnectionMessage) ? ((ConnectionMessage)raw).Connection : "",
                    FetchDuration = (raw is CommandMessage) ? ((CommandMessage)raw).FetchDuration : null,
                    Details = details,
                    Duration = raw.Duration,
                    Offset = raw.Offset,
                    Category = raw.EventCategory,
                    Text = raw.EventName ?? "",
                });
            }
        }

        void Aggregate2()
        {
            for (int i = 0; i < rawMessages.Count; i++)
            {
                var raw = rawMessages[i];
                var m = raw as CommandMessage;
                IEnumerable<object> details = null;
                int? rows = null;
                string text = raw.Text;
                var kind = raw.Kind & ~Kind.V2;
                switch (kind)
                {
                    case Kind.Sql:
                    case Kind.Scalar:
                    case Kind.NonQuery:
                    case Kind.Batch:
                        rows = m.Rows;
                        details = (m.Parameters != null ? m.Parameters.Cast<object>() : null);
                        break;
                    case Kind.Evict:
                        var e = raw as EvictMessage;
                        if (e.All)
                            text = "<ALL>";
                        else
                        {
                            text = "OIDs=" + e.OIDs; 
                            if (e.Classes != null && e.Classes.Length > 0)
                            {
                                text += " Classes="+string.Join(",",e.Classes);
                            }
                        }
                        break;
                    case Kind.CachedCount:
                    case Kind.CachedObject:
                    case Kind.CachedQuery:
                        break;
                    case Kind.Sql | Kind.Done:
                        var orig = FindReverseWithSameConnection(Kind.Sql, m.Connection);
                        if (orig != null)
                        {
                            TimeSpan fetch = orig.Duration;
                            orig.FetchDuration = fetch;
                            orig.Duration = m.Offset - orig.Offset;
                            orig.Rows = m.Rows;
                        }
                        continue;
                }
                aggregatedMessages.Add(new DataAccessTabItem()
                {
                    Id = raw.Id,
                    Ordinal = aggregatedMessages.Count,
                    Kind = kind,
                    Rows = rows,
                    Transaction = (raw is TransactionMessage) ? ((TransactionMessage)raw).Transaction : null,
                    Connection = (raw is ConnectionMessage) ? ((ConnectionMessage)raw).Connection : "",
                    FetchDuration = (raw is CommandMessage) ? ((CommandMessage)raw).FetchDuration : null,
                    Details = details,
                    Duration = raw.Duration,
                    Offset = raw.Offset,
                    Category = raw.EventCategory,
                    Text = text ?? "",
                });
            }
        }

        private static IEnumerable<object> CreateParameterDetails(ParameterInfo[] parameterInfo)
        {
            if (parameterInfo == null || parameterInfo.Length == 0)
                return null;
            var provider = System.Globalization.CultureInfo.InvariantCulture;
            object[] cpy = new object[parameterInfo.Length];
            for (int i = 0; i < parameterInfo.Length; i++)
            {
                var p = parameterInfo[i];
                object v = p.Value;
                if (v != null)
                {
                    switch (Type.GetTypeCode(v.GetType()))
                    {
                        case TypeCode.DateTime:
                            v = ((DateTime)v).ToString("o", provider);
                            break;
                        case TypeCode.Single:
                            v = ((Single)v).ToString("r", provider);
                            break;
                        case TypeCode.Double:
                            v = ((Double)v).ToString("r", provider);
                            break;
                        case TypeCode.Decimal:
                            v = ((Decimal)v).ToString("g", provider);
                            break;
                        case TypeCode.SByte:
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            v = ((IFormattable)v).ToString("d", provider);
                            break;
                        case TypeCode.Byte:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                            v = "0x"+((IFormattable)v).ToString("X", provider);
                            break;
                        default:
                            if (v is DateTimeOffset)
                                v = ((DateTimeOffset)v).ToString("o", provider);
                            else if (v is byte[])
                                v = "byte["+((byte[])v).Length+"]";
                            else
                                v = v.ToString();
                            break;
                    }
                }
                else
                    v = "<null>";
                cpy[i] = new ParameterInfo() { Name = p.Name, Value = v, Row = p.Row };
            }
            return cpy;
        }

        internal DataAccessTabStatistics GetStatistics()
        {
            var conns = new Dictionary<string, bool>();
            var txns = new Dictionary<int, bool>();
            var qrys = 0;
            int rowsFetched = 0;
            int l2query = 0;
            int l2objs = 0;
            TimeSpan execTime = TimeSpan.Zero;
            TimeSpan openTime = TimeSpan.Zero;

            foreach (var m in aggregatedMessages)
            {
                if (string.IsNullOrEmpty(m.Connection) == false)
                    conns[m.Connection] = true;
                if (m.Transaction.HasValue)
                    txns[m.Transaction.Value] = true;
                if (m.Kind == Kind.CachedQuery || m.Kind == Kind.CachedCount)
                    l2query++;
                else if (m.Kind == Kind.CachedObject)
                    l2objs++;
                if (m.Kind == Kind.Sql || m.Kind == Kind.Scalar || m.Kind == Kind.NonQuery || m.Kind == Kind.Batch)
                    qrys++;
                if (m.Rows.HasValue)
                    rowsFetched += m.Rows.Value;
                if (m.Kind == Kind.Open)
                    openTime += m.Duration;
                else
                    execTime += m.Duration;
            }
            return new DataAccessTabStatistics() { 
                ConnectionCount = conns.Count, 
                TransactionCount = txns.Count, 
                QueryCount = qrys, 
                Rows = rowsFetched,
                ExecutionTime = execTime,
                ConnectionOpenTime = openTime,
                SecondLevelObjects = l2objs,
                SecondLevelHits = l2query
            };
        }

        internal IEnumerable<DataAccessTabItem> GetItems()
        {
            return aggregatedMessages;
        }
    }
}
