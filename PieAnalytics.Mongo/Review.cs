using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.JsonResult;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PieAnalytics.Mongo
{
    public class Review
    {

        protected static IMongoClient _client;
        protected static IMongoDatabase _database;
        /// <summary>
        /// Insert API review data ino MongoDB
        /// </summary>
        /// <param name="jsondata">BsonArray as jsondata which contaons only reviews</param>
        /// <param name="jobid">The ctaual user jonb id from RDBMS</param>
        /// <param name="source">API source</param>
        /// <returns>-1 if unsuccessfully, 1 if success</returns>
        public int InsertReview(BsonDocument jsondata, string jobid, string source)
        {
            var d = new System.Configuration.AppSettingsReader().GetValue("MongoConnection", typeof(string));
            _client = new MongoDB.Driver.MongoClient(d.ToString());
            _database = _client.GetDatabase("PieAnalytics");
            string collectionname = "Reviews";
            var coll = _database.GetCollection<BsonDocument>(collectionname);
            var data = new BsonDocument
            {
                {"jobid",jobid},
                {"reviews",jsondata},
                {"source",source}
            };
            var insertdata = coll.InsertOneAsync(data);
            while (!insertdata.IsCompleted) ;
            return insertdata.Exception == null ? 1 : -1;
        }


    }
}
