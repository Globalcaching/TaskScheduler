﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class TaskPingSite : TaskBase
    {
        public TaskPingSite(Manager taskManager) :
            base(taskManager, typeof(TaskPingSite), "Ping site(s)", 0, 5, 0)
        {
            Details = "";
        }

        protected override void ServiceMethod()
        {
            try
            {
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    string webPage = wc.DownloadString("http://www.globalcaching.eu");
                }
            }
            catch
            {
            }
        }
    }
}