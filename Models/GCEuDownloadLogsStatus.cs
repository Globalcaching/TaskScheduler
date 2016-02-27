using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCEuDownloadLogsStatus")]
    public class GCEuDownloadLogsStatus
    {
        //request
        public int UserID { get; set; }
        public string UserNames { get; set; }
        public bool IncludeYourArchived { get; set; }
        public DateTime RequestedAt { get; set; }

        //status
        public string Status { get; set; } //aangevraagd|bezig|klaar|geannuleerd (reden)|fout (reden)
        public bool? Busy { get; set; }

        public string UserNamesCompleted { get; set; }
        public string UserNameBusy { get; set; }
        public int? TotalFindCount { get; set; }
        public int TotalLogsImported { get; set; }
        public DateTime LastUpdateAt { get; set; }
        public string LogTableName { get; set; } //in GCEuMacroData database
    }
}
