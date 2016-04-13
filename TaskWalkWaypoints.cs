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
            string[] activeCodes = new string[]{ "", "" };
            bool[] isScheduledCaches = new bool[] { false, false };
            long lastId = 0;
            List<long> gcidList;
            try
            {
                //first get scheduled
                using (var db = new PetaPoco.Database(Manager.SchedulerConnectionString, "System.Data.SqlClient"))
                {
                    for (int i = 0; i < activeCodes.Length; i++)
                    {
                        ScheduledWaypoint swp = db.FirstOrDefault<ScheduledWaypoint>("select top 1 * from ScheduledWaypoint where FullRefresh=1");
                        if (swp != null)
                        {
                            activeCodes[i] = swp.Code;
                            isScheduledCaches[i] = true;

                            db.Execute("delete from ScheduledWaypoint where Code=@0", swp.Code);
                        }
                        if (!isScheduledCaches[i])
                        {
                            gcidList = db.Fetch<long>("SELECT LastGeocacheID FROM TaskWalkWaypoints");
                            if (gcidList.Count > 0)
                            {
                                lastId = gcidList[0];
                            }
                            GeocacheInfo gi = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 GCComGeocache.ID, Code from [{0}].[dbo].[GCComGeocache] inner join [{1}].[dbo].[GCEuGeocache] on GCComGeocache.ID = GCEuGeocache.ID where GCComGeocache.ID>@0 and (Archived=0 or DATEDIFF(DAY,COALESCE(MostRecentArchivedDate, GETDATE()),GETDATE()) < 60) order by GCComGeocache.ID", GCComDataSupport.GeocachingDatabaseName, GCEuDataSupport.GlobalcachingDatabaseName), lastId);
                            if (gi == null)
                            {
                                lastId = 0;
                                gi = db.FirstOrDefault<GeocacheInfo>(string.Format("select top 1 ID, Code from [{0}].[dbo].[GCComGeocache] where ID>@0 order by ID", GCComDataSupport.GeocachingDatabaseName), lastId);
                            }
                            if (gi != null)
                            {
                                activeCodes[i] = gi.Code;
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
                }

                //update
                string token = GeocachingAPI.Instance.GetServiceToken(ref _tokenAccountIndex);
                if (token.Length > 0)
                {
                    var gcData = GeocachingAPI.GetGeocaches(token, activeCodes);
                    if (gcData != null)
                    {
                        TaskManager.ResetGeocachingComLiveAPINotAvailableCounter();
                        for (int i = 0; i < gcData.Length; i++)
                        {
                            var gc = gcData[i];
                            if (gc != null && (gc.CountryID == 4 || gc.CountryID == 8 || gc.CountryID == 141 || gc.StateID == 143 || gc.StateID == 139 || gc.StateID == 144 || gc.StateID == 142))
                            {
                                DataSupport.Instance.AddGeocache(gc);

                                //if scheduled, update logs too
                                if (isScheduledCaches[i])
                                {
                                    TaskUpdateLogs.AddScheduledWaypoint(gc.Code);
                                    TaskGeocacheImages.AddScheduledWaypoint(gc.Code);
                                }

                                _wpCount++;
                                if (isScheduledCaches[i])
                                {
                                    _scheduledCount++;
                                }

                                Details = string.Format("C:{0} T:{1} S:{2}", gc.Code, _wpCount, _scheduledCount);
                            }
                        }
                    }
                    else
                    {
                        Details = "ERROR";
                        TaskManager.IncrementGeocachingComLiveAPINotAvailableCounter();
                    }
                }
                ServiceInfo.ErrorInLastRun = false;
            }
            catch(Exception e)
            {
                ServiceInfo.ErrorInLastRun = true;
                Details = string.Format("{0}/{1} - {2}", activeCodes[0], activeCodes[1], e.Message);
            }
        }
    }
}
