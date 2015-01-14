using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class TaskMacroDataCleanup: TaskBase
    {
        public class MacroTableInfo
        {
            public string TableName { get; set; }
            public DateTime Created { get; set; }
        }

        public TaskMacroDataCleanup(Manager taskManager) :
            base(taskManager, typeof(TaskMacroDataCleanup), "Macro Data Cleanup", 7, 0, 0)
        {
            Details = "";
        }

        protected override void ServiceMethod()
        {
            try
            {
                using (var db = GCEuDataSupport.Instance.GetGCEuDataDatabase())
                {
                    DateTime dt = DateTime.Now.AddDays(-2);
                    //DateTime dt = DateTime.Now.AddSeconds(-30);
                    var tbil = db.Fetch<MacroTableInfo>("select * from GCEuMacroData.dbo.TableCreationInfo");
                    foreach (var tbi in tbil)
                    {
                        if (tbi.Created < dt)
                        {
                            //time to cleanup!
                            try
                            {
                                db.Execute(string.Format("drop table GCEuMacroData.dbo.{0}", tbi.TableName));
                            }
                            catch
                            {
                            }
                            db.Execute("delete from GCEuMacroData.dbo.TableCreationInfo where TableName=@0", tbi.TableName);
                            if (tbi.TableName.StartsWith("LiveAPIDownload_"))
                            {
                                //update download status
                                string[] parts = tbi.TableName.Split(new char[] { '_' });
                                if (parts.Length == 3)
                                {
                                    int usrId = int.Parse(parts[1]);
                                    db.Execute("update GCEuMacroData.dbo.LiveAPIDownloadStatus set TotalToDownload=0 where UserID=@0", usrId);
                                }
                            }
                        }
                    }
                    Details = tbil.Count().ToString();
                }

                ServiceInfo.ErrorInLastRun = false;
            }
            catch (Exception e)
            {
                Details = e.Message;
                ServiceInfo.ErrorInLastRun = true;
            }
        }

    }
}
