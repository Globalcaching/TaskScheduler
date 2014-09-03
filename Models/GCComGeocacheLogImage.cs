using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;

namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCComGeocacheLogImage")]
    public class GCComGeocacheLogImage
    {
        public long LogID { get; set; }
        public string Description { get; set; }
        public Guid Guid { get; set; }
        public string MobileUrl { get; set; }
        public string Name { get; set; }
        public string ThumbUrl { get; set; }
        public string Url { get; set; }

        public static GCComGeocacheLogImage From(long logId, Tucson.Geocaching.WCF.API.Geocaching1.Types.ImageData src)
        {
            GCComGeocacheLogImage result = new GCComGeocacheLogImage();
            result.LogID = logId;
            result.Description = src.Description;
            result.Guid = src.ImageGuid;
            result.MobileUrl = src.MobileUrl;
            result.Name = src.Name;
            result.ThumbUrl = src.ThumbUrl;
            result.Url = src.Url;
            return result;
        }
    }
}