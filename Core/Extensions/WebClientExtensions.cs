using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class WebClientExtensions
    {
        public static HttpStatusCode GetStatusCode(this WebClient client)
        {
            var responseField = client.GetType().GetField("m_WebResponse", BindingFlags.Instance | BindingFlags.NonPublic);

            if (responseField != null)
            {
                HttpWebResponse response = responseField.GetValue(client) as HttpWebResponse;
                if (response != null)
                {
                    return response.StatusCode;
                }
            }
            return HttpStatusCode.ExpectationFailed;
        }
    }
}
