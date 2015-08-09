using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Data.Entity.Migrations;

namespace PieAnalytics.DataEntity
{
    public class PieAnalyticsContext : DbContext
    {
        public PieAnalyticsContext()
            : base("DefaultConnection")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Jobs> Jobs { get; set; }
    }
}
