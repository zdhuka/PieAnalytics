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
    [Authorize]
    public class JobsController : Controller
    {
        private PieAnalyticsEntities db = new PieAnalyticsEntities();

        // GET: Jobs
        public ActionResult Index()
        {
            if (User.IsInRole("ADMIN"))
            {
                var result = db.Jobs.ToList();
                return View(result);
            }
            else
            {
                var jobUserID = db.UserProfiles.Where(m => m.UserName.Equals(User.Identity.Name)).Single().UserId;
                var result = db.Jobs.Where(m => m.UserID.Equals(jobUserID)).ToList();
                return View(result);
            }
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
