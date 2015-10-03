using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PieAnalytics.DataEntity;
using PieAnalytics.DataCollector;
using PieAnalytics.Mongo;
using MongoDB.Bson;
using PioneerApp.DataAnalysis;

namespace PieAnalytics.Controllers
{
    public class JobsController : Controller
    {
        private PieAnalyticsEntities db = new PieAnalyticsEntities();

        // GET: Jobs
        public ActionResult Index()
        {
            return View(db.Jobs.ToList());
        }

        // GET: Jobs/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Job job = db.Jobs.Find(id);
            if (job == null)
            {
                return HttpNotFound();
            }
            return View(job);
        }

        // GET: Jobs/Create
        public ActionResult Create()
        {
            //WindowsService Starts here

            System.Diagnostics.EventLog eventLog1 = new System.Diagnostics.EventLog();

            if (!System.Diagnostics.EventLog.SourceExists("PieAnalyticsWindowsService"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "PieAnalyticsWindowsService", "PieAnalyticsWindowsServiceLog");
            }
            eventLog1.Source = "PieAnalyticsWindowsServiceService";
            eventLog1.Log = "PieAnalyticsWindowsServiceLog";
            eventLog1.WriteEntry("Service Setting up");



            // TODO: Insert activities here.
            eventLog1.WriteEntry("Fetching query data from the System", System.Diagnostics.EventLogEntryType.Information);
            //var db = new PieAnalytics.DataEntity.PieAnalyticsEntities();
            var _mongodb = new Review();
            int status = (int)JobStatus.Queued;
            var jobentityquery = (from a in db.Jobs
                                  where a.Status == status
                                  orderby a.InsertDate descending
                                  select a);
            Job jobentity = jobentityquery.FirstOrDefault();
            if (jobentity != null)
            {
                jobentity.Status = (int)JobStatus.Processing;
                jobentity.UpdateDate = DateTime.Now;
                //Update Status of Job in Porcesing
                db.SaveChangesAsync();
                eventLog1.WriteEntry("Updating record for that query in the System", System.Diagnostics.EventLogEntryType.Information);

                foreach (var d in jobentity.Keywords.Split(',').ToList())
                {
                    eventLog1.WriteEntry("Fetching social media data into the System", System.Diagnostics.EventLogEntryType.Information);
                    //Call the Rest API's 
                    // Call the API and fetch data
                    BestBuy bestbuy = new BestBuy();
                    BsonDocument data = bestbuy.GetReview(d, "");

                    // Store the result in MongoDB
                    _mongodb.InsertReview(data, jobentity.JobID.ToString(), "BestBuy");
                    eventLog1.WriteEntry("Finished fetching record for that data into the System", System.Diagnostics.EventLogEntryType.Information);
                    //start MongoDB mapreduce
                }

                eventLog1.WriteEntry("Push sentiment analysis of that data within the System", System.Diagnostics.EventLogEntryType.Information);
                // Call Map Reduce Program for Aggreating data collected using mapreduce
                BestBuyAnalysis analysis = new BestBuyAnalysis();
                analysis.AnalyzeReviews(jobentity.JobID.ToString());
                //Update SQL database on sentiment analysis
                List<BsonDocument> ratedData = analysis.GetResultAnalysedReviws(jobentity.JobID.ToString());

                JobResult bestbuy_result = null;
                //For Best Buy
                foreach (var d in ratedData)
                {
                    BsonDocument _id = d.GetElement("_id").Value.ToBsonDocument();
                    if (bestbuy_result == null)
                    {
                        bestbuy_result = new JobResult();
                        bestbuy_result.JobID = new Guid(_id.GetElement("jobid").Value.ToString());
                        bestbuy_result.Source = (string)_id.GetElement("source").Value;
                        bestbuy_result.InsertDate = DateTime.Now;
                        bestbuy_result.UpdateDate = DateTime.Now;
                    }
                    if (bestbuy_result != null)
                    {
                        string rating = _id.GetElement("rating").Value.ToString();
                        var value = d.GetElement("value").Value.ToBsonDocument();
                        var count = value.GetElement("count").Value.ToString();
                        if (rating == "1")
                        {
                            bestbuy_result.Rating1 = Convert.ToInt32(count);
                        }
                        if (rating == "2")
                        {
                            bestbuy_result.Rating2 = Convert.ToInt32(count);
                        }
                        if (rating == "3")
                        {
                            bestbuy_result.Rating3 = Convert.ToInt32(count);
                        }
                        if (rating == "4")
                        {
                            bestbuy_result.Rating4 = Convert.ToInt32(count);
                        }
                        if (rating == "5")
                        {
                            bestbuy_result.Rating5 = Convert.ToInt32(count);
                        }
                    }
                }
                db.JobResults.Add(bestbuy_result);
                eventLog1.WriteEntry("Finished processing the query and data from/in/within the System", System.Diagnostics.EventLogEntryType.Information);
                // Update Job Status as completed
                jobentity.Status = (int)JobStatus.Finished;
                db.SaveChanges();
                eventLog1.WriteEntry("Finished exporting data and Job is completed", System.Diagnostics.EventLogEntryType.Information);
            }

            //WindowsService Ends here


            return View();
        }

        // POST: Jobs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "JobID,JobName,Keywords,Status,InsertDate,UpdateDate")] Job job)
        {
            if (ModelState.IsValid)
            {
                job.UserID = db.UserProfiles.Where(m => m.UserName.Equals(User.Identity.Name)).Single().UserId;
                job.JobID = Guid.NewGuid();
                db.Jobs.Add(job);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(job);
        }

        // GET: Jobs/Edit/5
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Job job = db.Jobs.Find(id);
            if (job == null)
            {
                return HttpNotFound();
            }
            return View(job);
        }

        // POST: Jobs/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "JobID,JobName,Keywords,Status,InsertDate,UpdateDate")] Job job)
        {
            if (ModelState.IsValid)
            {
                job.UserID = db.UserProfiles.Where(m => m.UserName.Equals(User.Identity.Name)).Single().UserId;
                job.UpdateDate = DateTime.Now;
                db.Entry(job).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(job);
        }

        // GET: Jobs/Delete/5
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Job job = db.Jobs.Find(id);
            if (job == null)
            {
                return HttpNotFound();
            }
            return View(job);
        }

        // POST: Jobs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            Job job = db.Jobs.Find(id);
            db.Jobs.Remove(job);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Jobs/Result/5
        public ActionResult Result(Guid? id)
        {
            if (id == null)
            {
                return HttpNotFound("Invalid/No URL Parameter found");
            }
            int userid = db.UserProfiles.Where(n => n.UserName.Equals(User.Identity.Name)).Single().UserId;
            Job job = db.Jobs.Where(m => m.UserID.Equals(userid)
                                    && m.JobID.Equals(id.Value)).SingleOrDefault();
            if (job == null)
            {
                return HttpNotFound("Incorrect JobID. Please make sure that job id belongs to you and you are logged into the system");
            }
            JobResult results = db.JobResults.Where(m => m.JobID.Equals(id.Value)).SingleOrDefault();
            return View(results);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
