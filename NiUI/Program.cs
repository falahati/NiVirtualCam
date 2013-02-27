using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;
using System.Diagnostics;
namespace NiUI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            frm_Main mainform = new frm_Main();
            if (Environment.CommandLine.ToLower().Contains("autoRun".ToLower()))
            {
                Process.Start(
                    new ProcessStartInfo(Application.ExecutablePath, "/auto_Corrected_Run")
                    {
                        WorkingDirectory = System.IO.Path.GetDirectoryName(Application.ExecutablePath),
                        UseShellExecute = true
                    });
                Environment.Exit(0);
            }
            if (Environment.CommandLine.ToLower().Contains("auto_Corrected_Run".ToLower()))
                mainform.IsAutoRun = true;
            try
            {
                SingleInstanceApplication.Run(mainform, StartupNextInstanceHandler);
            }
            catch (Exception)
            {
                Application.Run(mainform);
            }
        }

        static void StartupNextInstanceHandler(object sender, StartupNextInstanceEventArgs e)
        {
            frm_Main form = (Application.OpenForms[0] as frm_Main);
            if (!form.Visible)
                form.Visible = true;
            form.Activate();
        }
    }
}
