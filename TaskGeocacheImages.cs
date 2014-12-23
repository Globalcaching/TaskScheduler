using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class TaskGeocacheImages : TaskBase
    {
        private static List<string> _scheduledWaypoints = new List<string>();

        private int _wpCount = 0;
        private int _scheduledCount = 0;

        public class GeocacheInfo
        {
            public long ID { get; set; }
            public string Code { get; set; }
        }

        public TaskGeocacheImages(Manager taskManager) :
            base(taskManager, typeof(TaskGeocacheImages), "Get Geocache Images", 0, 0, 30)
        {
            _wpCount = 0;
            Details = _wpCount.ToString();
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
            List<long> gcidList;
            try
            {
                //first get scheduled
                using (var db = new PetaPoco.Database(Manager.SchedulerConnectionString, "System.Data.SqlClient"))
                {
                    activeCode = GetScheduledWaypoint();
                    if (string.IsNullOrEmpty(activeCode))
                    {
                        gcidList = db.Fetch<long>("SELECT LastGeocacheID FROM TaskGeocacheImages");
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
                                db.Execute("update TaskGeocacheImages set LastGeocacheID=@0", lastId);
                            }
                            else
                            {
                                db.Execute("insert into TaskGeocacheImages (LastGeocacheID) values (@0)", lastId);
                            }
                        }
                    }
                    else
                    {
                        isScheduledCache = true;
                        GeocacheInfo gi = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 ID, Code from [{0}].[dbo].[GCComGeocache] where Code=@0", GCComDataSupport.GeocachingDatabaseName), activeCode);
                        activeId = gi.ID;
                    }
                }

                if (!string.IsNullOrEmpty(activeCode) && activeId > 0)
                {
                    List<string> urls;
                    using (var db = GCComDataSupport.Instance.GetGCComDataDatabase())
                    {
                        urls = db.Fetch<string>("select Url from GCComGeocacheImage where GeocacheID=@0", activeId);
                    }
                    if (urls != null)
                    {
                        foreach (var s in urls)
                        {
                            string fn = System.IO.Path.GetFileName(s);
                            string localPath = System.IO.Path.Combine(GetFolder(activeCode, true), fn);
                            if (!System.IO.File.Exists(localPath))
                            {
                                try
                                {
                                    using (var wc = new WebClient())
                                    {
                                        wc.DownloadFile(s, localPath);
                                    }
                                }
                                catch
                                {
                                }
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
            catch (Exception e)
            {
                Details = string.Format("{0} - {1}", activeCode, e.Message);
                ServiceInfo.ErrorInLastRun = true;
            }
        }

        private string GetFolder(string gcCode, bool create)
        {
            string result = "c:\\GeocacheImages";
            if (create && !System.IO.Directory.Exists(result))
            {
                System.IO.Directory.CreateDirectory(result);
            }
            result = System.IO.Path.Combine(result, gcCode[gcCode.Length - 1].ToString());
            if (create && !System.IO.Directory.Exists(result))
            {
                System.IO.Directory.CreateDirectory(result);
            }
            result = System.IO.Path.Combine(result, gcCode[gcCode.Length - 2].ToString());
            if (create && !System.IO.Directory.Exists(result))
            {
                System.IO.Directory.CreateDirectory(result);
            }
            result = System.IO.Path.Combine(result, gcCode);
            if (create && !System.IO.Directory.Exists(result))
            {
                System.IO.Directory.CreateDirectory(result);
            }
            return result;
        }

    }
}
