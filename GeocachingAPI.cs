﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using www.geocaching.com.Geocaching1.Live.data;

namespace TaskScheduler
{
    public class GeocachingAPI
    {
        private static GeocachingAPI _uniqueInstance = null;
        private static object _singletonObject = new object();

        public List<GcComAccounts> GCComPMAccounts;

        public GeocachingAPI()
        {
            using (var db = new PetaPoco.Database(Manager.SchedulerConnectionString, "System.Data.SqlClient"))
            {
                GCComPMAccounts = db.Fetch<GcComAccounts>("where Enabled=1");
            }
        }

        public static GeocachingAPI Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_singletonObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new GeocachingAPI();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        public string GetServiceToken(ref int index)
        {
            string result = "";
            for (int i = 0; i < GCComPMAccounts.Count; i++)
            {
                index++;
                if (index >= GCComPMAccounts.Count) index = 0;
                result = GCComPMAccounts[index].Token;
                if (!string.IsNullOrEmpty(result))
                {
                    break;
                }
            }
            return result;
        }

        public static LiveClient GetLiveClient()
        {
            BinaryMessageEncodingBindingElement binaryMessageEncoding = new BinaryMessageEncodingBindingElement()
            {
                ReaderQuotas = new XmlDictionaryReaderQuotas()
                {
                    MaxStringContentLength = int.MaxValue,
                    MaxBytesPerRead = int.MaxValue,
                    MaxDepth = int.MaxValue,
                    MaxArrayLength = int.MaxValue
                }
            };

            HttpTransportBindingElement httpTransport = new HttpsTransportBindingElement()
            {
                MaxBufferSize = int.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                AllowCookies = false,
            };

            // add the binding elements into a Custom Binding
            CustomBinding binding = new CustomBinding(binaryMessageEncoding, httpTransport);

            EndpointAddress endPoint;
            endPoint = new EndpointAddress("https://api.groundspeak.com/LiveV6/Geocaching.svc/Silverlightsoap");

            return new LiveClient(binding, endPoint);
        }

        public static Tucson.Geocaching.WCF.API.Geocaching1.Types.AttributeType[] GetAttributeTypes(string token)
        {
            Tucson.Geocaching.WCF.API.Geocaching1.Types.AttributeType[] result = null;
            LiveClient lc = GetLiveClient();
            try
            {
                GetAttributeTypesDataResponse resp = lc.GetAttributeTypesData(token);
                if (resp.Status.StatusCode == 0)
                {
                    result = resp.AttributeTypes;
                }
            }
            catch
            {
            }
            lc.Close();
            return result;
        }

        public static Tucson.Geocaching.WCF.API.Geocaching1.Types.TrackableTravel[] GetTrackableTravel(string token, string tb)
        {
            Tucson.Geocaching.WCF.API.Geocaching1.Types.TrackableTravel[] result = null;

            LiveClient lc = GetLiveClient();
            try
            {
                GetTrackableTravelResponse resp = lc.GetTrackableTravelList(token, tb);
                if (resp.Status.StatusCode == 0)
                {
                    result = resp.TrackableTravels;
                }
            }
            catch
            {
            }
            lc.Close();
            return result;
        }

        public static Tucson.Geocaching.WCF.API.Geocaching1.Types.Trackable GetTrackable(string token, string tb)
        {
            Tucson.Geocaching.WCF.API.Geocaching1.Types.Trackable result = null;

            LiveClient lc = GetLiveClient();
            try
            {
                GetTrackableResponse resp = lc.GetTrackablesByTBCode(token, tb, 0);
                if (resp.Status.StatusCode == 0)
                {
                    if (resp.Trackables.Count() > 0)
                    {
                        result = resp.Trackables[0];
                    }
                    else
                    {
                        GCEuDataSupport.Instance.DeleteTrackable(tb);
                    }
                }
            }
            catch
            {
            }
            lc.Close();
            return result;
        }


        public static List<Tucson.Geocaching.WCF.API.Geocaching1.Types.TrackableLog> GetTrackableLogs(string token, string tb)
        {
            int pageSize = 30;
            int callDelay = 2100;

            long prevCollAt;
            long nextCollAt;

            List<Tucson.Geocaching.WCF.API.Geocaching1.Types.TrackableLog> result = new List<Tucson.Geocaching.WCF.API.Geocaching1.Types.TrackableLog>();
            LiveClient lc = GetLiveClient();
            try
            {
                prevCollAt = Environment.TickCount;
                GetTrackableLogsResponse glr = lc.GetTrackableLogsByTBCode(token, tb, result.Count, pageSize);
                while (glr.Status.StatusCode == 0 && glr.TrackableLogs.Count() > 0)
                {
                    int startIndex = result.Count;
                    foreach (Tucson.Geocaching.WCF.API.Geocaching1.Types.TrackableLog l in glr.TrackableLogs)
                    {
                        result.Add(l);
                    }
                    if (glr.TrackableLogs.Count() < pageSize)
                    {
                        break;
                    }
                    else
                    {
                        nextCollAt = prevCollAt + callDelay;
                        int delay = (int)(nextCollAt - Environment.TickCount);
                        if (delay > 0 && delay <= callDelay)
                        {
                            System.Threading.Thread.Sleep(delay); //max 30 per minute
                        }
                        prevCollAt = Environment.TickCount;
                        glr = lc.GetTrackableLogsByTBCode(token, tb, result.Count, pageSize);
                    }
                }
                if (glr.Status.StatusCode != 0)
                {
                    result = null;
                }
            }
            catch
            {
                result = null;
            }
            lc.Close();
            return result;
        }


        public static Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheType[] GetGeocacheTypes(string token)
        {
            Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheType[] result = null;
            LiveClient lc = GetLiveClient();
            try
            {
                GetGeocacheTypesResponse rs = lc.GetGeocacheTypes(token);
                if (rs != null && rs.Status.StatusCode == 0)
                {
                    result = rs.GeocacheTypes;
                }
            }
            catch
            {
            }
            return result;
        }

        public static Tucson.Geocaching.WCF.API.Geocaching1.Types.WptLogType[] GetLogTypes(string token)
        {
            Tucson.Geocaching.WCF.API.Geocaching1.Types.WptLogType[] result = null;
            LiveClient lc = GetLiveClient();
            try
            {
                GetWptLogTypesResponse rs = lc.GetWptLogTypes(token);
                if (rs != null && rs.Status.StatusCode == 0)
                {
                    result = rs.WptLogTypes;
                }
            }
            catch
            {
            }
            return result;
        }

        public static List<Tucson.Geocaching.WCF.API.Geocaching1.Types.Geocache> GetNewGeocaches(string token)
        {
            var result = new List<Tucson.Geocaching.WCF.API.Geocaching1.Types.Geocache>();

            LiveClient lc = GetLiveClient();
            try
            {
                SearchForGeocachesRequest sr = new SearchForGeocachesRequest();
                sr.AccessToken = token;
                sr.MaxPerPage = 50;
                sr.IsLite = true;
                sr.Countries = new Tucson.Geocaching.WCF.API.Geocaching1.Types.CountryFilter();
                sr.Countries.CountryIds = new int[] { 4, 8, 141 };
                sr.CachePublishedDate = new Tucson.Geocaching.WCF.API.Geocaching1.Types.CachePublishedDateFilter();
                sr.CachePublishedDate.Range = new DateRange();
                sr.CachePublishedDate.Range.StartDate = DateTime.Now.AddHours(-24).ToUniversalTime();
                //sr.StartIndex = 0;
                sr.GeocacheLogCount = 0;
                sr.TrackableLogCount = 0;
                sr.IsSummaryOnly = true;

                GetGeocacheDataResponse resp = lc.SearchForGeocaches(sr);
                while (resp.Status.StatusCode == 0 && resp.Geocaches != null && resp.Geocaches.Length>0)
                {
                    result.AddRange(resp.Geocaches);

                    System.Threading.Thread.Sleep(4000);
                    var mr = new GetMoreGeocachesRequest();
                    mr.AccessToken = token;
                    mr.GeocacheLogCount = 0;
                    mr.IsLite = true;
                    mr.IsSummaryOnly = true;
                    mr.MaxPerPage = 50;
                    mr.GeocacheLogCount = 0;
                    mr.TrackableLogCount = 0;
                    mr.StartIndex = result.Count;

                    resp = lc.GetMoreGeocaches(mr);
                }
            }
            catch
            {
            }
            lc.Close();
            return result;
        }


        public static Tucson.Geocaching.WCF.API.Geocaching1.Types.Geocache[] GetGeocaches(string token, string[] wp)
        {
            return GetGeocaches(token, wp, false);
        }
        public static Tucson.Geocaching.WCF.API.Geocaching1.Types.Geocache[] GetGeocaches(string token, string[] wp, bool isLite)
        {
            Tucson.Geocaching.WCF.API.Geocaching1.Types.Geocache[] result = null;

            LiveClient lc = GetLiveClient();
            try
            {
                SearchForGeocachesRequest sr = new SearchForGeocachesRequest();
                sr.AccessToken = token;
                sr.CacheCode = new Tucson.Geocaching.WCF.API.Geocaching1.Types.CacheCodeFilter();
                sr.CacheCode.CacheCodes = wp;
                sr.MaxPerPage = wp.Length;
                sr.IsLite = isLite;
                //sr.StartIndex = 0;
                sr.GeocacheLogCount = 0;
                sr.TrackableLogCount = 0;

                GetGeocacheDataResponse resp = lc.SearchForGeocaches(sr);
                if (resp.Status.StatusCode == 0)
                {
                    result = resp.Geocaches;
                }
                if (resp.CacheLimits != null)
                {
                    var ac = Instance.GCComPMAccounts.Where(x => x.Token == token).FirstOrDefault();
                    if (ac != null)
                    {
                        ac.CachesLeft = resp.CacheLimits.CachesLeft;
                        ac.CurrentCacheCount = resp.CacheLimits.CurrentCacheCount;
                        ac.LimitsUpdatedAt = DateTime.Now;
                        using (var db = new PetaPoco.Database(Manager.SchedulerConnectionString, "System.Data.SqlClient"))
                        {
                            db.Save(ac);
                        }
                    }
                }
            }
            catch
            {
            }
            lc.Close();
            return result;
        }

        public static List<Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog> GetLogsOfGeocache(string token, string wp)
        {
            int pageSize = 30;
            int callDelay = 4000;

            long prevCollAt;
            long nextCollAt;

            List<Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog> result = new List<Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog>();
            LiveClient lc = GetLiveClient();
            try
            {
                prevCollAt = Environment.TickCount;
                GetGeocacheLogResponse glr = lc.GetGeocacheLogsByCacheCode(token, wp, result.Count, pageSize);
                while (glr.Status.StatusCode == 0 && glr.Logs.Count() > 0)
                {
                    int startIndex = result.Count;
                    foreach (Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog l in glr.Logs)
                    {
                        result.Add(l);
                    }
                    if (glr.Logs.Count() < pageSize)
                    {
                        break;
                    }
                    else
                    {
                        nextCollAt = prevCollAt + callDelay;
                        int delay = (int)(nextCollAt - Environment.TickCount);
                        if (delay > 0 && delay <= callDelay)
                        {
                            System.Threading.Thread.Sleep(delay); //max 30 per minute
                        }
                        prevCollAt = Environment.TickCount;
                        glr = lc.GetGeocacheLogsByCacheCode(token, wp, result.Count, pageSize);
                    }
                }
                if (glr.Status.StatusCode != 0)
                {
                    result = null;
                }
            }
            catch
            {
                result = null;
            }
            lc.Close();
            return result;
        }

        public static List<Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog> GetLogsOfGeocache(string token, string wp, int maxLogs)
        {
            List<Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog> result = new List<Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog>();
            LiveClient lc = GetLiveClient();
            try
            {
                GetGeocacheLogResponse glr = lc.GetGeocacheLogsByCacheCode(token, wp, 0, maxLogs);
                if (glr.Status.StatusCode == 0 && glr.Logs.Count() > 0)
                {
                    foreach (Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheLog l in glr.Logs)
                    {
                        result.Add(l);
                    }
                }
                if (glr.Status.StatusCode != 0)
                {
                    result = null;
                }
            }
            catch
            {
                result = null;
            }
            lc.Close();
            return result;
        }

        public static PQData[] GetPocketQueryList(string token)
        {
            PQData[] result = null;
            LiveClient lc = GetLiveClient();
            try
            {
                GetPocketQueryListResponse resp = lc.GetPocketQueryList(token);
                if (resp.Status.StatusCode == 0)
                {
                    result = resp.PocketQueryList;
                }
            }
            catch
            {
            }
            lc.Close();
            return result;
        }

        public static byte[] GetPocketQueryData(string token, PQData pgData)
        {
            byte[] result = null;
            LiveClient lc = GetLiveClient();
            try
            {
                GetPocketQueryZippedFileResponse resp = lc.GetPocketQueryZippedFile(token, pgData.GUID);
                if (resp.Status.StatusCode == 0)
                {
                    result = Convert.FromBase64String(resp.ZippedFile);
                }
            }
            catch (Exception e)
            {
                string s = e.Message;
                if (s.Length == 0)
                {
                    s = "";
                }
            }
            lc.Close();
            return result;
        }


        public static Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheStatus[] GetGeocacheStatus(string token, string[] wpList)
        {
            Tucson.Geocaching.WCF.API.Geocaching1.Types.GeocacheStatus[] result = null;
            LiveClient lc = GetLiveClient();
            try
            {
                GetGeocacheStatusRequest req = new GetGeocacheStatusRequest();
                req.AccessToken = token;
                req.CacheCodes = wpList;
                GetGeocacheStatusResponse resp = lc.GetGeocacheStatus(req);
                if (resp.Status.StatusCode == 0)
                {
                    result = resp.GeocacheStatuses;
                }
            }
            catch
            {
            }
            lc.Close();
            return result;
        }

        public static BookmarkListEntry[] GetBookmarkListsByUserID(string token, long usrId)
        {
            BookmarkListEntry[] result = null;
            LiveClient lc = GetLiveClient();
            try
            {
                var resp = lc.GetBookmarkListsByUserID(token, usrId);
                if (resp.Status.StatusCode == 0)
                {
                    result = resp.BookmarkLists;
                }
            }
            catch
            {
            }
            lc.Close();
            return result;
        }

        public static BookmarkEntry[] GetBookmarkListByGuid(string token, Guid bmGuid)
        {
            BookmarkEntry[] result = null;
            LiveClient lc = GetLiveClient();
            try
            {
                var req = new GetBookmarkListByGuidRequest();
                req.AccessToken = token;
                req.BookmarkListGuid = bmGuid;
                var resp = lc.GetBookmarkListByGuid(req);
                if (resp.Status.StatusCode == 0)
                {
                    result = resp.BookmarkList;
                }
            }
            catch
            {
            }
            lc.Close();
            return result;
        }


        public static GetYourUserProfileResponse GetUserProfile(string token)
        {
            GetYourUserProfileResponse result = null;
            LiveClient lc = GetLiveClient();
            try
            {
                var req = new GetYourUserProfileRequest();
                req.AccessToken = token;
                req.ProfileOptions = new Tucson.Geocaching.WCF.API.Geocaching1.Types.YourUserProfileOptions();
                req.DeviceInfo = new Tucson.Geocaching.WCF.API.Geocaching1.Types.DeviceData();
                req.DeviceInfo.DeviceName = "globalcaching.eu";
                req.DeviceInfo.ApplicationSoftwareVersion = "V3.0.0.0";
                req.DeviceInfo.DeviceUniqueId = "internal";
                result = lc.GetYourUserProfile(req);
            }
            catch
            {
            }
            lc.Close();
            return result;
        }

        public static GetUserProfileResponse GetAnotherUsersProfile(string token, long id)
        {
            GetUserProfileResponse result = null;
            LiveClient lc = GetLiveClient();
            try
            {
                var req = new GetAnotherUsersProfileRequest();
                req.AccessToken = token;
                req.ProfileOptions = new Tucson.Geocaching.WCF.API.Geocaching1.Types.UserProfileOptions();
                req.UserID = id;
                result = lc.GetAnotherUsersProfile(req);
            }
            catch
            {
            }
            lc.Close();
            return result;
        }

    }

}
