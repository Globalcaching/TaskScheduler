using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCEuCodeCheckAttempt")]
    public class GCEuCodeCheckAttempt
    {
        public int CodeID { get; set; }
        public DateTime AttemptAt { get; set; }
        public string VisitorID { get; set; }
        public string Answer { get; set; }
        public string GroundspeakUserName { get; set; }
    }
}
