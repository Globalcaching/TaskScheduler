using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;

namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCComTrackableLog")]
    public class GCComTrackableLog
    {
        public int ID { get; set; }
        public long TrackableID { get; set; }
        public string Code { get; set; }
        public int? CacheID { get; set; }
        public long? LoggedBy { get; set; }
        public Guid LogGuid { get; set; }
        public bool LogIsEncoded { get; set; }
        public string LogText { get; set; }
        public long? WptLogTypeId { get; set; }
        public double? UpdatedLatitude { get; set; }
        public double? UpdatedLongitude { get; set; }
        public string Url { get; set; }
        public DateTime UTCCreateDate { get; set; }
        public DateTime VisitDate { get; set; }

        public static GCComTrackableLog From(TrackableLog src, long trackableId)
        {
            GCComTrackableLog result = new GCComTrackableLog();
            result.ID = src.ID;
            result.CacheID = src.CacheID;
            result.Code = src.Code;
            result.TrackableID = trackableId;
            if (src.LoggedBy != null)
            {
                result.LoggedBy = src.LoggedBy.Id;
            }
            result.LogGuid = src.LogGuid;
            result.LogIsEncoded = src.LogIsEncoded;
            result.LogText = src.LogText;
            if (src.LogType != null)
            {
                result.WptLogTypeId = src.LogType.WptLogTypeId;
            }
            result.UpdatedLatitude = src.UpdatedLatitude;
            result.UpdatedLongitude = src.UpdatedLongitude;
            result.Url = src.Url;
            result.UTCCreateDate = src.UTCCreateDate;
            result.VisitDate = src.VisitDate;
            return result;
        }
    }
}
