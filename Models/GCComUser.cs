using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tucson.Geocaching.WCF.API.Geocaching1.Types;


namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCComUser")]
    public class GCComUser
    {
        public long ID { get; set; }
        public string AvatarUrl { get; set; }
        public int? FindCount { get; set; }
        public int? GalleryImageCount { get; set; }
        public int? HideCount { get; set; }
        public long MemberTypeId { get; set; }
        public Guid PublicGuid { get; set; }
        public string UserName { get; set; }

        public static GCComUser From(Member src)
        {
            GCComUser result = new GCComUser();
            result.ID = src.Id == null ? 0 : (long)src.Id;
            result.AvatarUrl = src.AvatarUrl;
            result.FindCount = src.FindCount;
            result.GalleryImageCount = src.GalleryImageCount;
            result.HideCount = src.HideCount;
            result.MemberTypeId = src.MemberType == null ? 1 : src.MemberType.MemberTypeId;
            result.PublicGuid = src.PublicGuid;
            result.UserName = src.UserName;
            return result;
        }
    }
}