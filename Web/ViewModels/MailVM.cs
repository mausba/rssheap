using Core.Models;
using Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Web.ViewModels
{
    public class MailVM
    {
        public Article Article { get; set; }
        public ArticlesRequest Request { get; set; }
    }
}