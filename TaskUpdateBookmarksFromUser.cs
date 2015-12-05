using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Models;

namespace TaskScheduler
{
    public class TaskUpdateBookmarksFromUser: TaskBase
    {
        private int _specialIndex = 0;
        private long _counter = 0;

        public TaskUpdateBookmarksFromUser(Manager taskManager) :
            base(taskManager, typeof(TaskUpdateBookmarksFromUser), "Update BM from users", 0, 0, 5)
        {
        }

        protected override void ServiceMethod()
        {
            try
            {
                string token = GeocachingAPI.Instance.GetServiceToken(ref _specialIndex);

                if (token.Length > 0)
                {
                    using (var db = new PetaPoco.Database(Manager.SchedulerConnectionString, "System.Data.SqlClient"))
                    {
                        List<long> gcidList = db.Fetch<long>("SELECT LastUserID FROM TaskUpdateBookmarksFromUser");
                        long lastId = 0;
                        if (gcidList.Count > 0)
                        {
                            lastId = gcidList[0];
                        }

                        long usrId = db.FirstOrDefault<long>(string.Format("select top 1 ID from [{0}].[dbo].[GCComUser] where ID>@0 and MemberTypeId>1 order by ID", GCComDataSupport.GeocachingDatabaseName), lastId);
                        if (usrId == 0)
                        {
                            usrId = db.FirstOrDefault<long>(string.Format("select top 1 ID from [{0}].[dbo].[GCComUser] where ID>@0 and MemberTypeId>1 order by ID", GCComDataSupport.GeocachingDatabaseName), 0);
                        }
                        if (gcidList.Count > 0)
                        {
                            db.Execute("update TaskUpdateBookmarksFromUser set LastUserID=@0", usrId);
                        }
                        else
                        {
                            db.Execute("insert into TaskUpdateBookmarksFromUser (LastUserID) values (@0)", usrId);
                        }

                        if (usrId > 0)
                        {
                            var bminfos = GeocachingAPI.GetBookmarkListsByUserID(token, usrId);
                            if (bminfos != null)
                            {
                                var curList = db.Fetch<GCComBookmark>(string.Format("select * from [{0}].[dbo].[GCComBookmark] where GCComUserID=@0", GCComDataSupport.GeocachingDatabaseName), usrId);
                                List<GCComBookmark> newList = new List<GCComBookmark>();
                                foreach (var bminfo in bminfos)
                                {
                                    var bm = GCComBookmark.From(bminfo, usrId);
                                    //forget the bm if it is not accessible not shared
                                    if (bm.ListIsShared && bm.ListIsPublic && !bm.ListIsArchived)
                                    {
                                        newList.Add(bm);
                                    }
                                }

                                if (curList.Count > 0 || newList.Count > 0)
                                {
                                    using (var dbc = GCComDataSupport.Instance.GetGCComDataDatabase())
                                    {
                                        foreach (var bm in newList)
                                        {
                                            var oldBm = (from a in curList where a.ListID == bm.ListID select a).FirstOrDefault();
                                            if (oldBm == null)
                                            {
                                                dbc.Insert(bm);
                                                TaskUpdateBookmark.AddScheduledBookmark(bm.ListID, bm.ListGUID);
                                            }
                                            else
                                            {
                                                dbc.Update("GCComBookmark", "ListID", bm);
                                                curList.Remove(oldBm);
                                            }
                                        }
                                        foreach (var bm in curList)
                                        {
                                            dbc.Execute("delete from GCComBookmark where ListID=@0", bm.ListID);
                                        }
                                    }
                                }
                                _counter++;
                                Details = string.Format("{0}, total counter={1}, User={2}", DateTime.Now.ToString(), _counter.ToString(), usrId);
                                ServiceInfo.ErrorInLastRun = false;
                            }
                            else
                            {
                                Details = string.Format("{0}, total counter={1}, ERROR, User={2}", DateTime.Now.ToString(), _counter.ToString(), usrId);
                                ServiceInfo.ErrorInLastRun = true;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Details = e.Message;
                ServiceInfo.ErrorInLastRun = true;
            }
        }
    }
}
