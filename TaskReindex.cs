using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class TaskReindex : TaskBase
    {
        private DateTime _lastRunAtDate = DateTime.MinValue.Date;

        public TaskReindex(Manager taskManager) :
            base(taskManager, typeof(TaskReindex), "Reindex task", 1, 0, 0)
        {
            Details = "";
        }

        protected override void ServiceMethod()
        {
            try
            {
                if (DateTime.Now.Date != _lastRunAtDate)
                {
                    _lastRunAtDate = DateTime.Now.Date;
                    Details = "";
                    using (var db = GCEuDataSupport.Instance.GetGCEuDataDatabase())
                    {
                        db.CommandTimeout = 60;
                        db.Execute("ALTER INDEX [GCEuGeocache_ID] ON [dbo].[GCEuGeocache] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, IGNORE_DUP_KEY  = OFF, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCEuComUserNameChange_ID] ON [dbo].[GCEuComUserNameChange] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCEuFTFStats_Jaar] ON [dbo].[GCEuFTFStats] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCEuCCCUser_UserID] ON [dbo].[GCEuCCCUser] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, IGNORE_DUP_KEY  = OFF, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCEuUserSettings_YafUserID] ON [dbo].[GCEuUserSettings] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, IGNORE_DUP_KEY  = OFF, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                    }
                    using (var db = GCComDataSupport.Instance.GetGCComDataDatabase())
                    {
                        db.CommandTimeout = 90;
                        db.Execute("ALTER INDEX [GCComDataAdditionalWaypoints_Code] ON [dbo].[GCComDataAdditionalWaypoints] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, IGNORE_DUP_KEY  = OFF, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComDataAdditionalWaypoints_GeocacheID] ON [dbo].[GCComDataAdditionalWaypoints] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocache_Code] ON [dbo].[GCComGeocache] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, IGNORE_DUP_KEY  = OFF, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocache_CountryID] ON [dbo].[GCComGeocache] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocache_Guid] ON [dbo].[GCComGeocache] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, IGNORE_DUP_KEY  = OFF, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocache_ID] ON [dbo].[GCComGeocache] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, IGNORE_DUP_KEY  = OFF, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocache_Lat] ON [dbo].[GCComGeocache] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocache_Lon] ON [dbo].[GCComGeocache] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocacheImage_GeocacheID] ON [dbo].[GCComGeocacheImage] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocacheImage_Url] ON [dbo].[GCComGeocacheImage] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, IGNORE_DUP_KEY  = OFF, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocacheLog_FinderId] ON [dbo].[GCComGeocacheLog] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocacheLog_GeocacheID] ON [dbo].[GCComGeocacheLog] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocacheLog_ID] ON [dbo].[GCComGeocacheLog] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, IGNORE_DUP_KEY  = OFF, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocacheLog_VisitDate] ON [dbo].[GCComGeocacheLog] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocacheLogImage_GeocacheID] ON [dbo].[GCComGeocacheLogImage] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComGeocacheLogImage_Url] ON [dbo].[GCComGeocacheLogImage] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, IGNORE_DUP_KEY  = OFF, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComUser_ID] ON [dbo].[GCComUser] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, IGNORE_DUP_KEY  = OFF, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                        db.Execute("ALTER INDEX [GCComUser_UserName] ON [dbo].[GCComUser] REBUILD PARTITION = ALL WITH ( PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, ONLINE = OFF, SORT_IN_TEMPDB = OFF )");
                    }
                }
            }
            catch (Exception e)
            {
                Details = e.Message;
            }
        }
    }
}
