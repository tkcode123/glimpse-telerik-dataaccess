using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glimpse.TelerikDataAccess.Plugin.Model
{
    class NoConnectionMessage : DataAccessMessage
    {
        public NoConnectionMessage(string text) : base()
        {
            EventName = text;
        }
    }
}
