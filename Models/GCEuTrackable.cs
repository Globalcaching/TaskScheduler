using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCEuTrackable")]
    public class GCEuTrackable
    {
        public string Code { get; set; }
        public int GroupID { get; set; }
        public DateTime? Updated { get; set; }
        public double? Lat { get; set; }
        public double? Lon { get; set; }
        public double? Distance { get; set; }
        public int? Drops { get; set; }
        public int? Discovers { get; set; }
    }
}
