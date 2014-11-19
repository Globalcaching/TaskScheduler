using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TaskScheduler
{
    public class TaskPocketQuery: TaskBase
    {
        public TaskPocketQuery(Manager taskManager) :
            base(taskManager, typeof(TaskPocketQuery), "Pocket Query", 8, 0, 0)
        {
        }

        protected override void ServiceMethod()
        {
            using (var dbcon = new PetaPoco.Database(GCComDataSupport.Instance.GCComDataConnectionString, "System.Data.SqlClient"))
            {
                try
                {
                    foreach (GcComAccounts sai in GeocachingAPI.Instance.GCComPMAccounts)
                    {
                        try
                        {
                            if (sai.Token.Length > 0)
                            {
                                www.geocaching.com.Geocaching1.Live.data.PQData[] pqData = GeocachingAPI.GetPocketQueryList(sai.Token);
                                if (pqData != null)
                                {
                                    foreach (www.geocaching.com.Geocaching1.Live.data.PQData pq in pqData)
                                    {
                                        try
                                        {
                                            if (pq.IsDownloadAvailable)
                                            {
                                                //download
                                                byte[] data = GeocachingAPI.GetPocketQueryData(sai.Token, pq);
                                                //process
                                                if (data != null)
                                                {
                                                    ZipInputStream s = null;
                                                    s = new ZipInputStream(new System.IO.MemoryStream(data));
                                                    try
                                                    {
                                                        ZipEntry theEntry = s.GetNextEntry();
                                                        if (theEntry != null)
                                                        {
                                                            if (theEntry.Name.ToLower().IndexOf("-wpts.") > 0)
                                                            {
                                                                theEntry = s.GetNextEntry();
                                                            }
                                                        }
                                                        if (theEntry != null)
                                                        {
                                                            int size;
                                                            StringBuilder sb = new StringBuilder();
                                                            byte[] tmpdata = new byte[1024];
                                                            while (true)
                                                            {
                                                                size = s.Read(tmpdata, 0, tmpdata.Length);
                                                                if (size > 0)
                                                                {
                                                                    if (sb.Length == 0 && tmpdata[0] == 0xEF && size > 2)
                                                                    {
                                                                        sb.Append(System.Text.ASCIIEncoding.UTF8.GetString(tmpdata, 3, size - 3));
                                                                    }
                                                                    else
                                                                    {
                                                                        sb.Append(System.Text.ASCIIEncoding.UTF8.GetString(tmpdata, 0, size));
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    break;
                                                                }
                                                            }

                                                            ProcessGeocachingComGPX(dbcon, sb.ToString());
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        //"ERROR unzippig or processing PQ");
                                                    }
                                                    s.Close();
                                                }
                                                else
                                                {
                                                    //("ERROR downloading PQ");
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Details = e.Message;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Details = e.Message;
                        }
                    }
                    Details = "";
                    ServiceInfo.ErrorInLastRun = false;
                }
                catch (Exception e)
                {
                    Details = e.Message;
                    ServiceInfo.ErrorInLastRun = true;
                }
            }
        }

        private void ProcessGeocachingComGPX(PetaPoco.Database dbcon, string gpxDoc)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(gpxDoc);

            XmlElement root = xmlDoc.DocumentElement;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("x", root.NamespaceURI); // x is our temp alias
            nsmgr.AddNamespace("y", "http://www.groundspeak.com/cache/1/0"); // y is our temp alias
            XmlNodeList wps = root.SelectNodes("x:wpt", nsmgr);
            if (wps != null)
            {
                List<string> wpInPQ = new List<string>();
                foreach (XmlNode wpn in wps)
                {
                    string wp = wpn.SelectSingleNode("x:name", nsmgr).InnerText;
                    if (dbcon.Fetch<long>("SELECT ID FROM GCComGeocache WHERE Code=@0", wp).Count == 0)
                    {
                        ScheduledWaypoint swp = dbcon.FirstOrDefault<ScheduledWaypoint>(string.Format("select * from [{0}].[dbo].[ScheduledWaypoint] where Code=@0", Manager.SchedulerDatabase), wp);
                        if (swp == null)
                        {
                            swp = new ScheduledWaypoint();
                            swp.Code = wp;
                            swp.DateAdded = DateTime.Now;
                            swp.FullRefresh = true;
                            dbcon.Insert(string.Format("[{0}].[dbo].[ScheduledWaypoint]", Manager.SchedulerDatabase), null, false, swp);
                        }
                    }
                }
            }
        }
    }
}
