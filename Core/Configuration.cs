using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class Configuration
    {
        public static string GetConnectionString()
        {
            return ConfigurationManager.AppSettings["connstring"];
        }

        public static bool IsArtWebsite()
        {
            return ConfigurationManager.AppSettings["site"] == "art";
        }

        public static int GetHashIterations()
        {
            return 10002;
        }

        internal static string GetDomainKey()
        {
            return ConfigurationManager.AppSettings["domainkey"];
        }

        public static string GetSMTPUserName()
        {
            return ConfigurationManager.AppSettings["smtpusername"];
        }

        public static string GetSMTPPassword()
        {
            return ConfigurationManager.AppSettings["smtppassword"];
        }
    }
}
