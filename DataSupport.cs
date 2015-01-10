using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;
using System.Configuration;

namespace TaskScheduler
{
    public class DataSupport
    {
        private static DataSupport _uniqueInstance = null;
        private static object _lockObject = new object();

        private DataSupport()
        {
        }

        public static DataSupport Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new DataSupport();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        public void AddTrackable(Trackable tb, List<TrackableLog> logs, TrackableTravel[] trvls)
        {
            GCComDataSupport.Instance.AddTrackable(tb, logs);
            GCEuDataSupport.Instance.AddTrackable(tb, logs, trvls);
        }

        public void AddGeocache(Geocache gc)
        {
            GCEuDataSupport.Instance.AddGeocache(gc);
            GCComDataSupport.Instance.AddGeocache(gc);
        }

        public void AddLogs(long geocacheId, GeocacheLog[] logs)
        {
            AddLogs(geocacheId, logs, false, new DateTime(2000, 1, 1));
        }
        public void AddLogs(long geocacheId, GeocacheLog[] logs, bool checkRemoved, DateTime updateOnlyAfter)
        {
            using (PetaPoco.Database db = GCComDataSupport.Instance.GetGCComDataDatabase())
            {
                GCComDataSupport.Instance.AddLogs(db, geocacheId, logs, checkRemoved, updateOnlyAfter);
            }
            //adding logs will automatically update GCEuData!
        }

        public long? GetGeocacheID(string Code)
        {
            long? result = null;
            using (PetaPoco.Database db = GCComDataSupport.Instance.GetGCComDataDatabase())
            {
                var idl = db.Fetch<long?>("select ID from GCComGeocache where Code=@0", Code);
                if (idl != null && idl.Count > 0)
                {
                    result = (long)idl[0];
                }
            }
            return result;
        }
    }
}