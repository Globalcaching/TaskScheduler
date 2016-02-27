using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Models;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;
using www.geocaching.com.Geocaching1.Live.data;

namespace TaskScheduler
{
    public class TaskDownloadLogsOnRequest : TaskBase
    {
        private List<GCEuDownloadLogsStatus> _inProgressList;
        private bool _firstRun;

        public TaskDownloadLogsOnRequest(Manager taskManager) :
            base(taskManager, typeof(TaskDownloadLogsOnRequest), "Download Logs On Request", 0, 1, 0)
        {
            Details = "";
            _inProgressList = new List<GCEuDownloadLogsStatus>();
            _firstRun = true;
        }

        protected override void ServiceMethod()
        {
            try
            {
                using (var db = GCEuMacroDataSupport.Instance.GetGCEuMacroDataDatabase())
                {
                    if (_firstRun)
                    {
                        _firstRun = false;
                        //(restarted) check for previous progress and update status to cancelled
                        if (db.ExecuteScalar<int>("SELECT count(1) FROM GCEuMacroData.sys.tables WHERE name = 'GCEuDownloadLogsStatus'") == 0)
                        {
                            db.Execute(@"create table GCEuMacroData.dbo.GCEuDownloadLogsStatus
(
UserID int not null,
UserNames nvarchar(2000),
IncludeYourArchived bit not null,
RequestedAt datetime not null,
Status nvarchar(255),
Busy bit,
UserNamesCompleted nvarchar(2000),
UserNameBusy nvarchar(255),
TotalFindCount int,
TotalLogsImported int not null,
LastUpdateAt datetime,
LogTableName nvarchar(255)
)
");

                            db.Execute(@"CREATE UNIQUE NONCLUSTERED INDEX [GCEuDownloadLogsStatus-UserID] ON [GCEuMacroData].[dbo].[GCEuDownloadLogsStatus]
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
");

                        }
                        var cancelled = db.Fetch<GCEuDownloadLogsStatus>("where Busy=1");
                        foreach (var item in cancelled)
                        {
                            item.Busy = false;
                            UpdateStatus(db, item, "Onderbroken door een herstart van de globalcaching applicatie. Dien het verzoek nogmaals in.");
                        }
                    }

                    var newRequests = db.Fetch<GCEuDownloadLogsStatus>("where Busy is null");
                    foreach (var newreq in newRequests)
                    {
                        //new request during process?
                        lock (_inProgressList)
                        {
                            var inproc = (from a in _inProgressList where a.UserID == newreq.UserID select a).FirstOrDefault();
                            if (inproc == null)
                            {
                                newreq.Busy = true;
                                newreq.TotalLogsImported = 0;
                                newreq.LogTableName = string.Format("LiveAPILogs_{0}", newreq.UserID);
                                newreq.TotalFindCount = 0;
                                newreq.UserNameBusy = "";
                                newreq.UserNamesCompleted = "";
                                UpdateStatus(db, newreq, "Gestart");

                                if (db.ExecuteScalar<int>("SELECT count(1) FROM GCEuMacroData.sys.tables WHERE name = @0", newreq.LogTableName) == 0)
                                {
                                    db.Execute(string.Format("create table GCEuMacroData.dbo.{0}", newreq.LogTableName) +
@"(
	[ID] [bigint] NOT NULL,
	[GeocacheID] [bigint] NOT NULL,
	[CacheCode] [nvarchar](15) NOT NULL,
	[Code] [nvarchar](15) NOT NULL,
	[FinderId] [bigint] NOT NULL,
	[Guid] [uniqueidentifier] NOT NULL,
	[LogIsEncoded] [bit] NOT NULL,
	[LogText] [ntext] NULL,
	[WptLogTypeId] [bigint] NOT NULL,
	[UpdatedLatitude] [float] NULL,
	[UpdatedLongitude] [float] NULL,
	[Url] [nvarchar](255) NULL,
	[UTCCreateDate] [datetime] NOT NULL,
	[VisitDate] [datetime] NOT NULL,
    [IsArchived] [bit] NOT NULL,
    [NumberOfImages] [int] NOT NULL
)
");

                                    db.Execute(string.Format("CREATE NONCLUSTERED INDEX [GCEuMacroGeocacheLog_FinderId] ON [GCEuMacroData].[dbo].[{0}]", newreq.LogTableName) +
@"(
	[FinderId] ASC
)
INCLUDE ( 	[WptLogTypeId],
	[VisitDate]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
");

                                    db.Execute(string.Format("CREATE NONCLUSTERED INDEX [GCEuMacroGeocacheLog_VisitDate] ON [GCEuMacroData].[dbo].[{0}]", newreq.LogTableName) +
@"(
	[VisitDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
");


                                }
                                else
                                {
                                    db.Execute(string.Format("truncate table GCEuMacroData.dbo.{0}", newreq.LogTableName));
                                }


                                _inProgressList.Add(newreq);

                                System.Threading.Thread t = new System.Threading.Thread(() => ProcessDownloadThreadMethod(newreq));
                                t.IsBackground = true;
                                t.Start();
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void ProcessDownloadThreadMethod(GCEuDownloadLogsStatus reqRecord)
        {
            try
            {
                string token = null;
                using (var db = GCEuDataSupport.Instance.GetGCEuDataDatabase())
                {
                    token = db.ExecuteScalar<string>("select LiveAPIToken from GCEuUserSettings where YafUserID=@0", reqRecord.UserID);
                }

                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception("Autorisatie voor Live API is vereist.");
                }

                //get all users
                using (var db = GCEuMacroDataSupport.Instance.GetGCEuMacroDataDatabase())
                {
                    if (IsNewRequest(db, reqRecord)) goto stopthread;
                    UpdateStatus(db, reqRecord, "Bezig met het ophalen van de account informatie van de gevraagde namen.");
                }
                var names = reqRecord.UserNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (names.Length > 10)
                {
                    throw new Exception("Maximaal 10 namen zijn toegestaan.");
                }
                if (names.Length == 0)
                {
                    throw new Exception("Minimaal 1 naam is vereist.");
                }

                var gcComUsers = new List<GCComUser>();
                using (var db = GCComDataSupport.Instance.GetGCComDataDatabase())
                {
                    foreach (var n in names)
                    {
                        //we need to find the name in our database to get the ID
                        var usr = db.Fetch<GCComUser>("where UserName=@0", n.Trim()).FirstOrDefault();
                        if (usr == null)
                        {
                            throw new Exception(string.Format("Kan naam '{0}' niet vinden in database", n));
                        }
                        gcComUsers.Add(usr);
                    }
                }

                var gcComUserProfiles = new List<UserProfile>();
                foreach (var usr in gcComUsers)
                {
                    var usrProf = GeocachingAPI.GetAnotherUsersProfile(token, usr.ID);
                    if (usrProf == null)
                    {
                        throw new Exception(string.Format("Kan profiel van '{0}' niet ophalen van geocaching.com", usr.UserName));
                    }
                    gcComUserProfiles.Add(usrProf.Profile);
                }

                using (var db = GCEuMacroDataSupport.Instance.GetGCEuMacroDataDatabase())
                {
                    reqRecord.TotalFindCount = gcComUserProfiles.Sum(x => x.User.FindCount) ?? 0;
                    if (IsNewRequest(db, reqRecord)) goto stopthread;
                    UpdateStatus(db, reqRecord, "Bezig met ophalen van de logs...");
                }

                LiveClient lc = GeocachingAPI.GetLiveClient();
                try
                {
                    foreach (var usrProf in gcComUserProfiles)
                    {
                        using (var db = GCEuMacroDataSupport.Instance.GetGCEuMacroDataDatabase())
                        {
                            reqRecord.UserNameBusy = usrProf.User.UserName;
                            UpdateStatus(db, reqRecord, null);
                        }

                        var addedToDatabase = new HashSet<long>();
                        int page = 0;
                        var req = new GetUsersGeocacheLogsRequest();
                        req.AccessToken = token;
                        req.ExcludeArchived = !reqRecord.IncludeYourArchived;
                        req.Username = usrProf.User.UserName;
                        req.MaxPerPage = 30;
                        req.StartIndex = 0;
                        req.LogTypes = new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 22, 23, 24, 45, 46, 47, 74 };

                        int retryCount = 0;
                        bool finished = false;
                        while (!finished && retryCount < 3)
                        {
                            try
                            {
                                var resp = lc.GetUsersGeocacheLogs(req);
                                while (resp.Status.StatusCode == 0)
                                {
                                    retryCount = 0;
                                    using (var db = GCEuMacroDataSupport.Instance.GetGCEuMacroDataDatabase())
                                    {
                                        foreach (var l in resp.Logs)
                                        {
                                            if (!addedToDatabase.Contains(l.ID))
                                            {
                                                addedToDatabase.Add(l.ID);
                                                var ldb = GCComGeocacheLogEx.From(Helper.GetCacheIDFromCacheCode(l.CacheCode), l);
                                                db.Insert(reqRecord.LogTableName, null, false, ldb);

                                                reqRecord.TotalLogsImported++;
                                            }
                                        }
                                        if (IsNewRequest(db, reqRecord)) goto stopthread;
                                        UpdateStatus(db, reqRecord, null);
                                    }

                                    if (resp.Logs.Count() > 0)
                                    {
                                        page++;
                                        //req.StartIndex = logs.Count;
                                        req.StartIndex = page * req.MaxPerPage;
                                        System.Threading.Thread.Sleep(4000);
                                        resp = lc.GetUsersGeocacheLogs(req);
                                    }
                                    else
                                    {
                                        finished = true;
                                        break;
                                    }
                                }
                                if (resp.Status.StatusCode != 0)
                                {
                                    retryCount = 100;
                                    throw new Exception(resp.Status.StatusMessage);
                                }
                            }
                            catch
                            {
                                if (retryCount > 50)
                                {
                                    throw;
                                }
                                else
                                {
                                    retryCount++;
                                    System.Threading.Thread.Sleep(4000);
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(reqRecord.UserNamesCompleted))
                        {
                            reqRecord.UserNamesCompleted = usrProf.User.UserName;
                        }
                        else
                        {
                            reqRecord.UserNamesCompleted = string.Format("{0}, {1}", reqRecord.UserNamesCompleted, usrProf.User.UserName);
                        }
                        reqRecord.UserNameBusy = "";
                        using (var db = GCEuMacroDataSupport.Instance.GetGCEuMacroDataDatabase())
                        {
                            if (IsNewRequest(db, reqRecord)) goto stopthread;
                            UpdateStatus(db, reqRecord);
                        }
                    }
                }
                finally
                {
                    lc.Close();
                }

                using (var db = GCEuMacroDataSupport.Instance.GetGCEuMacroDataDatabase())
                {
                    reqRecord.Busy = false;
                    if (IsNewRequest(db, reqRecord)) goto stopthread;
                    UpdateStatus(db, reqRecord, "Logs zijn gedownload.");
                }
            }
            catch (Exception e)
            {
                using (var db = GCEuMacroDataSupport.Instance.GetGCEuMacroDataDatabase())
                {
                    reqRecord.Busy = false;
                    if (IsNewRequest(db, reqRecord)) goto stopthread;
                    UpdateStatus(db, reqRecord, string.Format("Er is een fout opgetreden: {0}", e.Message));
                }
            }
stopthread:
            lock (_inProgressList)
            {
                _inProgressList.Remove(reqRecord);
            }
        }

        private void UpdateStatus(PetaPoco.Database db, GCEuDownloadLogsStatus reqRecord, string status = null)
        {
            if (status != null)
            {
                reqRecord.Status = status;
            }
            reqRecord.LastUpdateAt = DateTime.Now;
            db.Update("GCEuDownloadLogsStatus", "UserID", reqRecord);
        }

        private bool IsNewRequest(PetaPoco.Database db, GCEuDownloadLogsStatus reqRecord)
        {
            return db.FirstOrDefault<GCEuDownloadLogsStatus>("where UserID=@0 and Busy is null", reqRecord.UserID) != null;
        }

    }
}
