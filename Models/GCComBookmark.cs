using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using www.geocaching.com.Geocaching1.Live.data;

namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCComBookmark")]
    public class GCComBookmark
    {
        public long ListID { get; set; }
        public long GCComUserID { get; set; }
        public string ListDescription { get; set; }
        public Guid ListGUID { get; set; }
        public bool ListIsArchived { get; set; }
        public bool ListIsPublic { get; set; }
        public bool ListIsShared { get; set; }
        public bool ListIsSpecial { get; set; }
        public string ListName { get; set; }
        public int ListTypeID { get; set; }
        public int NumberOfItems { get; set; }

        [PetaPoco.Ignore]
        public int? NumberOfKnownItems { get; set; } //items we also have in our database

        public static GCComBookmark From(BookmarkListEntry src, long ownerUserId)
        {
            var result = new GCComBookmark();
            result.GCComUserID = ownerUserId;
            result.ListDescription = src.ListDescription;
            result.ListGUID = src.ListGUID;
            result.ListID = src.ListID;
            result.ListIsArchived = src.ListIsArchived;
            result.ListIsPublic = src.ListIsPublic;
            result.ListIsShared = src.ListIsShared;
            result.ListIsSpecial = src.ListIsSpecial;
            result.ListName = src.ListName;
            result.ListTypeID = src.ListTypeID;
            result.NumberOfItems = src.NumberOfItems;
            return result;
        }
    }
}
