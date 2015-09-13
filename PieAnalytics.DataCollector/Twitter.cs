using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PieAnalytics.DataCollector
{
    public class Twitter
    {
        public string GetTweet(string query)
        {
            // oauth application keys and URL
            string url = string.Format("https://api.twitter.com/1.1/search/tweets.json?q={0}&lang=en&locale=en&count=100", query);
            string oauthconsumerkey = "Osh5P4cXZ21lu0YB6MurW0uz5";
            string oauthtoken = "248633224-qig937qb7id3Zn4rEmtyLNlaGn0zzhzeZFd47Vaq";
            string oauthconsumersecret = "KcW9BtLWtiewpeK0X1DshdzY2jC0HPHEdevfaJ2IA3bXCjQ13i";
            string oauthtokensecret = "5D0vUiGh2A4c9p9UHCkViHageXh3oD8DSsbg2GXP2ouaF";
            string oauthsignaturemethod = "HMAC-SHA1";
            string oauthversion = "1.0";
            string oauthnonce = Convert.ToBase64String(new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString()));
            TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            string oauthtimestamp = Convert.ToInt64(timeSpan.TotalSeconds).ToString();
            SortedDictionary<string, string> basestringParameters = new SortedDictionary<string, string>();
            basestringParameters.Add("q", query);
            basestringParameters.Add("oauth_version", oauthversion);
            basestringParameters.Add("oauth_consumer_key", oauthconsumerkey);
            basestringParameters.Add("oauth_nonce", oauthnonce);
            basestringParameters.Add("oauth_signature_method", oauthsignaturemethod);
            basestringParameters.Add("oauth_timestamp", oauthtimestamp);
            basestringParameters.Add("oauth_token", oauthtoken);
            //Build the signature string
            StringBuilder baseString = new StringBuilder();
            baseString.Append("GET" + "&");
            baseString.Append(EncodeCharacters(Uri.EscapeDataString(url.Split('?')[0]) + "&"));
            foreach (KeyValuePair<string, string> entry in basestringParameters)
            {
                baseString.Append(EncodeCharacters(Uri.EscapeDataString(entry.Key + "=" + entry.Value + "&")));
            }

            //Remove the trailing ambersand char last 3 chars - %26
            string finalBaseString = baseString.ToString().Substring(0, baseString.Length - 3);

            //Build the signing key
            string signingKey = EncodeCharacters(Uri.EscapeDataString(oauthconsumersecret)) + "&" +
            EncodeCharacters(Uri.EscapeDataString(oauthtokensecret));

            //Sign the request
            HMACSHA1 hasher = new HMACSHA1(new ASCIIEncoding().GetBytes(signingKey));
            string oauthsignature = Convert.ToBase64String(
              hasher.ComputeHash(new ASCIIEncoding().GetBytes(finalBaseString)));

            //Tell Twitter we don't do the 100 continue thing
            ServicePointManager.Expect100Continue = false;

            //authorization header
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(@url);
            StringBuilder authorizationHeaderParams = new StringBuilder();
            authorizationHeaderParams.Append("OAuth ");
            authorizationHeaderParams.Append("oauth_nonce=" + "\"" + Uri.EscapeDataString(oauthnonce) + "\",");
            authorizationHeaderParams.Append("oauth_signature_method=" + "\"" + Uri.EscapeDataString(oauthsignaturemethod) + "\",");
            authorizationHeaderParams.Append("oauth_timestamp=" + "\"" + Uri.EscapeDataString(oauthtimestamp) + "\",");
            authorizationHeaderParams.Append("oauth_consumer_key=" + "\"" + Uri.EscapeDataString(oauthconsumerkey) + "\",");
            if (!string.IsNullOrEmpty(oauthtoken))
                authorizationHeaderParams.Append("oauth_token=" + "\"" + Uri.EscapeDataString(oauthtoken) + "\",");
            authorizationHeaderParams.Append("oauth_signature=" + "\"" + Uri.EscapeDataString(oauthsignature) + "\",");
            authorizationHeaderParams.Append("oauth_version=" + "\"" + Uri.EscapeDataString(oauthversion) + "\"");

            webRequest.Headers.Add("Authorization", authorizationHeaderParams.ToString());

            webRequest.Method = "GET";
            webRequest.ContentType = "application/x-www-form-urlencoded";

            //Allow us a reasonable timeout in case Twitter's busy
            webRequest.Timeout = 3 * 60 * 1000;
            try
            {
                HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse;
                Stream dataStream = webResponse.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                return responseFromServer;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string EncodeCharacters(string data)
        {
            //as per OAuth Core 1.0 Characters in the unreserved character set MUST NOT be encoded
            //unreserved = ALPHA, DIGIT, '-', '.', '_', '~'
            if (data.Contains("!"))
                data = data.Replace("!", "%21");
            if (data.Contains("'"))
                data = data.Replace("'", "%27");
            if (data.Contains("("))
                data = data.Replace("(", "%28");
            if (data.Contains(")"))
                data = data.Replace(")", "%29");
            if (data.Contains("*"))
                data = data.Replace("*", "%2A");
            if (data.Contains(","))
                data = data.Replace(",", "%2C");

            return data;
        }

        //public string Tweet(string query)
        //{
        //    // Set up your credentials
        //    TwitterCredentials.SetCredentials("Access_Token", "Access_Token_Secret", "Consumer_Key", "Consumer_Secret");
        //    // Set up your application credentials
        //    TwitterCredentials.SetCredentials("Access_Token", "Access_Token_Secret", "Consumer_Key", "Consumer_Secret");
        //    var credentials = TwitterCredentials.CreateCredentials("Access_Token", "Access_Token_Secret", "Consumer_Key", "Consumer_Secret");
        //    TwitterCredentials.ExecuteOperationWithCredentials(credentials, () =>
        //    {
        //        Tweet.PublishTweet("myTweet");
        //    });

        //}
    }
}
