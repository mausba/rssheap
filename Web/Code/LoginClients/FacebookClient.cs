using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;

namespace MvcWeb.Code.LoginClients
{
    public class FacebookClient
    {
        private const String SESSION_NAME_TOKEN = "UserFacebookToken";
        public FacebookClient()
        {
            TokenEndpoint = new Uri("https://graph.facebook.com/oauth/access_token");
            AuthorizationEndpoint = new Uri("https://graph.facebook.com/oauth/authorize");
            MeGraphEndpoint = new Uri("https://graph.facebook.com/me");
            ClientIdentifier = "685201244886528";
            Secret = "cb030a81c783aec5893e57ff2003de3a";
            LocalSubDomain = "local.rssheap.com"; //TODO rename to the correct domain
        }

        public Uri TokenEndpoint { get; set; }
        public Uri AuthorizationEndpoint { get; set; }
        public Uri MeGraphEndpoint { get; set; }
        public String Secret { get; set; }
        public String ClientIdentifier { get; set; }
        private String LocalSubDomain { get; set; }


        public LoginResult Authorize(string returnUrl)
        {
            var errorReason = HttpContext.Current.Request.Params["error_reason"];
            var userDenied = errorReason != null;
            if (userDenied)
                return LoginResult.Denied;
            var verificationCode = HttpContext.Current.Request.Params["code"];
            var redirectUrl = GetResponseUrl();
            var needToGetVerificationCode = verificationCode == null;
            if (needToGetVerificationCode)
            {
                var url = AuthorizationEndpoint + "?" +
                          "client_id=" + ClientIdentifier + "&" +
                          "redirect_uri=" + redirectUrl + "&" +
                          "scope=email" + "&" +
                          "state=" + HttpUtility.UrlEncode(returnUrl);

                HttpContext.Current.Response.Redirect(url, true);
                return LoginResult.Denied;
            }
            var token = ExchangeCodeForToken(verificationCode, redirectUrl);
            HttpContext.Current.Session[SESSION_NAME_TOKEN] = token;
            return LoginResult.Authorized;
        }
        public Boolean IsCurrentUserAuthorized()
        {
            return HttpContext.Current.Session[SESSION_NAME_TOKEN] != null;
        }
        public FacebookGraph GetCurrentUser()
        {
            var token = HttpContext.Current.Session[SESSION_NAME_TOKEN];
            if (token == null)
                return null;
            var url = MeGraphEndpoint + "?" +
                      "access_token=" + token +
                      "&fields=id,name,email";
            var request = WebRequest.CreateDefault(new Uri(url));
            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    var responseReader = new StreamReader(responseStream);
                    var responseText = responseReader.ReadToEnd();
                    var user = FacebookGraph.Deserialize(responseText);
                    return user;
                }
            }
        }
        private String ExchangeCodeForToken(String code, Uri redirectUrl)
        {
            var url = TokenEndpoint + "?" +
                      "client_id=" + ClientIdentifier + "&" +
                      "redirect_uri=" + redirectUrl + "&" +
                      "client_secret=" + Secret + "&" +
                      "code=" + code;
            var request = WebRequest.CreateDefault(new Uri(url));
            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    var responseReader = new StreamReader(responseStream);
                    var responseText = responseReader.ReadToEnd();
                    JToken jToken = JToken.Parse(responseText);

                    var token = jToken["access_token"].Value<string>();
                    return token;
                }
            }
        }
        private Uri GetResponseUrl()
        {
            // Remove any parameters. Apparently Facebook does not support state: http://forum.developers.facebook.net/viewtopic.php?pid=255231
            // If you do not do this, you will get 'Error validating verification code'
            var url = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + "/Account/AuthenticatedWithFacebook";

            var replaceLocalhostWithSubdomain = HttpContext.Current.Request.Url.Host == "localhost";
            if (!replaceLocalhostWithSubdomain)
                return new Uri(url);
            //// Facebook does not like localhost, you can only use the configured url. To get around this,
            //// log into facebook
            //// and set your Site Domain setting, ie happycow.com. 
            //// Next edit C:\Windows\System32\drivers\etc\hosts, adding the line: 
            //// 127.0.0.1       local.happycow.cow
            //// And lastly, set LocalSubDomain to local.happycow.cow
            url = url.Replace("localhost", LocalSubDomain);
            url = url.Replace(":" + HttpContext.Current.Request.Url.Port, string.Empty);
            return new Uri(url);
        }
    }

    [DataContract]
    public class FacebookGraph
    {
        private static DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(FacebookGraph));
        // Note: Changed from int32 to string based on Antonin Jelinek advise of an overflow
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "email")]
        public string Email { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "first_name")]
        public string FirstName { get; set; }

        [DataMember(Name = "last_name")]
        public string LastName { get; set; }

        [DataMember(Name = "link")]
        public Uri Link { get; set; }

        [DataMember(Name = "birthday")]
        public string Birthday { get; set; }

        public static FacebookGraph Deserialize(string json)
        {
            if (String.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException("json");
            }

            return Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        }

        public static FacebookGraph Deserialize(Stream jsonStream)
        {
            if (jsonStream == null)
            {
                throw new ArgumentNullException("jsonStream");
            }

            return (FacebookGraph)jsonSerializer.ReadObject(jsonStream);
        }
    }
}