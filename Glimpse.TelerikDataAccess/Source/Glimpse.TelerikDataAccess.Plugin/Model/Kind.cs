using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glimpse.TelerikDataAccess.Plugin.Model
{
    [Flags]
    public enum Kind
    {
        None = 0x0,
        OpenDB = 0x0101,
        CloseDB = 0x0102,
        ChangeMeta = 0x0104,

        Sql = 0x1001,
        Reader = 0x1002,
        Scalar = 0x1004,
        NonQuery = 0x1008,
        Done = 0x1010,
        
        Begin = 0x2001,
        Commit = 0x2002,
        Rollback = 0x2004,
        Enlist = 0x2008,

        Evict = 0x4001,
        CachedObject = 0x4002,
        CachedQuery = 0x4004,
        CachedCount = 0x4008,

        Open = 0x8001,
        Close = 0x8002,
        GetSchema = 0x8004
    }
}
