using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCEuCCCUser")]
    public class GCEuCCCUser
    {
        public int UserID {get; set;}
        public bool Active {get; set;}
        public DateTime RegisteredAt {get; set;}
        public DateTime ModifiedAt {get; set;}
        public long GCComUserID {get; set;}
        public string Telnr {get; set;}
        public bool SMS {get; set;}
        public bool PreferSMS {get; set;}
        public string TwitterUsername {get; set;}
        public int UsersHelped {get; set;}
        public string Comment {get; set;}
        public bool HideEmailAddress {get; set;}
    }
}
