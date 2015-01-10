using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Models;

namespace TaskScheduler
{
    public class TaskUpdateTrackables : TaskBase
    {
        private int _tbCount = 0;
        private int _tokenAccountIndex = 0;

        public TaskUpdateTrackables(Manager taskManager) :
            base(taskManager, typeof(TaskUpdateTrackables), "Update Trackables", 0, 1, 0)
        {
            _tbCount = 0;
            Details = _tbCount.ToString();
        }

        protected override void ServiceMethod()
        {
            string activeCode = "";
            try
            {
                bool isScheduledCache = false;

                //first get scheduled
                using (var db = new PetaPoco.Database(Manager.SchedulerConnectionString, "System.Data.SqlClient"))
                {
                    ScheduledTrackable swp = db.FirstOrDefault<ScheduledTrackable>("select top 1 * from ScheduledTrackable");
                    if (swp != null)
                    {
                        activeCode = swp.Code;
                        isScheduledCache = true;

                        db.Execute("delete from ScheduledTrackable where Code=@0", swp.Code);
                    }
                    if (!isScheduledCache)
                    {
                        List<string> gcidList = db.Fetch<string>("SELECT LastTrackableCode FROM TaskUpdateTrackable");
                        if (gcidList.Count > 0)
                        {
                            activeCode = gcidList[0];
                        }
                        activeCode = db.FirstOrDefault<string>(string.Format("select top 1 Code from [{0}].[dbo].[GCEuTrackable] where Code>@0 and Updated<=@1 order by Code", GCEuDataSupport.GlobalcachingDatabaseName), activeCode, DateTime.Now.AddDays(-1));
                        if (string.IsNullOrEmpty(activeCode))
                        {
                            activeCode = "";
                            activeCode = db.FirstOrDefault<string>(string.Format("select top 1 Code from [{0}].[dbo].[GCEuTrackable] where Code>@0 and Updated<=@1 order by Code", GCEuDataSupport.GlobalcachingDatabaseName), activeCode, DateTime.Now.AddDays(-1));
                        }
                        if (!string.IsNullOrEmpty(activeCode))
                        {
                            if (gcidList.Count > 0)
                            {
                                db.Execute("update TaskUpdateTrackable set LastTrackableCode=@0", activeCode);
                            }
                            else
                            {
                                db.Execute("insert into TaskUpdateTrackable (LastTrackableCode) values (@0)", activeCode);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(activeCode))
                    {
                        //update
                        string token = GeocachingAPI.Instance.GetServiceToken(ref _tokenAccountIndex);
                        if (token.Length > 0)
                        {
                            var tb = GeocachingAPI.GetTrackable(token, activeCode);
                            if (tb != null)
                            {
                                var logs = GeocachingAPI.GetTrackableLogs(token, tb.Code);
                                var tbTravel = GeocachingAPI.GetTrackableTravel(token, tb.Code);
                                if (logs != null)
                                {
                                    //update tb
                                    DataSupport.Instance.AddTrackable(tb, logs, tbTravel);
                                    _tbCount++;
                                }
                            }
                        }
                    }
                }

                Details = string.Format("C:{0} T:{1}", activeCode??"", _tbCount);
                ServiceInfo.ErrorInLastRun = false;
            }
            catch (Exception e)
            {
                ServiceInfo.ErrorInLastRun = true;
                Details = string.Format("{0} - {1}", activeCode??"", e.Message);
            }
        }
    }
}
