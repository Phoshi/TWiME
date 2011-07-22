using System;
using System.IO;
using System.Windows.Forms;

namespace TWiME {
    internal static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try {
                Manager.Setup();
                Application.Run();
            }
            catch (Exception ex) {
                StreamWriter ohgodeverythingisfailingWriter = new StreamWriter("error.log");
                ohgodeverythingisfailingWriter.WriteLine("Critical Failure: "+ex.Message);
                ohgodeverythingisfailingWriter.Close();
                Taskbar.hidden = false;
                throw ex;
                Application.Exit();
            }
        }
    }
}