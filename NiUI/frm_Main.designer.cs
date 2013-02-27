namespace NiUI
{
    partial class frm_Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frm_Main));
            this.label1 = new System.Windows.Forms.Label();
            this.cb_device = new System.Windows.Forms.ComboBox();
            this.cb_type = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btn_apply = new System.Windows.Forms.Button();
            this.cb_fill = new System.Windows.Forms.CheckBox();
            this.cb_equal = new System.Windows.Forms.CheckBox();
            this.cb_invert = new System.Windows.Forms.CheckBox();
            this.pb_image = new System.Windows.Forms.PictureBox();
            this.halt_timer = new System.Windows.Forms.Timer(this.components);
            this.gb_depth = new System.Windows.Forms.GroupBox();
            this.gb_color = new System.Windows.Forms.GroupBox();
            this.cb_hd = new System.Windows.Forms.CheckBox();
            this.gb_ir = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.gb_general = new System.Windows.Forms.GroupBox();
            this.cb_smart = new System.Windows.Forms.CheckBox();
            this.cb_notification = new System.Windows.Forms.CheckBox();
            this.cb_startup = new System.Windows.Forms.CheckBox();
            this.cb_mirror = new System.Windows.Forms.CheckBox();
            this.gb_preview = new System.Windows.Forms.GroupBox();
            this.btn_stopstart = new System.Windows.Forms.Button();
            this.l_copyright = new System.Windows.Forms.LinkLabel();
            this.notify = new System.Windows.Forms.NotifyIcon(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pb_image)).BeginInit();
            this.gb_depth.SuspendLayout();
            this.gb_color.SuspendLayout();
            this.gb_ir.SuspendLayout();
            this.gb_general.SuspendLayout();
            this.gb_preview.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Device:";
            // 
            // cb_device
            // 
            this.cb_device.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_device.FormattingEnabled = true;
            this.cb_device.Location = new System.Drawing.Point(91, 13);
            this.cb_device.Name = "cb_device";
            this.cb_device.Size = new System.Drawing.Size(151, 21);
            this.cb_device.TabIndex = 1;
            this.cb_device.SelectedIndexChanged += new System.EventHandler(this.cb_device_SelectedIndexChanged);
            // 
            // cb_type
            // 
            this.cb_type.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_type.FormattingEnabled = true;
            this.cb_type.Location = new System.Drawing.Point(91, 40);
            this.cb_type.Name = "cb_type";
            this.cb_type.Size = new System.Drawing.Size(151, 21);
            this.cb_type.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 43);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Camera Type:";
            // 
            // btn_apply
            // 
            this.btn_apply.Location = new System.Drawing.Point(622, 385);
            this.btn_apply.Name = "btn_apply";
            this.btn_apply.Size = new System.Drawing.Size(87, 23);
            this.btn_apply.TabIndex = 10;
            this.btn_apply.Text = "Apply Settings";
            this.btn_apply.UseVisualStyleBackColor = true;
            this.btn_apply.Click += new System.EventHandler(this.btn_apply_Click);
            // 
            // cb_fill
            // 
            this.cb_fill.AutoSize = true;
            this.cb_fill.Location = new System.Drawing.Point(6, 19);
            this.cb_fill.Name = "cb_fill";
            this.cb_fill.Size = new System.Drawing.Size(80, 17);
            this.cb_fill.TabIndex = 0;
            this.cb_fill.Text = "Fill Shadow";
            this.cb_fill.UseVisualStyleBackColor = true;
            // 
            // cb_equal
            // 
            this.cb_equal.AutoSize = true;
            this.cb_equal.Location = new System.Drawing.Point(6, 42);
            this.cb_equal.Name = "cb_equal";
            this.cb_equal.Size = new System.Drawing.Size(116, 17);
            this.cb_equal.TabIndex = 1;
            this.cb_equal.Text = "Histogram Equalize";
            this.cb_equal.UseVisualStyleBackColor = true;
            // 
            // cb_invert
            // 
            this.cb_invert.AutoSize = true;
            this.cb_invert.Location = new System.Drawing.Point(6, 65);
            this.cb_invert.Name = "cb_invert";
            this.cb_invert.Size = new System.Drawing.Size(53, 17);
            this.cb_invert.TabIndex = 2;
            this.cb_invert.Text = "Invert";
            this.cb_invert.UseVisualStyleBackColor = true;
            // 
            // pb_image
            // 
            this.pb_image.BackColor = System.Drawing.Color.Black;
            this.pb_image.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pb_image.ErrorImage = null;
            this.pb_image.InitialImage = null;
            this.pb_image.Location = new System.Drawing.Point(11, 19);
            this.pb_image.Name = "pb_image";
            this.pb_image.Size = new System.Drawing.Size(450, 337);
            this.pb_image.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pb_image.TabIndex = 15;
            this.pb_image.TabStop = false;
            // 
            // halt_timer
            // 
            this.halt_timer.Interval = 1000;
            this.halt_timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // gb_depth
            // 
            this.gb_depth.Controls.Add(this.cb_fill);
            this.gb_depth.Controls.Add(this.cb_equal);
            this.gb_depth.Controls.Add(this.cb_invert);
            this.gb_depth.Location = new System.Drawing.Point(12, 67);
            this.gb_depth.Name = "gb_depth";
            this.gb_depth.Size = new System.Drawing.Size(230, 89);
            this.gb_depth.TabIndex = 4;
            this.gb_depth.TabStop = false;
            this.gb_depth.Text = "Depth Sensor Settings";
            // 
            // gb_color
            // 
            this.gb_color.Controls.Add(this.cb_hd);
            this.gb_color.Location = new System.Drawing.Point(12, 162);
            this.gb_color.Name = "gb_color";
            this.gb_color.Size = new System.Drawing.Size(230, 47);
            this.gb_color.TabIndex = 5;
            this.gb_color.TabStop = false;
            this.gb_color.Text = "Color Sensor Settings";
            // 
            // cb_hd
            // 
            this.cb_hd.AutoSize = true;
            this.cb_hd.Location = new System.Drawing.Point(6, 19);
            this.cb_hd.Name = "cb_hd";
            this.cb_hd.Size = new System.Drawing.Size(209, 17);
            this.cb_hd.TabIndex = 0;
            this.cb_hd.Text = "1.3 Mega Pixel Resolution (If available)";
            this.cb_hd.UseVisualStyleBackColor = true;
            // 
            // gb_ir
            // 
            this.gb_ir.Controls.Add(this.label3);
            this.gb_ir.Location = new System.Drawing.Point(12, 215);
            this.gb_ir.Name = "gb_ir";
            this.gb_ir.Size = new System.Drawing.Size(230, 45);
            this.gb_ir.TabIndex = 6;
            this.gb_ir.TabStop = false;
            this.gb_ir.Text = "IR Sensor Settings";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(93, 20);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "None";
            // 
            // gb_general
            // 
            this.gb_general.Controls.Add(this.cb_smart);
            this.gb_general.Controls.Add(this.cb_notification);
            this.gb_general.Controls.Add(this.cb_startup);
            this.gb_general.Controls.Add(this.cb_mirror);
            this.gb_general.Location = new System.Drawing.Point(12, 266);
            this.gb_general.Name = "gb_general";
            this.gb_general.Size = new System.Drawing.Size(230, 113);
            this.gb_general.TabIndex = 7;
            this.gb_general.TabStop = false;
            this.gb_general.Text = "General Settings";
            // 
            // cb_smart
            // 
            this.cb_smart.AutoSize = true;
            this.cb_smart.Enabled = false;
            this.cb_smart.Location = new System.Drawing.Point(6, 19);
            this.cb_smart.Name = "cb_smart";
            this.cb_smart.Size = new System.Drawing.Size(98, 17);
            this.cb_smart.TabIndex = 0;
            this.cb_smart.Text = "Smart Tracking";
            this.cb_smart.UseVisualStyleBackColor = true;
            // 
            // cb_notification
            // 
            this.cb_notification.AutoSize = true;
            this.cb_notification.Location = new System.Drawing.Point(6, 88);
            this.cb_notification.Name = "cb_notification";
            this.cb_notification.Size = new System.Drawing.Size(221, 17);
            this.cb_notification.TabIndex = 3;
            this.cb_notification.Text = "Show notification icon only when working";
            this.cb_notification.UseVisualStyleBackColor = true;
            // 
            // cb_startup
            // 
            this.cb_startup.AutoSize = true;
            this.cb_startup.Location = new System.Drawing.Point(6, 65);
            this.cb_startup.Name = "cb_startup";
            this.cb_startup.Size = new System.Drawing.Size(147, 17);
            this.cb_startup.TabIndex = 2;
            this.cb_startup.Text = "Run with windows startup";
            this.cb_startup.UseVisualStyleBackColor = true;
            // 
            // cb_mirror
            // 
            this.cb_mirror.AutoSize = true;
            this.cb_mirror.Location = new System.Drawing.Point(6, 42);
            this.cb_mirror.Name = "cb_mirror";
            this.cb_mirror.Size = new System.Drawing.Size(99, 17);
            this.cb_mirror.TabIndex = 1;
            this.cb_mirror.Text = "Global Mirroring";
            this.cb_mirror.UseVisualStyleBackColor = true;
            // 
            // gb_preview
            // 
            this.gb_preview.Controls.Add(this.pb_image);
            this.gb_preview.Location = new System.Drawing.Point(248, 13);
            this.gb_preview.Name = "gb_preview";
            this.gb_preview.Size = new System.Drawing.Size(471, 366);
            this.gb_preview.TabIndex = 11;
            this.gb_preview.TabStop = false;
            this.gb_preview.Text = "Current output";
            // 
            // btn_stopstart
            // 
            this.btn_stopstart.Location = new System.Drawing.Point(529, 385);
            this.btn_stopstart.Name = "btn_stopstart";
            this.btn_stopstart.Size = new System.Drawing.Size(87, 23);
            this.btn_stopstart.TabIndex = 9;
            this.btn_stopstart.Text = "Start Streaming";
            this.btn_stopstart.UseVisualStyleBackColor = true;
            this.btn_stopstart.Click += new System.EventHandler(this.btn_stopstart_Click);
            // 
            // l_copyright
            // 
            this.l_copyright.AutoSize = true;
            this.l_copyright.Location = new System.Drawing.Point(15, 390);
            this.l_copyright.Name = "l_copyright";
            this.l_copyright.Size = new System.Drawing.Size(165, 13);
            this.l_copyright.TabIndex = 8;
            this.l_copyright.TabStop = true;
            this.l_copyright.Text = "By Soroush Falahati (Falahati.net)";
            this.l_copyright.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.l_copyright_LinkClicked);
            // 
            // notify
            // 
            this.notify.Icon = ((System.Drawing.Icon)(resources.GetObject("notify.Icon")));
            this.notify.Text = "OpenNI Virtual Webcam Controller";
            this.notify.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notify_MouseDoubleClick);
            // 
            // frm_Main
            // 
            this.AcceptButton = this.btn_apply;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(732, 417);
            this.Controls.Add(this.l_copyright);
            this.Controls.Add(this.btn_stopstart);
            this.Controls.Add(this.gb_preview);
            this.Controls.Add(this.gb_general);
            this.Controls.Add(this.gb_ir);
            this.Controls.Add(this.gb_color);
            this.Controls.Add(this.gb_depth);
            this.Controls.Add(this.btn_apply);
            this.Controls.Add(this.cb_type);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cb_device);
            this.Controls.Add(this.label1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frm_Main";
            this.Text = "OpenNI Virtual Webcam (NiVirtualCam) Configurations";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frm_Main_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frm_Main_FormClosed);
            this.Load += new System.EventHandler(this.frm_Main_Load);
            this.Shown += new System.EventHandler(this.frm_Main_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pb_image)).EndInit();
            this.gb_depth.ResumeLayout(false);
            this.gb_depth.PerformLayout();
            this.gb_color.ResumeLayout(false);
            this.gb_color.PerformLayout();
            this.gb_ir.ResumeLayout(false);
            this.gb_ir.PerformLayout();
            this.gb_general.ResumeLayout(false);
            this.gb_general.PerformLayout();
            this.gb_preview.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cb_device;
        private System.Windows.Forms.ComboBox cb_type;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btn_apply;
        private System.Windows.Forms.CheckBox cb_fill;
        private System.Windows.Forms.CheckBox cb_equal;
        private System.Windows.Forms.CheckBox cb_invert;
        private System.Windows.Forms.PictureBox pb_image;
        private System.Windows.Forms.Timer halt_timer;
        private System.Windows.Forms.GroupBox gb_depth;
        private System.Windows.Forms.GroupBox gb_color;
        private System.Windows.Forms.CheckBox cb_hd;
        private System.Windows.Forms.GroupBox gb_ir;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox gb_general;
        private System.Windows.Forms.CheckBox cb_mirror;
        private System.Windows.Forms.CheckBox cb_startup;
        private System.Windows.Forms.CheckBox cb_notification;
        private System.Windows.Forms.GroupBox gb_preview;
        private System.Windows.Forms.Button btn_stopstart;
        private System.Windows.Forms.LinkLabel l_copyright;
        private System.Windows.Forms.NotifyIcon notify;
        private System.Windows.Forms.CheckBox cb_smart;
    }
}

