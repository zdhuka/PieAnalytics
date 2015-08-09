using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using WebMatrix.WebData;

namespace PieAnalytics.DataEntity
{
    public class DatabaseInitilizer
    {
        public static void InitilizeDatabase()
        {
            Database.SetInitializer<PieAnalyticsContext>(null);

            try
            {
                using (var context = new PieAnalyticsContext())
                {
                    if (!context.Database.Exists())
                    {
                        // Create the SimpleMembership database without Entity Framework migration schema
                        ((IObjectContextAdapter)context).ObjectContext.CreateDatabase();
                    }
                }
            }
            catch(Exception exp)
            {
                throw new InvalidOperationException("The ASP.NET database could not be initialized. For more information, please see http://go.microsoft.com/fwlink/?LinkId=256588", exp);
            }
        }
    }
}
