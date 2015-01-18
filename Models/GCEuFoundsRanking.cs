using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCEuFoundsRanking")]
    public class GCEuFoundsRanking
    {
        public long GCComUserID { get; set; }
        public int Ranking { get; set; }
        public int RankYear { get; set; }
        public int CountryID { get; set; }
        public int Founds { get; set; }
    }
}
