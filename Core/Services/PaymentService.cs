using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Caching;
using Core.Data;
using Core.Models;

namespace Core.Services
{
    public class PaymentService
    {
        private readonly DataProvider DataProvider = new DataProvider();

        public bool IsPro(User user)
        {
            if (user == null) return false;
            return GetProExpirationDate(user) > DateTime.Now;
        }

        public void ClearCachedInfo(User user)
        {
            CacheClient.Default.Remove("paymentsfor" + user.Id);
        }

        public bool IsProCached(CachePeriod cachePeriod, User user)
        {
            if (user == null) return false;

            var payments = CacheClient.Default.GetOrAdd<List<Payment>>("paymentsfor" + user.Id, cachePeriod,
                () => GetPayments(user.Id));
            if (payments == null) return false;

            return GetProExpirationDateForPayments(payments) > DateTime.Now;
        }

        public DateTime GetProExpirationDate(User user)
        {
            var payments = GetPayments(user.Id);
            return GetProExpirationDateForPayments(payments);
        }

        private DateTime GetProExpirationDateForPayments(List<Payment> payments)
        {
            var expiration = DateTime.Now;
            foreach (var payment in payments)
            {
                var paymentExpires = payment.Date;
                switch (payment.OrderType)
                {
                    case "year": paymentExpires = paymentExpires.AddYears(1); break;
                }
                if (paymentExpires > DateTime.Now)
                    expiration = paymentExpires;
            }
            return expiration;
        }

        public Payment GetPayment(string transactionId)
        {
            return DataProvider.GetPayment(transactionId);
        }

        public List<Payment> GetPayments(int userId)
        {
            return DataProvider.GetPayments(userId);
        }

        public Payment InsertPayment(Payment order)
        {
            order.Id = DataProvider.InsertPayment(order);
            return order;
        }
    }
}
