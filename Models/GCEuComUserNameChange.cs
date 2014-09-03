using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;

namespace TaskScheduler.Models
{

    [PetaPoco.TableName("GCEuComUserNameChange")]
    public class GCEuComUserNameChange
    {
        public long ID { get; set; }
        public string OldName { get; set; }
        public string NewName { get; set; }
        public DateTime DetectedAt { get; set; }

        public static GCEuComUserNameChange From(string oldName, Member src)
        {
            //default values
            GCEuComUserNameChange result = new GCEuComUserNameChange();
            result.ID = src.Id == null ? 0 : (long)src.Id;
            result.OldName = oldName ?? "";
            result.NewName = src.UserName ?? "";
            result.DetectedAt = DateTime.Now;
            return result;
        }
    }
}