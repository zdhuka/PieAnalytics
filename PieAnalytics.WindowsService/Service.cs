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
        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("In OnStart");

            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // Set up a timer to trigger every minute.
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 60000/2; // 60/2 = 30 seconds
            timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);

            timer.Start();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            //// TODO: Insert monitoring activities here.
            //eventLog1.WriteEntry("Monitoring the System", EventLogEntryType.Information);
            //try
            //{
            //    string path = System.IO.Path.Combine("D:\\PieAnalytics\\PieAnalytics\\PieAnalytics.WindowsService\\bin\\", "Test.txt");
            //    eventLog1.WriteEntry("Path:" + path, EventLogEntryType.Information);
            //    if (!System.IO.File.Exists(path))
            //    {
            //        eventLog1.WriteEntry("Creating File", EventLogEntryType.Information, eventID++);
            //        System.IO.File.Create(path).Close();
            //    }
            //    using (System.IO.StreamWriter file =
            //    new System.IO.StreamWriter(path, true))
            //    {
            //        file.WriteLine("Success");
            //        file.Close();
            //    }
            //}
            //catch (Exception exp)
            //{
            //    eventLog1.WriteEntry(exp.Message, EventLogEntryType.Error);
            //}



            // TODO: Insert activities here.
            eventLog1.WriteEntry("Fetching query data from the System", EventLogEntryType.Information);
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
                eventLog1.WriteEntry("Updating record for that query in the System", EventLogEntryType.Information);

                foreach (var d in jobentity.Keywords.Split(',').ToList())
                {
                    eventLog1.WriteEntry("Fetching social media data into the System", EventLogEntryType.Information);
                    //Call the Rest API's 
                    // Call the API and fetch data
                    BestBuy bestbuy = new BestBuy();
                    BsonDocument data = bestbuy.GetReview(d, "");

                    // Store the result in MongoDB
                    _mongodb.InsertReview(data, jobentity.JobID.ToString(), "BestBuy");
                    eventLog1.WriteEntry("Finished fetching record for that data into the System", EventLogEntryType.Information);
                    //start MongoDB mapreduce
                }

                eventLog1.WriteEntry("Push sentiment analysis of that data within the System", EventLogEntryType.Information);
                // Call Map Reduce Program for Aggreating data collected using mapreduce
                BestBuyAnalysis analysis = new BestBuyAnalysis();
                analysis.AnalyzeReviews(jobentity.JobID.ToString());
                //Update SQL database on sentiment analysis


                eventLog1.WriteEntry("Finished processing the query and data from/in/within the System", EventLogEntryType.Information);
                // Update Job Status as completed
                jobentity.Status = (int)JobStatus.Finished;
                db.SaveChanges();
            }
        }

        protected override void OnStop()
        {
            // Update the service state to Stop Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
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
