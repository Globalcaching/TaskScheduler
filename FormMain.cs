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

            textBox4.Text = Properties.Settings.Default.smtpServer ?? "";
            textBox5.Text = Properties.Settings.Default.smtpPort.ToString();
            checkBox1.Checked = Properties.Settings.Default.smtpUseSSL;
            textBox7.Text = Properties.Settings.Default.smtpAccountName ?? "";
            textBox6.Text = Properties.Settings.Default.smtpAccountPassword ?? "";

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

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (TaskSettingsBase c in flowLayoutPanel1.Controls)
            {
                c.Stop();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (TaskSettingsBase c in flowLayoutPanel1.Controls)
            {
                c.RefreshInfo();
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.smtpServer = textBox4.Text;
            Properties.Settings.Default.Save();
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            int i = Properties.Settings.Default.smtpPort;
            int.TryParse(textBox5.Text, out i);
            Properties.Settings.Default.smtpPort = i;
            Properties.Settings.Default.Save();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.smtpUseSSL = checkBox1.Checked;
            Properties.Settings.Default.Save();
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.smtpAccountName = textBox7.Text;
            Properties.Settings.Default.Save();
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.smtpAccountPassword = textBox6.Text;
            Properties.Settings.Default.Save();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            EMail.SendEMail("globalcaching@gmail.com", "test subject", "test body");
        }
    }
}
