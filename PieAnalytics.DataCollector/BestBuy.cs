using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PieAnalytics.DataCollector
{
    public class BestBuy
    {
        private static string API_KEY = "hgek2dttncr54tkxb5jgjqe7";
        private static int API_LIMIT_DAY = 50000;  //50,000 calls per day
        private static int API_LIMIT_SEC = 5;      //5 calls per sec
        private static DateTime LastAPICallMadeDate = DateTime.Now;
        private static int TotalAPICallMadeToday = 0;


        private string[] GetProducts(string query)
        {
            string resource_url = string.Format("http://api.bestbuy.com/v1/products(description={0}&customerReviewCount>15)?show=sku,name&pageSize=15&page=1&apiKey={1}&format=json",
                query, API_KEY);
            Uri uri = new Uri(resource_url);

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = uri;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // New code:
                var responseasync = client.GetAsync(uri);
                while (!responseasync.IsCompleted) ;
                HttpResponseMessage response = responseasync.Result;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync();
                    while (!data.IsCompleted) ;
                    var jsondata = JsonConvert.DeserializeObject(data.Result);

                }
                else
                {
                    throw new HttpRequestException(response.StatusCode.ToString());
                }

            }

            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(resource_url);
            //request.Method = "GET";
            //request.ContentType = "application/json";
            //WebResponse response = request.GetResponse();
            //response.GetResponseStream();
            return new string[] { };
        }

        public void GetReview(string query)
        {
            GetProducts(query);
        }
    }
}
