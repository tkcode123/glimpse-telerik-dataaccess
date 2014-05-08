using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Framework;
using Glimpse.Core.Message;
using Glimpse.TelerikDataAccess.Plugin.Model;
using System.Data.Common;
using System.IO;
using System.Data;

namespace Glimpse.TelerikDataAccess.Plugin.Tracing
{
    public class RuntimeTracer
    {
        internal static readonly RuntimeTracer Instance = new RuntimeTracer();
        private static readonly TimelineCategoryItem CategoryDataAccess = new TimelineCategoryItem("DataAccess", "#2db245", "Red");
        private static readonly TimelineCategoryItem CategoryDataAccessL2C = new TimelineCategoryItem("DataAccess Cache", "#2db245", "Red");
        private static readonly TimelineCategoryItem CategoryDataAccessPool = new TimelineCategoryItem("DataAccess Pool", "#2db245", "Red");

        private static void Publish(DataAccessMessage msg)
        {
            var ctx = TracingContextFactory.Current;
            var tim = ctx.Timer;
            if (tim != null)
            {
                var ts = tim.Point();
                DataAccessMessage result = msg.AsTimedMessage(ts);
                ctx.Broker.Publish(new DataAccessTimelineMessage(result, CategoryDataAccess));
                ctx.Broker.Publish(result);
            }
            else
            {
                msg.StartTime = DateTime.Now;
                msg.EventCategory = (msg.Kind == Kind.Open || msg.Kind == Kind.Close ? CategoryDataAccessPool : CategoryDataAccessL2C);
                ctx.Broker.Publish(msg);
            }
        }

        private static void Terminate(DataAccessMessage msg)
        {
            var ctx = TracingContextFactory.Current;
            var dur = ctx.Timer.Stop(msg.Offset);
            msg.Duration = dur.Duration;
        }

        private static int Hash(object o)
        {
            return o == null ? 0 : o.GetHashCode();
        }

        public T MissingInterfaceMethod<T>(string name, params object [] args)
        {
            return default(T);
        }

        #region OpenAccess Tracing
        public bool IsEnabled()
        {
            return true;
        }
        public  void Batch(string Id, DbDataAdapter adapter, DataRow[] rows)
        {
        }

        public  void BatchDone(string Id, int Rows, Exception e)
        {
        }

        public  void ConClose(string Id)
        {
            Publish(new ConnectionMessage() { Connection = Id, Kind = Kind.Close });
        }

        public  void ConOpen(string Id, string ConnectionString)
        {
            Publish(new ConnectionMessage() { Connection = Id, EventSubText = ConnectionString, Kind = Kind.Open });
        }

        public  void ConOpenDone(string Id, int Milliseconds, Exception e)
        {
            Publish(new ConnectionMessage() { Connection = Id, EventName = "OpenDone", Failure = e });
        }

        public  void GetSchema(string Id, string collection, string[] restrict)
        {
        }

        //public  void GetSchemaDone(string Id, string collection, DataTable dt)
        //{
        //}

        public  void Prepare(string Id, DbCommand Sql)
        {
        }

        public  void SetEnabled(bool v)
        {
        }

        public  void Sql(string Id, DbCommand Sql)
        {
            Publish(new CommandMessage() { Connection = Id, EventName = Sql.CommandText, Kind = Kind.Sql, Transaction = Hash(Sql.Transaction) });
        }

        public  void SqlFailure(string Id, Exception e)
        {
            Publish(new CommandMessage() { Connection = Id, Kind = Kind.Sql, Failure = e });
        }

        public  void SqlNonQuery(string Id, DbCommand Sql, int Ret)
        {
            Publish(new CommandMessage() { Connection = Id, EventName = Sql.CommandText, Kind = Kind.NonQuery, Affected = Ret, Transaction = Hash(Sql.Transaction) });
        }

        public  void SqlReaderClose(string Id, DbDataReader Rdr, int rows)
        {
            Publish(new CommandMessage() { Connection = Id, Kind = Kind.Done, Rows = rows });
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

        public  void StreamClose(Stream stream)
        {
        }

        public  void StreamFailure(Stream stream, Exception e)
        {
        }

        public  void StreamGetLength(Stream stream)
        {
        }

        public  void StreamOpened(string Id, DbDataReader Rdr, Stream stream)
        {
        }

        public  void StreamRead(Stream stream, int bytes)
        {
        }

        public  void StreamSeek(Stream stream, long offs, SeekOrigin orig)
        {
        }

        public  void StreamSetLength(Stream stream, long length)
        {
        }

        public  void StreamWrite(Stream stream, int bytes)
        {
        }

        public  void TxnBegin(string Id)
        {
        }

        public  void TxnBeginDone(string Id, DbTransaction t, Exception e)
        {
            Publish(new TransactionMessage() { Connection = Id, Kind = Kind.Begin, Failure = e, Transaction = Hash(t) });
        }

        public  void TxnCommit(string Id, DbTransaction t)
        {
        }

        public  void TxnCommitDone(string Id, Exception e)
        {
            Publish(new TransactionMessage() { Connection = Id, Kind = Kind.Commit, Failure = e });
        }

        public  void TxnEnlist(string Id, object t)
        {
        }

        public  void TxnEnlistDone(string Id, object t, Exception e)
        {
            Publish(new TransactionMessage() { Connection = Id, Kind = Kind.Enlist, Failure = e, Transaction = Hash(t) });
        }

        public  void TxnRollback(string Id, DbTransaction t)
        {
            Publish(new TransactionMessage() { Connection = Id,  Kind = Kind.Rollback, Transaction = Hash(t) });
        }

        public  void TxnRollbackDone(string Id, Exception e)
        {
        }

        public  void Write(string Id, string fmt, params object[] args)
        {
            Publish(new DataAccessMessage() { EventName = string.Format(fmt, args), Kind = Kind.None, EventSubText = Id });
        }

        #endregion
    }
}
