using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Data;

namespace Core.Services
{
    public class LogService
    {
        public int InsertError(string error, string source)
        {
            return new DataProvider().InsertLog(error, source);
        }
    }
}
