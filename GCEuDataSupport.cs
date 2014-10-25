using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;
using System.Configuration;
using TaskScheduler.Models;

namespace TaskScheduler
{
    public class GCEuDataSupport
    {
        private static GCEuDataSupport _uniqueInstance = null;
        private static object _lockObject = new object();

        public static string GlobalcachingDatabaseName = "GCEuData";

        private GCEuDataSupport()
        {
            //todo:
            //- get GlobalcachingDatabaseName from connection string
        }

        public static GCEuDataSupport Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new GCEuDataSupport();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        public string GCEuDataConnectionString
        {
            get
            {
                return string.Format("Data Source={0};Initial Catalog={1};Persist Security Info=True;User ID={2};Password={3};Connect Timeout=45;", Properties.Settings.Default.DatabaseServer, GlobalcachingDatabaseName, Properties.Settings.Default.DatabaseUser, Properties.Settings.Default.DatabasePassword);
            }
        }

        public PetaPoco.Database GetGCEuDataDatabase()
        {
            return new PetaPoco.Database(GCEuDataConnectionString, "System.Data.SqlClient");
        }

        public void AddGeocache(Geocache gc)
        {
            using (PetaPoco.Database db = GetGCEuDataDatabase())
            {
                if (db.Fetch<long>("SELECT ID FROM GCEuGeocache WHERE ID=@0", gc.ID).Count == 0)
                {
                    var gcData = GCEuGeocache.From(gc);
                    gcData.FoundCount = 0;
                    if (gc.Latitude!=null && gc.Longitude!=null)
                    {
                        gcData.City = Helper.GetCityName((double)gc.Latitude, (double)gc.Longitude);
                        gcData.Municipality = Helper.PointInMunicipality((double)gc.Latitude, (double)gc.Longitude);
                    }
                    gcData.PublishedAtDate = gc.UTCPlaceDate;
                    db.Insert(gcData);
                }
            }
        }

        public void SetFoundCountForGeocache(long geocacheId, int FavoriteCount, int LogImageCount, int incrementValue)
        {
            SetFoundCountForGeocache(geocacheId, FavoriteCount, LogImageCount, incrementValue, null);
        }
        public void SetFoundCountForGeocache(long geocacheId, int FavoriteCount, int LogImageCount, int incrementValue, DateTime? publishedDate)
        {
            using (PetaPoco.Database db = GetGCEuDataDatabase())
            {
                if (publishedDate == null)
                {
                    db.Execute("update GCEuGeocache set FoundCount = @0, LogImageCount = @1 where ID=@2", incrementValue, LogImageCount, geocacheId);
                }
                else
                {
                    db.Execute("update GCEuGeocache set FoundCount = @0, LogImageCount = @1, PublishedAtDate=@2 where ID=@3", incrementValue, LogImageCount, (DateTime)publishedDate, geocacheId);
                }
                db.Execute("update GCEuGeocache set FavPer100Found = CASE WHEN FoundCount=0 THEN 0 ELSE 100*CONVERT(FLOAT,@0)/CONVERT(FLOAT,FoundCount) END, LogImagePer100Found = CASE WHEN FoundCount=0 THEN 0 ELSE 100*CONVERT(FLOAT,@1)/CONVERT(FLOAT,FoundCount) END where ID=@2", FavoriteCount, LogImageCount, geocacheId);
            }
        }

        public void AddFoundCountForGeocache(long geocacheId, int FavoriteCount, int LogImageCount, int incrementValue)
        {
            using (PetaPoco.Database db = GetGCEuDataDatabase())
            {
                db.Execute("update GCEuGeocache set FoundCount = FoundCount + @0 where ID=@1", incrementValue, geocacheId);
                db.Execute("update GCEuGeocache set FavPer100Found = CASE WHEN FoundCount=0 THEN 0 ELSE 100*CONVERT(FLOAT,@0)/CONVERT(FLOAT,FoundCount) END, LogImagePer100Found = CASE WHEN FoundCount=0 THEN 0 ELSE 100*CONVERT(FLOAT,@1)/CONVERT(FLOAT,FoundCount) END where ID=@2", FavoriteCount, LogImageCount, geocacheId);
            }
        }

        public void GCComMemberNameChange(Member mb, string oldName)
        {
            using (PetaPoco.Database db = GetGCEuDataDatabase())
            {
                var gcData = GCEuComUserNameChange.From(oldName, mb);
                db.Insert(gcData);
            }
        }
    }
}