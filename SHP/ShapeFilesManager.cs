using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Web;

namespace TaskScheduler.SHP
{
    public class ShapeFilesManager
    {
        private static ShapeFilesManager _uniqueInstance = null;
        private static object _lockObject = new object();

        private List<ShapeFile> _shapeFiles = new List<ShapeFile>();

        private ShapeFilesManager()
        {
        }

        public static ShapeFilesManager Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            ShapeFilesManager sfm = new ShapeFilesManager();
                            sfm.Initialize();
                            _uniqueInstance = sfm;
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        public bool Initialize()
        {
            lock (this)
            {
                Clear();

                XmlDocument doc = new XmlDocument();
                doc.Load("Shapefiles.xml");
                XmlElement root = doc.DocumentElement;
                XmlNodeList files = root.SelectNodes("file");
                if (files != null)
                {
                    foreach (XmlNode f in files)
                    {
                        ShapeFile sf = new ShapeFile(f.Attributes["path"].Value);
                        if (sf.Initialize(f.Attributes["tablename"].Value, (ShapeFile.CoordType)Enum.Parse(typeof(ShapeFile.CoordType), f.Attributes["format"].Value), (AreaType)Enum.Parse(typeof(AreaType), f.Attributes["level"].Value), f.Attributes["id"].Value))
                        {
                            sf.Name = f.Attributes["description"].Value;
                            sf.Website = f.Attributes["website"].Value;
                            sf.EMail = f.Attributes["email"].Value;
                            _shapeFiles.Add(sf);
                        }
                    }
                }
            }
            return true;
        }

        private void Clear()
        {
            foreach (var sf in _shapeFiles)
            {
                sf.Dispose();
            }
            _shapeFiles.Clear();
        }

        public List<string> GetAdditionalInfoOfArea(AreaInfo ai, LatLon ll)
        {
            List<string> result;
            lock (this)
            {
                result = ai.Owner.GetAdditionalInfoOfArea(ai, ll);
            }
            return result;
        }

        public List<LatLonPolygon> GetPolygonOfArea(ShapeFile.IndexRecord rec)
        {
            List<LatLonPolygon> result = new List<LatLonPolygon>();
            lock (this)
            {
                rec.Owner.GetPolygonOfArea(result, rec);
            }
            return result;
        }

        public List<AreaInfo> GetAreasOfLocation(LatLon loc)
        {
            List<AreaInfo> result = new List<AreaInfo>();
            lock (this)
            {
                foreach (var sf in _shapeFiles)
                {
                    List<AreaInfo> areas = sf.GetAreasOfLocation(loc);
                    if (areas != null)
                    {
                        result.AddRange(areas);
                    }
                }
            }
            return result;
        }

        public List<AreaInfo> GetAreasOfLocation(LatLon loc, List<AreaInfo> inAreas)
        {
            List<AreaInfo> result = new List<AreaInfo>();
            lock (this)
            {
                foreach (var sf in _shapeFiles)
                {
                    List<AreaInfo> areas = sf.GetAreasOfLocation(loc, inAreas);
                    if (areas != null)
                    {
                        result.AddRange(areas);
                    }
                }
            }
            return result;
        }

        public List<AreaInfo> GetAreasByName(string name)
        {
            List<AreaInfo> result = new List<AreaInfo>();
            lock (this)
            {
                foreach (var sf in _shapeFiles)
                {
                    result.AddRange((from a in sf.AreaInfos where a.Name == name select a).ToList());
                }
            }
            return result;
        }

        public List<AreaInfo> GetAreasByName(string name, AreaType level)
        {
            List<AreaInfo> result = new List<AreaInfo>();
            lock (this)
            {
                foreach (var sf in _shapeFiles)
                {
                    result.AddRange((from a in sf.AreaInfos where a.Name == name && a.Level == level select a).ToList());
                }
            }
            return result;
        }

        public List<AreaInfo> GetAreasByID(object id)
        {
            List<AreaInfo> result = new List<AreaInfo>();
            lock (this)
            {
                foreach (var sf in _shapeFiles)
                {
                    result.AddRange((from a in sf.AreaInfos where a.ID.ToString() == id.ToString() select a).ToList());
                }
            }
            return result;
        }

        public List<AreaInfo> GetAreasByLevel(AreaType level)
        {
            List<AreaInfo> result = new List<AreaInfo>();
            lock (this)
            {
                foreach (var sf in _shapeFiles)
                {
                    result.AddRange((from a in sf.AreaInfos where a.Level == level select a).ToList());
                }
            }
            return result;
        }

        public void GetPolygonOfArea(AreaInfo area)
        {
            lock (this)
            {
                foreach (var sf in _shapeFiles)
                {
                    sf.GetPolygonOfArea(area);
                    if (area.Polygons != null)
                    {
                        break;
                    }
                }
            }
        }

    }
}
