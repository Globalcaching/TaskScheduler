using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;

namespace TaskScheduler.Models
{

    [PetaPoco.TableName("GCComGeocacheLog")]
    public class GCComGeocacheLog
    {
        public long ID { get; set; }
        public long GeocacheID { get; set; }
        public string CacheCode { get; set; }
        public string Code { get; set; }
        public long? FinderId { get; set; }
        public Guid Guid { get; set; }
        public bool LogIsEncoded { get; set; }
        public string LogText { get; set; }
        public long WptLogTypeId { get; set; }
        public double? UpdatedLatitude { get; set; }
        public double? UpdatedLongitude { get; set; }
        public string Url { get; set; }
        public DateTime UTCCreateDate { get; set; }
        public DateTime VisitDate { get; set; }

        public static GCComGeocacheLog From(long geocacheId, GeocacheLog src)
        {
            GCComGeocacheLog result = new GCComGeocacheLog();
            result.ID = src.ID;
            result.GeocacheID = geocacheId;
            result.CacheCode = src.CacheCode;
            result.Code = src.Code;
            result.FinderId = src.Finder == null ? null : src.Finder.Id;
            result.Guid = src.Guid;
            result.LogIsEncoded = src.LogIsEncoded;
            result.LogText = src.LogText;
            result.WptLogTypeId = src.LogType == null ? 0 : src.LogType.WptLogTypeId;
            result.UpdatedLatitude = src.UpdatedLatitude;
            result.UpdatedLongitude = src.UpdatedLongitude;
            result.Url = src.Url;
            result.UTCCreateDate = src.UTCCreateDate;
            result.VisitDate = src.VisitDate;
            return result;
        }
    }

}