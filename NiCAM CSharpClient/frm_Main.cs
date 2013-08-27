using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NiCAM_CSharpClient
{
    public partial class frm_Main : Form
    {
        public frm_Main()
        {
            InitializeComponent();
        }

        private void frm_Main_Shown(object sender, EventArgs e)
        {
            BitmapReceiver rec = new BitmapReceiver();
            while (true)
            {
                if (pb.Image != null)
                    pb.Image.Dispose();
                pb.Image = rec.ReadBitmap();
                Application.DoEvents();
                System.Threading.Thread.Sleep(16);
                Application.DoEvents();
            }
        }
    }
}
