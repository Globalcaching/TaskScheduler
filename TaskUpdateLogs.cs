using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class TaskUpdateLogs : TaskBase
    {
        private static List<string> _scheduledWaypoints = new List<string>();

        private int _wpCount = 0;
        private int _scheduledCount = 0;
        private int _tokenAccountIndex = 0;

        public TaskUpdateLogs(Manager taskManager) :
            base(taskManager, typeof(TaskUpdateLogs), "Update Logs", 0, 0, 3)
        {
            _wpCount = 0;
            Details = _wpCount.ToString();
        }

        public class GeocacheInfo
        {
            public long ID { get; set; }
            public string Code { get; set; }
        }

        public static void AddScheduledWaypoint(string code)
        {
            lock (_scheduledWaypoints)
            {
                if (!_scheduledWaypoints.Contains(code))
                {
                    _scheduledWaypoints.Add(code);
                }
            }
        }

        public static string GetScheduledWaypoint()
        {
            string result = null;
            lock (_scheduledWaypoints)
            {
                if (_scheduledWaypoints.Count > 0)
                {
                    result = _scheduledWaypoints[0];
                    _scheduledWaypoints.RemoveAt(0);
                }
            }
            return result;
        }

        protected override void ServiceMethod()
        {
            string activeCode = "";
            long activeId = 0;
            bool isScheduledCache = false;
            bool getAllLogs = true;
            long lastId = 0;
            List<long> gcidList;
            try
            {
                //first get scheduled
                using (var db = new PetaPoco.Database(Manager.SchedulerConnectionString, "System.Data.SqlClient"))
                {
                    activeCode = GetScheduledWaypoint();
                    if (!string.IsNullOrEmpty(activeCode))
                    {
                        isScheduledCache = true;
                        GeocacheInfo gi = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 ID, Code from [{0}].[dbo].[GCComGeocache] where Code=@0", GCComDataSupport.GeocachingDatabaseName), activeCode);
                        activeId = gi.ID;
                    }
                    else
                    {
                        ScheduledWaypoint swp = db.FirstOrDefault<ScheduledWaypoint>("select top 1 * from ScheduledWaypoint where FullRefresh=0");
                        if (swp != null)
                        {
                            activeCode = swp.Code;
                            isScheduledCache = true;
                            getAllLogs = false;

                            db.Execute("delete from ScheduledWaypoint where Code=@0", swp.Code);
                            GeocacheInfo gi = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 ID, Code from [{0}].[dbo].[GCComGeocache] where Code=@0", GCComDataSupport.GeocachingDatabaseName), activeCode);
                            activeId = gi.ID;
                        }
                    }
                    if (!isScheduledCache)
                    {
                        gcidList = db.Fetch<long>("SELECT LastGeocacheID FROM TaskUpdateLogs");
                        if (gcidList.Count > 0)
                        {
                            lastId = gcidList[0];
                        }
                        GeocacheInfo gi = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 GCComGeocache.ID, Code from [{0}].[dbo].[GCComGeocache] inner join [{1}].[dbo].[GCEuGeocache] on GCComGeocache.ID = GCEuGeocache.ID where GCComGeocache.ID>@0 and (Archived=0 or DATEDIFF(DAY,COALESCE(MostRecentArchivedDate, GETDATE()),GETDATE()) < 120) order by GCComGeocache.ID", GCComDataSupport.GeocachingDatabaseName, GCEuDataSupport.GlobalcachingDatabaseName), lastId);
                        if (gi==null)
                        {
                            lastId = 0;
                            gi = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 ID, Code from [{0}].[dbo].[GCComGeocache] where ID>@0 order by ID", GCComDataSupport.GeocachingDatabaseName), lastId);
                        }
                        if (gi != null)
                        {
                            activeCode = gi.Code;
                            lastId = gi.ID;
                            activeId = gi.ID;

                            if (gcidList.Count > 0)
                            {
                                db.Execute("update TaskUpdateLogs set LastGeocacheID=@0", lastId);
                            }
                            else
                            {
                                db.Execute("insert into TaskUpdateLogs (LastGeocacheID) values (@0)", lastId);
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(activeCode) && activeId>0)
                {
                    string token = GeocachingAPI.Instance.GetServiceToken(ref _tokenAccountIndex);
                    DateTime dt;
                    if (isScheduledCache)
                    {
                        dt = new DateTime(2000, 1, 1);
                    }
                    else
                    {
                        dt = DateTime.Now.AddMonths(-3);
                    }
                    if (token.Length > 0)
                    {
                        //update
                        if (getAllLogs)
                        {
                            List<Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog> logs = GeocachingAPI.GetLogsOfGeocache(token, activeCode);
                            if (logs != null)
                            {
                                DataSupport.Instance.AddLogs(activeId, logs.ToArray(), true, dt);
                            }
                        }
                        else
                        {
                            List<Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog> logs = GeocachingAPI.GetLogsOfGeocache(token, activeCode, 500);
                            if (logs != null)
                            {
                                DataSupport.Instance.AddLogs(activeId, logs.ToArray(), false, dt);
                            }
                        }

                        _wpCount++;
                        if (isScheduledCache)
                        {
                            _scheduledCount++;
                        }
                    }
                }

                Details = string.Format("C:{0} T:{1} S:{2}", activeCode, _wpCount, _scheduledCount);
                ServiceInfo.ErrorInLastRun = false;
            }
            catch(Exception e)
            {
                Details = string.Format("{0} - {1}", activeCode, e.Message);
                ServiceInfo.ErrorInLastRun = true;
            }
        }
    }
}
