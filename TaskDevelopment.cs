using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Models;

namespace TaskScheduler
{
    public class TaskDevelopment : TaskBase
    {
        private int _Count;

        public TaskDevelopment(Manager taskManager) :
            base(taskManager, typeof(TaskDevelopment), "Development task", 1, 0, 0)
        {
            _Count = 0;
            Details = _Count.ToString();
        }

        protected override void ServiceMethod()
        {
            try
            {
                //buildFoundRankings();
                //addMacroDataTables();
                //updatePublisheddate();
                //clearQueue();
            }
            catch (Exception e)
            {
                Details = e.Message;
            }
        }

        private void buildFoundRankings()
        {
            using (var db = GCEuDataSupport.Instance.GetGCEuDataDatabase())
            {
                db.CommandTimeout = 180;

                db.Execute("truncate table GCEuFoundsRanking");
                var countries = new int[] { 141, 4, 8};
                for (int i = 2001; i < 2016; i++)
                {
                    foreach (var c in countries)
                    {
                        db.Execute("insert into GCEuFoundsRanking (GCComUserID, Ranking, RankYear, CountryID, Founds) select b.FinderId as GCComUserID, ROW_NUMBER() OVER (order by b.Founds desc, FinderId desc) as Ranking, RankYear=@0, CountryID=@1, b.Founds from (select FinderId, count(1) as Founds from GCComData.dbo.GCComGeocacheLog with (nolock) inner join GCComData.dbo.GCComGeocache with (nolock) on GCComGeocacheLog.CacheCode=GCComGeocache.Code where YEAR(VisitDate)=@2 and WptLogTypeId in (2, 10, 11) and GCComGeocache.CountryID=@3 group by finderid) as b", i, c, i, c);
                    }
                }
                foreach (var c in countries)
                {
                    db.Execute("insert into GCEuFoundsRanking (GCComUserID, Ranking, RankYear, CountryID, Founds) select b.FinderId as GCComUserID, ROW_NUMBER() OVER (order by b.Founds desc, FinderId desc) as Ranking, RankYear=0, CountryID=@0, b.Founds from (select FinderId, count(1) as Founds from GCComData.dbo.GCComGeocacheLog with (nolock) inner join GCComData.dbo.GCComGeocache with (nolock) on GCComGeocacheLog.CacheCode=GCComGeocache.Code where WptLogTypeId in (2, 10, 11) and GCComGeocache.CountryID=@1 group by finderid) as b", c, c);
                }
            }
        }

        private void addMacroDataTables()
        {
            using (var db = GCEuDataSupport.Instance.GetGCEuDataDatabase())
            {
                var tbil = db.Fetch<string>("select TableName from GCEuMacroData.dbo.TableCreationInfo");
                var tables = db.Fetch<string>("SELECT name FROM GCEuMacroData.sys.tables WHERE name like 'macro_%' or name like 'LiveAPIDownload_%'");
                foreach (var t in tables)
                {
                    if (!tbil.Contains(t))
                    {
                        db.Execute("insert into GCEuMacroData.dbo.TableCreationInfo (TableName, Created) values (@0, @1)", t, DateTime.Now);
                    }
                }
            }
        }

        private void clearQueue()
        {
            using (var db = TaskManager.TaskSchedulerDatabase)
            {
                db.Execute("truncate table ScheduledWaypoint");
            }
        }

        private void updatePublisheddate()
        {
            using (var dbEU = GCEuDataSupport.Instance.GetGCEuDataDatabase())
            using (var dbCom = GCComDataSupport.Instance.GetGCComDataDatabase())
            {
                List<GCEuGeocache> gcEUCaches = dbEU.Fetch<GCEuGeocache>("where PublishedAtDate is NULL");
                foreach (var gc in gcEUCaches)
                {
                    DateTime? publishedDate = null;
                    var l = dbCom.FirstOrDefault<GCComGeocacheLog>("where GeocacheID=@0 and WptLogTypeId=24", gc.ID);
                    if (l != null)
                    {
                        publishedDate = l.VisitDate;
                    }
                    else
                    {
                        publishedDate = dbCom.ExecuteScalar<DateTime>("select UTCPlaceDate from GCComGeocache where ID=@0", gc.ID);
                    }
                    dbEU.Execute("update GCEuGeocache set PublishedAtDate=@0 where ID=@1", publishedDate, gc.ID);

                    _Count++;
                    Details = _Count.ToString();
                }
            }
        }
    }
}
