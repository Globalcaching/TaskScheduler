using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;

namespace TaskScheduler.Models
{

    [PetaPoco.TableName("GCComLogType")]
    public class GCComLogType
    {
        public long ID { get; set; }
        public bool AdminActionable { get; set; }
        public string ImageName { get; set; }
        public string ImageURL { get; set; }
        public bool OwnerActionable { get; set; }
        public string WptLogTypeName { get; set; }

        public static GCComLogType From(WptLogType src)
        {
            GCComLogType result = new GCComLogType();
            result.ID = src.WptLogTypeId;
            result.AdminActionable = src.AdminActionable;
            result.ImageName = src.ImageName;
            result.ImageURL = src.ImageURL;
            result.OwnerActionable = src.OwnerActionable;
            result.WptLogTypeName = src.WptLogTypeName;
            return result;
        }
    }
}