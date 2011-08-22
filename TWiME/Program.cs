using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;

namespace TWiME {
    internal static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main() {
            if (!isAdmin()) {
                restartWithAdminRights();
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try {
                Manager.Setup();
                Application.Run();
            }
            catch (Exception ex) {
                StreamWriter ohgodeverythingisfailingWriter = new StreamWriter("error.log");
                ohgodeverythingisfailingWriter.WriteLine("Critical Failure: "+ex);
                ohgodeverythingisfailingWriter.Close();
                foreach (var window in Manager.Windows) {
                    window.ForceVisible();
                }
                Taskbar.hidden = false;
                Application.Exit();
            }
        }

        private static bool isAdmin() {
            WindowsIdentity wi = WindowsIdentity.GetCurrent();
            WindowsPrincipal wp = new WindowsPrincipal(wi);
            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void restartWithAdminRights() {
            try {
                Process.Start(new ProcessStartInfo() {
                    Verb = "runas",
                    FileName = Application.ExecutablePath
                });
            }
            catch (Exception) {
                return;
            }
            Application.Exit();
        }
    }
}