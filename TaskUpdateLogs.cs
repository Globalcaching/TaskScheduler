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

        private volatile int _wpCount = 0;
        private volatile int _scheduledCount = 0;

        public TaskUpdateLogs(Manager taskManager) :
            base(taskManager, typeof(TaskUpdateLogs), "Update Logs", 0, 0, 3)
        {
            _wpCount = 0;
            Details = _wpCount.ToString();

            //test:
            //AddScheduledWaypoint("GC6F3JK");
            //AddScheduledWaypoint("GC6F7TA");
        }

        public class GeocacheInfo
        {
            public long ID { get; set; }
            public string Code { get; set; }
        }

        private class ThreadProps
        {
            public GeocacheInfo gi = null;
            public string token = null;
            public bool isScheduledCache = false;
            public string code = "";
            public DateTime dt;
            public List<Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog> logs;
        }
        private List<ThreadProps> _threadProps = new List<ThreadProps>();

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

        private static string GetScheduledWaypoint()
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

        private void UpdateDetails(string wpCode, bool isScheduled, string message)
        {
            lock (_scheduledWaypoints)
            {
                _wpCount++;
                if (isScheduled)
                {
                    _scheduledCount++;
                }
                Details = string.Format("C:{0} T:{1} S:{2} {3}", wpCode, _wpCount, _scheduledCount, message??"");
                PushDetails();
            }
        }

        public GeocacheInfo GetNextWaypoint(PetaPoco.Database db)
        {
            GeocacheInfo result = null;
            List<long> gcidList;
            long lastId = 0;
            lock (this)
            {
                gcidList = db.Fetch<long>("SELECT LastGeocacheID FROM TaskUpdateLogs");
                if (gcidList.Count > 0)
                {
                    lastId = gcidList[0];
                }
                result = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 GCComGeocache.ID, Code from [{0}].[dbo].[GCComGeocache] inner join [{1}].[dbo].[GCEuGeocache] on GCComGeocache.ID = GCEuGeocache.ID where GCComGeocache.ID>@0 and (Archived=0 or DATEDIFF(DAY,COALESCE(MostRecentArchivedDate, GETDATE()),GETDATE()) < 120) order by GCComGeocache.ID", GCComDataSupport.GeocachingDatabaseName, GCEuDataSupport.GlobalcachingDatabaseName), lastId);
                if (result == null)
                {
                    lastId = 0;
                    result = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 ID, Code from [{0}].[dbo].[GCComGeocache] where ID>@0 order by ID", GCComDataSupport.GeocachingDatabaseName), lastId);
                }
                if (result != null)
                {
                    lastId = result.ID;

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
            return result;
        }

        protected override void ServiceMethod()
        {
            List<Thread> threads = new List<Thread>();
            List<string> tokens = new List<string>();
            int specialIndex = 0;
            var token = GeocachingAPI.Instance.GetServiceToken(ref specialIndex);
            while (!tokens.Contains(token))
            {
                tokens.Add(token);
                token = GeocachingAPI.Instance.GetServiceToken(ref specialIndex);
            }
            foreach (var tkn in tokens)
            {
                var tp = new ThreadProps();
                tp.token = tkn;
                _threadProps.Add(tp);
                Thread t = new Thread(new ParameterizedThreadStart(ServicePerAccountMethod));
                t.Start(tp);
                threads.Add(t);
            }
            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        protected void ServicePerAccountMethod(object data)
        {
            var tp = data as ThreadProps;
            while (!base._stop)
            {
                tp.gi = null;
                tp.isScheduledCache = false;
                try
                {
                    //first get scheduled
                    using (var db = new PetaPoco.Database(Manager.SchedulerConnectionString, "System.Data.SqlClient"))
                    {
                        tp.code = GetScheduledWaypoint();
                        if (!string.IsNullOrEmpty(tp.code))
                        {
                            tp.isScheduledCache = true;
                            tp.gi = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 ID, Code from [{0}].[dbo].[GCComGeocache] where Code=@0", GCComDataSupport.GeocachingDatabaseName), tp.code);
                        }
                        if (!tp.isScheduledCache)
                        {
                            tp.gi = GetNextWaypoint(db);
                        }
                    }

                    if (tp.gi!=null)
                    {
                        if (tp.isScheduledCache)
                        {
                            tp.dt = new DateTime(2000, 1, 1);
                        }
                        else
                        {
                            tp.dt = DateTime.Now.AddMonths(-3);
                        }
                        //update
                        tp.logs = GeocachingAPI.GetLogsOfGeocache(tp.token, tp.gi.Code);
                        if (tp.logs != null)
                        {
                            DataSupport.Instance.AddLogs(tp.gi.ID, tp.logs.ToArray(), true, tp.dt);
                            using (var db = new PetaPoco.Database(GCEuDataSupport.Instance.GCEuDataConnectionString, "System.Data.SqlClient"))
                            {
                                db.Execute("update GCEuGeocache set AllLogUpdateDate = @0 where ID=@1", DateTime.Now, tp.gi.ID);
                            }
                            UpdateDetails(tp.gi.Code, tp.isScheduledCache, "");
                            //tp.logs = null;
                        }
                        else
                        {
                            UpdateDetails(tp.gi.Code, tp.isScheduledCache, "ERROR");
                        }
                    }
                    ServiceInfo.ErrorInLastRun = false;
                }
                catch (Exception e)
                {
                    UpdateDetails("", false, string.Format("ERROR: {0}", e.Message));
                    ServiceInfo.ErrorInLastRun = true;
                }

                var wt = DateTime.Now.AddMilliseconds(base._checkInterval);
                while (!base._stop && DateTime.Now < wt)
                {
                    System.Threading.Thread.Sleep(200);
                }
            }
        }
    }
}
