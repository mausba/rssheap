using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Web.ViewModels
{
    public class CardVM
    {
        public User User { get; set; }
        public Payment Payment { get; set; }
        public bool isPro { get; set; }
    }
}
