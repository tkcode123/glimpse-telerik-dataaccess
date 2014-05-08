using System;
using System.Collections.Generic;
using System.Linq;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Extensions;
using Glimpse.Core.Tab.Assist;
using Glimpse.TelerikDataAccess.Plugin.Model;
using Glimpse.TelerikDataAccess.Plugin.Util;
using Glimpse.TelerikDataAccess.Plugin.Tracing;

namespace Glimpse.TelerikDataAccess.Plugin.Tab
{
    public class DataAccess : TabBase, ITabSetup, IKey, ITabLayout, IDocumentation, ILayoutControl
    {
        internal static readonly object Layout = TabLayout.Create()
                .Cell("Statistics", TabLayout.Create()
                       .Row(r =>
                       {
                           r.Cell("connectionCount").WidthInPixels(100).AlignRight().WithTitle("# Connections");
                           r.Cell("queryCount").WidthInPixels(100).AlignRight().WithTitle("# Queries");
                           r.Cell("transactionCount").WidthInPixels(100).AlignRight().WithTitle("# Transactions");
                           r.Cell("secondLevelHits").WidthInPixels(100).AlignRight().WithTitle("# L2C Hits");
                           r.Cell("executionTime").WidthInPixels(150).AlignRight().Suffix(" ms").Class("mono").WithTitle("Total execution time");
                           r.Cell("connectionOpenTime").WidthInPixels(180).Suffix(" ms").AlignRight().Class("mono").WithTitle("Total connection open time");
                           //r.Cell("dummy").WithTitle("-");
                       }))
                .Cell("Activities", TabLayout.Create()
                       .Row(r =>
                       {
                           r.Cell("ordinal").AsKey().WidthInPixels(40).WithTitle("#");
                           r.Cell("connection").WidthInPixels(80).WithTitle("\u2301"); // http://unicode-table.com
                           r.Cell("action").WidthInPixels(50).WithTitle("Action");
                           r.Cell("text").AsCode(CodeType.Sql).DisablePreview().WithTitle("Text");
                           r.Cell("details").WidthInPixels(50).WithTitle("Details");
                           r.Cell("rows").WidthInPixels(40).WithTitle("Rows");
                           r.Cell("fetchDuration").WidthInPercent(7).Suffix(" ms").AlignRight().Class("mono").WithTitle("Fetch");
                           r.Cell("duration").WidthInPercent(8).Suffix(" ms").AlignRight().Class("mono").WithTitle("Duration");
                           r.Cell("offset").WidthInPercent(8).Suffix(" ms").AlignRight().Prefix("T+ ").Class("mono").WithTitle("Offset");
                       })).Build();

        public override string Name
        {
            get { return "DataAccess"; }
        }        

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key. Only valid JavaScript identifiers should be used for future compatibility.</value>
        public string Key
        {
            get { return "glimpse_dataaccess"; }
        }


        public string DocumentationUri
        {
            get { return Constants.DocumentationUrl4Tab; }
        }

        public bool KeysHeadings
        {
            get { return true; }
        }

        public void Setup(ITabSetupContext context)
        {
            context.PersistMessages<DataAccessMessage>();
        }

        public object GetLayout()
        {
            return Layout;
        }

        public override RuntimeEvent ExecuteOn
        {
            get { return RuntimeEvent.EndRequest; }
        }       

        public override object GetData(ITabContext context)
        {
            var agg = new DataAccessAggregator(context);
            if (agg.HasMessages)
            {
                return new Dictionary<string,object>() 
                {
                    { "Statistics", new object[] { agg.GetStatistics() } }, 
                    { "Activities", agg.GetItems() }
                };
            }
            return null;
            //var messages = context.GetMessages<DataAccessMessage>().ToList();
            //if (messages.Count > 0)
            //{
            //    return messages.Select(x => new DataAccessTabItem(x)).ToList();
            //}
            ////return new object[1].Select(x => new DataAccessTabItem(new NoConnectionMessage("nixda"))).ToList();
            //return null;
            //var sanitizer = new CommandSanitizer();
            //var aggregator = new MessageAggregator(messages);
            //var queryMetadata = aggregator.Aggregate();

            //if (queryMetadata == null)
            //{
            //    return null;
            //}

            //var connections = new List<object[]> { new object[] { "Commands per Connection", "Duration" } };

            //foreach (var connection in queryMetadata.Connections.Values)
            //{
            //    if (connection.Commands.Count == 0 && connection.Transactions.Count == 0)
            //    {
            //        continue;
            //    }

            //    var commands = new List<object[]> { new object[] { "Transaction Start", "Ordinal", "Command", "Parameters", "Records", "Duration", "Offset", "Async", "Transaction End", "Errors" } };
            //    var commandCount = 1;
            //    foreach (var command in connection.Commands.Values)
            //    {
            //        // Transaction Start
            //        List<object[]> headTransaction = null;
            //        if (command.HeadTransaction != null)
            //        {
            //            headTransaction = new List<object[]> { new object[] { "\t▼ Transaction - Started", "Isolation Level - " + command.HeadTransaction.IsolationLevel } };
            //            if (!command.HeadTransaction.Committed.HasValue)
            //            {
            //                headTransaction.Add(new object[] { string.Empty, "Transaction was never completed", "error" });
            //            }
            //        }

            //        // Transaction Finish
            //        List<object[]> tailTransaction = null;
            //        if (command.TailTransaction != null)
            //        {
            //            tailTransaction = new List<object[]> { new object[] { "\t▲ Transaction - Finished", "Status - " + (command.TailTransaction.Committed.GetValueOrDefault() ? "Committed" : "Rollbacked") } };
            //        }

            //        // Parameters
            //        List<object[]> parameters = null;
            //        if (command.Parameters.Count > 0)
            //        {
            //            parameters = new List<object[]> { new object[] { "Name", "Value", "Type", "Size" } };
            //            foreach (var parameter in command.Parameters)
            //            {
            //                parameters.Add(new[] { parameter.Name, parameter.Value, parameter.Type, parameter.Size });
            //            }
            //        }

            //        // Exception
            //        List<object[]> errors = null;
            //        if (command.Exception != null)
            //        {
            //            var exception = command.Exception.GetBaseException();
            //            var exceptionName = command.Exception != exception ? command.Exception.Message + ": " + exception.Message : exception.Message;

            //            errors = new List<object[]> { new object[] { "Error", "Stack" }, new object[] { exceptionName, exception.StackTrace } };
            //        }

            //        // Commands
            //        var records = command.RecordsAffected == null || command.RecordsAffected < 0 ? command.TotalRecords : command.RecordsAffected;

            //        var status = errors != null ? "error" : (command.IsDuplicate ? "warn" : string.Empty);
            //        commands.Add(new object[] { headTransaction, string.Format("{0}{1}", command.HasTransaction ? "\t\t\t" : string.Empty, commandCount++), sanitizer.Process(command.Command, command.Parameters), parameters, records, command.Duration, command.Offset, command.IsAsync, tailTransaction, errors, status });
            //    }

            //    connections.Add(new[] { commands, connection.Duration.HasValue ? (object)connection.Duration.Value : null });
            //}

            //if (connections.Count > 1)
            //{
            //    //SqlStatistics sqlStatistics = SqlStatisticsCalculator.Caluculate(queryMetadata);

            //    return new Dictionary<string, object>
            //    {
            //        //{ "SQL Statistics", new object[] { new { sqlStatistics.ConnectionCount, sqlStatistics.QueryCount, sqlStatistics.TransactionCount, sqlStatistics.QueryExecutionTime, sqlStatistics.ConnectionOpenTime } } }, 
            //        { "SQL Statistics", new object[] {1 } }, 
            //        { "Queries", connections }
            //    };
            //}

            //return null;
        }
    }
}