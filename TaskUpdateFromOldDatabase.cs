using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Models;

namespace TaskScheduler
{
    public class TaskUpdateFromOldDatabase: TaskBase
    {
        private int _wpCount;

        public TaskUpdateFromOldDatabase(Manager taskManager) :
            base(taskManager, typeof(TaskUpdateFromOldDatabase), "Update from old database", 0, 0, 20)
        {
            _wpCount = 0;
            Details = _wpCount.ToString();
        }

        protected override void ServiceMethod()
        {
            try
            {
                List<long> parelCaches = new List<long>();
                ///CCC users
                List<GCEuCCCUser> cccUsers = new List<GCEuCCCUser>();
                using (var dbcon = new DBCon())
                using (var dbcon2 = new DBCon())
                {
                    var dr = dbcon.ExecuteReader("select GCComGeocache.ID from GCComData.dbo.GCComGeocache inner join ParelVanDeMaand on GCComGeocache.Code COLLATE DATABASE_DEFAULT = ParelVanDeMaand.Waypoint COLLATE DATABASE_DEFAULT");
                    while (dr.Read())
                    {
                        parelCaches.Add(dr.GetInt64(0));
                    }
                    foreach (var id in parelCaches)
                    {
                        if ((int)dbcon.ExecuteScalar(string.Format("select count(1) from GCEuData.dbo.GCEuParel where GeocacheID={0}", id)) == 0)
                        {
                            dbcon.ExecuteNonQuery(string.Format("insert into GCEuData.dbo.GCEuParel (GeocacheID) values ({0})", id));
                        }
                    }

                    dr = dbcon.ExecuteReader("select * from CCCUsers with (nolock)");
                    while (dr.Read())
                    {
                        GCEuCCCUser usr = new GCEuCCCUser();
                        usr.Active = (bool)dr["Active"];
                        usr.Comment = (string)dr["Comment"];
                        usr.HideEmailAddress = (bool)dr["HideEmailAddress"];
                        usr.ModifiedAt = DateTime.Parse(dr["ModifiedAt"] as string);
                        usr.PreferSMS = (bool)dr["PreferSMS"];
                        usr.RegisteredAt = DateTime.Parse(dr["RegisteredAt"] as string);
                        usr.SMS = (bool)dr["SMS"];
                        usr.Telnr = (string)dr["Telnr"];
                        usr.TwitterUsername = (string)dr["TwitterUsername"];
                        usr.UserID = (int)dr["UserID"];
                        usr.UsersHelped = (int)dr["UsersHelped"];

                        object o = dbcon2.ExecuteScalar(string.Format("select top 1 UserID from GCCOMUsers where Username='{0}'", dr["gccomName"].ToString().Replace("'", "''")));
                        if (o != null)
                        {
                            usr.GCComUserID = (int)o;
                            cccUsers.Add(usr);
                        }
                    }
                }
                if (_stop) return;
                using (var db = GCEuDataSupport.Instance.GetGCEuDataDatabase())
                {
                    List<GCEuCCCUser> updateCCCUsers = db.Fetch<GCEuCCCUser>("");
                    foreach (var cusr in cccUsers)
                    {
                        var updUsr = updateCCCUsers.FirstOrDefault(x => x.UserID == cusr.UserID);
                        if (updUsr == null)
                        {
                            db.Insert(cusr);
                        }
                        else
                        {
                            db.Update("GCEuCCCUser", "UserID", cusr);
                            updateCCCUsers.Remove(updUsr);
                        }
                    }

                    foreach (var cusr in updateCCCUsers)
                    {
                        db.Execute("delete from GCEuCCCUser where UserID=@0", cusr.UserID);
                    }

                    //coord check
                    db.Execute("insert into GCEuData.dbo.GCEuCoordCheckCode (Code, UserID, Lat, Lon, NotifyOnFailure, NotifyOnSuccess, Radius ) select CoordCheckWaypoints.Waypoint as Code, CoordCheckWaypoints.UserID, CoordCheckWaypoints.Lat, CoordCheckWaypoints.Lon, CoordCheckWaypoints.NotifyOnFailure, CoordCheckWaypoints.NotifyOnSuccess, CoordCheckWaypoints.Radius from globalcaching.dbo.CoordCheckWaypoints left join GCEuData.dbo.GCEuCoordCheckCode on CoordCheckWaypoints.Waypoint COLLATE DATABASE_DEFAULT = GCEuCoordCheckCode.Code COLLATE DATABASE_DEFAULT where GCEuCoordCheckCode.Code is null");
                    db.Execute("truncate table GCEuData.dbo.GCEuCoordCheckAttempt");
                    db.Execute("insert into GCEuData.dbo.GCEuCoordCheckAttempt (Waypoint, Lat, Lon, VisitorID, AttemptAt ) select CoordCheckAttempts.Waypoint, CoordCheckAttempts.Lat, CoordCheckAttempts.Lon, CoordCheckAttempts.VisitorID, Convert(datetime, substring(CoordCheckAttempts.AttemptAt,1,19), 20) as AttemptAt from globalcaching.dbo.CoordCheckAttempts left join GCEuData.dbo.GCEuCoordCheckAttempt on CoordCheckAttempts.Waypoint COLLATE DATABASE_DEFAULT = GCEuCoordCheckAttempt.Waypoint COLLATE DATABASE_DEFAULT and Convert(datetime, substring(CoordCheckAttempts.AttemptAt,1,19), 20) = GCEuCoordCheckAttempt.AttemptAt  where GCEuCoordCheckAttempt.Waypoint is null");

                    //code check
                    db.Execute("truncate table GCEuData.dbo.GCEuCodeCheckCode");
                    db.Execute("truncate table GCEuData.dbo.GCEuCodeCheckAttempt");
                    var ccList = db.Fetch<GCEuCodeCheckCode>("select * from Globalcaching.dbo.CodeCheckCode");
                    foreach (var cc in ccList)
                    {
                        int orgID = cc.ID;
                        cc.ID = 0;
                        db.Insert(cc);
                        db.Execute(string.Format("insert into GCEuData.dbo.GCEuCodeCheckAttempt (CodeID, AttemptAt, VisitorID, Answer, GroundspeakUserName) select CodeID = {0}, AttemptAt, VisitorID, Answer, GroundspeakUserName from Globalcaching.dbo.CodeCheckAttempt where CodeID=@0", cc.ID), orgID);
                        /*
                        var attempts = db.Fetch<GCEuCodeCheckAttempt>("select * from Globalcaching.dbo.CodeCheckAttempt where CodeID=@0", orgID);
                        foreach (var att in attempts)
                        {
                            att.CodeID = cc.ID;
                            db.Insert(att);
                        }
                         * */
                    }
                }


                //geocaches, FTF, City atec.
                using (var db = GCComDataSupport.Instance.GetGCComDataDatabase())
                {
                    List<GCEuGeocache> gcEUCaches = db.SkipTake<GCEuGeocache>(0, 30000,
                        PetaPoco.Sql.Builder.Select("GCEuGeocache.*")
                        .From("GCComGeocache")
                        .InnerJoin(string.Format("[{0}].[dbo].[GCEuGeocache]", GCEuDataSupport.GlobalcachingDatabaseName)).On("GCComGeocache.ID = GCEuGeocache.ID")
                        .Where("CountryID=141")
                        .Append("AND (Municipality is NULL OR City is NULL OR DistanceChecked=0 OR FTFCompleted=0)")
                        );

                    if (gcEUCaches.Count > 0)
                    {
                        using (var dbcon = new DBCon())
                        {
                            foreach (var gc in gcEUCaches)
                            {
                                if (_stop)
                                {
                                    break;
                                }

                                string wp = db.ExecuteScalar<string>("select Code from GCComGeocache where ID=@0", gc.ID);
                                var dr = dbcon.ExecuteReader(string.Format("select City, County, FTFUsername, STFUsername, TTFUsername, Afstand, AfstandChecked from Caches where Waypoint='{0}'", wp));
                                if (dr.Read())
                                {
                                    gc.City = dr["City"] as string ?? "";
                                    gc.Municipality = dr["County"] as string ?? "";

                                    bool? distChecked = dr["AfstandChecked"] == DBNull.Value ? null : (bool?)dr["AfstandChecked"];
                                    double? dist = dr["Afstand"] == DBNull.Value ? null : (double?)dr["Afstand"];
                                    gc.DistanceChecked = (distChecked == true || dist != null);
                                    if (dist != null)
                                    {
                                        gc.Distance = (double)dist;
                                    }

                                    string usrn1 = dr["FTFUsername"] as string;
                                    string usrn2 = dr["STFUsername"] as string;
                                    string usrn3 = dr["TTFUsername"] as string;
                                    if (gc.FTFUserID == null && !string.IsNullOrEmpty(usrn1))
                                    {
                                        dr = dbcon.ExecuteReader(string.Format("select top 1 * from UserLogs where Username='{0}' and Waypoint='{1}' and found=1 order by Logdate", usrn1, wp));
                                        if (dr.Read())
                                        {
                                            gc.FTFUserID = (int)dr["GCComUserID"];
                                            gc.FTFAtDate = DateTime.Parse(dr["Logdate"] as string);
                                        }
                                    }
                                    if (gc.STFUserID == null && !string.IsNullOrEmpty(usrn2))
                                    {
                                        dr = dbcon.ExecuteReader(string.Format("select top 1 * from UserLogs where Username='{0}' and Waypoint='{1}' and found=1 order by Logdate", usrn2, wp));
                                        if (dr.Read())
                                        {
                                            gc.STFUserID = (int)dr["GCComUserID"];
                                            gc.STFAtDate = DateTime.Parse(dr["Logdate"] as string);
                                        }
                                    }
                                    if (gc.TTFUserID == null && !string.IsNullOrEmpty(usrn3))
                                    {
                                        dr = dbcon.ExecuteReader(string.Format("select top 1 * from UserLogs where Username='{0}' and Waypoint='{1}' and found=1 order by Logdate", usrn3, wp));
                                        if (dr.Read())
                                        {
                                            gc.TTFUserID = (int)dr["GCComUserID"];
                                            gc.TTFAtDate = DateTime.Parse(dr["Logdate"] as string);
                                        }
                                    }
                                    gc.FTFCompleted = (usrn1 != null && usrn2 != null && usrn3!=null);

                                    db.Update(string.Format("[{0}].[dbo].[GCEuGeocache]", GCEuDataSupport.GlobalcachingDatabaseName), "ID", gc);

                                    _wpCount++;
                                    Details = _wpCount.ToString();
                                }
                            }
                        }
                    }
                    else
                    {
                        Details = "Up to date!";
                    }
                    ServiceInfo.ErrorInLastRun = false;
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
