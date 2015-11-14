using MongoDB.Bson;
using PieAnalytics.DataCollector;
using PieAnalytics.DataEntity;
using PieAnalytics.Mongo;
using PioneerApp.DataAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PieAnalytics.WindowsService
{
    public partial class Service : ServiceBase
    {
        private System.ComponentModel.IContainer components = null;
        private System.Diagnostics.EventLog eventLog1;
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
        //int eventID = 0;
        private PieAnalyticsEntities _db = new PieAnalyticsEntities();
        ServiceStatus serviceStatus = new ServiceStatus();

        public Service(string[] args)
        {
            this.AutoLog = false;
            InitializeComponent();
            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("PieAnalyticsWindowsService"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "PieAnalyticsWindowsService", "PieAnalyticsWindowsServiceLog");
            }
            eventLog1.Source = "PieAnalyticsWindowsServiceService";
            eventLog1.Log = "PieAnalyticsWindowsServiceLog";
            eventLog1.WriteEntry("Service Setting up");
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.ServiceName = "PieAnalyticsWindowsService";
            // update the service state to start pending.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnStart(string[] args)
        {
            DebugMode();
            // Set up a timer to trigger every minute.
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 60000; // 60/2 = 30 seconds
            timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);

            timer.Start();
            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        [Conditional("DEBUG_SERVICE")]
        private static void DebugMode()
        {
            Debugger.Break();
        }


        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            //WindowsService Starts here
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
            var jobentityquery = (from a in _db.Jobs
                                  where a.Status == status
                                  orderby a.InsertDate descending
                                  select a);
            Job jobentity = jobentityquery.FirstOrDefault();
            if (jobentity != null)
            {
                jobentity.Status = (int)JobStatus.Processing;
                jobentity.UpdateDate = DateTime.Now;
                //Update Status of Job in Porcesing
                _db.SaveChangesAsync();
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
                _db.JobResults.Add(bestbuy_result);
                eventLog1.WriteEntry("Finished processing the query and data from/in/within the System", System.Diagnostics.EventLogEntryType.Information);
                // Update Job Status as completed
                jobentity.Status = (int)JobStatus.Finished;
                _db.SaveChanges();
                eventLog1.WriteEntry("Finished exporting data and Job is completed", System.Diagnostics.EventLogEntryType.Information);
            }

            //WindowsService Ends here
        }

        protected override void OnStop()
        {
            // Update the service state to Stop Pending.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            eventLog1.WriteEntry("In onStop.");

            // Update the service state to Stopped.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnContinue()
        {
            eventLog1.WriteEntry("In OnContinue.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

    }

    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public long dwServiceType;
        public ServiceState dwCurrentState;
        public long dwControlsAccepted;
        public long dwWin32ExitCode;
        public long dwServiceSpecificExitCode;
        public long dwCheckPoint;
        public long dwWaitHint;
    };
}
