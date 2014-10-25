using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;
using System.Configuration;
using www.geocaching.com.Geocaching1.Live.data;
using TaskScheduler.Models;

namespace TaskScheduler
{
    public class GCComDataSupport
    {
        private static GCComDataSupport _uniqueInstance = null;
        private static object _lockObject = new object();

        public static string GeocachingDatabaseName = "GCComData";
        public static long[] FoundLogTypeIds = new long[] { 2, 10, 11 };

        private GCComDataSupport()
        {
        }

        public static GCComDataSupport Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new GCComDataSupport();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        public string GCComDataConnectionString
        {
            get
            {
                return string.Format("Data Source={0};Initial Catalog={1};Persist Security Info=True;User ID={2};Password={3};Connect Timeout=45;", Properties.Settings.Default.DatabaseServer, GeocachingDatabaseName, Properties.Settings.Default.DatabaseUser, Properties.Settings.Default.DatabasePassword);
            }
        }

        public PetaPoco.Database GetGCComDataDatabase()
        {
            return new PetaPoco.Database(GCComDataConnectionString, "System.Data.SqlClient");
        }

        public void UpdateGCComDataTypes()
        {
            try
            {
                using (PetaPoco.Database db = GetGCComDataDatabase())
                {
                    //get type info from geocaching.com and update database
                    //right now the assumption is: only add items

                    int index = 0;
                    string token = GeocachingAPI.Instance.GetServiceToken(ref index);

                    if (token.Length > 0)
                    {
                        var availableAttributes = GeocachingAPI.GetAttributeTypes(token);
                        if (availableAttributes != null)
                        {
                            var storedAttributes = db.Fetch<GCComAttributeType>("");
                            foreach (var attr in availableAttributes)
                            {
                                if (storedAttributes == null || storedAttributes.Where(x => x.ID == attr.ID).FirstOrDefault() == null)
                                {
                                    var at = GCComAttributeType.From(attr);
                                    db.Insert(at);
                                }
                            }
                        }

                        var availableLogTypes = GeocachingAPI.GetLogTypes(token);
                        if (availableLogTypes != null)
                        {
                            var storedLogTypes = db.Fetch<GCComLogType>("");
                            foreach (var attr in availableLogTypes)
                            {
                                if (storedLogTypes == null || storedLogTypes.Where(x => x.ID == attr.WptLogTypeId).FirstOrDefault() == null)
                                {
                                    var at = GCComLogType.From(attr);
                                    db.Insert(at);
                                }
                            }
                        }

                        var availableGeocacheTypes = GeocachingAPI.GetGeocacheTypes(token);
                        if (availableGeocacheTypes != null)
                        {
                            var storedGeocacheTypes = db.Fetch<GCComGeocacheType>("");
                            foreach (var attr in availableGeocacheTypes)
                            {
                                if (storedGeocacheTypes == null || storedGeocacheTypes.Where(x => x.ID == attr.GeocacheTypeId).FirstOrDefault() == null)
                                {
                                    var at = GCComGeocacheType.From(attr);
                                    db.Insert(at);
                                }
                            }
                        }

                    }
                }
            }
            catch
            {
            }
        }

        public void AddGeocache(Geocache gc)
        {
            using (PetaPoco.Database db = GetGCComDataDatabase())
            {
                //geocache itself
                var gcData = GCComGeocache.From(gc);
                if (db.Fetch<long>("SELECT ID FROM GCComGeocache WHERE ID=@0", gcData.ID).Count == 0)
                {
                    db.Insert(gcData);
                }
                else
                {
                    db.Update("GCComGeocache", "ID", gcData);
                }

                UpdateAttributes(db, gc.ID, gc.Attributes);

                if (gc.AdditionalWaypoints != null)
                {
                    AddAdditionalWaypoints(db, gc.ID, gc.AdditionalWaypoints);
                }

                if (gc.Owner != null)
                {
                    AddMember(db, gc.Owner);
                }

                if (gc.GeocacheLogs != null)
                {
                    AddLogs(db, gc.ID, gc.GeocacheLogs);
                }

                if (gc.Images != null)
                {
                    AddGeocacheImages(db, gc.ID, gc.Images);
                }
            }
        }

        private void UpdateAttributes(PetaPoco.Database db, long geocacheId, Tucson.Geocaching.WCF.API.Geocaching1.Types.Attribute[] attrs)
        {
            var curAttr = db.Fetch<GCComGeocacheAttribute>("where GeocacheID=@0", geocacheId);
            bool doUpdate = false;
            if (attrs == null)
            {
                doUpdate = curAttr != null && curAttr.Count > 0;
            }
            else if (curAttr.Count == attrs.Length)
            {
                foreach (var c in curAttr)
                {
                    if (attrs.Where(x => x.AttributeTypeID == c.AttributeTypeID && x.IsOn == c.IsOn).FirstOrDefault() == null)
                    {
                        doUpdate = true;
                        break;
                    }
                }
            }
            else
            {
                doUpdate = true;
            }
            if (doUpdate)
            {
                db.Execute("delete from GCComGeocacheAttribute where GeocacheID=@0", geocacheId);
                foreach (var a in attrs)
                {
                    var gcData = GCComGeocacheAttribute.From(geocacheId, a);
                    db.Insert(gcData);
                }
            }
        }

        public void AddMember(PetaPoco.Database db, Member mb)
        {
            var mData = GCComUser.From(mb);
            var nameList = db.Fetch<string>("SELECT UserName FROM GCComUser WHERE ID=@0", mData.ID);
            if (nameList.Count == 0)
            {
                db.Insert(mData);
            }
            else
            {
                db.Update("GCComUser", "ID", mData);
                if (nameList[0] != mData.UserName)
                {
                    //name change!
                    GCEuDataSupport.Instance.GCComMemberNameChange(mb, nameList[0]);
                }
            }
        }

        private void AddGeocacheImages(PetaPoco.Database db, long geocacheId, ImageData[] imgs)
        {
            foreach (var l in imgs)
            {
                AddGeocacheImage(db, geocacheId, l);
            }
        }
        private void AddGeocacheImage(PetaPoco.Database db, long geocacheId, ImageData img)
        {
            var gcData = GCComGeocacheImage.From(geocacheId, img);
            if (db.Fetch<string>("SELECT Url FROM GCComGeocacheImage WHERE Url=@0", gcData.Url).Count == 0)
            {
                db.Insert(gcData);
            }
            else
            {
                db.Update("GCComGeocacheImage", "Url", gcData);
            }
        }

        public void AddLogs(PetaPoco.Database db, long geocacheId, GeocacheLog[] logs)
        {
            AddLogs(db, geocacheId, logs, false, new DateTime(2000, 1, 1));
        }
        public void AddLogs(PetaPoco.Database db, long geocacheId, GeocacheLog[] logs, bool checkRemoved, DateTime updateOnlyAfter)
        {
            int foundCount = 0;
            int incFoundCount = 0;
            DateTime? publishedDate = null;
            List<long> currentLogIds = db.Fetch<long>("select ID from GCComGeocacheLog with (nolock) where GeocacheID=@0", geocacheId);
            List<string> currentLogImageIds = db.Fetch<string>("select GCComGeocacheLogImage.Url from GCComGeocacheLogImage with (nolock) inner join GCComGeocacheLog with (nolock) on GCComGeocacheLogImage.LogID = GCComGeocacheLog.ID where GCComGeocacheLog.GeocacheID=@0", geocacheId);
            foreach (var l in logs)
            {
                if (l.LogType != null)
                {
                    if (FoundLogTypeIds.Contains(l.LogType.WptLogTypeId))
                    {
                        foundCount++;
                    }
                    else if (l.LogType.WptLogTypeId == 24)
                    {
                        publishedDate = l.VisitDate;
                    }
                }
                if (l.UTCCreateDate >= updateOnlyAfter || l.VisitDate >= updateOnlyAfter)
                {
                    if (AddLog(db, geocacheId, l, currentLogIds, currentLogImageIds))
                    {
                        incFoundCount++;
                    }
                }
                else if (l.Finder != null) //still update member
                {
                    AddMember(db, l.Finder);
                }
            }
            int favPoints = 0;
            int logImgCount = 0;
            var fpl = db.Fetch<int?>("SELECT FavoritePoints FROM GCComGeocache WHERE ID=@0", geocacheId);
            if (fpl != null && fpl.Count > 0)
            {
                favPoints = fpl[0] ?? 0;
            }
            logImgCount = currentLogImageIds.Count;
            if (checkRemoved)
            {
                var ids = db.Fetch<long>("SELECT ID FROM GCComGeocacheLog WHERE GeocacheID=@0", geocacheId);
                foreach (long l in ids)
                {
                    if (logs.Where(x => x.ID == l).Count() == 0)
                    {
                        db.Execute("delete from GCComGeocacheLog where ID=@0", l);
                    }
                }
                GCEuDataSupport.Instance.SetFoundCountForGeocache(geocacheId, favPoints, logImgCount, foundCount, publishedDate);
            }
            else
            {
                if (incFoundCount > 0)
                {
                    GCEuDataSupport.Instance.AddFoundCountForGeocache(geocacheId, favPoints, logImgCount, incFoundCount);
                }
            }
        }

        private bool AddLog(PetaPoco.Database db, long geocacheId, GeocacheLog log, List<long> currentLogIds, List<string> currentLogImageIds)
        {
            bool result = false;
            var gcData = GCComGeocacheLog.From(geocacheId, log);
            bool logIsNew;
            if (currentLogIds != null)
            {
                logIsNew = !currentLogIds.Contains(gcData.ID);
            }
            else
            {
                logIsNew = db.Fetch<long>("SELECT ID FROM GCComGeocacheLog WHERE ID=@0", gcData.ID).Count == 0;
            }
            if (logIsNew)
            {
                db.Insert(gcData);
                if (log.LogType != null)
                {
                    if (FoundLogTypeIds.Contains(log.LogType.WptLogTypeId))
                    {
                        result = true;
                    }
                }
            }
            else
            {
                db.Update("GCComGeocacheLog", "ID", gcData);
            }
            if (log.Images != null)
            {
                AddGeocacheLogImages(db, gcData.ID, log.Images, currentLogImageIds);
            }
            if (log.Finder != null)
            {
                AddMember(db, log.Finder);
            }
            return result;
        }

        private void AddGeocacheLogImages(PetaPoco.Database db, long logId, ImageData[] imgs, List<string> currentLogImageIds)
        {
            foreach (var l in imgs)
            {
                AddGeocacheLogImage(db, logId, l, currentLogImageIds);
            }
        }
        private void AddGeocacheLogImage(PetaPoco.Database db, long logId, ImageData img, List<string> currentLogImageIds)
        {
            var gcData = GCComGeocacheLogImage.From(logId, img);
            bool isNewImage;
            if (currentLogImageIds != null)
            {
                isNewImage = !currentLogImageIds.Contains(gcData.Url);
            }
            else
            {
                isNewImage = db.Fetch<string>("SELECT Url FROM GCComGeocacheLogImage WHERE Url=@0", gcData.Url).Count == 0;
            }
            if (isNewImage)
            {
                db.Insert(gcData);
            }
            else
            {
                db.Update("GCComGeocacheLogImage", "Url", gcData);
            }
        }

        private void AddAdditionalWaypoints(PetaPoco.Database db, long geocacheId, AdditionalWaypoint[] wpts)
        {
            foreach (var l in wpts)
            {
                AddAdditionalWaypoint(db, geocacheId, l);
            }
        }

        private void AddAdditionalWaypoint(PetaPoco.Database db, long geocacheId, AdditionalWaypoint wp)
        {
            var gcData = GCComDataAdditionalWaypoints.From(geocacheId, wp);
            if (db.Fetch<string>("SELECT Code FROM GCComDataAdditionalWaypoints WHERE Code=@0", gcData.Code).Count == 0)
            {
                db.Insert(gcData);
            }
            else
            {
                db.Update("GCComDataAdditionalWaypoints", "Code", gcData);
            }
        }

    }
}