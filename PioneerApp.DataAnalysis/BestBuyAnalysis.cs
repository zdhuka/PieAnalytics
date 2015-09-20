using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core;

namespace PioneerApp.DataAnalysis
{
    public class BestBuyAnalysis
    {
        protected static IMongoClient _client;
        protected static IMongoDatabase _database;

        public int AnalyzeReviews(string jobid)
        {
            var d = new System.Configuration.AppSettingsReader().GetValue("MongoConnection", typeof(string));
            _client = new MongoDB.Driver.MongoClient(d.ToString());
            _database = _client.GetDatabase("PieAnalytics");
            BsonJavaScript map = @"
            function() {
                var data = this.reviews.reviews;
                var jid = this.jobid;
                var src = this.source;
                data.forEach(function(d){
                    var key = { rating : d.rating, jobid : jid, source : src};
                    var value = { count: 1 };
                    emit(key, value);
                });
            }";

            BsonJavaScript reduce = @"        
            function(key, values) {
                var result = {count : 0};
                values.forEach(function(value){               
                    result.count += value.count;
                });
                return result;
            }";

            var collection = _database.GetCollection<BsonDocument>("Reviews");
            var options = new MapReduceOptions<BsonDocument, BsonDocument>();
            options.OutputOptions = MapReduceOutputOptions.Merge("ReviewResults");
            var filter = Builders<BsonDocument>.Filter.Eq("jobid", jobid);
            options.Filter = filter;
            var asyncresults = collection.MapReduceAsync<BsonDocument>(map, reduce, options);
            while (!asyncresults.IsCompleted) ;
            return asyncresults.Exception == null ? 1 : -1;
        }

    }
}
