using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;

namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCComGeocacheImage")]
    public class GCComGeocacheImage
    {
        public long GeocacheID { get; set; }
        public string Description { get; set; }
        public Guid Guid { get; set; }
        public string MobileUrl { get; set; }
        public string Name { get; set; }
        public string ThumbUrl { get; set; }
        public string Url { get; set; }

        public static GCComGeocacheImage From(long geocacheId, Tucson.Geocaching.WCF.API.Geocaching1.Types.ImageData src)
        {
            GCComGeocacheImage result = new GCComGeocacheImage();
            result.GeocacheID = geocacheId;
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