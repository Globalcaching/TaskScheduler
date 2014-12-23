using PetaPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class Manager: IDisposable
    {
        private SchedulerStatus TaskSchedularStatus;
        public List<TaskBase> Tasks;
        private int _wwwErrors = 0;
        private int _apiErrors = 0;

#if DEBUG
        public static string SchedulerDatabase = "TaskScheduler_tst";
#else
        public static string SchedulerDatabase = "TaskScheduler";
#endif
        public static string SchedulerConnectionString
        {
            get
            {
                return string.Format("Data Source={0};Initial Catalog={1};Persist Security Info=True;User ID={2};Password={3};Connect Timeout=45;", Properties.Settings.Default.DatabaseServer, SchedulerDatabase, Properties.Settings.Default.DatabaseUser, Properties.Settings.Default.DatabasePassword);
            }
        }

        public Manager()
        {
            using (var db = TaskSchedulerDatabase)
            {
                TaskSchedularStatus = db.FirstOrDefault<SchedulerStatus>("");
                if (TaskSchedularStatus == null)
                {
                    TaskSchedularStatus = new SchedulerStatus();
                    TaskSchedularStatus.GCComWWWError = false;
                    TaskSchedularStatus.LiveAPIError = false;
                    db.Save(TaskSchedularStatus);
                }
                Tasks = new List<TaskBase>();
                Tasks.Add(new TaskMostRecentLogs(this));
                Tasks.Add(new TaskWalkWaypoints(this));
                Tasks.Add(new TaskUpdateLogs(this));
                Tasks.Add(new TaskPocketQuery(this));
                Tasks.Add(new TaskUpdateStatus(this));
                Tasks.Add(new TaskUpdateFTFStats(this));
                Tasks.Add(new TaskPingSite(this));
                Tasks.Add(new TaskReindex(this));
                Tasks.Add(new TaskGeocacheImages(this));
#if DEBUG
                Tasks.Add(new TaskUpdateFromOldDatabase(this));
                Tasks.Add(new TaskDevelopment(this));
#endif
                foreach (var t in Tasks)
                {
                    if (t.ServiceInfo.Enabled)
                    {
                        t.ServiceStart();
                    }
                }
            }
        }

        public PetaPoco.Database TaskSchedulerDatabase
        {
            get { return new Database(SchedulerConnectionString, "System.Data.SqlClient"); }
        }

        public void Dispose()
        {
            foreach (var t in Tasks)
            {
                t.ServiceStop();
            }
        }

        public void IncrementGeocachingComWWWNotAvailableCounter()
        {
            lock (this)
            {
                if (!TaskSchedularStatus.GCComWWWError)
                {
                    _wwwErrors++;
                    if (_wwwErrors > 2)
                    {
                        TaskSchedularStatus.GCComWWWError = true;
                        using (var TaskSchedulerDatabase = new Database(SchedulerConnectionString, "System.Data.SqlClient"))
                        {
                            TaskSchedulerDatabase.Update(TaskSchedularStatus);
                        }
                    }
                }
            }
        }
        public void ResetGeocachingComWWWNotAvailableCounter()
        {
            lock (this)
            {
                _wwwErrors = 0;
                if (TaskSchedularStatus.GCComWWWError)
                {
                    _wwwErrors++;
                    TaskSchedularStatus.GCComWWWError = false;
                    using (var TaskSchedulerDatabase = new Database(SchedulerConnectionString, "System.Data.SqlClient"))
                    {
                        TaskSchedulerDatabase.Update(TaskSchedularStatus);
                    }
                }
            }
        }

        public void IncrementGeocachingComLiveAPINotAvailableCounter()
        {
            lock (this)
            {
                if (!TaskSchedularStatus.LiveAPIError)
                {
                    _apiErrors++;
                    if (_apiErrors > 2)
                    {
                        TaskSchedularStatus.LiveAPIError = true;
                        using (var TaskSchedulerDatabase = new Database(SchedulerConnectionString, "System.Data.SqlClient"))
                        {
                            TaskSchedulerDatabase.Update(TaskSchedularStatus);
                        }
                    }
                }
            }
        }
        public void ResetGeocachingComLiveAPINotAvailableCounter()
        {
            lock (this)
            {
                _apiErrors = 0;
                if (TaskSchedularStatus.LiveAPIError)
                {
                    _apiErrors++;
                    TaskSchedularStatus.LiveAPIError = false;
                    using (var TaskSchedulerDatabase = new Database(SchedulerConnectionString, "System.Data.SqlClient"))
                    {
                        TaskSchedulerDatabase.Update(TaskSchedularStatus);
                    }
                }
            }
        }

    }
}
