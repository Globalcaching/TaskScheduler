using System;
using System.Data;
using System.Collections;
using System.Data.SqlClient;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Text;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using System.Globalization;

namespace TaskScheduler
{
    public class Helper
    {
        public Helper()
        {
        }


        public static int GetCacheIDFromCacheCode(string cacheCode)
        {
            const string v = "0123456789ABCDEFGHJKMNPQRTVWXYZ";

            int result = 0;
            try
            {
                string s = cacheCode.Substring(2).ToUpper();
                int baseValue = 31;
                if (s.Length < 4 || (s.Length == 4 && s[0] <= 'F'))
                {
                    baseValue = 16;
                }
                int mult = 1;
                while (s.Length > 0)
                {
                    char c = s[s.Length - 1];
                    result += mult * v.IndexOf(c);
                    mult *= baseValue;
                    s = s.Substring(0, s.Length - 1);
                }
                if (baseValue > 16)
                {
                    result -= 411120;
                }
            }
            catch
            {
                result = -1;
            }
            return result;
        }

        public static string GetCacheCodeFromCacheID(int cacheId)
        {
            const string v = "0123456789ABCDEFGHJKMNPQRTVWXYZ";

            string result = "";
            try
            {
                int i = cacheId;
                int baseValue = 31;
                if (i <= 65535)
                {
                    baseValue = 16;
                }
                else
                {
                    i += 411120;
                }
                while (i > 0)
                {
                    result = string.Concat(v[i % baseValue], result);
                    i /= baseValue;
                }
                result = string.Concat("GC", result);
            }
            catch
            {
                result = "";
            }
            return result;
        }

        public static string GetCityName(LatLon ll)
        {
            return GetCityName(ll.lat, ll.lon);
        }
        public static string GetCityName(double lat, double lon)
        {
            string result = GetCityNameOSM(lat, lon);
            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(string.Format("http://maps.googleapis.com/maps/api/geocode/xml?latlng={0},{1}&sensor=false", lat.ToString().Replace(',', '.'), lon.ToString().Replace(',', '.')));
                    wr.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                    wr.Method = WebRequestMethods.Http.Get;
                    HttpWebResponse webResponse = (HttpWebResponse)wr.GetResponse();
                    StreamReader reader = new StreamReader(webResponse.GetResponseStream());
                    string doc = reader.ReadToEnd();
                    webResponse.Close();
                    if (doc != null && doc.Length > 0)
                    {
                        XmlDocument xdoc = new XmlDocument();
                        xdoc.LoadXml(doc);
                        XmlNodeList nl = xdoc.SelectNodes("GeocodeResponse/result/address_component");
                        foreach (XmlNode n in nl)
                        {
                            XmlNode nt = n.SelectSingleNode("type");
                            if (nt != null && nt.InnerText == "locality")
                            {
                                result = n.SelectSingleNode("long_name").InnerText;
                                break;
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            return result;
        }

        public static string GetCityNameOSM(double lat, double lon)
        {
            string result = "";
            try
            {
                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(string.Format("http://nominatim.openstreetmap.org/reverse?format=xml&lat={0}&lon={1}&zoom=18&addressdetails=1", lat.ToString().Replace(',', '.'), lon.ToString().Replace(',', '.')));
                wr.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                wr.Method = WebRequestMethods.Http.Get;
                HttpWebResponse webResponse = (HttpWebResponse)wr.GetResponse();
                StreamReader reader = new StreamReader(webResponse.GetResponseStream());
                string doc = reader.ReadToEnd();
                webResponse.Close();
                if (doc != null && doc.Length > 0)
                {
                    XmlDocument xdoc = new XmlDocument();
                    xdoc.LoadXml(doc);
                    XmlNode n = xdoc.SelectSingleNode("reversegeocode/addressparts/city");
                    if (n != null)
                    {
                        result = n.InnerText;
                    }
                }
            }
            catch
            {
            }
            return result;
        }


        public static double ConvertToDouble(string s)
        {
            return Convert.ToDouble(s.Replace(',', '.'), CultureInfo.InvariantCulture);
        }


        public static string PointInMunicipality(double lat, double lon)
        {
            var ll = new LatLon();
            ll.lat = lat;
            ll.lon = lon;
            return PointInMunicipality(ll);
        }
        public static string PointInMunicipality(LatLon ll)
        {
            //original, this was for the (Dutch) province only
            string result = "";

            try
            {
                List<SHP.AreaInfo> ais = SHP.ShapeFilesManager.Instance.GetAreasOfLocation(ll, SHP.ShapeFilesManager.Instance.GetAreasByLevel(SHP.AreaType.Municipality));
                if (ais.Count > 0)
                {
                    result = ais[0].Name;
                }
            }
            catch
            {
            }
            return result;
        }

        public static string PointInProvince(double lat, double lon)
        {
            var ll = new LatLon();
            ll.lat = lat;
            ll.lon = lon;
            return PointInProvince(ll);
        }
        public static string PointInProvince(LatLon ll)
        {
            //original, this was for the (Dutch) province only
            string result = "";

            try
            {
                List<SHP.AreaInfo> ais = SHP.ShapeFilesManager.Instance.GetAreasOfLocation(ll, SHP.ShapeFilesManager.Instance.GetAreasByLevel(SHP.AreaType.State));
                if (ais.Count > 0)
                {
                    result = ais[0].Name;
                }
            }
            catch
            {
            }
            return result;
        }

    }

}