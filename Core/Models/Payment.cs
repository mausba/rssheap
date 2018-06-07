using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TransactionId { get; set; }
        public string OrderType { get; set; }
        public decimal Amount { get; set; }
        public string Email { get; set; }
        public DateTime Date { get; set; }
        public string FormValues { get; set; }

        public bool IsNew { get; set; }
    }
}
