﻿using System;
using System.Collections.Generic;
using System.Linq;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Extensions;
using Glimpse.Core.Tab.Assist;
using Glimpse.TelerikDataAccess.Plugin.Model;
using Glimpse.TelerikDataAccess.Plugin.Tracing;

namespace Glimpse.TelerikDataAccess.Plugin.Tab
{
    public class DataAccess : TabBase, ITabSetup, IKey, ITabLayout, IDocumentation, ILayoutControl
    {
        internal static readonly object Layout = TabLayout.Create()
                .Cell("Statistics", TabLayout.Create()
                       .Row(r =>
                       {
                           r.Cell("connectionCount").WidthInPixels(50).AlignRight().WithTitle("# Connections");
                           r.Cell("queryCount").WidthInPixels(50).AlignRight().WithTitle("# SQL Queries");
                           r.Cell("splittedCount").WidthInPixels(50).AlignRight().WithTitle("# Splitted LINQ");
                           r.Cell("rows").WidthInPixels(50).AlignRight().WithTitle("# Rows");
                           r.Cell("transactionCount").WidthInPixels(50).AlignRight().WithTitle("# Transactions");
                           r.Cell("secondLevelHits").WidthInPixels(50).AlignRight().WithTitle("# L2C Query Hits");
                           r.Cell("secondLevelObjects").WidthInPixels(50).AlignRight().WithTitle("# L2C Object Hits");
                           r.Cell("gcCounts").AlignRight().WidthInPixels(150).WithTitle("# GC Collects");
                           r.Cell("executionTime").WidthInPixels(150).AlignRight().Suffix(" ms").Class("mono").WithTitle("\u03a3 Execution Time");
                           r.Cell("connectionOpenTime").WidthInPixels(150).Suffix(" ms").AlignRight().Class("mono").WithTitle("\u03a3 Connection Opening Time");
                       }))
                .Cell("Activities", TabLayout.Create()
                       .Row(r =>
                       {
                           r.Cell("hint").DisablePreview().Class("glimpse_metadata").WidthInPixels(20).WithTitle("!");
                           r.Cell("ordinal").AsKey().AlignRight().WidthInPixels(20).WithTitle("#");
                           r.Cell("connection").AlignRight().WidthInPixels(30).WithTitle("\u260e"); // \2301 http://unicode-table.com
                           r.Cell("transaction").AlignRight().WidthInPixels(30).WithTitle("Txn"); 
                           r.Cell("action").WidthInPixels(50).WithTitle("Action");
                           r.Cell("text").AsCode(CodeType.Sql).DisablePreview().WithTitle("Text");
                           r.Cell("details").WidthInPercent(20).DisablePreview().WithTitle("Details");
                           r.Cell("rows").AlignRight().WidthInPixels(30).WithTitle("Rows");
                           r.Cell("fetchDuration").WidthInPercent(6).Suffix(" ms").AlignRight().Class("mono").WithTitle("Fetch");
                           r.Cell("duration").WidthInPercent(6).Suffix(" ms").AlignRight().Class("mono").WithTitle("Duration");
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
            get { return RuntimeEvent.EndSessionAccess; }
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
        }
    }
}