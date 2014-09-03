using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class DBCon : IDisposable
    {
        private SqlConnection _dbcon = null;
        private SqlCommand _cmd = null;
        private SqlDataReader _rdr = null;

        public DBCon()
        {
            _dbcon = new SqlConnection(string.Format("Data Source={0};Initial Catalog=Globalcaching;Persist Security Info=True;User ID={1};Password={2};Connect Timeout=45;", Properties.Settings.Default.DatabaseServer, Properties.Settings.Default.DatabaseUser, Properties.Settings.Default.DatabasePassword));
            _dbcon.Open();
        }

        public SqlConnection Connection
        {
            get
            {
                return _dbcon;
            }
        }

        public SqlCommand Command
        {
            get
            {
                if (_cmd == null)
                {
                    _cmd = _dbcon.CreateCommand();
                }
                return _cmd;
            }
        }

        public SqlDataReader ExecuteReader(string command)
        {
            if (_rdr != null && !_rdr.IsClosed)
            {
                _rdr.Close();
            }
            Command.CommandText = command;
            _rdr = _cmd.ExecuteReader();
            return _rdr;
        }

        public object ExecuteScalar(string command)
        {
            if (_rdr != null && !_rdr.IsClosed)
            {
                _rdr.Close();
            }
            Command.CommandText = command;
            return _cmd.ExecuteScalar();
        }

        public int ExecuteNonQuery(string command)
        {
            if (_rdr != null && !_rdr.IsClosed)
            {
                _rdr.Close();
            }
            Command.CommandText = command;
            return _cmd.ExecuteNonQuery();
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_rdr != null)
            {
                if (!_rdr.IsClosed)
                {
                    _rdr.Close();
                }
                _rdr.Dispose();
                _rdr = null;
            }
            if (_cmd != null)
            {
                _cmd.Dispose();
                _cmd = null;
            }
            if (_dbcon != null)
            {
                _dbcon.Close();
                _dbcon = null;
            }
        }

        #endregion
    }

}
