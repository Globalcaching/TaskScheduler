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
        public Database TaskSchedulerDatabase = null;
        private SchedulerStatus TaskSchedularStatus;
        public List<TaskBase> Tasks;

        public static string SchedulerDatabase = "TaskScheduler_tst";
        public static string SchedulerConnectionString
        {
            get
            {
                return string.Format("Data Source={0};Initial Catalog={1};Persist Security Info=True;User ID={2};Password={3};Connect Timeout=45;", Properties.Settings.Default.DatabaseServer, SchedulerDatabase, Properties.Settings.Default.DatabaseUser, Properties.Settings.Default.DatabasePassword);
            }
        }

        public Manager()
        {
            TaskSchedulerDatabase = new Database(SchedulerConnectionString, "System.Data.SqlClient");
            TaskSchedularStatus = TaskSchedulerDatabase.SingleOrDefault<SchedulerStatus>("");
            if (TaskSchedularStatus==null)
            {
                TaskSchedularStatus = new SchedulerStatus();
                TaskSchedularStatus.GCComWWWError = false;
                TaskSchedularStatus.LiveAPIError = false;
                TaskSchedulerDatabase.Save(TaskSchedularStatus);
            }
            Tasks = new List<TaskBase>();
            Tasks.Add(new TaskMostRecentLogs(this));
            Tasks.Add(new TaskWalkWaypoints(this));
            Tasks.Add(new TaskUpdateLogs(this));
            Tasks.Add(new TaskPocketQuery(this));
            Tasks.Add(new TaskUpdateStatus(this));
            Tasks.Add(new TaskUpdateFromOldDatabase(this));
            Tasks.Add(new TaskDevelopment(this));
            foreach (var t in Tasks)
            {
                if (t.ServiceInfo.Enabled)
                {
                    t.ServiceStart();
                }
            }
        }

        public void Dispose()
        {
            foreach (var t in Tasks)
            {
                t.ServiceStop();
            }
            if (TaskSchedulerDatabase != null)
            {
                TaskSchedulerDatabase.Dispose();
                TaskSchedulerDatabase = null;
            }
        }

        public void UpdateServiceInfo(ServiceInfo si)
        {
            lock (TaskSchedulerDatabase)
            {
                TaskSchedulerDatabase.Update(si);
            }
        }

        public void IncrementGeocachingComWWWNotAvailableCounter()
        {

        }
        public void ResetGeocachingComWWWNotAvailableCounter()
        {

        }

    }
}
