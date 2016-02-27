using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class GCEuMacroDataSupport
    {
        private static GCEuMacroDataSupport _uniqueInstance = null;
        private static object _lockObject = new object();

        public static string MacroDatabaseName = "GCEuMacroData";

        private GCEuMacroDataSupport()
        {
            //todo:
            //- get GlobalcachingDatabaseName from connection string
        }

        public static GCEuMacroDataSupport Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new GCEuMacroDataSupport();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        public string GCEuMacroDataConnectionString
        {
            get
            {
                return string.Format("Data Source={0};Initial Catalog={1};Persist Security Info=True;User ID={2};Password={3};Connect Timeout=45;", Properties.Settings.Default.DatabaseServer, MacroDatabaseName, Properties.Settings.Default.DatabaseUser, Properties.Settings.Default.DatabasePassword);
            }
        }

        public PetaPoco.Database GetGCEuMacroDataDatabase()
        {
            return new PetaPoco.Database(GCEuMacroDataConnectionString, "System.Data.SqlClient");
        }
    }
}
