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
            // Fetch data from MongoDB
            var db = new PieAnalytics.DataEntity.PieAnalyticsEntities();
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
                foreach (var d in jobentity.Keywords.Split(',').ToList())
                {
                    // Call the API and fetch data
                    BestBuy bestbuy = new BestBuy();
                    var data = bestbuy.GetReview(d, "");

                    // Store the result in MongoDB
                    _mongodb.InsertReview(data, jobentity.JobID.ToString(), "BestBuy");
                }

                // Call Map Reduce Program for Aggreating data collected using mapreduce
                BestBuyAnalysis analysis = new BestBuyAnalysis();
                analysis.AnalyzeReviews(jobentity.JobID.ToString());

                // Update Job Status as completed
                jobentity.Status = (int)JobStatus.Finished;
                db.SaveChanges();
            }
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
        public ActionResult Result(Guid id)
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
