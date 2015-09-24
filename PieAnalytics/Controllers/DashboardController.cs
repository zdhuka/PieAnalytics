using PieAnalytics.DataEntity;
using PieAnalytics.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PieAnalytics.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class DashboardController : Controller
    {
        public PieAnalyticsEntities db = new PieAnalyticsEntities();
        // GET: Dashboard
        public ActionResult Index()
        {
            return View(db);
        }
    }
}