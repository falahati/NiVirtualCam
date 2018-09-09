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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using NiTEWrapper;
using NiUI.Properties;
using OpenNIWrapper;

namespace NiUI
{
    // ReSharper disable once InconsistentNaming
    public sealed partial class frm_Main
    {
        private readonly BitmapBroadcaster _broadcaster;

        private RectangleF _activePosition = new RectangleF(0, 0, 0, 0);

        private short _activeUserId;

        private Bitmap _bitmap;

        private Rectangle _currentCropping = Rectangle.Empty;

        private Device _currentDevice;

        private VideoStream _currentSensor;

        private HandTracker _handTracker;

        private int _noClientTicker;

        private bool _isHd;

        private bool _isIdle = true;

        private VideoFrameRef.CopyBitmapOptions _renderOptions;

        private bool _softMirror;

        private UserTracker _userTracker;

        public bool IsAutoRun { get; set; }

        // ReSharper disable once FlagArgument
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

        private static void RegisterFilter()
        {
            var filterAddress = Path.Combine(Environment.CurrentDirectory, "NiVirtualCamFilter.dll");

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
                var p = new Process
                {
                    StartInfo =
                        new ProcessStartInfo("regsvr32.exe", "/s \"" + filterAddress + "\"")
                };
                p.Start();
                p.WaitForExit();
            }
            catch
            {
                // ignored
            }
        }

        private void CurrentSensorOnNewFrame(VideoStream vStream)
        {
            try
            {
                if (vStream.IsValid && vStream.IsFrameAvailable() && !_isIdle)
                {
                    using (var frame = vStream.ReadFrame())
                    {
                        if (frame.IsValid)
                        {
                            lock (_bitmap)
                            {
                                try
                                {
                                    frame.UpdateBitmap(_bitmap, _renderOptions);
                                }
                                catch (Exception)
                                {
                                    _bitmap = frame.ToBitmap(_renderOptions);
                                }

                                var position = new Rectangle(new Point(0, 0), _bitmap.Size);

                                if (_currentCropping == Rectangle.Empty)
                                {
                                    _currentCropping = position;
                                }

                                if (Settings.Default.SmartCam)
                                {
                                    if (_activeUserId > 0)
                                    {
                                        position.X = (int)(_activePosition.X * _bitmap.Size.Width);
                                        position.Width = (int)(_activePosition.Width * _bitmap.Size.Width);
                                        position.Y = (int)(_activePosition.Y * _bitmap.Size.Height);
                                        position.Height = (int)(_activePosition.Height * _bitmap.Size.Height);

                                        position.Width =
                                            (int)
                                            ((decimal)_bitmap.Size.Width / _bitmap.Size.Height * position.Height);
                                        position.X -= position.Width / 2;

                                        position.X = Math.Max(position.X, 0);
                                        position.X = Math.Min(position.X, _bitmap.Size.Width - position.Width);
                                        position.Y = Math.Max(position.Y, 0);
                                        position.Y = Math.Min(position.Y, _bitmap.Size.Height - position.Height);
                                    }
                                }

                                if (_currentCropping != position)
                                {
                                    if (Math.Abs(position.X - _currentCropping.X) > 8 ||
                                        Math.Abs(position.Width - _currentCropping.Width) > 5)
                                    {
                                        _currentCropping.X += Math.Min(
                                            position.X - _currentCropping.X,
                                            _bitmap.Size.Width / 50);
                                        _currentCropping.Width += Math.Min(
                                            position.Width - _currentCropping.Width,
                                            _bitmap.Size.Width / 25);
                                    }

                                    if (Math.Abs(position.Y - _currentCropping.Y) > 8 ||
                                        Math.Abs(position.Height - _currentCropping.Height) > 5)
                                    {
                                        _currentCropping.Y += Math.Min(
                                            position.Y - _currentCropping.Y,
                                            _bitmap.Size.Height / 50);
                                        _currentCropping.Height +=
                                            Math.Min(
                                                position.Height - _currentCropping.Height,
                                                _bitmap.Size.Height / 25);
                                    }
                                }

                                if (_currentCropping.Size != _bitmap.Size)
                                {
                                    using (var g = Graphics.FromImage(_bitmap))
                                    {
                                        if (_currentCropping != Rectangle.Empty)
                                        {
                                            g.DrawImage(
                                                _bitmap,
                                                new Rectangle(new Point(0, 0), _bitmap.Size),
                                                _currentCropping,
                                                GraphicsUnit.Pixel);
                                        }

                                        g.Save();
                                    }
                                }

                                if (_softMirror)
                                {
                                    _bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                                }
                            }

                            if (!_isIdle)
                            {
                                _broadcaster.SendBitmap(_bitmap);
                            }

                            BeginInvoke(
                                (Action)delegate
                                {
                                    if (!_isIdle)
                                    {
                                        lock (_bitmap)
                                        {
                                            pb_image.Image?.Dispose();
                                            pb_image.Image = new Bitmap(_bitmap, pb_image.Size);
                                            pb_image.Refresh();
                                        }
                                    }
                                });
                        }
                    }
                }
            }
            catch
            {
                var newBitmap = new Bitmap(640, 480);
                if (!_isIdle)
                {
                    _broadcaster.SendBitmap(newBitmap);
                }
                BeginInvoke(
                    (Action)delegate
                    {
                        if (!_isIdle)
                        {
                            lock (_bitmap)
                            {
                                pb_image.Image?.Dispose();
                                pb_image.Image = newBitmap;
                                pb_image.Refresh();
                            }
                        }
                    });
            }
        }

        private void DeviceChanged()
        {
            cb_type.Items.Clear();
            gb_ir.Enabled = false;
            gb_color.Enabled = false;
            gb_depth.Enabled = false;
            cb_smart.Enabled = false;

            if (cb_device.SelectedItem is DeviceInfo deviceInfo)
            {
                var isNewDevice = _currentDevice == null || deviceInfo.Uri == _currentDevice.DeviceInfo.Uri;
                var newDevice = isNewDevice ? deviceInfo.OpenDevice() : _currentDevice;

                if (newDevice.HasSensor(Device.SensorType.Color))
                {
                    cb_type.Items.Add("Color");
                    gb_color.Enabled = true;
                }

                if (newDevice.HasSensor(Device.SensorType.Ir))
                {
                    cb_type.Items.Add("IR");
                    gb_ir.Enabled = true;
                }

                if (newDevice.HasSensor(Device.SensorType.Depth))
                {
                    cb_type.Items.Add("Depth");
                    gb_depth.Enabled = true;
                    cb_smart.Enabled = true;
                }

                if (cb_type.Items.Count < 0)
                {
                    cb_type.SelectedIndex = 0;
                }

                if (isNewDevice)
                {
                    newDevice.Close();
                }
            }
        }

        private void Init()
        {
            try
            {
                HandleError(OpenNI.Initialize());
                NiTE.Initialize();
                OpenNI.OnDeviceConnected += OpenNiOnDeviceConnectionStateChanged;
                OpenNI.OnDeviceDisconnected += OpenNiOnDeviceConnectionStateChanged;
                OpenNI.OnDeviceStateChanged += OpenNiOnDeviceStateChanged;
                UpdateDevicesList();
                notify.Visible = !Settings.Default.AutoNotification;
                ReadSettings();
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

        private void DoesNeedHalt()
        {
            if (_broadcaster != null)
            {
                if (_broadcaster.HasClient || Visible)
                {
                    _noClientTicker = 0;

                    if (_isIdle)
                    {
                        _broadcaster.SendBitmap(Resources.PleaseWait);

                        if (Start())
                        {
                            _isIdle = false;
                        }
                        else
                        {
                            _broadcaster.ClearScreen();
                        }
                    }
                }
                else
                {
                    _noClientTicker++;

                    if (_noClientTicker > 60 && !_isIdle) // 1min of no data
                    {
                        _isIdle = true;
                        Stop(false);
                    }
                }
            }
        }

        private void NiTeOnNewData(HandTracker handTracker)
        {
            try
            {
                if (Settings.Default.SmartCam &&
                    _userTracker != null &&
                    _userTracker.IsValid &&
                    _handTracker != null &&
                    _handTracker.IsValid)
                {
                    using (var userFrame = _userTracker.ReadFrame())
                    {
                        using (var handFrame = _handTracker.ReadFrame())
                        {
                            foreach (var gesture in handFrame.Gestures)
                            {
                                if (!gesture.IsComplete)
                                {
                                    continue;
                                }

                                var handPos = _handTracker.ConvertHandCoordinatesToDepth(gesture.CurrentPosition);
                                short userId =
                                    Marshal.ReadByte(
                                        userFrame.UserMap.Pixels +
                                        (int) (handPos.Y * userFrame.UserMap.DataStrideBytes) +
                                        (int) (handPos.X * 2));

                                if (userId > 0)
                                {
                                    _activeUserId = userId;
                                }
                            }
                        }

                        if (_activeUserId > 0)
                        {
                            var user = userFrame.GetUserById(_activeUserId);

                            if (user.IsValid && user.IsVisible && user.CenterOfMass.Z > 0)
                            {
                                var position = new RectangleF(0, 0, 0, 0);
                                var userLocation = _userTracker.ConvertJointCoordinatesToDepth(user.CenterOfMass);
                                var pSize =
                                    (int)
                                    (Math.Max((int) ((4700 - user.CenterOfMass.Z) * 0.08), 50) *
                                     ((float) userFrame.UserMap.FrameSize.Height / 480));
                                position.Y = (int) userLocation.Y - pSize;
                                position.Height = pSize;
                                position.X = (int) userLocation.X;
                                _activePosition.X = position.X / userFrame.UserMap.FrameSize.Width;
                                _activePosition.Width = position.Width / userFrame.UserMap.FrameSize.Width;
                                _activePosition.Y = position.Y / userFrame.UserMap.FrameSize.Height;
                                _activePosition.Height = position.Height / userFrame.UserMap.FrameSize.Height;

                                return;
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }

            _activeUserId = 0;
        }

        private void OpenNiOnDeviceConnectionStateChanged(DeviceInfo device)
        {
            BeginInvoke((Action) UpdateDevicesList);
        }

        private void OpenNiOnDeviceStateChanged(DeviceInfo device, OpenNI.DeviceState state)
        {
            BeginInvoke((Action) UpdateDevicesList);
        }

        private void ReadSettings()
        {
            cb_device.SelectedIndex = -1;

            if (!Settings.Default.DeviceURI.Equals(string.Empty))
            {
                foreach (var item in cb_device.Items)
                {
                    if (item is DeviceInfo &&
                        (item as DeviceInfo).Uri.Equals(
                            Settings.Default.DeviceURI,
                            StringComparison.CurrentCultureIgnoreCase))
                    {
                        cb_device.SelectedItem = item;
                    }
                }

                DeviceChanged();
                cb_type.SelectedIndex = -1;

                if (Settings.Default.CameraType != -1)
                {
                    foreach (var item in cb_type.Items)
                    {
                        if (Settings.Default.CameraType == 1 &&
                            item is string &&
                            (item as string).Equals("IR", StringComparison.CurrentCultureIgnoreCase))
                        {
                            cb_type.SelectedItem = item;
                        }

                        if (Settings.Default.CameraType == 2 &&
                            item is string &&
                            (item as string).Equals("Color", StringComparison.CurrentCultureIgnoreCase))
                        {
                            cb_type.SelectedItem = item;
                        }

                        if (Settings.Default.CameraType == 3 &&
                            item is string &&
                            (item as string).Equals("Depth", StringComparison.CurrentCultureIgnoreCase))
                        {
                            cb_type.SelectedItem = item;
                        }
                    }
                }
            }

            cb_notification.Checked = Settings.Default.AutoNotification;
            cb_hd.Checked = Settings.Default.Color_HD;
            cb_fill.Checked = Settings.Default.Depth_Fill;
            cb_equal.Checked = Settings.Default.Depth_Histogram;
            cb_invert.Checked = Settings.Default.Depth_Invert;
            cb_mirror.Checked = Settings.Default.Mirroring;
            cb_smart.Checked = Settings.Default.SmartCam;

            try
            {
                var registryKey =
                    Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");

                if (registryKey?.GetValue("OpenNI Virtual Webcam Server") != null)
                {
                    cb_startup.Checked = true;
                }
            }
            catch
            {
                // ignored
            }
        }

        private void SaveSettings()
        {
            Settings.Default.DeviceURI = "";

            if (cb_device.SelectedItem is DeviceInfo info && info.IsValid)
            {
                Settings.Default.DeviceURI = info.Uri;
            }

            Settings.Default.CameraType = -1;

            if (cb_type.SelectedItem is string selectedType)
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

            Settings.Default.AutoNotification = cb_notification.Checked;
            Settings.Default.Color_HD = cb_hd.Checked;
            Settings.Default.Depth_Fill = cb_fill.Checked;
            Settings.Default.Depth_Histogram = cb_equal.Checked;
            Settings.Default.Depth_Invert = cb_invert.Checked;
            Settings.Default.Mirroring = cb_mirror.Checked;
            Settings.Default.SmartCam = cb_smart.Checked;
            Settings.Default.Save();

            try
            {
                var registryKey =
                    Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

                if (registryKey != null)
                {
                    if (cb_startup.Checked)
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
            catch
            {
                // ignored
            }
        }

        private bool Start()
        {
            RegisterFilter();

            if (_isIdle && _broadcaster.HasServer())
            {
                MessageBox.Show(
                    @"Only one server is allowed.",
                    @"Multi-Server",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }

            var isSameDevice = _currentDevice != null &&
                               _currentDevice.IsValid &&
                               _currentDevice.DeviceInfo.Uri == Settings.Default.DeviceURI;
            var isSameSensor = isSameDevice &&
                               _currentSensor != null &&
                               _currentSensor.IsValid &&
                               _currentSensor.SensorInfo.GetSensorType() ==
                               (Device.SensorType) Settings.Default.CameraType;

            if (!isSameDevice)
            {
                if (Settings.Default.DeviceURI == string.Empty)
                {
                    _currentDevice = null;
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
                    _currentDevice = null;
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
                    _currentDevice = Device.Open(Settings.Default.DeviceURI);
                }
                catch (Exception ex)
                {
                    _currentDevice = null;
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
                    _currentSensor =
                        _currentDevice.CreateVideoStream((Device.SensorType) Settings.Default.CameraType);
                    _currentSensor.OnNewFrame += CurrentSensorOnNewFrame;
                }
                catch (Exception ex)
                {
                    _currentSensor = null;
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
                _currentSensor.Stop();
            }

            var videoModes = _currentSensor.SensorInfo.GetSupportedVideoModes().ToArray();
            VideoMode selectedVideoMode = null;

            switch (_currentSensor.SensorInfo.GetSensorType())
            {
                case Device.SensorType.Color:
                    _renderOptions = VideoFrameRef.CopyBitmapOptions.Force24BitRgb;

                    if (Settings.Default.Color_HD)
                    {
                        foreach (var vm in videoModes)
                        {
                            if (vm.Resolution.Width == 1280 &&
                                (vm.Resolution.Height == 960 || vm.Resolution.Height == 1024))
                            {
                                if ((selectedVideoMode == null ||
                                     selectedVideoMode.Fps < vm.Fps &&
                                     vm.DataPixelFormat < selectedVideoMode.DataPixelFormat) &&
                                    vm.DataPixelFormat != VideoMode.PixelFormat.Jpeg &&
                                    vm.DataPixelFormat != VideoMode.PixelFormat.Yuv422)
                                {
                                    selectedVideoMode = vm;
                                }
                            }
                        }

                        _isHd = selectedVideoMode != null;

                        if (!_isHd)
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
                        foreach (var vm in videoModes)
                        {
                            if (vm.Resolution.Width == 640 && vm.Resolution.Height == 480)
                            {
                                if ((selectedVideoMode == null ||
                                     selectedVideoMode.Fps < vm.Fps &&
                                     vm.DataPixelFormat < selectedVideoMode.DataPixelFormat) &&
                                    vm.DataPixelFormat != VideoMode.PixelFormat.Jpeg &&
                                    vm.DataPixelFormat != VideoMode.PixelFormat.Yuv422)
                                {
                                    selectedVideoMode = vm;
                                }
                            }
                        }
                    }

                    break;
                case Device.SensorType.Depth:
                    _renderOptions = VideoFrameRef.CopyBitmapOptions.Force24BitRgb |
                                    VideoFrameRef.CopyBitmapOptions.DepthFillShadow;

                    if (Settings.Default.Depth_Fill)
                    {
                        if (cb_mirror.Enabled && cb_mirror.Checked)
                        {
                            _renderOptions |= VideoFrameRef.CopyBitmapOptions.DepthFillRigthBlack;
                        }
                        else
                        {
                            _renderOptions |= VideoFrameRef.CopyBitmapOptions.DepthFillLeftBlack;
                        }
                    }

                    if (Settings.Default.Depth_Invert)
                    {
                        _renderOptions |= VideoFrameRef.CopyBitmapOptions.DepthInvert;
                    }

                    if (Settings.Default.Depth_Histogram)
                    {
                        _renderOptions |= VideoFrameRef.CopyBitmapOptions.DepthHistogramEqualize;
                    }

                    foreach (var vm in videoModes)
                    {
                        if (vm.Resolution.Width == 640 && vm.Resolution.Height == 480)
                        {
                            if ((selectedVideoMode == null || selectedVideoMode.Fps < vm.Fps) &&
                                (vm.DataPixelFormat == VideoMode.PixelFormat.Depth1Mm ||
                                 vm.DataPixelFormat == VideoMode.PixelFormat.Depth100Um))
                            {
                                selectedVideoMode = vm;
                            }
                        }
                    }

                    break;
                case Device.SensorType.Ir:
                    _renderOptions = VideoFrameRef.CopyBitmapOptions.Force24BitRgb;

                    foreach (var vm in videoModes)
                    {
                        if (vm.Resolution.Width == 640 && vm.Resolution.Height == 480)
                        {
                            if ((selectedVideoMode == null ||
                                 selectedVideoMode.Fps < vm.Fps &&
                                 vm.DataPixelFormat < selectedVideoMode.DataPixelFormat) &&
                                vm.DataPixelFormat != VideoMode.PixelFormat.Jpeg &&
                                vm.DataPixelFormat != VideoMode.PixelFormat.Yuv422)
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
                    if (_currentSensor.VideoMode.Fps != selectedVideoMode.Fps ||
                        _currentSensor.VideoMode.DataPixelFormat != selectedVideoMode.DataPixelFormat ||
                        _currentSensor.VideoMode.Resolution.Width != selectedVideoMode.Resolution.Width ||
                        _currentSensor.VideoMode.Resolution.Height != selectedVideoMode.Resolution.Height)
                    {
                        _currentSensor.VideoMode = selectedVideoMode;
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

            _softMirror = Settings.Default.Mirroring;

            if (Settings.Default.SmartCam)
            {
                try
                {
                    if (!isSameDevice || _userTracker == null || _handTracker == null || !_userTracker.IsValid || !_handTracker.IsValid)
                    {
                        _userTracker = UserTracker.Create(_currentDevice);
                        _handTracker = HandTracker.Create(_currentDevice);
                        _handTracker.StartGestureDetection(GestureData.GestureType.HandRaise);
                        _handTracker.OnNewData += NiTeOnNewData;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            if (!HandleError(_currentSensor.Start()))
            {
                Stop(false);

                return false;
            }

            btn_stopstart.Text = @"Stop Streaming";
            _isIdle = false;
            notify.Visible = true;

            return true;
        }

        private void Stop(bool shouldApply)
        {
            var isSameDevice = shouldApply &&
                               _currentDevice != null &&
                               _currentDevice.IsValid &&
                               _currentDevice.DeviceInfo.Uri == Settings.Default.DeviceURI;
            var isSameSensor = shouldApply &&
                               isSameDevice &&
                               _currentSensor != null &&
                               _currentSensor.IsValid &&
                               _currentSensor.SensorInfo.GetSensorType() ==
                               (Device.SensorType) Settings.Default.CameraType;

            if (!isSameSensor)
            {
                if (_currentSensor != null && _currentSensor.IsValid)
                {
                    _currentSensor.Stop();
                    _currentSensor.OnNewFrame -= CurrentSensorOnNewFrame;
                }

                _currentSensor = null;
            }

            if (!isSameDevice)
            {
                if (_currentDevice != null && _currentDevice.IsValid)
                {
                    _currentDevice.Close();
                }

                _currentDevice = null;
            }

            _isIdle = true;
            btn_stopstart.Text = @"Start Streaming";

            if (!shouldApply)
            {
                _broadcaster.ClearScreen();
                pb_image.Image = null;
                pb_image.Refresh();
            }

            if (Settings.Default.AutoNotification)
            {
                notify.Visible = false;
            }
        }

        private void UpdateDevicesList()
        {
            var devices = OpenNI.EnumerateDevices();
            cb_device.Items.Clear();

            if (devices.Length == 0)
            {
                cb_device.Items.Add("None");
            }

            var inList = false;

            foreach (var device in devices)
            {
                cb_device.Items.Add(device);

                if (device.Uri == Settings.Default.DeviceURI)
                {
                    inList = true;
                }
            }

            if (!inList)
            {
                Settings.Default.DeviceURI = string.Empty;
            }

            if (cb_device.SelectedIndex == -1)
            {
                cb_device.SelectedIndex = 0;
            }
        }
    }
}