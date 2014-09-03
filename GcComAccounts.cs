using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    [PetaPoco.TableName("GcComAccounts")]
    [PetaPoco.PrimaryKey("ID")]
    public class GcComAccounts
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public string Token { get; set; }
        public bool Enabled { get; set; }
    }
}
