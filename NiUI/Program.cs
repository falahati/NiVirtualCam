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
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;

namespace NiUI
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var mainForm = new frm_Main();

            if (Environment.CommandLine.ToLower().Contains("autoRun".ToLower()))
            {
                var address = Path.GetDirectoryName(Application.ExecutablePath);

                if (address != null)
                {
                    Process.Start(
                        new ProcessStartInfo(Application.ExecutablePath, "/auto_Corrected_Run")
                        {
                            WorkingDirectory =
                                address,
                            UseShellExecute =
                                true
                        });
                }

                Environment.Exit(0);
            }

            if (Environment.CommandLine.ToLower().Contains("auto_Corrected_Run".ToLower()))
            {
                mainForm.IsAutoRun = true;
            }

            try
            {
                SingleInstanceApplication.Run(mainForm, StartupNextInstanceHandler);
            }
            catch (Exception)
            {
                Application.Run(mainForm);
            }
        }

        private static void StartupNextInstanceHandler(object sender, StartupNextInstanceEventArgs e)
        {
            if (Application.OpenForms[0] is frm_Main form)
            {
                if (!form.Visible)
                {
                    form.Visible = true;
                }

                form.Activate();
            }
        }
    }
}