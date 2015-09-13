using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace PieAnalytics.DataCollector
{
    public class BestBuy
    {
        private static string API_KEY = "hgek2dttncr54tkxb5jgjqe7";
        private static int API_LIMIT_DAY = 50000;  //50,000 calls per day
        private static int API_LIMIT_SEC = 5;      //5 calls per sec
        private static DateTime LastAPICallMadeDate = DateTime.Now;
        private static int TotalAPICallMadeToday = 0;

        public BsonDocument GetReview(string query, string page)
        {
            string resource_url = string.Format("http://api.bestbuy.com/v1/reviews(title={0}*|comment={0}*)?format=json&apiKey={1}&pageSize=15&page=1", query, API_KEY);
            Uri uri = new Uri(resource_url);

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = uri;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                var responseasync = client.GetAsync(uri);
                while (!responseasync.IsCompleted) ;
                HttpResponseMessage response = responseasync.Result;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync();
                    while (!data.IsCompleted) ;
                    var jsonData = Newtonsoft.Json.Linq.JObject.Parse(data.Result);
                    var jdata = jsonData["reviews"].ToList();
                    var reviews = new BsonArray();
                    for(int i=0; i<jdata.Count();i++)
                    {
                        var rev = new BsonDocument
                        {
                            {"id",jdata[i]["id"].ToString()},
                            {"sku",jdata[i]["sku"].ToString()},
                            {"rating",jdata[i]["rating"].ToString()},
                            {"title",jdata[i]["title"].ToString()},
                            {"comment",jdata[i]["comment"].ToString()},
                            {"submissionTime",jdata[i]["submissionTime"].ToString()}
                        };

                        reviews.Add(rev);
                    }
                    var document = new BsonDocument
                    {
                        {"reviews", reviews}
                    };

                    return document;

                }
                else
                {
                    throw new HttpRequestException(response.StatusCode.ToString());
                }
            }
        }

    }
}
