using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    [PetaPoco.TableName("ScheduledTrackable")]
    public class ScheduledTrackable
    {
        public string Code {get; set;}
        public DateTime DateAdded {get; set;}
    }
}
