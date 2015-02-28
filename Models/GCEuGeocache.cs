using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;

namespace TaskScheduler.Models
{

    [PetaPoco.TableName("GCEuGeocache")]
    public class GCEuGeocache
    {
        public long ID { get; set; }
        public long? FTFUserID { get; set; }
        public long? STFUserID { get; set; }
        public long? TTFUserID { get; set; }
        public string Municipality { get; set; }
        public string City { get; set; }
        public int FoundCount { get; set; }
        public double? Distance { get; set; }
        public bool DistanceChecked { get; set; }
        public bool FTFCompleted { get; set; }
        public DateTime? FTFAtDate { get; set; }
        public DateTime? STFAtDate { get; set; }
        public DateTime? TTFAtDate { get; set; }
        public DateTime? PublishedAtDate { get; set; }
        public double? FavPer100Found { get; set; }
        public int? LogImageCount { get; set; }
        public double? LogImagePer100Found { get; set; }
        public int FTFFoundCount { get; set; }
        public DateTime? MostRecentFoundDate { get; set; }
        public DateTime? MostRecentArchivedDate { get; set; }
        public int PMFoundCount { get; set; }
        
        public static GCEuGeocache From(Geocache src)
        {
            //default values
            GCEuGeocache result = new GCEuGeocache();
            result.ID = src.ID;
            result.FTFUserID = null;
            result.STFUserID = null;
            result.TTFUserID = null;
            result.Municipality = null;
            result.City = null;
            result.FoundCount = 0;
            result.PMFoundCount = 0;
            result.LogImageCount = 0;
            result.Distance = null;
            result.DistanceChecked = false;
            result.FTFCompleted = false;
            result.FTFAtDate = null;
            result.STFAtDate = null;
            result.TTFAtDate = null;
            result.PublishedAtDate = null;
            result.FavPer100Found = null;
            result.LogImagePer100Found = null;
            result.FTFFoundCount = 0;
            result.MostRecentFoundDate = null;
            result.MostRecentArchivedDate = null;
            return result;
        }
    }
}