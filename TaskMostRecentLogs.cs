using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class TaskMostRecentLogs: TaskBase
    {
        public class GCCacheLogInfo
        {
            public string LinkToLog = null;
            public string LinkToWp = null;
            public string Wp = null;
        }

        private GCCacheLogInfo _lastMostRecentLog = null;
        private long _logCount;
        private string _geocachingComUrl = "http://www.geocaching.com";

        public TaskMostRecentLogs(Manager taskManager) :
            base(taskManager, typeof(TaskMostRecentLogs), "Most recent logs", 0, 0, 20)
        {
            _logCount = 0;
            Details = _logCount.ToString();
        }

        protected override void ServiceMethod()
        {
            bool logsOnPage = false;
            try
            {
                using (var db = GCComDataSupport.Instance.GetGCComDataDatabase())
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    string webPage = wc.DownloadString(string.Format("{0}/seek/history.aspx", _geocachingComUrl));
                    if (webPage != null)
                    {
                        GCCacheLogInfo firstOfPage = null;
                        //scheme:
                        //profile, cache, cache, log
                        //search for profile, then cache, then log

                        bool first = true;
                        char[] findEndChar = new char[] { ' ', '"', '>', '\'' };
                        int pos = webPage.IndexOf(string.Format("{0}/profile/?", _geocachingComUrl), StringComparison.OrdinalIgnoreCase);
                        while (pos > 0)
                        {
                            logsOnPage = true;
                            GCCacheLogInfo ci = new GCCacheLogInfo();

                            pos = webPage.IndexOf(string.Format("{0}/seek/cache_details.aspx?", _geocachingComUrl), pos, StringComparison.OrdinalIgnoreCase);
                            int pos2 = webPage.IndexOfAny(findEndChar, pos);
                            ci.LinkToWp = webPage.Substring(pos, pos2 - pos);

                            pos = webPage.IndexOf(string.Format("{0}/seek/log.aspx?", _geocachingComUrl), pos, StringComparison.OrdinalIgnoreCase);
                            pos = webPage.IndexOf('=', pos) + 1;
                            pos2 = webPage.IndexOfAny(findEndChar, pos);
                            ci.LinkToLog = webPage.Substring(pos, pos2 - pos);

                            if (first)
                            {
                                firstOfPage = ci;
                            }
                            if (_lastMostRecentLog == null)
                            {
                                _lastMostRecentLog = ci;
                            }
                            else if (_lastMostRecentLog.LinkToLog == ci.LinkToLog)
                            {
                                //been here, done that
                                break;
                            }
                            first = false;

                            string guid = ci.LinkToWp.Substring(ci.LinkToWp.IndexOf('=') + 1);
                            string code = db.FirstOrDefault<string>("select Code from GCComGeocache where GUID=@0 and (CountryID=141 or CountryID=4 or CountryID=8)", Guid.Parse(guid));
                            if (!string.IsNullOrEmpty(code))
                            {
                                ScheduledWaypoint swp = db.FirstOrDefault<ScheduledWaypoint>(string.Format("select * from [{0}].[dbo].[ScheduledWaypoint] where Code=@0", Manager.SchedulerDatabase), code);
                                if (swp == null)
                                {
                                    swp = new ScheduledWaypoint();
                                    swp.Code = code;
                                    swp.DateAdded = DateTime.Now;
                                    swp.FullRefresh = false;
                                    db.Insert(string.Format("[{0}].[dbo].[ScheduledWaypoint]", Manager.SchedulerDatabase), null, false, swp);

                                    _logCount++;
                                    Details = _logCount.ToString();
                                }
                            }
                            pos = webPage.IndexOf(string.Format("{0}/profile/?", _geocachingComUrl), pos, StringComparison.OrdinalIgnoreCase);
                        }
                        _lastMostRecentLog = firstOfPage;
                    }
                }
            }
            catch(Exception e)
            {
                Details = e.Message;
            }

            if (!logsOnPage)
            {
                TaskManager.IncrementGeocachingComWWWNotAvailableCounter();
            }
            else
            {
                TaskManager.ResetGeocachingComWWWNotAvailableCounter();
            }
        }

    }
}
