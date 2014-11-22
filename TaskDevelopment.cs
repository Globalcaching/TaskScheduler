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
                //updatePublisheddate();
                clearQueue();
            }
            catch (Exception e)
            {
                Details = e.Message;
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
