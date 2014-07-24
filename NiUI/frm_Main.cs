/*  
    Copyright (C) 2013  Soroush Falahati - soroush@falahati.net

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see [http://www.gnu.org/licenses/].
    */

namespace NiUI
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows.Forms;

    using NiTEWrapper;

    using NiUI.Properties;

    using OpenNIWrapper;

    // ReSharper disable once InconsistentNaming
    public partial class frm_Main : Form
    {
        public frm_Main()
        {
            this.InitializeComponent();
            base.Text += string.Format(" v{0}", Application.ProductVersion);
            this.bitmap = new Bitmap(1, 1);
            this.broadcaster = new BitmapBroadcaster();
        }

        private void DeviceSelectedIndexChanged(object sender, EventArgs e)
        {
            this.DeviceChanged();
        }

        private void ApplyClick(object sender, EventArgs e)
        {
            // Save Settings
            this.SaveSettings();
            // Apply Settings
            this.broadcaster.SendBitmap(Resources.PleaseWait);
            this.Stop(true);
            this.halt_timer.Stop();
            if (this.Start())
            {
                this.iNoClient = 0;
                this.halt_timer.Start();
            }
            else
            {
                this.broadcaster.ClearScreen();
            }
        }

        private void FrmMainFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.halt_timer.Enabled)
            {
                e.Cancel =
                    MessageBox.Show(
                        @"Closing this form when you stopped streaming video to applications, will close this program completely. Are you sure?!",
                        @"Closing Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) != DialogResult.Yes;
                return;
            }
            this.Visible = false;
            e.Cancel = true;
        }

        private void FrmMainFormClosed(object sender, FormClosedEventArgs e)
        {
            OpenNI.Shutdown();
            NiTE.Shutdown();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            this.IsNeedHalt();
        }

        private void CopyrightLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://falahati.net");
        }

        private void StopStartClick(object sender, EventArgs e)
        {
            if (this.isIdle)
            {
                this.broadcaster.SendBitmap(Resources.PleaseWait);
                if (this.Start())
                {
                    this.iNoClient = 0;
                    this.halt_timer.Start();
                }
                else
                {
                    this.broadcaster.ClearScreen();
                }
            }
            else
            {
                this.Stop(false);
                this.halt_timer.Stop();
            }
        }

        private void NotifyMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!this.Visible)
                {
                    this.Visible = true;
                }
                this.Activate();
            }
        }

        private void FrmMainShown(object sender, EventArgs e)
        {
            this.lbl_wait.Dock = DockStyle.Fill;
            this.Enabled = false;
            Application.DoEvents();
            this.broadcaster.SendBitmap(Resources.PleaseWait);
            this.Init();
            if (this.isIdle && this.cb_device.SelectedIndex != -1 && this.cb_type.SelectedIndex != -1)
            {
                if (this.Start())
                {
                    this.iNoClient = 0;
                    this.halt_timer.Start();
                }
                else
                {
                    this.broadcaster.ClearScreen();
                }
            }
            else
            {
                this.broadcaster.ClearScreen();
            }
            if (!this.isIdle)
            {
                if (this.IsAutoRun)
                {
                    this.Visible = false;
                }
            }
            this.lbl_wait.Visible = false;
            this.Enabled = true;
            Application.DoEvents();
        }
    }
}