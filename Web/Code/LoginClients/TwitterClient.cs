using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.OAuth.ChannelElements;
using DotNetOpenAuth.OAuth.Messages;
using DotNetOpenAuth.OpenId.Extensions.OAuth;

namespace MvcWeb.Code.LoginClients
{
    public class TwitterClient
    {
        public string UserName { get; set; }
        public string UserId { get; set; }

        private static readonly ServiceProviderDescription ServiceDescription =
            new ServiceProviderDescription
            {
                RequestTokenEndpoint = new MessageReceivingEndpoint(
                                           "https://api.twitter.com/oauth/request_token",
                                           HttpDeliveryMethods.GetRequest |
                                           HttpDeliveryMethods.AuthorizationHeaderRequest),

                UserAuthorizationEndpoint = new MessageReceivingEndpoint(
                                          "https://api.twitter.com/oauth/authorize",
                                          HttpDeliveryMethods.GetRequest |
                                          HttpDeliveryMethods.AuthorizationHeaderRequest),

                AccessTokenEndpoint = new MessageReceivingEndpoint(
                                          "https://api.twitter.com/oauth/access_token",
                                          HttpDeliveryMethods.GetRequest |
                                          HttpDeliveryMethods.AuthorizationHeaderRequest),

                TamperProtectionElements = new ITamperProtectionChannelBindingElement[] { new HmacSha1SigningBindingElement() },
            };

        IConsumerTokenManager _tokenManager;

        public TwitterClient(IConsumerTokenManager tokenManager)
        {
            _tokenManager = tokenManager;
        }

        public void StartAuthentication(string returnUrl)
        {
            var request = HttpContext.Current.Request;
            using (var twitter = new WebConsumer(ServiceDescription, _tokenManager))
            {
                var callbackUrl = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + "/Account/AuthenticatedWithTwitter";
                if (!returnUrl.IsNullOrEmpty())
                    callbackUrl += "?returnUrl=" + HttpUtility.UrlEncode(returnUrl);

                var callBackUrl = new Uri(callbackUrl);
                twitter.Channel.Send(
                    twitter.PrepareRequestUserAuthorization(callBackUrl, null, null)
                );
            }
        }

        public bool FinishAuthentication()
        {
            using (var twitter = new WebConsumer(ServiceDescription, _tokenManager))
            {
                var accessTokenResponse = twitter.ProcessUserAuthorization();
                if (accessTokenResponse != null)
                {
                    UserId = accessTokenResponse.ExtraData["user_id"];
                    UserName = accessTokenResponse.ExtraData["screen_name"];
                    return true;
                }
            }

            return false;
        }
    }

    internal class InMemoryTokenManager : IConsumerTokenManager
    {
        private Dictionary<string, string> tokensAndSecrets =
            new Dictionary<string, string>();

        public InMemoryTokenManager(string consumerKey, string consumerSecret)
        {
            if (String.IsNullOrEmpty(consumerKey))
            {
                throw new ArgumentNullException("consumerKey");
            }

            this.ConsumerKey = consumerKey;
            this.ConsumerSecret = consumerSecret;
        }

        public string ConsumerKey { get; private set; }
        public string ConsumerSecret { get; private set; }

        public string GetTokenSecret(string token)
        {
            return this.tokensAndSecrets[token];
        }

        public void StoreNewRequestToken(UnauthorizedTokenRequest request,
                                        ITokenSecretContainingMessage response)
        {
            this.tokensAndSecrets[response.Token] = response.TokenSecret;
        }

        public void ExpireRequestTokenAndStoreNewAccessToken(string consumerKey,
            string requestToken, string accessToken, string accessTokenSecret)
        {
            this.tokensAndSecrets.Remove(requestToken);
            this.tokensAndSecrets[accessToken] = accessTokenSecret;
        }

        public TokenType GetTokenType(string token)
        {
            throw new NotImplementedException();
        }

        public void StoreOpenIdAuthorizedRequestToken(string consumerKey,
            AuthorizationApprovedResponse authorization)
        {
            this.tokensAndSecrets[authorization.RequestToken] = String.Empty;
        }
    }
}