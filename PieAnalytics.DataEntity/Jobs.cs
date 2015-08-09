using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PieAnalytics.DataEntity
{
    [Table("Jobs")]
    public class Jobs
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int JobId { get; set; }
        public string JobName { get; set; }
        public string keywords { get; set; }
    }
}
