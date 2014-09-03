using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;

namespace TaskScheduler.Models
{

    [PetaPoco.TableName("GCComAttributeType")]
    public class GCComAttributeType
    {
        public int ID { get; set; }
        public int CategoryID { get; set; }
        public string Description { get; set; }
        public bool HasNoOption { get; set; }
        public bool HasYesOption { get; set; }
        public string IconName { get; set; }
        public string Name { get; set; }
        public string NoIconName { get; set; }
        public string NotChosenIconName { get; set; }
        public string YesIconName { get; set; }

        public static GCComAttributeType From(AttributeType src)
        {
            GCComAttributeType result = new GCComAttributeType();
            result.ID = src.ID;
            result.CategoryID = src.CategoryID;
            result.Description = src.Description;
            result.HasNoOption = src.HasNoOption;
            result.HasYesOption = src.HasYesOption;
            result.IconName = src.IconName;
            result.Name = src.Name;
            result.NoIconName = src.NoIconName;
            result.NotChosenIconName = src.NotChosenIconName;
            result.YesIconName = src.YesIconName;
            return result;
        }
    }
}