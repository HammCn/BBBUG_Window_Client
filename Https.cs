using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BBBUG {
    public static class Https
    {
        public static string CodeSuccess = "200";
        public static string CodeForbbiden = "403";
        public static string CodeLogin = "401";
        public static string CodeRedirectForce = "301";
        public static string CodeRedirect = "302";
        public static string CodeError = "500";
        public static string BaseApiUrl = "https://api.bbbug.com/api/";
        public static string AccessToken = "";
        public static string Plat = "windows app";
        public static string Version = "10000";

        public static async Task<JObject> PostAsync(string url, Dictionary<string, string> postDict)
        {
            if (!postDict.ContainsKey("access_token"))
            {
                postDict.Add("access_token", Https.AccessToken);
            }
            else
            {
                postDict["access_token"] = Https.AccessToken;
            }
            if (!postDict.ContainsKey("plat"))
            {
                postDict.Add("plat", Https.Plat);
            }
            else
            {
                postDict["plat"] = Https.Plat;
            }
            if (!postDict.ContainsKey("version"))
            {
                postDict.Add("version", Https.Version);
            }
            else
            {
                postDict["version"] = Https.Version;
            }
            var postData = new FormUrlEncodedContent(postDict);
            Console.WriteLine(postData.ToString());
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip
            };
            using (var http = new HttpClient(handler))
            {
                var response = await http.PostAsync(Https.BaseApiUrl + url, postData);
                string data = await response.Content.ReadAsStringAsync();
                return (JObject)JsonConvert.DeserializeObject(data);
            }
        }
        public static async Task<HttpResponseMessage> PostMusicUrl(string url, Dictionary<string, string> postDict)
        {
            if (!postDict.ContainsKey("access_token"))
            {
                postDict.Add("access_token", Https.AccessToken);
            }
            else
            {
                postDict["access_token"] = Https.AccessToken;
            }
            if (!postDict.ContainsKey("plat"))
            {
                postDict.Add("plat", Https.Plat);
            }
            else
            {
                postDict["plat"] = Https.Plat;
            }
            if (!postDict.ContainsKey("version"))
            {
                postDict.Add("version", Https.Version);
            }
            else
            {
                postDict["version"] = Https.Version;
            }
            var postData = new FormUrlEncodedContent(postDict);
            Console.WriteLine(postData.ToString());
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                AllowAutoRedirect=false
            };
            using (var http = new HttpClient(handler))
            {
                var response = await http.PostAsync(Https.BaseApiUrl + url, postData);
                return response;
            }
        }
    }

}