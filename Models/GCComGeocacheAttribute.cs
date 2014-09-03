using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;

namespace TaskScheduler.Models
{

    [PetaPoco.TableName("GCComGeocacheAttribute")]
    public class GCComGeocacheAttribute
    {
        public long GeocacheID { get; set; }
        public int AttributeTypeID { get; set; }
        public bool IsOn { get; set; }

        public static GCComGeocacheAttribute From(long geocacheId, Tucson.Geocaching.WCF.API.Geocaching1.Types.Attribute src)
        {
            GCComGeocacheAttribute result = new GCComGeocacheAttribute();
            result.GeocacheID = geocacheId;
            result.AttributeTypeID = src.AttributeTypeID;
            result.IsOn = src.IsOn;
            return result;
        }
    }
}