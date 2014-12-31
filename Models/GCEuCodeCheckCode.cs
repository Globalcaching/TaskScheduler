using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCEuCodeCheckCode")]
    [PetaPoco.PrimaryKey("ID")]
    public class GCEuCodeCheckCode
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public string PublicCode { get; set; }
        public bool CaseSensative { get; set; }
        public string AnswerText { get; set; }
        public int DelaySeconds { get; set; }
        public bool EmailNotifyOnFail { get; set; }
        public bool EmailNotifyOnSuccess { get; set; }
        public bool GroundspeakAuthReq { get; set; }
        public string AnswerCode { get; set; }
    }
}
