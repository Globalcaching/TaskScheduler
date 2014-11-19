using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class TaskUpdateStatus: TaskBase
    {
        private int _specialIndex = 0;
        private long _counter = 0;

        public class GeocacheInfo
        {
            public long ID { get; set; }
            public string Code { get; set; }
        }

        public TaskUpdateStatus(Manager taskManager) :
            base(taskManager, typeof(TaskUpdateStatus), "Update status", 0, 1, 0)
        {
        }

        protected override void ServiceMethod()
        {
            try
            {
                string token = GeocachingAPI.Instance.GetServiceToken(ref _specialIndex);

                if (token.Length > 0)
                {
                    using (var db = new PetaPoco.Database(Manager.SchedulerConnectionString, "System.Data.SqlClient"))
                    {
                        List<long> gcidList = db.Fetch<long>("SELECT LastGeocacheID FROM TaskUpdateStatus");
                        long lastId = 0;
                        if (gcidList.Count > 0)
                        {
                            lastId = gcidList[0];
                        }

                        List<GeocacheInfo> gil = db.SkipTake<GeocacheInfo>(0, 100, string.Format("select ID, Code from [{0}].[dbo].[GCComGeocache] where ID>@0 order by ID", GCComDataSupport.GeocachingDatabaseName), lastId);
                        if (gil.Count < 100)
                        {
                            lastId = 0;
                        }
                        else
                        {
                            lastId = gil[gil.Count - 1].ID;
                        }
                        if (gcidList.Count > 0)
                        {
                            db.Execute("update TaskUpdateStatus set LastGeocacheID=@0", lastId);
                        }
                        else
                        {
                            db.Execute("insert into TaskUpdateStatus (LastGeocacheID) values (@0)", lastId);
                        }

                        if (gil.Count > 0)
                        {
                            Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheStatus[] gstats = GeocachingAPI.GetGeocacheStatus(token, (from a in gil select a.Code).ToArray());
                            if (gstats != null)
                            {
                                foreach (Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheStatus stats in gstats)
                                {
                                    db.Execute(string.Format("update [{0}].[dbo].[GCComGeocache] set Archived=@0, Available=@1 where Code>@2", GCComDataSupport.GeocachingDatabaseName), stats.Archived, stats.Available, stats.CacheCode);
                                }
                                _counter++;
                                Details = string.Format("{0}, total counter={1}, this batch={2}, lastWP={3}", DateTime.Now.ToString(), _counter.ToString(), gstats.Length, lastId);
                            }
                            else
                            {
                                Details = string.Format("{0}, total counter={1}, this batch=ERROR, lastWP={2}", DateTime.Now.ToString(), _counter.ToString(), lastId);
                            }
                        }
                    }
                }
                ServiceInfo.ErrorInLastRun = false;
            }
            catch (Exception e)
            {
                Details = e.Message;
                ServiceInfo.ErrorInLastRun = true;
            }
        }
    }
}
