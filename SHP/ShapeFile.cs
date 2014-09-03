using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;

namespace TaskScheduler.SHP
{
    public class ShapeFile: IDisposable
    {
        public enum ShapeType : int
        {
            NullShape = 0,
            Point = 1,
            PolyLine = 3,
            Polygon = 5,
            MultiPoint = 8,
            PointZ = 11,
            PolyLineZ = 13,
            PolygonZ = 15,
            MultiPointZ = 18,
            PointM = 21,
            PolyLineM = 23,
            PolygonM = 25,
            MultiPointM = 28,
            MultiPatch = 31
        }

        public enum CoordType : int
        {
            WGS84,
            DutchGrid
        }

        public class IndexRecord
        {
            public ShapeFile Owner { get; set; }

            public object ID { get; set; }
            public bool Ignore { get; set; }

            //from shx file
            public int Offset { get; set; }
            public int ContentLength { get; set; }

            //from shp file
            public ShapeType ShapeType { get; set; }
            public double XMin { get; set; }
            public double XMax { get; set; }
            public double YMin { get; set; }
            public double YMax { get; set; }

            //from dbf file
            public string Name { get; set; }
        }

        private string _shpFilename;
        private FileStream _shpFileStream = null;
        private byte[] _buffer = new byte[8];
        private CoordType _coordType;
        private AreaType _areaType;

        public string Website { get; set; }
        public string EMail { get; set; }
        public string Name { get; set; }
        public string ID { get; private set; }

        //shp header
        private int _shpFileSize = -1;   //The value for file length is the total length of the file in 16-bit words (including the fifty
                                        //16-bit words that make up the header)
        private int _shpVersion = -1;    //1000
        private ShapeType _shpShapeType = ShapeType.NullShape;
        private double _shpXMin;
        private double _shpXMax;
        private double _shpYMin;
        private double _shpYMax;

        private IndexRecord[] _indexRecords = null;
        private List<AreaInfo> _areaInfos = new List<AreaInfo>();

        public ShapeFile(string shpFileName)
        {
            _shpFilename = shpFileName;
        }

        public IndexRecord[] IndexRecords
        {
            get { return _indexRecords; }
        }

        public List<AreaInfo> AreaInfos
        {
            get { return _areaInfos; }
        }

        public string[] GetFields()
        {
            string[] result = null;
            try
            {
                using (DotNetDBF.DBFReader dbf = new DotNetDBF.DBFReader(string.Format("{0}dbf", _shpFilename.Substring(0, _shpFilename.Length - 3))))
                {
                    var fields = dbf.Fields;
                    result = (from s in fields select s.Name).ToArray();
                }
            }
            catch
            {
            }
            return result;
        }

        public List<string> GetAdditionalInfoOfArea(AreaInfo ai, LatLon ll)
        {
            List<string> result = new List<string>();
            //for now, just take first record of area
            try
            {
                using (DotNetDBF.DBFReader dbf = new DotNetDBF.DBFReader(string.Format("{0}dbf", _shpFilename.Substring(0, _shpFilename.Length - 3))))
                {
                    string[] flds = (from s in dbf.Fields select s.Name).ToArray();
                    dbf.SetSelectFields(flds);
                    foreach (IndexRecord ir in _indexRecords)
                    {
                        object[] recs = dbf.NextRecord();
                        if (!ir.Ignore && ir.Name == ai.Name)
                        {
                            for (int i = 0; i < flds.Length; i++ )
                            {
                                result.Add(string.Format("{0}: {1}", flds[i], recs[i] == null ? "" : recs[i].ToString()));
                            }
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
            return result;
        }

        public bool Initialize(string dbfNameFieldName, CoordType coordType, AreaType areaType, string id)
        {
            bool result = false;
            try
            {
                _coordType = coordType;
                _shpFileStream = File.OpenRead(_shpFilename);
                _areaType = areaType;
                ID = id;
                int FileCode = GetInt32(_shpFileStream, false);
                if (FileCode==9994)
                {
                    _shpFileStream.Position = 24;
                    _shpFileSize = GetInt32(_shpFileStream, false);
                    _shpVersion = GetInt32(_shpFileStream, true);
                    _shpShapeType = (ShapeType)GetInt32(_shpFileStream, true);
                    _shpXMin = GetDouble(_shpFileStream, true);
                    _shpYMin = GetDouble(_shpFileStream, true);
                    _shpXMax = GetDouble(_shpFileStream, true);
                    _shpYMax = GetDouble(_shpFileStream, true);

                    using (FileStream fs = File.OpenRead(string.Format("{0}shx", _shpFilename.Substring(0, _shpFilename.Length - 3))))
                    {
                        FileCode = GetInt32(fs, false);
                        if (FileCode == 9994)
                        {
                            fs.Position = 24;
                            int shxFileSize = GetInt32(fs, false);
                            int shxVersion = GetInt32(fs, true);

                            int intRecordCount = ((shxFileSize * 2) - 100) / 8;
                            fs.Position = 100;
                            _indexRecords = new IndexRecord[intRecordCount];
                            for (int i = 0; i < intRecordCount; i++)
                            {
                                _indexRecords[i] = new IndexRecord() { Offset = GetInt32(fs, false) * 2, ContentLength = GetInt32(fs, false) * 2 };
                                _indexRecords[i].ID = string.Format("{0}.{1}", this.ID, i);
                                _indexRecords[i].Owner = this;
                            }
                            for (int i = 0; i < intRecordCount; i++)
                            {
                                IndexRecord ir = _indexRecords[i];
                                _shpFileStream.Position = ir.Offset + 8;
                                ir.ShapeType = (ShapeType)GetInt32(_shpFileStream, true);
                                if (ir.ShapeType == ShapeType.NullShape)
                                {
                                    ir.ShapeType = _shpShapeType;
                                }
                                switch (ir.ShapeType)
                                {
                                    case ShapeType.Polygon:
                                    case ShapeType.PolygonZ:
                                    case ShapeType.PolygonM:
                                    case ShapeType.MultiPatch:
                                        ir.XMin = GetDouble(_shpFileStream, true);
                                        ir.YMin = GetDouble(_shpFileStream, true);
                                        ir.XMax = GetDouble(_shpFileStream, true);
                                        ir.YMax = GetDouble(_shpFileStream, true);
                                        ir.Ignore = false;
                                        break;
                                    default:
                                        ir.Ignore = true;
                                        break;
                                }
                            }
                            using (DotNetDBF.DBFReader dbf = new DotNetDBF.DBFReader(string.Format("{0}dbf", _shpFilename.Substring(0, _shpFilename.Length - 3))))
                            {
                                var fields = dbf.Fields;
                                dbf.SetSelectFields(new string[]{dbfNameFieldName});
                                var rec = dbf.NextRecord();
                                int index = 0;
                                while (rec != null)
                                {
                                    if (!_indexRecords[index].Ignore)
                                    {
                                        _indexRecords[index].Name = rec[0].ToString();
                                        if (_indexRecords[index].Name == "Fryslân" || _indexRecords[index].Name == "Frysl�n")
                                        {
                                            _indexRecords[index].Name = "Friesland";
                                        }
                                    }
                                    else
                                    {
                                        _indexRecords[index].Name = null;
                                    }
                                    index++;
                                    if (index < _indexRecords.Length)
                                    {
                                        rec = dbf.NextRecord();
                                    }
                                    else
                                    {
                                        rec = null;
                                    }
                                }
                            }

                            // all ok, check if we need to convert the coords to WGS84, the internal coord system
                            if (_coordType == CoordType.DutchGrid)
                            {
                                LatLon ll = LatLon.FromRD(_shpXMin, _shpYMin);
                                _shpYMin = ll.lat;
                                _shpXMin = ll.lon;
                                ll = LatLon.FromRD(_shpXMax, _shpYMax);
                                _shpYMax = ll.lat;
                                _shpXMax = ll.lon;

                                for (int i = 0; i < intRecordCount; i++)
                                {
                                    IndexRecord ir = _indexRecords[i];
                                    ll = LatLon.FromRD(ir.XMin, ir.YMin);
                                    ir.YMin = ll.lat;
                                    ir.XMin = ll.lon;
                                    ll = LatLon.FromRD(ir.XMax, ir.YMax);
                                    ir.YMax = ll.lat;
                                    ir.XMax = ll.lon;
                                }
                            }
                            int idIndex = 0;
                            var areaNames = (from a in _indexRecords select a.Name).Distinct();
                            foreach (var name in areaNames)
                            {
                                idIndex++;
                                var records = from r in _indexRecords where r.Name == name select r;
                                AreaInfo ai = new AreaInfo();
                                ai.Owner = this;
                                ai.ID = string.Format("{0}{1}", id, idIndex);
                                ai.Level = areaType;
                                ai.MaxLat = records.Max(x => x.YMax);
                                ai.MaxLon = records.Max(x => x.XMax);
                                ai.MinLat = records.Min(x => x.YMin);
                                ai.MinLon = records.Min(x => x.XMin);
                                ai.Name = name;
                                ai.ParentID = null; //not supported
                                _areaInfos.Add(ai);
                            }

                            result = true;
                        }
                    }
                }
            }
            catch
            {
            }
            return result;
        }

        public List<AreaInfo> GetAreasOfLocation(LatLon loc)
        {
            List<AreaInfo> result = null;
            if (loc.lat >= _shpYMin && loc.lat <= _shpYMax && loc.lon >= _shpXMin && loc.lon <= _shpXMax)
            {
                //all areas with point in envelope
                var ais = from r in _areaInfos where loc.lat >= r.MinLat && loc.lat <= r.MaxLat && loc.lon >= r.MinLon && loc.lon <= r.MaxLon select r;
                foreach (var ai in ais)
                {
                    if (IsLocationInArea(loc, ai))
                    {
                        if (result == null)
                        {
                            result = new List<AreaInfo>();
                        }
                        result.Add(ai);
                    }
                }

            }
            return result;
        }

        public List<AreaInfo> GetAreasOfLocation(LatLon loc, List<AreaInfo> inAreas)
        {
            List<AreaInfo> result = null;
            if (loc.lat >= _shpYMin && loc.lat <= _shpYMax && loc.lon >= _shpXMin && loc.lon <= _shpXMax)
            {
                //all areas with point in envelope
                var ais = from r in _areaInfos 
                          join b in inAreas on r equals b
                          where loc.lat >= r.MinLat && loc.lat <= r.MaxLat && loc.lon >= r.MinLon && loc.lon <= r.MaxLon select r;
                foreach (var ai in ais)
                {
                    if (IsLocationInArea(loc, ai))
                    {
                        if (result == null)
                        {
                            result = new List<AreaInfo>();
                        }
                        result.Add(ai);
                    }
                }

            }
            return result;
        }

        private bool IsLocationInArea(LatLon loc, AreaInfo area)
        {
            bool result = false;
            //point in envelope of area
            if (loc.lat >= area.MinLat && loc.lat <= area.MaxLat && loc.lon >= area.MinLon && loc.lon <= area.MaxLon)
            {
                bool releasePoly = area.Polygons == null;
                if (area.Polygons == null) GetPolygonOfArea(area);
                if (area.Polygons != null)
                {
                    foreach (var r in area.Polygons)
                    {
                        //point in envelope of polygon
                        if (loc.lat >= r.MinLat && loc.lat <= r.MaxLat && loc.lon >= r.MinLon && loc.lon <= r.MaxLon)
                        {
                            if (r.PointInPolygon(loc))
                            {
                                result = true;
                                break;
                            }
                        }
                    }
                }
                if (releasePoly) area.Polygons = null;
            }
            return result;
        }

        public void GetPolygonOfArea(AreaInfo area)
        {
            if (_areaInfos.Contains(area))
            {
                //ours
                //get all records and add the data
                area.Polygons = new List<LatLonPolygon>();
                try
                {
                    var recs = from r in _indexRecords where r.Name == area.Name select r;
                    foreach (var rec in recs)
                    {
                        GetPolygonOfArea(area.Polygons, rec);
                    }
                }
                catch
                {
                }
            }
        }

        public void GetPolygonOfArea(List<LatLonPolygon> polygons, IndexRecord rec)
        {
            switch (rec.ShapeType)
            {
                case ShapeType.Polygon:
                    GetPolygonOfArea_Polygon(polygons, rec);
                    break;
                case ShapeType.PolygonM:
                    GetPolygonOfArea_PolygonM(polygons, rec);
                    break;
                case ShapeType.PolygonZ:
                    GetPolygonOfArea_PolygonZ(polygons, rec);
                    break;
                case ShapeType.MultiPatch:
                    GetPolygonOfArea_MultiPatch(polygons, rec);
                    break;
                default:
                    break;
            }
        }

        private void GetPolygonOfArea_Polygon(List<LatLonPolygon> polygons, IndexRecord rec)
        {
            _shpFileStream.Position = rec.Offset + 8 + 36; //skip bounding box and shapetype
            int numberOfPolygons = GetInt32(_shpFileStream, true);
            int numberOfPoints = GetInt32(_shpFileStream, true);
            int[] pointIndexFirstPointPerPolygon = new int[numberOfPolygons];
            for (int i = 0; i < numberOfPolygons; i++)
            {
                pointIndexFirstPointPerPolygon[i] = GetInt32(_shpFileStream, true);
            }
            for (int i = 0; i < numberOfPolygons; i++)
            {
                LatLonPolygon pg = new LatLonPolygon();
                pg.ID = string.Format("{0}.{1}", rec.ID, i);
                int pointCount;
                if (i < numberOfPolygons - 1)
                {
                    pointCount = pointIndexFirstPointPerPolygon[i + 1] - pointIndexFirstPointPerPolygon[i];
                }
                else
                {
                    pointCount = numberOfPoints - pointIndexFirstPointPerPolygon[i];
                }
                for (int p = 0; p < pointCount; p++)
                {
                    double x = GetDouble(_shpFileStream, true);
                    double y = GetDouble(_shpFileStream, true);
                    if (_coordType == CoordType.DutchGrid)
                    {
                        pg.AddLocation(LatLon.FromRD(x,y));
                    }
                    else
                    {
                        LatLon ll = new LatLon();
                        ll.lat = y;
                        ll.lon = x;
                        pg.AddLocation(ll);
                    }
                }
                polygons.Add(pg);
            }
        }

        private void GetPolygonOfArea_PolygonM(List<LatLonPolygon> polygons, IndexRecord rec)
        {
            //extra M information is after the Polygon info, so..
            GetPolygonOfArea_Polygon(polygons, rec);
        }
        private void GetPolygonOfArea_PolygonZ(List<LatLonPolygon> polygons, IndexRecord rec)
        {
            //extra Z information is after the Polygon info, so..
            GetPolygonOfArea_Polygon(polygons, rec);
        }

        private void GetPolygonOfArea_MultiPatch(List<LatLonPolygon> polygons, IndexRecord rec)
        {
            //NOTE: at this point we ignore the type (outer ring, inner ring
            //this is not correct and should be implemented correctly
            //suggestion: Add a property Exclude to Framework.Data.Polygon

            _shpFileStream.Position = rec.Offset + 8 + 36; //skip bounding box and shapetype
            int numberOfPolygons = GetInt32(_shpFileStream, true);
            int numberOfPoints = GetInt32(_shpFileStream, true);
            int[] pointIndexFirstPointPerPolygon = new int[numberOfPolygons];
            int[] partsTypePerPolygon = new int[numberOfPolygons];
            for (int i = 0; i < numberOfPolygons; i++)
            {
                pointIndexFirstPointPerPolygon[i] = GetInt32(_shpFileStream, true);
            }
            for (int i = 0; i < numberOfPolygons; i++)
            {
                partsTypePerPolygon[i] = GetInt32(_shpFileStream, true);
            }
            for (int i = 0; i < numberOfPolygons; i++)
            {
                LatLonPolygon pg = new LatLonPolygon();
                pg.ID = string.Format("{0}.{1}", rec.ID, i);
                int pointCount;
                if (i < numberOfPolygons - 1)
                {
                    pointCount = pointIndexFirstPointPerPolygon[i + 1] - pointIndexFirstPointPerPolygon[i];
                }
                else
                {
                    pointCount = numberOfPoints - pointIndexFirstPointPerPolygon[i];
                }
                for (int p = 0; p < pointCount; p++)
                {
                    double x = GetDouble(_shpFileStream, true);
                    double y = GetDouble(_shpFileStream, true);
                    if (_coordType == CoordType.DutchGrid)
                    {
                        pg.AddLocation(LatLon.FromRD(x, y));
                    }
                    else
                    {
                        LatLon ll = new LatLon();
                        ll.lat = y;
                        ll.lon = x;
                        pg.AddLocation(ll);
                    }
                }
                polygons.Add(pg);
            }
        }


        private int GetInt32(FileStream fs, bool littleEndian)
        {
            fs.Read(_buffer, 0, 4);
            if (littleEndian == BitConverter.IsLittleEndian)
            {
                return BitConverter.ToInt32(_buffer, 0);
            }
            else
            {
                return BitConverter.ToInt32(ReverseBytes(_buffer,0,4), 0);
            }
        }
        private double GetDouble(FileStream fs, bool littleEndian)
        {
            fs.Read(_buffer, 0, 8);
            if (littleEndian == BitConverter.IsLittleEndian)
            {
                return BitConverter.ToDouble(_buffer, 0);
            }
            else
            {
                return BitConverter.ToDouble(ReverseBytes(_buffer, 0, 8), 0);
            }
        }

        public static byte[] ReverseBytes(byte[] inArray, int startIndex, int length)
        {
            byte temp;
            int ctr = startIndex;
            int highCtr = startIndex + length - 1;

            for (int i = 0; i < length / 2; i++)
            {
                temp = inArray[ctr];
                inArray[ctr] = inArray[highCtr];
                inArray[highCtr] = temp;
                highCtr--;
                ctr++;
            }
            return inArray;
        }

        public void Dispose()
        {
            if (_shpFileStream != null)
            {
                _shpFileStream.Dispose();
                _shpFileStream = null;
            }
        }
    }
}
