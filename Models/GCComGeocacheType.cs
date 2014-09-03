using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;


namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCComGeocacheType")]
    public class GCComGeocacheType
    {
        public long ID { get; set; }
        public string Description { get; set; }
        public string GeocacheTypeName { get; set; }
        public string ImageURL { get; set; }
        public bool IsContainer { get; set; }

        public static GCComGeocacheType From(GeocacheType src)
        {
            GCComGeocacheType result = new GCComGeocacheType();
            result.ID = src.GeocacheTypeId;
            result.Description = src.Description;
            result.GeocacheTypeName = src.GeocacheTypeName;
            result.ImageURL = src.ImageURL;
            result.IsContainer = src.IsContainer;
            return result;
        }
    }
}