using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class TaskWalkWaypoints : TaskBase
    {
        private int _wpCount = 0;
        private int _scheduledCount = 0;
        private int _tokenAccountIndex = 0;

        public class GeocacheInfo
        {
            public long ID { get; set; }
            public string Code { get; set; }
        }

        public TaskWalkWaypoints(Manager taskManager) :
            base(taskManager, typeof(TaskWalkWaypoints), "Walk Waypoints", 0, 4, 0)
        {
            _wpCount = 0;
            Details = _wpCount.ToString();
        }

        protected override void ServiceMethod()
        {
            string activeCode = "";
            bool isScheduledCache = false;
            long lastId = 0;
            List<long> gcidList;
            try
            {
                //first get scheduled
                using (var db = new PetaPoco.Database(Manager.SchedulerConnectionString, "System.Data.SqlClient"))
                {
                    ScheduledWaypoint swp = db.FirstOrDefault<ScheduledWaypoint>("select top 1 * from ScheduledWaypoint where FullRefresh=1");
                    if (swp != null)
                    {
                        activeCode = swp.Code;
                        isScheduledCache = true;

                        db.Execute("delete from ScheduledWaypoint where Code=@0", swp.Code);
                    }
                    if (!isScheduledCache)
                    {
                        gcidList = db.Fetch<long>("SELECT LastGeocacheID FROM TaskWalkWaypoints");
                        if (gcidList.Count > 0)
                        {
                            lastId = gcidList[0];
                        }
                        GeocacheInfo gi = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 ID, Code from [{0}].[dbo].[GCComGeocache] where ID>@0 order by ID", GCComDataSupport.GeocachingDatabaseName), lastId);
                        if (gi == null)
                        {
                            lastId = 0;
                            gi = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 ID, Code from [{0}].[dbo].[GCComGeocache] where ID>@0 order by ID", GCComDataSupport.GeocachingDatabaseName), lastId);
                        }
                        if (gi != null)
                        {
                            activeCode = gi.Code;
                            lastId = gi.ID;

                            if (gcidList.Count > 0)
                            {
                                db.Execute("update TaskWalkWaypoints set LastGeocacheID=@0", lastId);
                            }
                            else
                            {
                                db.Execute("insert into TaskWalkWaypoints (LastGeocacheID) values (@0)", lastId);
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(activeCode))
                {
                    //update
                    string token = GeocachingAPI.Instance.GetServiceToken(ref _tokenAccountIndex);
                    if (token.Length > 0)
                    {
                        Tucson.Geocaching.WCF.API.Geocaching1.Types.Geocache gcData = GeocachingAPI.GetGeocache(token, activeCode);
                        if (gcData != null)
                        {
                            DataSupport.Instance.AddGeocache(gcData);

                            //if scheduled, update logs too
                            if (isScheduledCache)
                            {
                                TaskUpdateLogs.AddScheduledWaypoint(activeCode);
                            }
                        }
                    }

                    _wpCount++;
                    if (isScheduledCache)
                    {
                        _scheduledCount++;
                    }
                }

                Details = string.Format("C:{0} T:{1} S:{2}", activeCode, _wpCount, _scheduledCount);
                ServiceInfo.ErrorInLastRun = false;
            }
            catch(Exception e)
            {
                ServiceInfo.ErrorInLastRun = true;
                Details = string.Format("{0} - {1}", activeCode, e.Message);
            }
        }
    }
}
