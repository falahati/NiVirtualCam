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

using Vector2D = System.Windows.Vector;

namespace NiUI
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    using Microsoft.Win32;

    using NiTEWrapper;

    using NiUI.Properties;

    using OpenNIWrapper;

    // ReSharper disable once InconsistentNaming
    public partial class frm_Main
    {
        private readonly BitmapBroadcaster broadcaster;

        private RectangleF activePosition = new RectangleF(0, 0, 0, 0);

        private short activeUserId;

        private Bitmap bitmap;

        private Rectangle currentCropping = Rectangle.Empty;

        private Device currentDevice;

        private VideoStream currentSensor;

        private HandTracker hTracker;

        private int iNoClient;

        private bool isHd;

        private bool isIdle = true;

        private VideoFrameRef.CopyBitmapOptions renderOptions;

        private bool softMirror;

        private UserTracker uTracker;

        public bool IsAutoRun { get; set; }

        private static bool HandleError(OpenNI.Status status)
        {
            if (status == OpenNI.Status.Ok)
            {
                return true;
            }
            MessageBox.Show(
                string.Format("Error: {0} - {1}", status, OpenNI.LastError),
                @"Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Asterisk);
            return false;
        }

        private void UpdateDevicesList()
        {
            DeviceInfo[] devices = OpenNI.EnumerateDevices();
            this.cb_device.Items.Clear();
            if (devices.Length == 0)
            {
                this.cb_device.Items.Add("None");
            }
            bool inList = false;
            foreach (DeviceInfo device in devices)
            {
                this.cb_device.Items.Add(device);
                if (device.Uri == Settings.Default.DeviceURI)
                {
                    inList = true;
                }
            }
            if (!inList)
            {
                Settings.Default.DeviceURI = string.Empty;
            }
            if (this.cb_device.SelectedIndex == -1)
            {
                this.cb_device.SelectedIndex = 0;
            }
        }

        private static void RegisterFilter()
        {
            string filterAddress = Path.Combine(Environment.CurrentDirectory, "NiVirtualCamFilter.dll");
            if (!File.Exists(filterAddress))
            {
                MessageBox.Show(
                    @"NiVirtualCamFilter.dll has not been found. Please reinstall this program.",
                    @"Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Environment.Exit(1);
            }
            try
            {
                Process p = new Process
                                {
                                    StartInfo =
                                        new ProcessStartInfo("regsvr32.exe", "/s \"" + filterAddress + "\"")
                                };
                p.Start();
                p.WaitForExit();
            }
            catch (Exception)
            {
            }
        }

        private void Init()
        {
            try
            {
                HandleError(OpenNI.Initialize());
                NiTE.Initialize();
                OpenNI.OnDeviceConnected += this.OpenNiOnDeviceConnectionStateChanged;
                OpenNI.OnDeviceDisconnected += this.OpenNiOnDeviceConnectionStateChanged;
                OpenNI.OnDeviceStateChanged += this.OpenNiOnDeviceStateChanged;
                this.UpdateDevicesList();
                this.notify.Visible = !Settings.Default.AutoNotification;
                this.ReadSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Fatal Error: {0}", ex.Message),
                    @"Execution Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        private void DeviceChanged()
        {
            this.cb_type.Items.Clear();
            this.gb_ir.Enabled = false;
            this.gb_color.Enabled = false;
            this.gb_depth.Enabled = false;
            this.cb_smart.Enabled = false;
            DeviceInfo deviceInfo = this.cb_device.SelectedItem as DeviceInfo;
            if (deviceInfo != null)
            {
                bool isNewDevice = this.currentDevice == null || deviceInfo.Uri == this.currentDevice.DeviceInfo.Uri;
                Device newDevice = isNewDevice ? deviceInfo.OpenDevice() : this.currentDevice;
                if (newDevice.HasSensor(Device.SensorType.Color))
                {
                    this.cb_type.Items.Add("Color");
                    this.gb_color.Enabled = true;
                }
                if (newDevice.HasSensor(Device.SensorType.Ir))
                {
                    this.cb_type.Items.Add("IR");
                    this.gb_ir.Enabled = true;
                }
                if (newDevice.HasSensor(Device.SensorType.Depth))
                {
                    this.cb_type.Items.Add("Depth");
                    this.gb_depth.Enabled = true;
                    this.cb_smart.Enabled = true;
                }
                if (this.cb_type.Items.Count < 0)
                {
                    this.cb_type.SelectedIndex = 0;
                }
                if (isNewDevice)
                {
                    newDevice.Close();
                }
            }
        }

        private void IsNeedHalt()
        {
            if (this.broadcaster != null)
            {
                if (this.broadcaster.HasClient || this.Visible)
                {
                    this.iNoClient = 0;
                    if (this.isIdle)
                    {
                        this.broadcaster.SendBitmap(Resources.PleaseWait);
                        if (this.Start())
                        {
                            this.isIdle = false;
                        }
                        else
                        {
                            this.broadcaster.ClearScreen();
                        }
                    }
                }
                else
                {
                    this.iNoClient++;
                    if (this.iNoClient > 60 && !this.isIdle) // 1min of no data
                    {
                        this.isIdle = true;
                        this.Stop(false);
                    }
                }
            }
        }

        private void ReadSettings()
        {
            this.cb_device.SelectedIndex = -1;
            if (!Settings.Default.DeviceURI.Equals(string.Empty))
            {
                foreach (object item in this.cb_device.Items)
                {
                    if (item is DeviceInfo
                        && (item as DeviceInfo).Uri.Equals(
                            Settings.Default.DeviceURI,
                            StringComparison.CurrentCultureIgnoreCase))
                    {
                        this.cb_device.SelectedItem = item;
                    }
                }
                this.DeviceChanged();
                this.cb_type.SelectedIndex = -1;
                if (Settings.Default.CameraType != -1)
                {
                    foreach (object item in this.cb_type.Items)
                    {
                        if (Settings.Default.CameraType == 1 && item is string
                            && (item as string).Equals("IR", StringComparison.CurrentCultureIgnoreCase))
                        {
                            this.cb_type.SelectedItem = item;
                        }
                        if (Settings.Default.CameraType == 2 && item is string
                            && (item as string).Equals("Color", StringComparison.CurrentCultureIgnoreCase))
                        {
                            this.cb_type.SelectedItem = item;
                        }
                        if (Settings.Default.CameraType == 3 && item is string
                            && (item as string).Equals("Depth", StringComparison.CurrentCultureIgnoreCase))
                        {
                            this.cb_type.SelectedItem = item;
                        }
                    }
                }
            }
            this.cb_notification.Checked = Settings.Default.AutoNotification;
            this.cb_hd.Checked = Settings.Default.Color_HD;
            this.cb_fill.Checked = Settings.Default.Depth_Fill;
            this.cb_equal.Checked = Settings.Default.Depth_Histogram;
            this.cb_invert.Checked = Settings.Default.Depth_Invert;
            this.cb_mirror.Checked = Settings.Default.Mirroring;
            this.cb_smart.Checked = Settings.Default.SmartCam;
            try
            {
                RegistryKey registryKey =
                    Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                if (registryKey != null && registryKey.GetValue("OpenNI Virtual Webcam Server") != null)
                {
                    this.cb_startup.Checked = true;
                }
            }
            catch (Exception)
            {
            }
        }

        private void SaveSettings()
        {
            Settings.Default.DeviceURI = "";
            if (this.cb_device.SelectedItem is DeviceInfo && ((DeviceInfo)this.cb_device.SelectedItem).IsValid)
            {
                Settings.Default.DeviceURI = (this.cb_device.SelectedItem as DeviceInfo).Uri;
            }
            Settings.Default.CameraType = -1;
            string selectedType = this.cb_type.SelectedItem as string;
            if (selectedType != null)
            {
                switch (selectedType)
                {
                    case "Color":
                        Settings.Default.CameraType = 2;
                        break;
                    case "Depth":
                        Settings.Default.CameraType = 3;
                        break;
                    case "IR":
                        Settings.Default.CameraType = 1;
                        break;
                }
            }
            Settings.Default.AutoNotification = this.cb_notification.Checked;
            Settings.Default.Color_HD = this.cb_hd.Checked;
            Settings.Default.Depth_Fill = this.cb_fill.Checked;
            Settings.Default.Depth_Histogram = this.cb_equal.Checked;
            Settings.Default.Depth_Invert = this.cb_invert.Checked;
            Settings.Default.Mirroring = this.cb_mirror.Checked;
            Settings.Default.SmartCam = this.cb_smart.Checked;
            Settings.Default.Save();
            try
            {
                RegistryKey registryKey =
                    Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (registryKey != null)
                {
                    if (this.cb_startup.Checked)
                    {
                        registryKey.SetValue(
                            "OpenNI Virtual Webcam Server",
                            "\"" + Process.GetCurrentProcess().MainModule.FileName + "\" /autoRun");
                    }
                    else
                    {
                        registryKey.DeleteValue("OpenNI Virtual Webcam Server");
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void Stop(bool isApply)
        {
            bool isSameDevice = isApply && this.currentDevice != null && this.currentDevice.IsValid
                                && this.currentDevice.DeviceInfo.Uri == Settings.Default.DeviceURI;
            bool isSameSensor = isApply && isSameDevice && this.currentSensor != null && this.currentSensor.IsValid
                                && this.currentSensor.SensorInfo.GetSensorType()
                                == (Device.SensorType)Settings.Default.CameraType;
            if (!isSameSensor)
            {
                if (this.currentSensor != null && this.currentSensor.IsValid)
                {
                    this.currentSensor.Stop();
                    this.currentSensor.OnNewFrame -= this.CurrentSensorOnNewFrame;
                }
                this.currentSensor = null;
            }
            if (!isSameDevice)
            {
                if (this.currentDevice != null && this.currentDevice.IsValid)
                {
                    this.currentDevice.Close();
                }
                this.currentDevice = null;
            }
            this.isIdle = true;
            this.btn_stopstart.Text = @"Start Streaming";
            if (!isApply)
            {
                this.broadcaster.ClearScreen();
                this.pb_image.Image = null;
                this.pb_image.Refresh();
            }
            if (Settings.Default.AutoNotification)
            {
                this.notify.Visible = false;
            }
        }

        private bool Start()
        {
            RegisterFilter();
            if (this.isIdle && this.broadcaster.HasServer())
            {
                MessageBox.Show(
                    @"Only one server is allowed.",
                    @"Multi-Server",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
            bool isSameDevice = this.currentDevice != null && this.currentDevice.IsValid
                                && this.currentDevice.DeviceInfo.Uri == Settings.Default.DeviceURI;
            bool isSameSensor = isSameDevice && this.currentSensor != null && this.currentSensor.IsValid
                                && this.currentSensor.SensorInfo.GetSensorType()
                                == (Device.SensorType)Settings.Default.CameraType;
            if (!isSameDevice)
            {
                if (Settings.Default.DeviceURI == string.Empty)
                {
                    this.currentDevice = null;
                    MessageBox.Show(
                        @"Please select a device to open and then click Apply.",
                        @"Device Open",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }
            }
            if (!isSameSensor)
            {
                if (Settings.Default.CameraType == -1)
                {
                    this.currentDevice = null;
                    MessageBox.Show(
                        @"Please select a sensor to open and then click Apply.",
                        @"Sensor Create",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }
            }
            if (!isSameDevice)
            {
                try
                {
                    this.currentDevice = Device.Open(Settings.Default.DeviceURI);
                }
                catch (Exception ex)
                {
                    this.currentDevice = null;
                    MessageBox.Show(
                        string.Format("Can not open selected Device. {0}", ex.Message),
                        @"Device Open",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }
            }
            if (!isSameSensor)
            {
                try
                {
                    this.currentSensor =
                        this.currentDevice.CreateVideoStream((Device.SensorType)Settings.Default.CameraType);
                    this.currentSensor.OnNewFrame += this.CurrentSensorOnNewFrame;
                }
                catch (Exception ex)
                {
                    this.currentSensor = null;
                    MessageBox.Show(
                        string.Format("Can not open selected Sensor from selected Device. {0}", ex.Message),
                        @"Sensor Create",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }
            }
            else
            {
                this.currentSensor.Stop();
            }
            VideoMode[] vmodes = this.currentSensor.SensorInfo.GetSupportedVideoModes().ToArray();
            VideoMode selectedVideoMode = null;
            switch (this.currentSensor.SensorInfo.GetSensorType())
            {
                case Device.SensorType.Color:
                    this.renderOptions = VideoFrameRef.CopyBitmapOptions.Force24BitRgb;
                    if (Settings.Default.Color_HD)
                    {
                        foreach (VideoMode vm in vmodes)
                        {
                            if (vm.Resolution.Width == 1280
                                && (vm.Resolution.Height == 960 || vm.Resolution.Height == 1024))
                            {
                                if ((selectedVideoMode == null
                                     || (selectedVideoMode.Fps < vm.Fps
                                         && vm.DataPixelFormat < selectedVideoMode.DataPixelFormat))
                                    && vm.DataPixelFormat != VideoMode.PixelFormat.Jpeg
                                    && vm.DataPixelFormat != VideoMode.PixelFormat.Yuv422)
                                {
                                    selectedVideoMode = vm;
                                }
                            }
                        }
                        this.isHd = selectedVideoMode != null;
                        if (!this.isHd)
                        {
                            MessageBox.Show(
                                @"This device doesn't support ~1.3MP resolution.",
                                @"HD Resolution",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                    }
                    if (selectedVideoMode == null)
                    {
                        foreach (VideoMode vm in vmodes)
                        {
                            if (vm.Resolution == new Size(640, 480))
                            {
                                if ((selectedVideoMode == null
                                     || (selectedVideoMode.Fps < vm.Fps
                                         && vm.DataPixelFormat < selectedVideoMode.DataPixelFormat))
                                    && vm.DataPixelFormat != VideoMode.PixelFormat.Jpeg
                                    && vm.DataPixelFormat != VideoMode.PixelFormat.Yuv422)
                                {
                                    selectedVideoMode = vm;
                                }
                            }
                        }
                    }
                    break;
                case Device.SensorType.Depth:
                    this.renderOptions = VideoFrameRef.CopyBitmapOptions.Force24BitRgb
                                         | VideoFrameRef.CopyBitmapOptions.DepthFillShadow;
                    if (Settings.Default.Depth_Fill)
                    {
                        if (this.cb_mirror.Enabled && this.cb_mirror.Checked)
                        {
                            this.renderOptions |= VideoFrameRef.CopyBitmapOptions.DepthFillRigthBlack;
                        }
                        else
                        {
                            this.renderOptions |= VideoFrameRef.CopyBitmapOptions.DepthFillLeftBlack;
                        }
                    }
                    if (Settings.Default.Depth_Invert)
                    {
                        this.renderOptions |= VideoFrameRef.CopyBitmapOptions.DepthInvert;
                    }
                    if (Settings.Default.Depth_Histogram)
                    {
                        this.renderOptions |= VideoFrameRef.CopyBitmapOptions.DepthHistogramEqualize;
                    }
                    foreach (VideoMode vm in vmodes)
                    {
                        if (vm.Resolution == new Size(640, 480))
                        {
                            if ((selectedVideoMode == null || selectedVideoMode.Fps < vm.Fps)
                                && (vm.DataPixelFormat == VideoMode.PixelFormat.Depth1Mm
                                    || vm.DataPixelFormat == VideoMode.PixelFormat.Depth100Um))
                            {
                                selectedVideoMode = vm;
                            }
                        }
                    }
                    break;
                case Device.SensorType.Ir:
                    this.renderOptions = VideoFrameRef.CopyBitmapOptions.Force24BitRgb;
                    foreach (VideoMode vm in vmodes)
                    {
                        if (vm.Resolution == new Size(640, 480))
                        {
                            if ((selectedVideoMode == null
                                 || (selectedVideoMode.Fps < vm.Fps
                                     && vm.DataPixelFormat < selectedVideoMode.DataPixelFormat))
                                && vm.DataPixelFormat != VideoMode.PixelFormat.Jpeg
                                && vm.DataPixelFormat != VideoMode.PixelFormat.Yuv422)
                            {
                                selectedVideoMode = vm;
                            }
                        }
                    }
                    break;
            }

            if (selectedVideoMode != null)
            {
                try
                {
                    if (this.currentSensor.VideoMode.Fps != selectedVideoMode.Fps
                        || this.currentSensor.VideoMode.DataPixelFormat != selectedVideoMode.DataPixelFormat
                        || this.currentSensor.VideoMode.Resolution != selectedVideoMode.Resolution)
                    {
                        this.currentSensor.VideoMode = selectedVideoMode;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        string.Format("Can not set active video mode to {0}. {1}", selectedVideoMode, ex.Message),
                        @"Sensor Config",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }
            }
            else
            {
                MessageBox.Show(
                    @"No acceptable video mode found.",
                    @"Sensor Config",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
            this.softMirror = Settings.Default.Mirroring;
            if (Settings.Default.SmartCam)
            {
                try
                {
                    if (!isSameDevice
                        || (this.uTracker == null || this.hTracker == null || !this.uTracker.IsValid
                            || !this.hTracker.IsValid))
                    {
                        this.uTracker = UserTracker.Create(this.currentDevice);
                        this.hTracker = HandTracker.Create(this.currentDevice);
                        this.hTracker.StartGestureDetection(GestureData.GestureType.HandRaise);
                        this.hTracker.OnNewData += this.NiTeOnNewData;
                    }
                }
                catch (Exception)
                {
                }
            }
            if (!HandleError(this.currentSensor.Start()))
            {
                this.Stop(false);
                return false;
            }
            this.btn_stopstart.Text = @"Stop Streaming";
            this.isIdle = false;
            this.notify.Visible = true;
            return true;
        }

        private void NiTeOnNewData(HandTracker handTracker)
        {
            try
            {
                if (Settings.Default.SmartCam && this.uTracker != null && this.uTracker.IsValid && this.hTracker != null
                    && this.hTracker.IsValid)
                {
                    using (UserTrackerFrameRef userframe = this.uTracker.ReadFrame())
                    {
                        using (HandTrackerFrameRef handframe = this.hTracker.ReadFrame())
                        {
                            foreach (GestureData gesture in handframe.Gestures)
                            {
                                if (!gesture.IsComplete)
                                {
                                    continue;
                                }
                                PointF handPos = this.hTracker.ConvertHandCoordinatesToDepth(gesture.CurrentPosition);
                                short userId =
                                    Marshal.ReadByte(
                                        userframe.UserMap.Pixels + (int)(handPos.Y * userframe.UserMap.DataStrideBytes)
                                        + (int)(handPos.X * 2));
                                if (userId > 0)
                                {
                                    this.activeUserId = userId;
                                }
                            }
                            handframe.Release();
                        }
                        if (this.activeUserId > 0)
                        {
                            UserData user = userframe.GetUserById(this.activeUserId);
                            if (user.IsValid && user.IsVisible && user.CenterOfMass.Z > 0)
                            {
                                RectangleF position = new RectangleF(0, 0, 0, 0);
                                PointF botlocation = this.uTracker.ConvertJointCoordinatesToDepth(user.CenterOfMass);
                                int pSize =
                                    (int)
                                    (Math.Max((int)((4700 - user.CenterOfMass.Z) * 0.08), 50)
                                     * ((float)userframe.UserMap.FrameSize.Height / 480));
                                position.Y = (int)botlocation.Y - pSize;
                                position.Height = pSize;
                                position.X = (int)botlocation.X;
                                this.activePosition.X = position.X / userframe.UserMap.FrameSize.Width;
                                this.activePosition.Width = position.Width / userframe.UserMap.FrameSize.Width;
                                this.activePosition.Y = position.Y / userframe.UserMap.FrameSize.Height;
                                this.activePosition.Height = position.Height / userframe.UserMap.FrameSize.Height;
                                userframe.Release();
                                return;
                            }
                        }
                        userframe.Release();
                    }
                }
            }
            catch (Exception)
            {
            }
            this.activeUserId = 0;
        }

        private void OpenNiOnDeviceStateChanged(DeviceInfo device, OpenNI.DeviceState state)
        {
            this.BeginInvoke((Action)this.UpdateDevicesList);
        }

        private void OpenNiOnDeviceConnectionStateChanged(DeviceInfo device)
        {
            this.BeginInvoke((Action)this.UpdateDevicesList);
        }

        private void CurrentSensorOnNewFrame(VideoStream vStream)
        {
            if (vStream.IsValid && vStream.IsFrameAvailable() && !this.isIdle)
            {
                using (VideoFrameRef frame = vStream.ReadFrame())
                {
                    if (frame.IsValid)
                    {
                        lock (this.bitmap)
                        {
                            try
                            {
                                frame.UpdateBitmap(this.bitmap, this.renderOptions);
                            }
                            catch (Exception)
                            {
                                this.bitmap = frame.ToBitmap(this.renderOptions);
                            }
                        }
                        Rectangle position = new Rectangle(new Point(0, 0), this.bitmap.Size);
                        if (this.currentCropping == Rectangle.Empty)
                        {
                            this.currentCropping = position;
                        }
                        if (Settings.Default.SmartCam)
                        {
                            if (this.activeUserId > 0)
                            {
                                position.X = (int)(this.activePosition.X * this.bitmap.Size.Width);
                                position.Width = (int)(this.activePosition.Width * this.bitmap.Size.Width);
                                position.Y = (int)(this.activePosition.Y * this.bitmap.Size.Height);
                                position.Height = (int)(this.activePosition.Height * this.bitmap.Size.Height);

                                position.Width =
                                    (int)(((Decimal)this.bitmap.Size.Width / this.bitmap.Size.Height) * position.Height);
                                position.X -= (position.Width / 2);

                                position.X = Math.Max(position.X, 0);
                                position.X = Math.Min(position.X, this.bitmap.Size.Width - position.Width);
                                position.Y = Math.Max(position.Y, 0);
                                position.Y = Math.Min(position.Y, this.bitmap.Size.Height - position.Height);
                            }
                        }
                        if (this.currentCropping != position)
                        {
                            if (Math.Abs(position.X - this.currentCropping.X) > 8
                                || Math.Abs(position.Width - this.currentCropping.Width) > 5)
                            {
                                this.currentCropping.X += Math.Min(
                                    position.X - this.currentCropping.X,
                                    this.bitmap.Size.Width / 50);
                                this.currentCropping.Width += Math.Min(
                                    position.Width - this.currentCropping.Width,
                                    this.bitmap.Size.Width / 25);
                            }
                            if (Math.Abs(position.Y - this.currentCropping.Y) > 8
                                || Math.Abs(position.Height - this.currentCropping.Height) > 5)
                            {
                                this.currentCropping.Y += Math.Min(
                                    position.Y - this.currentCropping.Y,
                                    this.bitmap.Size.Height / 50);
                                this.currentCropping.Height += Math.Min(
                                    position.Height - this.currentCropping.Height,
                                    this.bitmap.Size.Height / 25);
                            }
                        }
                        lock (this.bitmap)
                        {
                            if (this.currentCropping.Size != this.bitmap.Size)
                            {
                                using (Graphics g = Graphics.FromImage(this.bitmap))
                                {
                                    if (this.currentCropping != Rectangle.Empty)
                                    {
                                        g.DrawImage(
                                            this.bitmap,
                                            new Rectangle(new Point(0, 0), this.bitmap.Size),
                                            this.currentCropping,
                                            GraphicsUnit.Pixel);
                                    }
                                    g.Save();
                                }
                            }
                            if (this.softMirror)
                            {
                                this.bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                            }
                        }

                        if (!this.isIdle)
                        {
                            this.broadcaster.SendBitmap(this.bitmap);
                        }
                        this.BeginInvoke(
                            (Action)delegate
                                {
                                    if (!this.isIdle)
                                    {
                                        lock (this.bitmap)
                                        {
                                            if (this.pb_image.Image != null)
                                            {
                                                this.pb_image.Image.Dispose();
                                            }
                                            this.pb_image.Image = new Bitmap(this.bitmap, this.pb_image.Size);
                                            this.pb_image.Refresh();
                                        }
                                    }
                                });
                    }
                }
            }
        }
    }
}