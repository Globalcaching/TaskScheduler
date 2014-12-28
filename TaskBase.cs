using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskScheduler
{
    public class TaskBase
    {
        public Manager TaskManager { get; set; }
        public ServiceInfo ServiceInfo;
        public volatile bool Busy;
        protected volatile bool _stop;

        private string _details;
        public string Details
        {
            get
            {
                string result;
                lock(this)
                {
                    result = _details;
                }
                return result;
            }
            set
            {
                lock(this)
                {
                    _details = value;
                }
            }
        }

        private int _checkInterval;
        private Thread _cfThread;
        private AutoResetEvent[] _cfTriggers = new AutoResetEvent[2];
        private struct Triggers
        {
            public const int StopTrigger = 0;
            public const int StartTrigger = 1;
        }

        public TaskBase(Manager taskManager, Type type, string description, int hours, int minutes, int seconds)
        {
            Busy = false;
            _stop = false;
            TaskManager = taskManager;
            using (var db = TaskManager.TaskSchedulerDatabase)
            {
                ServiceInfo = db.FirstOrDefault<ServiceInfo>("where ClassName=@0", type.ToString());
                if (ServiceInfo == null)
                {
                    ServiceInfo = new ServiceInfo();
                    ServiceInfo.ClassName = type.ToString();
                    ServiceInfo.Description = description;
                    ServiceInfo.Interval = new DateTime(2000, 1, 1, hours, minutes, seconds, 0);
                    db.Save(ServiceInfo);
                }
            }
            _checkInterval = (ServiceInfo.Interval.Hour * 60 * 60 * 1000) + (ServiceInfo.Interval.Minute * 60 * 1000) + (ServiceInfo.Interval.Second * 1000);
        }

        public virtual UserControl GetSettingsControl()
        {
            return new TaskSettingsBase(this);
        }

        public void UpdateServiceInfo()
        {
            using (var db = TaskManager.TaskSchedulerDatabase)
            {
                db.Update(ServiceInfo);
            }
            _checkInterval = (ServiceInfo.Interval.Hour * 60 * 60 * 1000) + (ServiceInfo.Interval.Minute * 60 * 1000) + (ServiceInfo.Interval.Second * 1000);
            if (ServiceInfo.Enabled)
            {
                ServiceStart();
            }
            else
            {
                ServiceStop();
            }
        }

        public void ServiceStart()
        {
            if (_cfThread == null)
            {
                _stop = false;

                _cfTriggers[Triggers.StopTrigger] = new AutoResetEvent(false);
                _cfTriggers[Triggers.StartTrigger] = new AutoResetEvent(false);

                _cfThread = new Thread(new ThreadStart(this.ServiceThreadMethod));
                _cfThread.IsBackground = true;
                _cfThread.Start();

                Start();
            }
        }
        public void ServiceStop()
        {
            if (_cfThread != null)
            {
                if (Busy)
                {
                    _stop = true;
                    while (Busy)
                    {
                        Thread.Sleep(10);
                    }
                }
                _cfTriggers[Triggers.StopTrigger].Set();
                _cfThread.Join();
                _cfThread = null;

                Stop();
            }
        }
        public void ServiceRunNow()
        {
            ServiceStart();
            _cfTriggers[Triggers.StartTrigger].Set();
        }

        protected virtual void Start()
        {
        }

        protected virtual void Stop()
        {
        }

        protected void ServiceThreadMethod()
        {
            while (true)
            {
                try
                {
                    int trig = WaitHandle.WaitAny(_cfTriggers, _checkInterval, false);
                    if (trig == Triggers.StopTrigger)
                    {
                        break;
                    }
                    else if (trig == Triggers.StartTrigger || (!ServiceInfo.RunAfter.HasValue || !ServiceInfo.RunBefore.HasValue))
                    {
                        Busy = true;
                        ServiceMethod();
                        ServiceInfo.LastRun = DateTime.Now;
                        ServiceInfo.InfoMessage = Details;
                        using (var db = TaskManager.TaskSchedulerDatabase)
                        {
                            db.Save(ServiceInfo);
                        }
                    }
                    else
                    {
                        //check interval
                        //check double check...
                        if (ServiceInfo.RunAfter.HasValue && ServiceInfo.RunBefore.HasValue)
                        {
                            DateTime dt = DateTime.Now;
                            DateTime st = DateTime.Now.Date + (ServiceInfo.RunAfter.Value - ServiceInfo.RunAfter.Value.Date);
                            DateTime et = DateTime.Now.Date + (ServiceInfo.RunBefore.Value - ServiceInfo.RunBefore.Value.Date);
                            if (dt >= st && dt <= et)
                            {
                                Busy = true;
                                ServiceMethod();
                                ServiceInfo.LastRun = DateTime.Now;
                                ServiceInfo.InfoMessage = Details;
                                using (var db = TaskManager.TaskSchedulerDatabase)
                                {
                                    db.Save(ServiceInfo);
                                }
                            }
                        }
                    }
                    Busy = false;
                    if (_stop)
                    {
                        break;
                    }
                }
                catch
                {
                    //what ever happens, we need to continue!
                }
            }
        }

        protected virtual void ServiceMethod()
        {
        }
    }
}
