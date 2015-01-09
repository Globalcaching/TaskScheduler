using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;

namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCComTrackable")]
    public class GCComTrackable
    {
        public long ID { get; set; }
        public string Code { get; set; }
        public bool? AllowedToBeCollected { get; set; }
        public long BugTypeID { get; set; }
        public string CurrentGeocacheCode { get; set; }
        public string CurrentGoal { get; set; }
        public long? CurrentOwnerId { get; set; }
        public DateTime DateCreated { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
        public string Name { get; set; }
        public long? OriginalOwnerId { get; set; }
        public string TBTypeName { get; set; }
        public string TBTypeNameSingular { get; set; }
        public string TrackingCode { get; set; }
        public string Url { get; set; }
        public long? UserCount { get; set; }
        public long WptTypeID { get; set; }

        public static GCComTrackable From(Trackable src)
        {
            GCComTrackable result = new GCComTrackable();
            result.ID = src.Id;
            result.Code = src.Code;
            result.AllowedToBeCollected = src.AllowedToBeCollected;
            result.BugTypeID = src.BugTypeID;
            result.CurrentGeocacheCode = src.CurrentGeocacheCode;
            result.CurrentGoal = src.CurrentGoal;
            if (src.CurrentOwner != null)
            {
                result.CurrentOwnerId = src.CurrentOwner.Id;
            }
            result.DateCreated = src.DateCreated;
            result.Description = src.Description;
            result.IconUrl = src.IconUrl;
            result.Name = src.Name;
            if (src.OriginalOwner != null)
            {
                result.OriginalOwnerId = src.OriginalOwner.Id;
            }
            result.TBTypeName = src.TBTypeName;
            result.TBTypeNameSingular = src.TBTypeNameSingular;
            result.TrackingCode = src.TrackingCode;
            result.Url = src.Url;
            result.UserCount = src.UserCount;
            result.WptTypeID = src.WptTypeID;
            return result;
        }
    }
}
