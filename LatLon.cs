using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;

namespace TaskScheduler
{
    [Serializable]
    public class LatLon
    {
        public double lat = 0;
        public double lon = 0;

        public LatLon()
        {
        }

        public override string ToString()
        {
            return string.Format("{0},{1}", lat.ToString().Replace(',', '.'), lon.ToString().Replace(',', '.'));
        }

        public static LatLon ConvertFrom(string LatDegrees, string LatMinutes, string LonDegrees, string LonMinutes)
        {
            LatLon result = new LatLon();
            try
            {
                result.lat = Helper.ConvertToDouble(LatDegrees) + (Helper.ConvertToDouble(LatMinutes) / 60.0);
                result.lon = Helper.ConvertToDouble(LonDegrees) + (Helper.ConvertToDouble(LonMinutes) / 60.0);
            }
            catch
            {
                result = null;
            }
            return result;
        }

        public static LatLon FromString(string s)
        {
            LatLon result = null;
            try
            {
                s = s.ToUpper();
                string[] parts = s.Split(new char[] { ' ', 'N', 'E', 'S', 'W', '.', '°', ',', '\'' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 6 || parts.Length == 4)
                {
                    LatLon ll = new LatLon();
                    if (parts.Length == 6)
                    {
                        ll.lat = Helper.ConvertToDouble(parts[0]) + ((Helper.ConvertToDouble(parts[1]) + (Helper.ConvertToDouble(parts[2]) / 1000.0)) / 60.0);
                        ll.lon = Helper.ConvertToDouble(parts[3]) + ((Helper.ConvertToDouble(parts[4]) + (Helper.ConvertToDouble(parts[5]) / 1000.0)) / 60.0);

                        if (s.IndexOf("S", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            ll.lat = -1.0 * ll.lat;
                        }
                        if (s.IndexOf("W", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            ll.lon = -1.0 * ll.lon;
                        }
                    }
                    else
                    {
                        ll.lat = Helper.ConvertToDouble(string.Format("{0},{1}", parts[0], parts[1]));
                        ll.lon = Helper.ConvertToDouble(string.Format("{0},{1}", parts[2], parts[3]));
                    }
                    result = ll;
                }
                else if (parts.Length == 2)
                {
                    double x = Helper.ConvertToDouble(parts[0]);
                    double y = Helper.ConvertToDouble(parts[1]);
                    result = LatLon.FromRD(x, y);

                }
            }
            catch
            {
            }
            return result;
        }

        public double x
        {
            get { return lon; }
        }
        public double y
        {
            get { return lat; }
        }


        public static LatLon FromRD(double x, double y)
        {
            LatLon result = null;
            double lattitude;
            double longitude;
            double f0, l0, x0, y0;
            double a01, a02, a03, a04, a20, a21, a22, a23, a24, a40, a41, a42;
            double b10, b11, b12, b13, b14, b15, b30, b31, b32, b33, b50, b51;

            double dx, dy, dl, df;
            double l, f;

            if (x < 0 || x > 290000)
            {
                lattitude = 0.0;
                longitude = 0.0;
                return result;
            }
            else if (y < 290000 || y > 630000)
            {
                lattitude = 0.0;
                longitude = 0.0;
                return result;
            }

            x0 = 155000;
            y0 = 463000;
            f0 = 52.156160556;
            l0 = 5.387638889;
            a01 = 3236.0331637;
            b10 = 5261.3028966;
            a20 = -32.5915821;
            b11 = 105.9780241;
            a02 = -0.2472814;
            b12 = 2.4576469;
            a21 = -0.8501341;
            b30 = -0.8192156;
            a03 = -0.0655238;
            b31 = -0.0560092;
            a22 = -0.0171137;
            b13 = 0.0560089;
            a40 = 0.0052771;
            b32 = -0.0025614;
            a23 = -0.0003859;
            b14 = 0.001277;
            a41 = 0.0003314;
            b50 = 0.0002574;
            a04 = 0.0000371;
            b33 = -0.0000973;
            a42 = 0.0000143;
            b51 = 0.0000293;
            a24 = -0.000009;
            b15 = 0.0000291;



            dx = (x - x0) * 0.00001;
            dy = (y - y0) * 0.00001;

            df = a01 * dy + a20 * Math.Pow(dx, 2) + a02 * Math.Pow(dy, 2) + a21 * Math.Pow(dx, 2) * dy + a03 * Math.Pow(dy, 3);
            df = df + a40 * Math.Pow(dx, 4) + a22 * Math.Pow(dx, 2) * Math.Pow(dy, 2) + a04 * Math.Pow(dy, 4) + a41 * Math.Pow(dx, 4) * dy;
            df = df + a23 * Math.Pow(dx, 2) * Math.Pow(dy, 3) + a42 * Math.Pow(dx, 4) * Math.Pow(dy, 2) + a24 * Math.Pow(dx, 2) * Math.Pow(dy, 4);
            f = f0 + df / 3600; //N RD-bessel

            dl = b10 * dx + b11 * dx * dy + b30 * Math.Pow(dx, 3) + b12 * dx * Math.Pow(dy, 2) + b31 * Math.Pow(dx, 3) * dy;
            dl = dl + b13 * dx * Math.Pow(dy, 3) + b50 * Math.Pow(dx, 5) + b32 * Math.Pow(dx, 3) * Math.Pow(dy, 2) + b14 * dx * Math.Pow(dy, 4);
            dl = dl + b51 * Math.Pow(dx, 5) * dy + b33 * Math.Pow(dx, 3) * Math.Pow(dy, 3) + b15 * dx * Math.Pow(dy, 5);
            l = l0 + dl / 3600; //E RD-bessel

            lattitude = f + (-96.862 - (f - 52) * 11.714 - (l - 5) * 0.125) * 0.00001; //N WGS84
            longitude = l + ((f - 52) * 0.329 - 37.902 - (l - 5) * 14.667) * 0.00001; //E WGS84

            result = new LatLon();
            result.lat = lattitude;
            result.lon = longitude;

            return result;
        }

        public static bool RDFromLatLong(double lattitude, double longitude, out double x, out double y)
        {
            double f0, l0;
            double x0, y0;
            double c01, c03, c05, c11, c13, c21, c23, c31, c41;
            double d02, d04, d10, d12, d14, d20, d22, d30, d32, d40;
            double dx, dy, dl, df;
            double l, f;

            if (lattitude < 50.579 || lattitude > 53.639)
            {
                x = 0;
                y = 0;
                return false;
            }
            else if (longitude < 3.043 || longitude > 7.429)
            {
                x = 0;
                y = 0;
                return false;
            }

            x0 = 155000;
            y0 = 463000;
            f0 = 52.15616056;
            l0 = 5.38763889;
            c01 = 190066.98903;
            d10 = 309020.3181;
            c11 = -11830.85831;
            d02 = 3638.36193;
            c21 = -114.19754;
            d12 = -157.95222;
            c03 = -32.3836;
            d20 = 72.97141;
            c31 = -2.34078;
            d30 = 59.79734;
            c13 = -0.60639;
            d22 = -6.43481;
            c23 = 0.15774;
            d04 = 0.09351;
            c41 = -0.04158;
            d32 = -0.07379;
            c05 = -0.00661;
            d14 = -0.05419;
            d40 = -0.03444;


            f = lattitude - (-96.862 - (lattitude - 52) * 11.714 - (longitude - 5) * 0.125) * 0.00001; //N bessel
            l = longitude - ((lattitude - 52) * 0.329 - 37.902 - (longitude - 5) * 14.667) * 0.00001; //E bessel

            df = (f - f0) * 0.36;
            dl = (l - l0) * 0.36;

            dx = c01 * dl + c11 * df * dl + c21 * Math.Pow(df, 2) * dl + c03 * Math.Pow(dl, 3);
            dx = dx + c31 * Math.Pow(df, 3) * dl + c13 * df * Math.Pow(dl, 3) + c23 * Math.Pow(df, 2) * Math.Pow(dl, 3);
            dx = dx + c41 * Math.Pow(df, 4) * dl + c05 * Math.Pow(dl, 5);
            x = (int)Math.Round(x0 + dx); //RD x


            dy = d10 * df + d20 * Math.Pow(df, 2) + d02 * Math.Pow(dl, 2) + d12 * df * Math.Pow(dl, 2);
            dy = dy + d30 * Math.Pow(df, 3) + d22 * Math.Pow(df, 2) * Math.Pow(dl, 2) + d40 * Math.Pow(df, 4);
            dy = dy + d04 * d14 + d32 * Math.Pow(df, 3) * Math.Pow(dl, 2) + d14 * df * Math.Pow(dl, 4);
            y = (int)Math.Round(y0 + dy); //RD y

            return true;
        }
    }
}