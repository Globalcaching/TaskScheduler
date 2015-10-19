using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class TaskCheckDonators : TaskBase
    {
        public class GCEuDonations
        {
            public int ID { get; set; }
            public DateTime ExpirationDate { get; set; }
            public bool ThankYouSent { get; set; }
            public bool ReminderSent { get; set; }

            public string Name { get; set; }
            public string EMail { get; set; }
        }

        public TaskCheckDonators(Manager taskManager) :
            base(taskManager, typeof(TaskCheckDonators), "Check Donators", 0, 5, 0)
        {
            Details = "";
        }

        protected override void ServiceMethod()
        {
            try
            {
                using (var db = GCEuDataSupport.Instance.GetGCEuDataDatabase())
                {
                    var record = db.FirstOrDefault<GCEuDonations>("select ID, ThankYouSent, ReminderSent, ExpirationDate, Name, EMail from GCEuDonations inner join Globalcaching.dbo.yaf_User on GCEuDonations.UserID = yaf_User.UserID where ThankYouSent=0 or (ReminderSent=0 and ExpirationDate<=@0)", DateTime.Now.AddDays(7));
                    if (record != null)
                    {
                        var subject = "Donatie Globalcaching";
                        var body = new StringBuilder();
                        body.AppendLine(string.Format("Hallo {0}", record.Name));
                        body.AppendLine();
                        if (!record.ThankYouSent)
                        {
                            body.AppendLine("We hebben een donatie mogen ontvangen van. Hartelijk dank daarvoor!");
                            body.AppendLine("Jouw account heeft de donateursstatus gekregen en hiermee kun je gebruik maken van alle donateursfuncties op globalcaching.eu");
                            body.AppendLine("De donatiestatus zal voor een jaar gelden en een week voor het verlopen van de einddatum zal er een herinnerings email verstuurd worden.");
                            db.Execute("update GCEuDonations set ThankYouSent=1 where ID=@0", record.ID);
                        }
                        else //if (!record.ReminderSent)
                        {
                            body.AppendLine("Het is al bijna een jaar geleden dat je gedoneerd hebt.");
                            body.AppendLine(string.Format("De donatiestatus van jouw account zal op {0} verlopen", record.ExpirationDate.ToLongDateString()));
                            db.Execute("update GCEuDonations set ReminderSent=1 where ID=@0", record.ID);
                        }
                        body.AppendLine();
                        body.AppendLine("Met vriendelijke groeten,");
                        body.AppendLine("Globalcaching");
                        //EMail.SendEMail("globalcaching@gmail.com", subject, body.ToString());
                        EMail.SendEMail(record.EMail, subject, body.ToString());
                    }
                }
            }
            catch
            {
            }
        }
    }
}
