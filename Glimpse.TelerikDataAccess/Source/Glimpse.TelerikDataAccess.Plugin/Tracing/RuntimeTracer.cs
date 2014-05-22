using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using Glimpse.Core.Message;
using Glimpse.TelerikDataAccess.Plugin.Model;
using Glimpse.TelerikDataAccess.Plugin.Inspector;

namespace Glimpse.TelerikDataAccess.Plugin.Tracing
{
    /// <summary>
    /// Tracing endpoints for Telerik DataAccess Runtime internal tracing.
    /// </summary>
    public class RuntimeTracer
    {
        internal static readonly RuntimeTracer Instance = new RuntimeTracer();
        private static readonly TimeSpan NearlyNothing = new TimeSpan(10L);
        internal static readonly TimelineCategoryItem CategoryDataAccess = new TimelineCategoryItem("DataAccess", "#2db245", "Red");
        internal static readonly TimelineCategoryItem CategoryDataAccessL2C = new TimelineCategoryItem("DataAccess Cache", "#2db245", "Red");
        internal static readonly TimelineCategoryItem CategoryDataAccessPool = new TimelineCategoryItem("DataAccess Pool", "#2db245", "Red");

        private static void Publish(DataAccessMessage msg)
        {
            var tim = TelerikDataAccessInspector.Timer;
            if (tim != null)
            {
                msg.EventCategory = CategoryDataAccess;
                msg.AsTimedMessage(tim.Point());
                TelerikDataAccessInspector.Broker.Publish(msg);
            }
            else
            {
                msg.StartTime = DateTime.Now;
                msg.EventCategory = (msg.Kind & Kind.Cache) ==  0 ? CategoryDataAccessPool : CategoryDataAccessL2C;
                TelerikDataAccessInspector.Broker.Publish(msg);
            }
        }

        private static void Terminate(DataAccessMessage msg)
        {
            var dur = TelerikDataAccessInspector.Timer.Stop(msg.Offset);
            msg.Duration = dur.Duration;
        }

        private static int? Hash(object o)
        {
            if (o != null)
                return o.GetHashCode();
            return null;
        }

        private static Model.ParameterInfo[] Extract(DbParameterCollection p)
        {
            Model.ParameterInfo[] result = null;
            if (p.Count > 0)
            {
                result = new Model.ParameterInfo[p.Count];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i].Name = p[i].ParameterName;
                    result[i].Value = p[i].Value;
                }
            }
            return result;
        }

        private static Model.ParameterInfo[] Extract(DbParameterCollection p, DataRow[] rows, DataRowVersion version)
        {
            List<Model.ParameterInfo> result = new List<Model.ParameterInfo>(rows.Length*p.Count);
            if (p.Count > 0)
            {
                for (int r = 0; r < rows.Length; r++)
                {
                    for (int i = 0; i < p.Count; i++)
                    {
                        var sub = new Model.ParameterInfo();
                        var parameter = p[i];
                        sub.Row = r;
                        sub.Name = parameter.ParameterName;
                        sub.Value = rows[r][parameter.SourceColumn, version];
                        result.Add(sub);
                    }
                }
            }
            return result.ToArray();
        }

        // The method beeing called when a required method override is missing.
        public T InterfaceMethod<T>(string name, params object [] args)
        {
            return default(T);
        }
        
        #region OpenAccess Tracing Common
        
        public bool IsEnabled()
        {
            return true;
        }

        #endregion

        #region OpenAccess Tracing v1
        public  void Batch(string Id, DbDataAdapter adapter, DataRow[] rows)
        {
            DataRowVersion version = DataRowVersion.Current;
            DbCommand cmd = adapter.InsertCommand ?? adapter.UpdateCommand ?? adapter.DeleteCommand;
            if (cmd == adapter.DeleteCommand)
                version = DataRowVersion.Original;
            Publish(new CommandMessage() { Connection = Id, EventName = cmd.CommandText, Kind = Kind.Batch, Transaction = Hash(cmd.Transaction), Parameters = Extract(cmd.Parameters, rows, version) });
        }

        public  void BatchDone(string Id, int rows, Exception e)
        {
            Publish(new CommandMessage() { Connection = Id, Kind = Kind.Batch | Kind.Done, Rows = rows });
        }

        public  void ConClose(string Id)
        {
            Publish(new ConnectionMessage() { Connection = Id, Kind = Kind.Close });
        }

        public  void ConOpen(string Id, string ConnectionString)
        {
            Publish(new ConnectionMessage() { Connection = Id, Text = ConnectionString, Kind = Kind.Open });
        }

        public  void ConOpenDone(string Id, int Milliseconds, Exception e)
        {
            Publish(new ConnectionMessage() { Connection = Id, Kind = Kind.Open | Kind.Done, Failure = e });
        }

        public  void GetSchema(string Id, string collection, string[] restrict)
        {
            Publish(new ConnectionMessage() { Connection = Id, Kind = Kind.GetSchema, Text = collection });
        }

        //public void GetSchemaDone(string Id, string collection, DataTable dt)
        //{
        //    Publish(new ConnectionMessage() { Connection = Id, Kind = Kind.GetSchema | Kind.Done, EventSubText = collection });
        //}

        public  void Prepare(string Id, DbCommand Sql)
        {
        }

        public  void SetEnabled(bool v)
        {
        }

        public  void Sql(string Id, DbCommand Sql)
        {
            Publish(new CommandMessage() { Connection = Id, EventName = Sql.CommandText, Kind = Kind.Sql, Transaction = Hash(Sql.Transaction), Parameters = Extract(Sql.Parameters) });
        }

        public  void SqlFailure(string Id, Exception e)
        {
            Publish(new CommandMessage() { Connection = Id, Kind = Kind.Sql, Failure = e });
        }

        public  void SqlNonQuery(string Id, DbCommand Sql, int Ret)
        {
            Publish(new CommandMessage() { Connection = Id, EventName = Sql.CommandText, Kind = Kind.NonQuery, Rows = Ret, Transaction = Hash(Sql.Transaction), Parameters = Extract(Sql.Parameters) });
        }

        public  void SqlReaderClose(string Id, DbDataReader Rdr, int rows)
        {
            Publish(new CommandMessage() { Connection = Id, Kind = Kind.Sql | Kind.Done, Rows = rows });
        }

        public  void SqlReaderNextResult(string Id, DbDataReader Rdr)
        {
        }

        public  void SqlReaderOpen(string Id, DbDataReader Rdr)
        {
            Publish(new CommandMessage() { Connection = Id, Kind = Kind.Reader });
        }

        public  void SqlReaderRead(string Id, DbDataReader Rdr)
        {
        }

        public  void SqlScalar(string Id, string Ret)
        {
            Publish(new CommandMessage() { Connection = Id, Kind = Kind.Scalar });
        }     

        public  void TxnBegin(string Id)
        {
            Publish(new TransactionMessage() { Connection = Id, Kind = Kind.Begin });
        }

        public  void TxnBeginDone(string Id, DbTransaction t, Exception e)
        {
            Publish(new TransactionMessage() { Connection = Id, Kind = Kind.Begin | Kind.Done, Failure = e, Transaction = Hash(t), Text = (t != null ? t.IsolationLevel.ToString() : null) });
        }

        public  void TxnCommit(string Id, DbTransaction t)
        {
            Publish(new TransactionMessage() { Connection = Id, Kind = Kind.Commit, Transaction = Hash(t) });
        }

        public  void TxnCommitDone(string Id, Exception e)
        {
            Publish(new TransactionMessage() { Connection = Id, Kind = Kind.Commit | Kind.Done, Failure = e });
        }

        public  void TxnEnlist(string Id, object t)
        {
            Publish(new TransactionMessage() { Connection = Id, Kind = Kind.Enlist, Transaction = Hash(t) });
        }

        public  void TxnEnlistDone(string Id, object t, Exception e)
        {
            Publish(new TransactionMessage() { Connection = Id, Kind = Kind.Enlist | Kind.Done, Failure = e, Transaction = Hash(t) });
        }

        public  void TxnRollback(string Id, DbTransaction t)
        {
            Publish(new TransactionMessage() { Connection = Id,  Kind = Kind.Rollback, Transaction = Hash(t) });
        }

        public  void TxnRollbackDone(string Id, Exception e)
        {
            Publish(new TransactionMessage() { Connection = Id, Kind = Kind.Rollback | Kind.Done });
        }

        public  void Write(string Id, string fmt, params object[] args)
        {
            Publish(new DataAccessMessage() { EventName = string.Format(fmt, args), Kind = Kind.None, Text = Id });
        }

        #endregion

        #region Stream Support (same in v1/v2)
        public void StreamClose(Stream stream)
        {
        }

        public void StreamFailure(Stream stream, Exception e)
        {
        }

        public void StreamGetLength(Stream stream)
        {
        }

        public void StreamOpened(string Id, DbDataReader Rdr, Stream stream)
        {
        }

        public void StreamRead(Stream stream, int bytes)
        {
        }

        public void StreamSeek(Stream stream, long offs, SeekOrigin orig)
        {
        }

        public void StreamSetLength(Stream stream, long length)
        {
        }

        public void StreamWrite(Stream stream, int bytes)
        {
        }
        #endregion

        #region OpenAccess Tracing v2
       
        private static Core.Extensibility.TimerResult GetPoint()
        {
            var tim = TelerikDataAccessInspector.Timer;
            if (tim != null)
                return tim.Point();
            return new Core.Extensibility.TimerResult() { StartTime = DateTime.Now };
        }
       
        private T SetDuration<T>(object info) where T : DataAccessMessage
        {
            var point = GetPoint();
            T result = info as T;
            if (result.Offset == TimeSpan.Zero || point.Offset == TimeSpan.Zero)
            {
                result.Duration = point.StartTime - result.StartTime;
                result.Offset = TimeSpan.Zero;
            }
            else
                result.Duration = point.Offset - result.Offset;
            return result;
        }

        private static void Publish2(DataAccessMessage msg)
        {
            if (msg.Offset == TimeSpan.Zero)
                msg.EventCategory = (msg.Kind & Kind.Cache) == 0 ? CategoryDataAccessPool : CategoryDataAccessL2C;
            else
                msg.EventCategory = CategoryDataAccess;
            TelerikDataAccessInspector.Broker.Publish(msg);
        }

        public object ConOpen2(string Id, string ConnectionString)
        {
            return new ConnectionMessage()
            {
                Connection = Id,
                Text = ConnectionString,
                Kind = Model.Kind.Open | Kind.V2
            }.AsTimedMessage(GetPoint());
        }
        public void ConOpenDone(object info, Exception e) 
        {
            var msg = SetDuration<ConnectionMessage>(info);
            msg.Failure = e;
            Publish2(msg);
        }

        public object TxnBegin(string Id, System.Data.IsolationLevel isolation)
        {
            return new TransactionMessage()
            {
                Connection = Id,
                Text = isolation.ToString(),
                Kind = Model.Kind.Begin | Kind.V2
            }.AsTimedMessage(GetPoint());
        }

        public void TxnBeginDone(object info, DbTransaction t, Exception e)
        {
            var msg = SetDuration<TransactionMessage>(info);
            msg.Failure = e;
            msg.Transaction = Hash(t);
            Publish2(msg);
        }
        
        public object TxnEnlist2(string Id, object t) 
        {
            return new TransactionMessage()
            {
                Connection = Id,
                Text = (t ?? "").ToString(),
                Kind = Model.Kind.Enlist | Kind.V2
            }.AsTimedMessage(GetPoint());
        }

        public void TxnEnlistDone(object info, Exception e) 
        {
            var msg = SetDuration<TransactionMessage>(info);
            msg.Failure = e;
            Publish2(msg);
        }

        public void TxnCompleted(string Id, object t, string state) 
        {
            var point = GetPoint();
            var msg = new TransactionMessage();
            msg.StartTime = point.StartTime;
            msg.Offset = point.Offset;
            msg.Connection = Id;
            msg.Kind = Kind.Commit | Kind.V2;
            msg.Text = state;
            Publish2(msg);
        }

        public object TxnCommit2(string Id, DbTransaction t) 
        {
            return new TransactionMessage()
            {
                Connection = Id,
                Transaction = Hash(t),
                Kind = Model.Kind.Commit | Kind.V2
            }.AsTimedMessage(GetPoint());
        }
        public void TxnCommitDone(object info, Exception e) 
        {
            var msg = SetDuration<TransactionMessage>(info);
            msg.Failure = e;
            Publish2(msg);
        }

        public object TxnRollback2(string Id, DbTransaction t)
        {
            return new TransactionMessage()
            {
                Connection = Id,
                Transaction = Hash(t),
                Kind = Model.Kind.Rollback | Kind.V2
            }.AsTimedMessage(GetPoint());
        }
        
        public void TxnRollbackDone(object info, Exception e)
        {
            var msg = SetDuration<TransactionMessage>(info);
            msg.Failure = e;
            Publish2(msg);
        }

        public object SqlBegin(string Id, DbCommand Sql) 
        {
            return new CommandMessage() { 
                        Connection = Id, 
                        Text = Sql.CommandText,
                        Kind = Kind.Sql | Kind.V2,
                        Transaction = Hash(Sql.Transaction),
                        Parameters = Extract(Sql.Parameters)}.AsTimedMessage(GetPoint());
        }
        
        public void SqlFailure(object info, Exception e) 
        {
            var msg = SetDuration<CommandMessage>(info);
            msg.Failure = e;
            Publish2(msg);
        }
        
        public void SqlNonQuery(object info, int Ret) 
        {
            var msg = SetDuration<CommandMessage>(info);
            msg.Kind = Kind.NonQuery | Kind.V2;
            msg.Rows = Math.Max(0, Ret);
            Publish2(msg);
        }

        public void SqlScalar(object info, object Ret)
        {
            var msg = SetDuration<CommandMessage>(info);
            msg.Kind = Kind.Scalar | Kind.V2;
            msg.Rows = (Ret == null || DBNull.Value.Equals(Ret)) ? 0 : 1;
            Publish2(msg);
        }

        public void SqlReaderOpen(object info, DbDataReader Rdr) 
        {
            var msg = SetDuration<CommandMessage>(info);
            Publish2(msg);
        }

        public object Batch2(string Id, DbDataAdapter adapter, DataRow[] rows) 
        {
            var cmd = adapter.InsertCommand ?? adapter.UpdateCommand ?? adapter.DeleteCommand;
            var version = adapter.DeleteCommand == cmd ? DataRowVersion.Original : DataRowVersion.Current;
            return new CommandMessage()
            {
                Connection = Id,
                Text = cmd.CommandText,
                Kind = Model.Kind.Batch | Kind.V2,
                Transaction = Hash(cmd.Transaction),
                Parameters = Extract(cmd.Parameters, rows, version)
            }.AsTimedMessage(GetPoint());
        }

        public void BatchDone(object info, int Rows, Exception e) 
        {
            var msg = SetDuration<CommandMessage>(info);
            msg.Rows = Rows;
            msg.Failure = e;
            Publish2(msg);
        }

        public object GetSchema2(string Id, string collection, string[] restrict) 
        {
            return new CommandMessage()
            {
                Connection = Id,
                Text = collection,
                Kind = Model.Kind.GetSchema | Kind.V2
            }.AsTimedMessage(GetPoint());
        }
        public void GetSchemaDone(object info, System.Data.DataTable dt) 
        {
            var msg = SetDuration<CommandMessage>(info);
            msg.Rows = dt.Rows.Count;
            Publish2(msg);
        }
        public void CacheEvicted(string id, bool remote, int oids, string[] classes, bool all) 
        {
            var msg = new EvictMessage()
            {
                Connection = id,
                Classes = classes,
                All = all,
                OIDs = oids,
                Kind = Kind.Evict,
                Remote = remote
            }.AsTimedMessage(GetPoint());
            msg.Duration = NearlyNothing;
            Publish2(msg);
        }
        public void CacheHitQuery(string id, bool count) 
        {
            var msg = new CacheMessage()
            {
                Connection = id,
                Kind = count ? Kind.CachedCount : Kind.CachedQuery
            }.AsTimedMessage(GetPoint());
            msg.Duration = NearlyNothing;
            Publish2(msg);
        }
        public void CacheHitQuery2(string id, string filter, bool count)
        {
            var msg = new CacheMessage()
            {
                Connection = id,
                Text = filter,
                Kind = count ? Kind.CachedCount : Kind.CachedQuery
            }.AsTimedMessage(GetPoint());
            msg.Duration = NearlyNothing;
            Publish2(msg);
        }

        public void CacheHitObject(string id, int objs)
        {
            var msg = new CacheMessage()
            {
                Connection = id,
                Kind = Kind.CachedObject,
                Objects = objs
            }.AsTimedMessage(GetPoint());
            msg.Duration = NearlyNothing;
            Publish2(msg);
        }

        public void OpenDatabase(string url, bool metaOnly)
        {
            var msg = new DataAccessMessage()
            {
                Kind = Kind.OpenDB,
                Text = metaOnly ? "MetaOnly:"+url : url,
            }.AsTimedMessage(GetPoint());
            Publish2(msg);
        }
        public void CloseDatabase(string url)
        {
            var msg = new DataAccessMessage()
            {
                Kind = Kind.CloseDB,
                Text = url
            }.AsTimedMessage(GetPoint());
            Publish2(msg);
        }

        public void ReplacedMetadata(string url)
        {
            var msg = new DataAccessMessage()
            {
                Kind = Kind.ChangeMeta,
                Text = url
            }.AsTimedMessage(GetPoint());
            Publish2(msg);
        }

        public object LinqBegin(object comp)
        {
            return new LinqMessage() { Compiler = Hash(comp), Kind = Kind.Linq }.AsTimedMessage(GetPoint());
        }
        public void Linq(object info, string expression, Exception e) 
        {
            var msg = SetDuration<LinqMessage>(info);
            msg.Text = expression;
            msg.Failure = e;
            Publish2(msg);
        }
        
        public object LinqCompile(object comp) 
        {
            return new LinqMessage() { Compiler = Hash(comp), Kind = Kind.Translate}.AsTimedMessage(GetPoint());
        }
        public void LinqCompiled(object info) 
        {
            var msg = SetDuration<LinqMessage>(info);
            Publish2(msg);
        }
        public void LinqSplit(string expression, string split) 
        {
            var msg = new LinqMessage()
            {
                Kind = Kind.Splitted,
                Text = expression,
                EventName = split
            }.AsTimedMessage(GetPoint());
            Publish2(msg);
        }

        #endregion
    }
}
