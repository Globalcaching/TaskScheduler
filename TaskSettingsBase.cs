using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskScheduler
{
    public partial class TaskSettingsBase : UserControl
    {
        private TaskBase _task;

        public TaskSettingsBase()
        {
            InitializeComponent();
        }

        public TaskSettingsBase(TaskBase tsk)
        {
            InitializeComponent();

            _task = tsk;
            groupBox1.Text = tsk.ServiceInfo.Description;
            UpdateControl();
        }

        public void UpdateControl()
        {
            lock (_task.ServiceInfo)
            {
                checkBox1.Checked = _task.ServiceInfo.Enabled;
                checkBox2.Checked = _task.Busy;
                checkBox3.Checked = _task.ServiceInfo.RunAfter.HasValue && _task.ServiceInfo.RunBefore.HasValue;
                if (_task.ServiceInfo.RunAfter.HasValue)
                {
                    dateTimePicker1.Value = (DateTime)_task.ServiceInfo.RunAfter;
                }
                if (_task.ServiceInfo.RunBefore.HasValue)
                {
                    dateTimePicker2.Value = (DateTime)_task.ServiceInfo.RunBefore;
                }
                checkBox4.Checked = _task.ServiceInfo.ErrorInLastRun;
                numericUpDown1.Value = _task.ServiceInfo.Interval.Hour;
                numericUpDown2.Value = _task.ServiceInfo.Interval.Minute;
                numericUpDown3.Value = _task.ServiceInfo.Interval.Second;
                if (_task.ServiceInfo.LastRun.HasValue)
                {
                    dateTimePicker3.Value = (DateTime)_task.ServiceInfo.LastRun;
                }
                textBox1.Text = _task.Details ?? "";
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            UpdateControl();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _task.ServiceStop();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _task.ServiceStart();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _task.ServiceInfo.Enabled = checkBox1.Checked;
            if (checkBox3.Checked)
            {
                _task.ServiceInfo.RunAfter = dateTimePicker1.Value;
                _task.ServiceInfo.RunBefore = dateTimePicker2.Value;
            }
            else
            {
                _task.ServiceInfo.RunAfter = null;
                _task.ServiceInfo.RunBefore = null;
            }
            _task.ServiceInfo.Interval = new DateTime(2000, 1, 1, (int)numericUpDown1.Value, (int)numericUpDown2.Value, (int)numericUpDown3.Value, 0);
            _task.UpdateServiceInfo();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            _task.ServiceRunNow();
        }
    }
}
