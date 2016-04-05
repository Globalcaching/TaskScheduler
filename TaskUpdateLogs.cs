using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class TaskUpdateLogs : TaskBase
    {
        private static List<string> _scheduledWaypoints = new List<string>();

        private int _wpCount = 0;
        private int _scheduledCount = 0;
        private int _tokenAccountIndex = 0;
        private string _lastErrorMessage = "";

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
            string[] activeCodes = new string[] { "", "" };
            long[] activeIds = new long[] { 0, 0 };
            bool[] isScheduledCaches = new bool[] { false, false };
            long lastId = 0;
            List<long> gcidList;
            try
            {
                ServiceInfo.ErrorInLastRun = false;

                //first get scheduled
                using (var db = new PetaPoco.Database(Manager.SchedulerConnectionString, "System.Data.SqlClient"))
                {
                    for (int i = 0; i < activeCodes.Length; i++)
                    {
                        activeCodes[i] = GetScheduledWaypoint();
                        if (!string.IsNullOrEmpty(activeCodes[i]))
                        {
                            isScheduledCaches[i] = true;
                            GeocacheInfo gi = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 ID, Code from [{0}].[dbo].[GCComGeocache] where Code=@0", GCComDataSupport.GeocachingDatabaseName), activeCodes[i]);
                            activeIds[i] = gi.ID;
                        }
                        if (!isScheduledCaches[i])
                        {
                            gcidList = db.Fetch<long>("SELECT LastGeocacheID FROM TaskUpdateLogs");
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
                                activeCodes[i] = gi.Code;
                                lastId = gi.ID;
                                activeIds[i] = gi.ID;

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
                }

                var tasks = new List<Task>();
                string token1 = GeocachingAPI.Instance.GetServiceToken(ref _tokenAccountIndex);
                if (!string.IsNullOrEmpty(token1))
                {
                    tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                DateTime dt;
                                if (isScheduledCaches[0])
                                {
                                    dt = new DateTime(2000, 1, 1);
                                }
                                else
                                {
                                    dt = DateTime.Now.AddMonths(-3);
                                }
                                List<Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog> logs = GeocachingAPI.GetLogsOfGeocache(token1, activeCodes[0]);
                                if (logs != null)
                                {
                                    DataSupport.Instance.AddLogs(activeIds[0], logs.ToArray(), true, dt);
                                }
                            }
                            catch (Exception e)
                            {
                                _lastErrorMessage = string.Format("ERROR: {0} - {1}", activeCodes[0], e.Message);
                                Details = string.Format("{0} - {1}", activeCodes[0], e.Message);
                                ServiceInfo.ErrorInLastRun = true;
                            }
                        }));
                }
                string token2 = GeocachingAPI.Instance.GetServiceToken(ref _tokenAccountIndex);
                if (!string.IsNullOrEmpty(token2) && token2 != token1)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            DateTime dt;
                            if (isScheduledCaches[1])
                            {
                                dt = new DateTime(2000, 1, 1);
                            }
                            else
                            {
                                dt = DateTime.Now.AddMonths(-3);
                            }
                            List<Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog> logs = GeocachingAPI.GetLogsOfGeocache(token2, activeCodes[1]);
                            if (logs != null)
                            {
                                DataSupport.Instance.AddLogs(activeIds[1], logs.ToArray(), true, dt);
                            }
                        }
                        catch (Exception e)
                        {
                            _lastErrorMessage = string.Format("ERROR: {0} - {1}", activeCodes[1], e.Message);
                            Details = string.Format("{0} - {1}", activeCodes[1], e.Message);
                            ServiceInfo.ErrorInLastRun = true;
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
                _wpCount += activeCodes.Length;
                foreach (var b in isScheduledCaches)
                {
                    if (b) _scheduledCount++;
                }

                Details = string.Format("C:{0} T:{1} S:{2} ({3})", activeCodes[1], _wpCount, _scheduledCount, _lastErrorMessage);
            }
            catch (Exception e)
            {
                _lastErrorMessage = string.Format("{0}/{1} - {2}", activeCodes[0], activeCodes[1], e.Message);
                Details = string.Format("{0}/{1} - {2}", activeCodes[0], activeCodes[1], e.Message);
                ServiceInfo.ErrorInLastRun = true;
            }
        }
    }
}
