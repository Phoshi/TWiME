using System;
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
            Manager.Setup();
            Application.Run();
        }
    }
}