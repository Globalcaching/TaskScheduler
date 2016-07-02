using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Models;

namespace TaskScheduler
{
    public class TaskUpdateFTFStats : TaskBase
    {
        public class UserInfo
        {
            public long? FTFUserID { get; set; }
            public long? STFUserID { get; set; }
            public long? TTFUserID { get; set; }
            public DateTime? FTFAtDate { get; set; }
            public DateTime? STFAtDate { get; set; }
            public DateTime? TTFAtDate { get; set; }
        }

        public TaskUpdateFTFStats(Manager taskManager) :
            base(taskManager, typeof(TaskUpdateFTFStats), "Update FTF stats", 23, 0, 0)
        {
            Details = "";
        }

        protected override void ServiceMethod()
        {
            try
            {
                updateFTFStats();
                updateFoundRankingStats();

                Details = "OK";
                ServiceInfo.ErrorInLastRun = false;
            }
            catch(Exception e)
            {
                Details = e.Message;
                ServiceInfo.ErrorInLastRun = true;
            }
        }

        private void updateFoundRankingStats()
        {
            using (var db = GCEuDataSupport.Instance.GetGCEuDataDatabase())
            {
                db.CommandTimeout = 300;

                int startYear = DateTime.Now.AddMonths(-2).Year;
                var countries = new int[] { 141, 4, 8 };
                //takes too long
                /*
                for (int i = startYear; i <= DateTime.Now.Year; i++)
                {
                    foreach (var c in countries)
                    {
                        //we get the current ranking and the new ranking
                        //foreach new ranking, we compare with the new
                        //if changed, we add or update
                        var currentRanking = db.Fetch<GCEuFoundsRanking>("where RankYear=@0 and CountryID=@1", i, c);
                        var newRanking = db.Fetch<GCEuFoundsRanking>("select b.FinderId as GCComUserID, ROW_NUMBER() OVER (order by b.Founds desc, FinderId desc) as Ranking, RankYear=@0, CountryID=@1, b.Founds from (select FinderId, count(1) as Founds from GCComData.dbo.GCComGeocacheLog with (nolock) inner join GCComData.dbo.GCComGeocache with (nolock) on GCComGeocacheLog.CacheCode=GCComGeocache.Code where YEAR(VisitDate)=@2 and WptLogTypeId in (2, 10, 11) and GCComGeocache.CountryID=@3 group by finderid) as b", i, c, i, c);
                        compareAndUpdateFoundsRanking(db, currentRanking, newRanking);
                    }
                }
                foreach (var c in countries)
                {
                    var currentRanking = db.Fetch<GCEuFoundsRanking>("where RankYear=0 and CountryID=@0", c);
                    var newRanking = db.Fetch<GCEuFoundsRanking>("select b.FinderId as GCComUserID, ROW_NUMBER() OVER (order by b.Founds desc, FinderId desc) as Ranking, RankYear=0, CountryID=@0, b.Founds from (select FinderId, count(1) as Founds from GCComData.dbo.GCComGeocacheLog with (nolock) inner join GCComData.dbo.GCComGeocache with (nolock) on GCComGeocacheLog.CacheCode=GCComGeocache.Code where WptLogTypeId in (2, 10, 11) and GCComGeocache.CountryID=@1 group by finderid) as b", c, c);
                    compareAndUpdateFoundsRanking(db, currentRanking, newRanking);
                }
                 * */
                for (int i = startYear; i <= DateTime.Now.Year; i++)
                {
                    foreach (var c in countries)
                    {
                        //we get the current ranking and the new ranking
                        //foreach new ranking, we compare with the new
                        //if changed, we add or update
                        db.Execute("delete from GCEuFoundsRanking where RankYear=@0 and CountryID=@1", i, c);
                        db.Execute("insert into GCEuFoundsRanking (GCComUserID, Ranking, RankYear, CountryID, Founds) select b.FinderId as GCComUserID, ROW_NUMBER() OVER (order by b.Founds desc, FinderId desc) as Ranking, RankYear=@0, CountryID=@1, b.Founds from (select FinderId, count(1) as Founds from GCComData.dbo.GCComGeocacheLog with (nolock) inner join GCComData.dbo.GCComGeocache with (nolock) on GCComGeocacheLog.CacheCode=GCComGeocache.Code where YEAR(VisitDate)=@2 and WptLogTypeId in (2, 10, 11) and GCComGeocache.CountryID=@3 group by finderid) as b", i, c, i, c);
                    }
                }
                foreach (var c in countries)
                {
                    db.Execute("delete from GCEuFoundsRanking where RankYear=0 and CountryID=@0", c);
                    db.Execute("insert into GCEuFoundsRanking (GCComUserID, Ranking, RankYear, CountryID, Founds) select b.FinderId as GCComUserID, ROW_NUMBER() OVER (order by b.Founds desc, FinderId desc) as Ranking, RankYear=0, CountryID=@0, b.Founds from (select FinderId, count(1) as Founds from GCComData.dbo.GCComGeocacheLog with (nolock) inner join GCComData.dbo.GCComGeocache with (nolock) on GCComGeocacheLog.CacheCode=GCComGeocache.Code where WptLogTypeId in (2, 10, 11) and GCComGeocache.CountryID=@1 group by finderid) as b", c, c);
                }
            }
        }

        private void compareAndUpdateFoundsRanking(PetaPoco.Database db, List<GCEuFoundsRanking> currentRanking, List<GCEuFoundsRanking> newRanking)
        {
            foreach (var nr in newRanking)
            {
                var cr = (from a in currentRanking where a.GCComUserID == nr.GCComUserID select a).FirstOrDefault();
                if (cr == null)
                {
                    db.Insert(nr);
                }
                else
                {
                    if (nr.Ranking != cr.Ranking || nr.Founds != cr.Founds)
                    {
                        db.Execute("update GCEuFoundsRanking set Ranking=@0, Founds=@1 where RankYear=@2 and CountryID=@3 and GCComUserID=@4", nr.Ranking, nr.Founds, nr.RankYear, nr.CountryID, nr.GCComUserID);
                    }
                    currentRanking.Remove(cr);
                }
            }
            foreach (var cr in currentRanking)
            {
                db.Execute("delete from GCEuFoundsRanking where RankYear=@0 and CountryID=@1 and GCComUserID=@2", cr.RankYear, cr.CountryID, cr.GCComUserID);
            }
        }


        private void updateFTFStats()
        {
            using (var db = GCEuDataSupport.Instance.GetGCEuDataDatabase())
            {
                List<GCEuFTFStats> ftfTable = new List<GCEuFTFStats>();
                List<UserInfo> allGeocaches = db.Fetch<UserInfo>(@"select FTFUserID, STFUserID, TTFUserID, FTFAtDate, STFAtDate, TTFAtDate from GCEuGeocache where FTFUserID is not null or STFUserID is not null or TTFUserID is not null");

                //
                //ranking over all years
                //
                ftfTable.Clear();
                foreach (var gcInfo in allGeocaches)
                {
                    if (gcInfo.FTFUserID != null)
                    {
                        GCEuFTFStats rec = ftfTable.Where(x => x.UserID == gcInfo.FTFUserID).FirstOrDefault();
                        if (rec == null)
                        {
                            rec = new GCEuFTFStats() { UserID = (long)gcInfo.FTFUserID, Jaar = null };
                            ftfTable.Add(rec);
                        }
                        rec.FTFCount++;
                    }
                    if (gcInfo.STFUserID != null)
                    {
                        GCEuFTFStats rec = ftfTable.Where(x => x.UserID == gcInfo.STFUserID).FirstOrDefault();
                        if (rec == null)
                        {
                            rec = new GCEuFTFStats() { UserID = (long)gcInfo.STFUserID, Jaar = null };
                            ftfTable.Add(rec);
                        }
                        rec.STFCount++;
                    }
                    if (gcInfo.TTFUserID != null)
                    {
                        GCEuFTFStats rec = ftfTable.Where(x => x.UserID == gcInfo.TTFUserID).FirstOrDefault();
                        if (rec == null)
                        {
                            rec = new GCEuFTFStats() { UserID = (long)gcInfo.TTFUserID, Jaar = null };
                            ftfTable.Add(rec);
                        }
                        rec.TTFCount++;
                    }
                }
                updateFTFTable(db, ftfTable, null);

                //
                //ranking over all years
                //
                for (int jaar = 2000; jaar <= DateTime.Now.Year; jaar++)
                {
                    ftfTable.Clear();
                    foreach (var gcInfo in allGeocaches)
                    {
                        if (gcInfo.FTFUserID != null && gcInfo.FTFAtDate.HasValue && gcInfo.FTFAtDate.Value.Year == jaar)
                        {
                            GCEuFTFStats rec = ftfTable.Where(x => x.UserID == gcInfo.FTFUserID).FirstOrDefault();
                            if (rec == null)
                            {
                                rec = new GCEuFTFStats() { UserID = (long)gcInfo.FTFUserID, Jaar = jaar };
                                ftfTable.Add(rec);
                            }
                            rec.FTFCount++;
                        }
                        if (gcInfo.STFUserID != null && gcInfo.STFAtDate.HasValue && gcInfo.STFAtDate.Value.Year == jaar)
                        {
                            GCEuFTFStats rec = ftfTable.Where(x => x.UserID == gcInfo.STFUserID).FirstOrDefault();
                            if (rec == null)
                            {
                                rec = new GCEuFTFStats() { UserID = (long)gcInfo.STFUserID, Jaar = jaar };
                                ftfTable.Add(rec);
                            }
                            rec.STFCount++;
                        }
                        if (gcInfo.TTFUserID != null && gcInfo.TTFAtDate.HasValue && gcInfo.TTFAtDate.Value.Year == jaar)
                        {
                            GCEuFTFStats rec = ftfTable.Where(x => x.UserID == gcInfo.TTFUserID).FirstOrDefault();
                            if (rec == null)
                            {
                                rec = new GCEuFTFStats() { UserID = (long)gcInfo.TTFUserID, Jaar = jaar };
                                ftfTable.Add(rec);
                            }
                            rec.TTFCount++;
                        }
                    }
                    updateFTFTable(db, ftfTable, jaar);
                }
            }
        }

        private void updateFTFTable(PetaPoco.Database db, List<GCEuFTFStats> ftfTable, int? jaar)
        {
            ftfTable.Sort((x, y) =>
            {
                if (x == null) return -1;
                else if (y == null) return 1;
                else if (x.FTFCount > y.FTFCount) return -1;
                else if (x.FTFCount < y.FTFCount) return 1;
                else if (x.STFCount > y.STFCount) return -1;
                else if (x.STFCount < y.STFCount) return 1;
                else if (x.TTFCount > y.TTFCount) return -1;
                else if (x.TTFCount < y.TTFCount) return 1;
                else return 0;
            });
            for (int i = 0; i < ftfTable.Count; i++)
            {
                ftfTable[i].Position = i+1;
            }
            ftfTable.Sort((x, y) =>
            {
                if (x == null) return -1;
                else if (y == null) return 1;
                else return (y.FTFCount * 5 + y.STFCount * 3 + y.TTFCount).CompareTo(x.FTFCount * 5 + x.STFCount * 3 + x.TTFCount);
            });
            for (int i = 0; i < ftfTable.Count; i++)
            {
                ftfTable[i].PositionPoints = i+1;
            }

            string jaarClause;
            
            List<GCEuFTFStats> curTable;
            if (jaar == null)
            {
                jaarClause = "Jaar is null";
            }
            else
            {
                jaarClause = string.Format("Jaar = {0}", jaar);
            }
            curTable = db.Fetch<GCEuFTFStats>(string.Format("where {0}", jaarClause));
            //delete or update
            foreach (var item in curTable)
            {
                var rec = ftfTable.Where(x => x.UserID == item.UserID).FirstOrDefault();
                if (rec == null)
                {
                    //delete
                    db.Execute(string.Format("delete from GCEuFTFStats where UserID=@0 and {0}", jaarClause), item.UserID, jaar);
                }
                else if (item.Position!=rec.Position
                    || item.PositionPoints != rec.PositionPoints
                    || item.STFCount != rec.STFCount
                    || item.FTFCount != rec.FTFCount
                    || item.TTFCount != rec.TTFCount
                    )
                {
                    db.Execute(string.Format("update GCEuFTFStats set FTFCount=@0, STFCount=@1, TTFCount=@2, Position=@3, PositionPoints=@4 where UserID=@5 and {0}", jaarClause),
                        rec.FTFCount,
                        rec.STFCount,
                        rec.TTFCount,
                        rec.Position,
                        rec.PositionPoints,
                        rec.UserID);
                }
            }
            //insert
            foreach (var item in ftfTable)
            {
                var rec = curTable.Where(x => x.UserID == item.UserID).FirstOrDefault();
                if (rec == null)
                {
                    db.Insert(item);
                }
            }
        }
    }
}
