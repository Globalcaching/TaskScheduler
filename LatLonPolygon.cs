using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TaskScheduler
{
    public class LatLonPolygon
    {
        public object ID { get; set; }

        public List<LatLon> points = new List<LatLon>();

        public double MinLat { get; set; }
        public double MinLon { get; set; }
        public double MaxLat { get; set; }
        public double MaxLon { get; set; }

        public void AddLocation(LatLon loc)
        {
            if (points.Count == 0)
            {
                MinLat = loc.lat;
                MinLon = loc.lon;
                MaxLat = loc.lat;
                MaxLon = loc.lon;
            }
            else
            {
                if (loc.lat < MinLat)
                {
                    MinLat = loc.lat;
                }
                else if (loc.lat > MaxLat)
                {
                    MaxLat = loc.lat;
                }
                if (loc.lon < MinLon)
                {
                    MinLon = loc.lon;
                }
                else if (loc.lon > MaxLon)
                {
                    MaxLon = loc.lon;
                }
            }
            points.Add(loc);
        }

        public bool PointInPolygon(LatLon ll)
        {
            bool result = false;

            double MULTIPLEFACTOR = 100000.0;
            double EPSSHIFT = 0.000001;
            double EPS = 0.000001;

            double r;
            double l;
            double t;
            double b;
            double sect;
            double q;

            LatLon p = new LatLon();
            p.lat = ll.lat;
            p.lon = ll.lon;

            p.lat = (int)(MULTIPLEFACTOR * p.lat) / MULTIPLEFACTOR + EPSSHIFT;
            p.lon = (int)(MULTIPLEFACTOR * p.lon) / MULTIPLEFACTOR + EPSSHIFT;

            LatLon A = new LatLon();
            LatLon B = new LatLon();

            int parity1 = 0;
            int parity2 = 0;

            for (int i = 0; i < points.Count - 1; i++)
            {
                A.lat = points[i].lat;
                A.lon = points[i].lon;
                B.lat = points[i + 1].lat;
                B.lon = points[i + 1].lon;

                A.lat = (int)(MULTIPLEFACTOR * A.lat) / MULTIPLEFACTOR;
                A.lon = (int)(MULTIPLEFACTOR * A.lon) / MULTIPLEFACTOR;
                B.lat = (int)(MULTIPLEFACTOR * B.lat) / MULTIPLEFACTOR;
                B.lon = (int)(MULTIPLEFACTOR * B.lon) / MULTIPLEFACTOR;

                l = r = A.x;
                b = t = A.y;
                if (B.x < l)
                    l = B.x;
                else
                    r = B.x;
                if (B.y < b)
                    b = B.y;
                else
                    t = B.y;

                if ((b > p.y) || (t < p.y))
                    continue; // toto je dooooost divne

                q = (p.y - A.y) / (B.y - A.y);

                sect = A.x + q * (B.x - A.x);

                if (System.Math.Abs(p.x - sect) < EPS)
                {
                    // point is on the boundary
                    // preco sa overuje iba - ova suradnica?
                    return true;
                }

                if (sect < p.x)
                    parity1++;
                else
                    parity2++;

            }

            parity1 = parity1 % 2;
            parity2 = parity2 % 2;

            result = (parity1 != 0);

            return result;
        }
    }
}