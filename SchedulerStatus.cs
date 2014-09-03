using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    [PetaPoco.TableName("SchedulerStatus")]
    [PetaPoco.PrimaryKey("ID")]
    public class SchedulerStatus
    {
        public long ID { get; set; }
        public bool LiveAPIError { get; set; }
        public bool GCComWWWError { get; set; }
    }
}
