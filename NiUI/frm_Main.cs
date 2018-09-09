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

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using NiTEWrapper;
using NiUI.Properties;
using OpenNIWrapper;

namespace NiUI
{
    // ReSharper disable once InconsistentNaming
    public sealed partial class frm_Main : Form
    {
        public frm_Main()
        {
            InitializeComponent();
            Text += string.Format(" v{0}", Application.ProductVersion);
            _bitmap = new Bitmap(1, 1);
            _broadcaster = new BitmapBroadcaster();
        }

        private void ApplyClick(object sender, EventArgs e)
        {
            // Save Settings
            SaveSettings();
            // Apply Settings
            _broadcaster.SendBitmap(Resources.PleaseWait);
            Stop(true);
            halt_timer.Stop();

            if (Start())
            {
                _noClientTicker = 0;
                halt_timer.Start();
            }
            else
            {
                _broadcaster.ClearScreen();
            }
        }

        private void CopyrightLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://falahati.net");
        }

        private void DeviceSelectedIndexChanged(object sender, EventArgs e)
        {
            DeviceChanged();
        }

        private void FrmMainFormClosed(object sender, FormClosedEventArgs e)
        {
            OpenNI.Shutdown();
            NiTE.Shutdown();
        }

        private void FrmMainFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!halt_timer.Enabled)
            {
                e.Cancel =
                    MessageBox.Show(
                        @"Closing this form when you stopped streaming video to applications, will close this program completely. Are you sure?!",
                        @"Closing Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) !=
                    DialogResult.Yes;

                return;
            }

            Visible = false;
            e.Cancel = true;
        }

        private void FrmMainShown(object sender, EventArgs e)
        {
            lbl_wait.Dock = DockStyle.Fill;
            Enabled = false;
            Application.DoEvents();
            _broadcaster.SendBitmap(Resources.PleaseWait);
            Init();

            if (_isIdle && cb_device.SelectedIndex != -1 && cb_type.SelectedIndex != -1)
            {
                if (Start())
                {
                    _noClientTicker = 0;
                    halt_timer.Start();
                }
                else
                {
                    _broadcaster.ClearScreen();
                }
            }
            else
            {
                _broadcaster.ClearScreen();
            }

            if (!_isIdle)
            {
                if (IsAutoRun)
                {
                    Visible = false;
                }
            }

            lbl_wait.Visible = false;
            Enabled = true;
            Application.DoEvents();
        }

        private void NotifyMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!Visible)
                {
                    Visible = true;
                }

                Activate();
            }
        }

        private void StopStartClick(object sender, EventArgs e)
        {
            if (_isIdle)
            {
                _broadcaster.SendBitmap(Resources.PleaseWait);

                if (Start())
                {
                    _noClientTicker = 0;
                    halt_timer.Start();
                }
                else
                {
                    _broadcaster.ClearScreen();
                }
            }
            else
            {
                Stop(false);
                halt_timer.Stop();
            }
        }

        private void TimerTick(object sender, EventArgs e)
        {
            DoesNeedHalt();
        }
    }
}