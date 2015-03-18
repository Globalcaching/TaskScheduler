using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Models;

namespace TaskScheduler
{
    public class TaskUpdateLastLogs : TaskBase
    {
        private static List<string> _scheduledWaypoints = new List<string>();

        private int _wpCount = 0;
        private int _scheduledCount = 0;
        private int _tokenAccountIndex = 0;
        private List<GCEuLiveAPIHelpers> _availableTokens = null;

        public TaskUpdateLastLogs(Manager taskManager) :
            base(taskManager, typeof(TaskUpdateLastLogs), "Update Last Logs", 0, 0, 2)
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
            long lastId = 0;
            string token = null;
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

                            db.Execute("delete from ScheduledWaypoint where Code=@0", swp.Code);
                            GeocacheInfo gi = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 ID, Code from [{0}].[dbo].[GCComGeocache] where Code=@0", GCComDataSupport.GeocachingDatabaseName), activeCode);
                            activeId = gi.ID;
                        }
                    }

                    if (!isScheduledCache)
                    {
                        gcidList = db.Fetch<long>("SELECT LastGeocacheID FROM TaskUpdateLastLogs");
                        if (gcidList.Count > 0)
                        {
                            lastId = gcidList[0];
                        }
                        GeocacheInfo gi = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 GCComGeocache.ID, Code from [{0}].[dbo].[GCComGeocache] inner join [{1}].[dbo].[GCEuGeocache] on GCComGeocache.ID = GCEuGeocache.ID where GCComGeocache.ID>@0 and (Archived=0 or DATEDIFF(DAY,COALESCE(MostRecentArchivedDate, GETDATE()),GETDATE()) < 120) order by GCComGeocache.ID", GCComDataSupport.GeocachingDatabaseName, GCEuDataSupport.GlobalcachingDatabaseName), lastId);
                        if (gi == null)
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
                                db.Execute("update TaskUpdateLastLogs set LastGeocacheID=@0", lastId);
                            }
                            else
                            {
                                db.Execute("insert into TaskUpdateLastLogs (LastGeocacheID) values (@0)", lastId);
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(activeCode) && activeId > 0)
                {
                    token = GetToken();
                    if (!string.IsNullOrEmpty(token))
                    {
                        var dt = DateTime.Now.AddMonths(-3);
                        List<Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog> logs = GeocachingAPI.GetLogsOfGeocache(token, activeCode, 30);
                        if (logs != null)
                        {
                            DataSupport.Instance.AddLogs(activeId, logs.ToArray(), false, dt);
                        }
                        else
                        {
                            //check if token is still valid
                            var resp = GeocachingAPI.GetUserProfile(token);
                            if (resp != null && resp.Status != null && resp.Status.StatusCode != 0)
                            {
                                _availableTokens = null;
                                using (var db = new PetaPoco.Database(GCEuDataSupport.Instance.GCEuDataConnectionString, "System.Data.SqlClient"))
                                {
                                    db.Execute("delete from GCEuLiveAPIHelpers where LiveAPIToken=@0", token);
                                }
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
            catch (Exception e)
            {
                Details = string.Format("{0} - {1}", activeCode, e.Message);
                ServiceInfo.ErrorInLastRun = true;
            }
        }

        private string GetToken()
        {
            string result = null;
            if (_availableTokens == null)
            {
                using (var db = new PetaPoco.Database(GCEuDataSupport.Instance.GCEuDataConnectionString, "System.Data.SqlClient"))
                {
                    _availableTokens = db.Fetch<GCEuLiveAPIHelpers>("");
                }
                _tokenAccountIndex = 0;
            }
            _tokenAccountIndex++;
            if (_tokenAccountIndex >= _availableTokens.Count())
            {
                _tokenAccountIndex = 0;
            }
            if (_tokenAccountIndex < _availableTokens.Count())
            {
                result = _availableTokens[_tokenAccountIndex].LiveAPIToken;
            }
            return result;
        }
    }
}
