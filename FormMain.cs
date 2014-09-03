using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskScheduler
{
    public partial class FormMain : Form
    {
        public Manager TaskManager = null;

        public FormMain()
        {
            InitializeComponent();

            if (Properties.Settings.Default.UpgradeNeeded)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeNeeded = false;
                Properties.Settings.Default.Save();
            }

            textBox1.Text = Properties.Settings.Default.DatabaseServer ?? "";
            textBox2.Text = Properties.Settings.Default.DatabaseUser ?? "";
            textBox3.Text = Properties.Settings.Default.DatabasePassword ?? "";

            if (!string.IsNullOrEmpty(Properties.Settings.Default.DatabaseServer))
            {
                try
                {
                    var sfm = SHP.ShapeFilesManager.Instance;
                    TaskManager = new Manager();

                    foreach (var t in TaskManager.Tasks)
                    {
                        flowLayoutPanel1.Controls.Add(t.GetSettingsControl());
                    }
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (TaskManager!=null)
            {
                TaskManager.Dispose();
                TaskManager = null;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GCComDataSupport.Instance.UpdateGCComDataTypes();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.DatabaseServer = textBox1.Text;
            Properties.Settings.Default.Save();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.DatabaseUser = textBox2.Text;
            Properties.Settings.Default.Save();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.DatabasePassword = textBox3.Text;
            Properties.Settings.Default.Save();
        }
    }
}
