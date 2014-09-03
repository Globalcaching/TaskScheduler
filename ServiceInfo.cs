using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    [PetaPoco.TableName("ServiceInfo")]
    [PetaPoco.PrimaryKey("ID")]
    public class ServiceInfo
    {
        public long ID { get; set; }
        public string ClassName { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public bool ErrorInLastRun { get; set; }
        public DateTime Interval { get; set; }
        public DateTime? LastRun { get; set; }
        public string InfoMessage { get; set; }
        public DateTime? RunAfter { get; set; }
        public DateTime? RunBefore { get; set; }
    }
}
