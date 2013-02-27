using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenNIWrapper;
using System.Drawing;

namespace NiUI
{
    public partial class frm_Main
    {
        bool isHD = false;
        int iNoClient;
        bool isIdle = true;
        VideoFrameRef.copyBitmapOptions renderOptions;
        Device currentDevice;
        VideoStream currentSensor;
        Bitmap bitmap;
        BitmapBroadcaster broadcaster;
        public bool IsAutoRun { get; set; }
        private bool HandleError(OpenNI.Status status)
        {
            if (status == OpenNI.Status.OK)
                return true;
            MessageBox.Show("Error: " + status.ToString() + " - " + OpenNI.LastError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            return false;
        }

        private void UpdateDevicesList()
        {
            DeviceInfo[] devices = OpenNI.EnumerateDevices();
            cb_device.Items.Clear();
            if (devices.Length == 0)
                cb_device.Items.Add("None");
            for (int i = 0; i < devices.Length; i++)
                cb_device.Items.Add(devices[i]);
            if (cb_device.SelectedIndex == -1)
                cb_device.SelectedIndex = 0;
        }

        void Init()
        {
            try
            {
                broadcaster = new BitmapBroadcaster();
                HandleError(OpenNI.Initialize());
                OpenNI.onDeviceConnected += new OpenNI.DeviceConnectionStateChanged(OpenNI_onDeviceConnectionStateChanged);
                OpenNI.onDeviceDisconnected += new OpenNI.DeviceConnectionStateChanged(OpenNI_onDeviceConnectionStateChanged);
                OpenNI.onDeviceStateChanged += new OpenNI.DeviceStateChanged(OpenNI_onDeviceStateChanged);
                UpdateDevicesList();
                notify.Visible = !NiUI.Properties.Settings.Default.AutoNotification;
                ReadSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal Error: " + ex.Message, "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        void DeviceChanged()
        {
            cb_type.Items.Clear();
            gb_ir.Enabled = false;
            gb_color.Enabled = false;
            gb_depth.Enabled = false;
            cb_smart.Enabled = false;
            if (cb_device.SelectedItem != null && cb_device.SelectedItem is DeviceInfo)
            {
                Device newDevice;
                bool isNewDevice = currentDevice == null || ((DeviceInfo)cb_device.SelectedItem).URI == currentDevice.DeviceInfo.URI;
                if (isNewDevice)
                    newDevice = ((DeviceInfo)cb_device.SelectedItem).OpenDevice();
                else
                    newDevice = currentDevice;
                if (newDevice.hasSensor(Device.SensorType.COLOR))
                {
                    cb_type.Items.Add("Color");
                    gb_color.Enabled = true;
                }
                if (newDevice.hasSensor(Device.SensorType.IR))
                {
                    cb_type.Items.Add("IR");
                    gb_ir.Enabled = true;
                }
                if (newDevice.hasSensor(Device.SensorType.DEPTH))
                {
                    cb_type.Items.Add("Depth");
                    gb_depth.Enabled = true;
                    //cb_smart.Enabled = true;
                }
                if (cb_type.Items.Count < 0)
                    cb_type.SelectedIndex = 0;
                if (isNewDevice)
                    newDevice.Close();
            }
        }

        void IsNeedHalt()
        {
            if (broadcaster != null)
            {
                if (broadcaster.hasClient || this.Visible)
                {
                    iNoClient = 0;
                    if (isIdle)
                    {
                        isIdle = false;
                        Start();
                    }
                }
                else
                {
                    iNoClient++;
                    if (iNoClient > 120 && !isIdle) // 2min of no data
                    {
                        isIdle = true;
                        Stop(false);
                    }
                }
            }
        }

        void ReadSettings()
        {
            cb_device.SelectedIndex = -1;
            if (!NiUI.Properties.Settings.Default.DeviceURI.Equals(string.Empty))
            {
                foreach (object item in cb_device.Items)
                {
                    if (item is DeviceInfo && (item as DeviceInfo).URI.Equals(NiUI.Properties.Settings.Default.DeviceURI, StringComparison.CurrentCultureIgnoreCase))
                        cb_device.SelectedItem = item;
                }
                DeviceChanged();
                cb_type.SelectedIndex = -1;
                if (NiUI.Properties.Settings.Default.CameraType != -1)
                    foreach (object item in cb_type.Items)
                    {
                        if (NiUI.Properties.Settings.Default.CameraType == 1 && item is string && (item as string).Equals("IR", StringComparison.CurrentCultureIgnoreCase))
                            cb_type.SelectedItem = item;
                        if (NiUI.Properties.Settings.Default.CameraType == 2 && item is string && (item as string).Equals("Color", StringComparison.CurrentCultureIgnoreCase))
                            cb_type.SelectedItem = item;
                        if (NiUI.Properties.Settings.Default.CameraType == 3 && item is string && (item as string).Equals("Depth", StringComparison.CurrentCultureIgnoreCase))
                            cb_type.SelectedItem = item;
                    }
            }
            cb_notification.Checked = NiUI.Properties.Settings.Default.AutoNotification;
            cb_hd.Checked = NiUI.Properties.Settings.Default.Color_HD;
            cb_fill.Checked = NiUI.Properties.Settings.Default.Depth_Fill;
            cb_equal.Checked = NiUI.Properties.Settings.Default.Depth_Histogram;
            cb_invert.Checked = NiUI.Properties.Settings.Default.Depth_Invert;
            cb_mirror.Checked = NiUI.Properties.Settings.Default.Mirroring;
            cb_smart.Checked = NiUI.Properties.Settings.Default.SmartCam;
            try
            {
                if (Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run").GetValue("OpenNI Virtual Webcam Server") != null)
                    cb_startup.Checked = true;
            }
            catch (Exception) { }
        }
        void SaveSettings()
        {
            NiUI.Properties.Settings.Default.DeviceURI = "";
            if (cb_device.SelectedItem != null && cb_device.SelectedItem is DeviceInfo && ((DeviceInfo)cb_device.SelectedItem).isValid)
                NiUI.Properties.Settings.Default.DeviceURI = (cb_device.SelectedItem as DeviceInfo).URI;
            NiUI.Properties.Settings.Default.CameraType = -1;
            if (cb_type.SelectedItem != null && cb_type.SelectedItem is string)
            {
                switch ((string)cb_type.SelectedItem)
                {
                    case "Color":
                        NiUI.Properties.Settings.Default.CameraType = 2;
                        break;
                    case "Depth":
                        NiUI.Properties.Settings.Default.CameraType = 3;
                        break;
                    case "IR":
                        NiUI.Properties.Settings.Default.CameraType = 1;
                        break;
                }
            }
            NiUI.Properties.Settings.Default.AutoNotification = cb_notification.Checked;
            NiUI.Properties.Settings.Default.Color_HD = cb_hd.Checked;
            NiUI.Properties.Settings.Default.Depth_Fill = cb_fill.Checked;
            NiUI.Properties.Settings.Default.Depth_Histogram = cb_equal.Checked;
            NiUI.Properties.Settings.Default.Depth_Invert = cb_invert.Checked;
            NiUI.Properties.Settings.Default.Mirroring = cb_mirror.Checked;
            NiUI.Properties.Settings.Default.SmartCam = cb_smart.Checked;
            NiUI.Properties.Settings.Default.Save();
            try
            {
                if (cb_startup.Checked)
                    Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true).SetValue("OpenNI Virtual Webcam Server", "\"" + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + "\" /autoRun");
                else
                    Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true).DeleteValue("OpenNI Virtual Webcam Server");
            }
            catch (Exception) { }
        }

        void Stop(bool isApply)
        {
            bool isSameDevice = isApply && currentDevice != null && currentDevice.isValid && currentDevice.DeviceInfo.URI == NiUI.Properties.Settings.Default.DeviceURI;
            bool isSameSensor = isApply && isSameDevice && currentSensor != null && currentSensor.isValid && currentSensor.SensorInfo.getSensorType() == (Device.SensorType)NiUI.Properties.Settings.Default.CameraType;
            if (!isSameSensor)
            {
                if (currentSensor != null && currentSensor.isValid)
                {
                    currentSensor.Stop();
                    currentSensor.onNewFrame -= currentSensor_onNewFrame;
                }
                currentSensor = null;
            }
            if (!isSameDevice)
            {
                if (currentDevice != null && currentDevice.isValid)
                    currentDevice.Close();
                currentDevice = null;
            }
            isIdle = true;
            btn_stopstart.Text = "Start Streaming";
            if (!isApply)
            {
                Bitmap bi = new Bitmap(640, 480, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                broadcaster.SendBitmap(bi);
                pb_image.Image = new Bitmap(bi, pb_image.Size);
                pb_image.Refresh();
            }
            if (NiUI.Properties.Settings.Default.AutoNotification)
                notify.Visible = false;
        }

        bool Start()
        {
            if (this.isIdle && broadcaster.hasServer())
            {
                MessageBox.Show("Only one server is allowed.", "Multi-Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            bool isSameDevice = currentDevice != null && currentDevice.isValid && currentDevice.DeviceInfo.URI == NiUI.Properties.Settings.Default.DeviceURI;
            bool isSameSensor = isSameDevice && currentSensor != null && currentSensor.isValid && currentSensor.SensorInfo.getSensorType() == (Device.SensorType)NiUI.Properties.Settings.Default.CameraType;
            if (!isSameDevice)
            {
                try
                {
                    currentDevice = Device.Open(NiUI.Properties.Settings.Default.DeviceURI);
                }
                catch (Exception ex)
                {
                    currentDevice = null;
                    MessageBox.Show("Can not open selected Device. " + ex.Message, "Device Open", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            if (!isSameSensor)
            {
                try
                {
                    currentSensor = currentDevice.CreateVideoStream((Device.SensorType)NiUI.Properties.Settings.Default.CameraType);
                    currentSensor.onNewFrame += currentSensor_onNewFrame;
                }
                catch (Exception ex)
                {
                    currentSensor = null;
                    MessageBox.Show("Can not open selected Sensor from selected Device. " + ex.Message, "Sensor Create", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            currentSensor.Stop();
            VideoMode[] vmodes = currentSensor.SensorInfo.getSupportedVideoModes();
            VideoMode selectedVideoMode = null;
            switch (currentSensor.SensorInfo.getSensorType())
            {
                case Device.SensorType.COLOR:
                    renderOptions = VideoFrameRef.copyBitmapOptions.Force24BitRGB;
                    if (NiUI.Properties.Settings.Default.Color_HD)
                    {
                        foreach (VideoMode vm in vmodes)
                            if (vm.Resolution.Width == 1280 && (vm.Resolution.Height == 960 || vm.Resolution.Height == 1024))
                                if ((selectedVideoMode == null || (selectedVideoMode.FPS < vm.FPS && vm.DataPixelFormat < selectedVideoMode.DataPixelFormat)) &&
                                    vm.DataPixelFormat != VideoMode.PixelFormat.JPEG && vm.DataPixelFormat != VideoMode.PixelFormat.YUV422)
                                    selectedVideoMode = vm;
                        isHD = selectedVideoMode != null;
                        if (!isHD)
                            MessageBox.Show("This device doesn't support ~1.3MP resolution.", "HD Resolution", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    if (selectedVideoMode == null)
                        foreach (VideoMode vm in vmodes)
                            if (vm.Resolution == new Size(640, 480))
                                if ((selectedVideoMode == null || (selectedVideoMode.FPS < vm.FPS && vm.DataPixelFormat < selectedVideoMode.DataPixelFormat)) &&
                                    vm.DataPixelFormat != VideoMode.PixelFormat.JPEG && vm.DataPixelFormat != VideoMode.PixelFormat.YUV422)
                                    selectedVideoMode = vm;
                    break;
                case Device.SensorType.DEPTH:
                    renderOptions = VideoFrameRef.copyBitmapOptions.Force24BitRGB | VideoFrameRef.copyBitmapOptions.DepthFillShadow;
                    if (NiUI.Properties.Settings.Default.Depth_Fill)
                        if (cb_mirror.Enabled && cb_mirror.Checked)
                            renderOptions |= VideoFrameRef.copyBitmapOptions.DepthFillRigthBlack;
                        else
                            renderOptions |= VideoFrameRef.copyBitmapOptions.DepthFillLeftBlack;
                    if (NiUI.Properties.Settings.Default.Depth_Invert)
                        renderOptions |= VideoFrameRef.copyBitmapOptions.DepthInvert;
                    if (NiUI.Properties.Settings.Default.Depth_Histogram)
                        renderOptions |= VideoFrameRef.copyBitmapOptions.DepthHistogramEqualize;
                    foreach (VideoMode vm in vmodes)
                        if (vm.Resolution == new Size(640, 480))
                            if ((selectedVideoMode == null || selectedVideoMode.FPS < vm.FPS) &&
                                (vm.DataPixelFormat == VideoMode.PixelFormat.DEPTH_1MM || vm.DataPixelFormat == VideoMode.PixelFormat.DEPTH_100UM))
                                selectedVideoMode = vm;
                    break;
                case Device.SensorType.IR:
                    renderOptions = VideoFrameRef.copyBitmapOptions.Force24BitRGB;
                    foreach (VideoMode vm in vmodes)
                        if (vm.Resolution == new Size(640, 480))
                            if ((selectedVideoMode == null || (selectedVideoMode.FPS < vm.FPS && vm.DataPixelFormat < selectedVideoMode.DataPixelFormat)) &&
                                vm.DataPixelFormat != VideoMode.PixelFormat.JPEG && vm.DataPixelFormat != VideoMode.PixelFormat.YUV422)
                                selectedVideoMode = vm;
                    break;
                default:
                    break;
            }
            if (selectedVideoMode != null)
                try
                {
                    if (currentSensor.VideoMode.FPS != selectedVideoMode.FPS || currentSensor.VideoMode.DataPixelFormat != selectedVideoMode.DataPixelFormat || currentSensor.VideoMode.Resolution != selectedVideoMode.Resolution)
                        currentSensor.VideoMode = selectedVideoMode;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Can not set active video mode to " + selectedVideoMode.ToString() + ". " + ex.Message, "Sensor Config", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            else
            {
                MessageBox.Show("No acceptable video mode found.", "Sensor Config", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (NiUI.Properties.Settings.Default.Mirroring)
                try
                {
                    if (currentSensor.Mirroring != cb_mirror.Checked)
                        currentSensor.Mirroring = cb_mirror.Checked;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Can not enable mirroring. " + ex.Message, "Sensor Config", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            if (!HandleError(currentSensor.Start()))
            {
                Stop(false);
                return false;
            }
            btn_stopstart.Text = "Stop Streaming";
            isIdle = false;
            notify.Visible = true;
            return true;
        }
        void OpenNI_onDeviceStateChanged(DeviceInfo Device, OpenNI.DeviceState state)
        {
            this.BeginInvoke((Action)delegate
            {
                UpdateDevicesList();
            });
        }

        void OpenNI_onDeviceConnectionStateChanged(DeviceInfo Device)
        {
            this.BeginInvoke((Action)delegate
            {
                UpdateDevicesList();
            });
        }

        void currentSensor_onNewFrame(VideoStream vStream)
        {
            if (vStream.isValid && vStream.isFrameAvailable() && !isIdle)
            {
                using (VideoFrameRef frame = vStream.readFrame())
                {
                    if (frame.isValid)
                    {
                        lock (bitmap)
                        {
                            try
                            {
                                frame.updateBitmap(bitmap, renderOptions);
                            }
                            catch (Exception)
                            {
                                bitmap = frame.toBitmap(renderOptions);
                            }
                        }
                        if (!isIdle)
                            broadcaster.SendBitmap(bitmap);
                        this.BeginInvoke((Action)delegate
                        {
                            if (!isIdle)
                                lock (bitmap)
                                {
                                    if (pb_image.Image != null)
                                        pb_image.Image.Dispose();
                                    pb_image.Image = new Bitmap(bitmap, pb_image.Size);
                                    pb_image.Refresh();
                                }
                        });
                    }
                }
            }
        }
    }
}
