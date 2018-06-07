using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Utilities;

namespace Core.Extensions
{
    public static class ActionExtensions
    {
        public static void TryAction(Action action, string errorMessage = null)
        {
            try
            {
                action();
            }
            catch(Exception ex)
            {
                if (!errorMessage.IsNullOrEmpty())
                {
                    Mail.SendMeAnEmail(errorMessage, ex.ToString());
                }
            }
        }
    }
}
