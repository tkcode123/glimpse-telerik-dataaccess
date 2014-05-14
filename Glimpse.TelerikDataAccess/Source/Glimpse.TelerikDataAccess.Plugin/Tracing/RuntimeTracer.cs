using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using Glimpse.Core.Message;
using Glimpse.TelerikDataAccess.Plugin.Model;

namespace Glimpse.TelerikDataAccess.Plugin.Tracing
{
    public class RuntimeTracer
    {
        internal static readonly RuntimeTracer Instance = new RuntimeTracer();
        internal static readonly TimelineCategoryItem CategoryDataAccess = new TimelineCategoryItem("DataAccess", "#2db245", "Red");
        internal static readonly TimelineCategoryItem CategoryDataAccessL2C = new TimelineCategoryItem("DataAccess Cache", "#2db245", "Red");
        internal static readonly TimelineCategoryItem CategoryDataAccessPool = new TimelineCategoryItem("DataAccess Pool", "#2db245", "Red");

        private static void Publish(DataAccessMessage msg)
        {
            var ctx = TracingContextFactory.Current;
            var tim = ctx.Timer;
            if (tim != null)
            {
                msg.EventCategory = CategoryDataAccess;
                msg.AsTimedMessage(tim.Point());
                ctx.Broker.Publish(msg);
            }
            else
            {
                msg.StartTime = DateTime.Now;
                msg.EventCategory = (msg.Kind & Kind.Cache) ==  0 ? CategoryDataAccessPool : CategoryDataAccessL2C;
                ctx.Broker.Publish(msg);
            }
        }

        private static void Terminate(DataAccessMessage msg)
        {
            var ctx = TracingContextFactory.Current;
            var dur = ctx.Timer.Stop(msg.Offset);
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

        private static Model.ParameterInfo[] Extract(DbParameterCollection p, DataRow[] rows)
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
                        sub.Value = rows[r][parameter.SourceColumn];
                        result.Add(sub);
                    }
                }
            }
            return result.ToArray();
        }

        public T MissingInterfaceMethod<T>(string name, params object [] args)
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
            DbCommand cmd = adapter.InsertCommand ?? adapter.UpdateCommand ?? adapter.DeleteCommand;
            Publish(new CommandMessage() { Connection = Id, EventName = cmd.CommandText, Kind = Kind.Batch, Transaction = Hash(cmd.Transaction), Parameters = Extract(cmd.Parameters, rows) });
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
    }
}
