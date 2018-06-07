using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Core.Utilities
{
    public static class Google
    {
        public static int GetNumberOfShares(string url)
        {
            try
            {
                string googleApiUrl = "https://clients6.google.com/rpc?key=AIzaSyCKSbrvQasunBoV16zDH9R33D88CeLr9gQ";

                string postData = @"[{""method"":""pos.plusones.get"",""id"":""p"",""params"":{""nolog"":true,""id"":""" + url + @""",""source"":""widget"",""userId"":""@viewer"",""groupId"":""@self""},""jsonrpc"":""2.0"",""key"":""p"",""apiVersion"":""v1""}]";

                System.Net.HttpWebRequest request = (HttpWebRequest)WebRequest.Create(googleApiUrl);
                request.Method = "POST";
                request.ContentType = "application/json-rpc";
                request.ContentLength = postData.Length;

                var writeStream = request.GetRequestStream();
                var encoding = new UTF8Encoding();
                var bytes = encoding.GetBytes(postData);
                writeStream.Write(bytes, 0, bytes.Length);
                writeStream.Close();
                 
                var response = (HttpWebResponse)request.GetResponse();
                var responseStream = response.GetResponseStream();
                var readStream = new StreamReader(responseStream, Encoding.UTF8);
                var jsonString = readStream.ReadToEnd();

                readStream.Close();
                response.Close();

                var json = JArray.Parse(jsonString);
                int count = Int32.Parse(json[0]["result"]["metadata"]["globalCounts"]["count"].Value<string>().Replace(".0", ""));

                return count;
            }
            catch { }
            return 0;
        }
    }
}
