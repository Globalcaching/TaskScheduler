using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Models;

namespace TaskScheduler
{
    public class TaskUpdateBookmark : TaskBase
    {
        private static List<BookmarkInfo> _scheduledBookmarks = new List<BookmarkInfo>();

        private int _specialIndex = 0;
        private int _bmCount = 0;
        private int _scheduledCount = 0;

        public class BookmarkInfo
        {
            public long ListID { get; set; }
            public Guid ListGUID { get; set; }
        }

        public TaskUpdateBookmark(Manager taskManager) :
            base(taskManager, typeof(TaskUpdateBookmark), "Update Bookmark", 0, 1, 0)
        {
            _bmCount = 0;
            Details = _bmCount.ToString();
        }

        public static void AddScheduledBookmark(long bmListID, Guid bmGuid)
        {
            lock (_scheduledBookmarks)
            {
                if ((from a in _scheduledBookmarks where a.ListID==bmListID select a).FirstOrDefault()==null)
                {
                    _scheduledBookmarks.Add(new BookmarkInfo() { ListID = bmListID, ListGUID = bmGuid });
                }
            }
        }

        public static BookmarkInfo GetScheduledBookmark()
        {
            BookmarkInfo result = null;
            lock (_scheduledBookmarks)
            {
                if (_scheduledBookmarks.Count > 0)
                {
                    result = _scheduledBookmarks[0];
                    _scheduledBookmarks.RemoveAt(0);
                }
            }
            return result;
        }

        protected override void ServiceMethod()
        {
            BookmarkInfo activeBM = null;
            bool isScheduledBM = false;
            string token = null;
            try
            {
                //first get scheduled
                using (var db = new PetaPoco.Database(Manager.SchedulerConnectionString, "System.Data.SqlClient"))
                {
                    activeBM = GetScheduledBookmark();
                    if (activeBM != null)
                    {
                        isScheduledBM = true;
                    }
                    else
                    {
                        long lastId = 0;
                        List<long> gcidList = db.Fetch<long>("SELECT LastListID FROM TaskUpdateBookmark");
                        if (gcidList.Count > 0)
                        {
                            lastId = gcidList[0];
                        }
                        activeBM = db.FirstOrDefault<BookmarkInfo>(string.Format("select top 1 ListID, ListGUID from [{0}].[dbo].[GCComBookmark] where ListID>@0 order by ListID", GCComDataSupport.GeocachingDatabaseName), lastId);
                        if (activeBM == null)
                        {
                            lastId = 0;
                            activeBM = db.FirstOrDefault<BookmarkInfo>(string.Format("select top 1 ListID, ListGUID from [{0}].[dbo].[GCComBookmark] where ListID>@0 order by ListID", GCComDataSupport.GeocachingDatabaseName), 0);
                        }
                        if (activeBM != null)
                        {
                            if (gcidList.Count > 0)
                            {
                                db.Execute("update TaskUpdateBookmark set LastListID=@0", activeBM.ListID);
                            }
                            else
                            {
                                db.Execute("insert into TaskUpdateBookmark (LastListID) values (@0)", activeBM.ListID);
                            }
                        }
                    }
                }

                if (activeBM != null)
                {
                    token = GeocachingAPI.Instance.GetServiceToken(ref _specialIndex);

                    if (!string.IsNullOrEmpty(token))
                    {
                        var gcList = GeocachingAPI.GetBookmarkListByGuid(token, activeBM.ListGUID);
                        if (gcList != null)
                        {
                            using (var dbc = GCComDataSupport.Instance.GetGCComDataDatabase())
                            {
                                var curList = dbc.Fetch<long>("select GCComGeocacheID from GCComBookmarkContent where GCComBookmarkListID=@0", activeBM.ListID);
                                var curCount = curList.Count;
                                var newList = new List<long>();
                                foreach (var bmc in gcList)
                                {
                                    newList.Add(Helper.GetCacheIDFromCacheCode(bmc.CacheCode));
                                }
                                foreach (var id in newList)
                                {
                                    if (!curList.Contains(id))
                                    {
                                        dbc.Execute("insert into GCComBookmarkContent (GCComBookmarkListID, GCComGeocacheID) values (@0, @1)", activeBM.ListID, id);
                                    }
                                    else
                                    {
                                        curList.Remove(id);
                                    }
                                }
                                foreach (var id in curList)
                                {
                                    dbc.Execute("delete from GCComBookmarkContent where GCComBookmarkListID=@0 and GCComGeocacheID=@1", activeBM.ListID, id);
                                }
                                dbc.Execute("update GCComBookmark set NumberOfKnownItems = (select count(1) from GCComBookmarkContent inner join GCComGeocache on GCComBookmarkContent.GCComGeocacheID = GCComGeocache.ID where GCComBookmarkContent.GCComBookmarkListID=@1) where ListID=@0", activeBM.ListID, activeBM.ListID);
                            }
                        }

                        _bmCount++;
                        if (isScheduledBM)
                        {
                            _scheduledCount++;
                        }
                    }
                    Details = string.Format("BM:{0} T:{1} S:{2}", activeBM.ListID, _bmCount, _scheduledCount);
                }

                ServiceInfo.ErrorInLastRun = false;
            }
            catch (Exception e)
            {
                Details = string.Format("{0} - {1}", activeBM == null ? 0 : activeBM.ListID, e.Message);
                ServiceInfo.ErrorInLastRun = true;
            }
        }
    }
}
