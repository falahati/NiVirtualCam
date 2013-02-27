using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenNIWrapper;

namespace NiUI
{
    public partial class frm_Main : Form
    {

        public frm_Main()
        {
            InitializeComponent();
            this.Text += " v" + Application.ProductVersion.ToString();
            bitmap = new Bitmap(1, 1);
        }

        private void cb_device_SelectedIndexChanged(object sender, EventArgs e)
        {
            DeviceChanged();
        }

        private void btn_apply_Click(object sender, EventArgs e)
        {
            // Save Settings
            SaveSettings();
            // Apply Settings
            Stop(true);
            halt_timer.Stop();
            if (Start())
            {
                iNoClient = 0;
                halt_timer.Start();
            }
        }

        private void frm_Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!halt_timer.Enabled)
            {
                e.Cancel = MessageBox.Show("Closing this form when you stopped streaming video to applications, will close this program completely. Are you sure?!", "Closing Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != System.Windows.Forms.DialogResult.Yes;
                return;
            }
            this.Visible = false;
            e.Cancel = true;
        }

        private void frm_Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            OpenNI.Shutdown();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            IsNeedHalt();
        }

        private void l_copyright_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://falahati.net");
        }

        private void btn_stopstart_Click(object sender, EventArgs e)
        {
            if (isIdle)
            {
                if (Start())
                {
                    iNoClient = 0;
                    halt_timer.Start();
                }
            }
            else
            {
                Stop(false);
                halt_timer.Stop();
            }
        }

        private void notify_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (!this.Visible)
                    this.Visible = true;
                this.Activate();
            }
        }

        private void frm_Main_Shown(object sender, EventArgs e)
        {
            if (!isIdle)
            {
                if (IsAutoRun)
                    this.Visible = false;
            }
        }

        private void frm_Main_Load(object sender, EventArgs e)
        {
            Init();
            if (isIdle && cb_device.SelectedIndex != -1 && cb_type.SelectedIndex != -1)
            {
                if (Start())
                {
                    iNoClient = 0;
                    halt_timer.Start();
                }
            }
        }
    }
}
