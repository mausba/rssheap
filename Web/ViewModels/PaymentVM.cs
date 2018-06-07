using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Core.Models;

namespace Web.ViewModels
{
    public class PaymentVM
    {
        public User User { get; set; }
        public Payment Payment { get; set; }
    }
}