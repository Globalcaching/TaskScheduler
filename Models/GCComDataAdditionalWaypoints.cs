using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using www.geocaching.com.Geocaching1.Live.data;

namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCComDataAdditionalWaypoints")]
    public class GCComDataAdditionalWaypoints
    {
        public long GeocacheID { get; set; }
        public string Code { get; set; }
        public string Comment { get; set; }
        public string Description { get; set; }
        public Guid GUID { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        public string UrlName { get; set; }
        public DateTime UTCEnteredDate { get; set; }
        public int WptTypeID { get; set; }

        public static GCComDataAdditionalWaypoints From(long geocacheId, AdditionalWaypoint src)
        {
            GCComDataAdditionalWaypoints result = new GCComDataAdditionalWaypoints();
            result.GeocacheID = geocacheId;
            result.Code = src.Code;
            result.Comment = src.Comment;
            result.Description = src.Description;
            result.GUID = src.GUID;
            result.Latitude = src.Latitude;
            result.Longitude = src.Longitude;
            result.Name = src.Name;
            result.Type = src.Type;
            result.Url = src.Url;
            result.UrlName = src.UrlName;
            result.UTCEnteredDate = src.UTCEnteredDate;
            result.WptTypeID = src.WptTypeID;
            return result;
        }
    }
}