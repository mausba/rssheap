using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MvcWeb.ViewModels
{
    public class JsonVM
    {
        public bool ok { get; set; }
        public string error { get; set; }
        public object tag { get; set; }
    }
}