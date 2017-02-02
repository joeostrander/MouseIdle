using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MouseIdle
{
    public partial class Form1 : Form
    {

        static int maxIdleSeconds = 60;
        static Point pos_last;
        static Point pos_original;
        static Point pos_hidden;
        static DateTime lastMovement;
        static bool boolHidden = false;
        static bool boolRunning = true;
        static bool boolStartup = false;
        static bool boolUnhide = false;
        static bool boolMouseDownLeft = false;
        static bool boolMouseDownRight = false;
        static bool boolMouseWheelMove = false;
        static int Delta;
        static int cornerIndex = 0;
        


        #region Constant, Structure and Delegate Definitions
        /// <summary>
        /// defines the callback type for the hook
        /// </summary>
        public delegate int mouseHookProc(int code, int wParam, ref Msllhookstruct lParam);


        public struct Msllhookstruct
        {
            public Point Location;
            public int MouseData;
            public int Flags;
            public int Time;
            public int ExtraInfo;
        }

        const int WH_MOUSE_LL = 14;

        const int WM_MOUSEMOVE = 0x200;
        const int WM_MOUSEWHEEL = 0x20a;
        const int WM_LBUTTONDOWN = 0x201;
        const int WM_LBUTTONUP = 0x202;
        const int WM_RBUTTONDOWN = 0x204;
        const int WM_RBUTTONUP = 0x205;
        const int WM_MBUTTONDOWN = 0x207;
        const int WM_MBUTTONUP = 0x208;


        const int MOUSEEVENTF_LEFTDOWN = 0x2;
        const int MOUSEEVENTF_LEFTUP = 0x4;
        const int MOUSEEVENTF_RIGHTDOWN = 0x8;
        const int MOUSEEVENTF_RIGHTUP = 0x10;
        const int MOUSEEVENTF_WHEEL = 0x0800;


        #endregion


        #region DLL imports


        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, int dwData, UIntPtr dwExtraInfo);


        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, mouseHookProc callback, IntPtr hInstance, uint threadId);

        /// <summary>
        /// Unhooks the windows hook.
        /// </summary>
        /// <param name="hInstance">The hook handle that was returned from SetWindowsHookEx</param>
        /// <returns>True if successful, false otherwise</returns>
        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        /// <summary>
        /// Calls the next hook.
        /// </summary>
        /// <param name="idHook">The hook id</param>
        /// <param name="nCode">The hook code</param>
        /// <param name="wParam">The wparam.</param>
        /// <param name="lParam">The lparam.</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref Msllhookstruct lParam);

        /// <summary>
        /// Loads the library.
        /// </summary>
        /// <param name="lpFileName">Name of the library</param>
        /// <returns>A handle to the library</returns>
        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);
        #endregion

        IntPtr hhook = IntPtr.Zero;
        private mouseHookProc hookProcDelegate;


        public Form1()
        {
            InitializeComponent();
            this.Text = Application.ProductName;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {

            if (!boolRunning)
                return;

            if (boolUnhide)
            {
                Cursor.Position = pos_original;
                this.Icon = Properties.Resources.mouse_on;
                notifyIcon1.Icon = Properties.Resources.mouse_on;
                Console.WriteLine("UNHIDE! {0}", pos_original.ToString());
                boolUnhide = false;
                boolHidden = false;
                pos_last = pos_original;
                lastMovement = DateTime.Now;
                if (boolMouseWheelMove) {
                    boolMouseWheelMove = false;
                    WheelMove();
                }
                else if (boolMouseDownLeft)
                {
                    boolMouseDownLeft = false;
                    LeftDown();
                }
                else if (boolMouseDownRight)
                {
                    boolMouseDownRight = false;
                    RightDown();
                }
                return;
            }


            int secs;
            Int32.TryParse(textBoxMaxIdleSeconds.Text, out secs);
            if (secs < 5)
            {
                secs = 60;
            }
            maxIdleSeconds = secs;

            TimeSpan ts = new TimeSpan();
            ts = DateTime.Now - lastMovement;
            int secondsIdle = (int)ts.TotalSeconds;
            if (secondsIdle >= maxIdleSeconds)
            {
                if (!boolHidden)
                {
                    //if the workstation locked... it'll read 0,0 location
                    if (Cursor.Position.X == 0 && Cursor.Position.Y == 0)
                        return;
                    labelStatus.Text = "Idle.";
                    boolHidden = true;
                    pos_hidden = getHiddenPoint();
                    pos_last = pos_hidden;
                    pos_original = Cursor.Position;
                    Cursor.Position = pos_hidden;
                    Console.WriteLine("HIDE IT!!! {0}", pos_original);
                    this.Icon = Properties.Resources.mouse_off;
                    notifyIcon1.Icon = Properties.Resources.mouse_off;
                }
            }
            else
            {
                labelStatus.Text = "Idle time:  " + secondsIdle + "<" + maxIdleSeconds;
            }
            if (boolHidden)
            {
                labelStatus.Text = "Hidden.  Idle time:  " + secondsIdle;
            }
            else
            {
                labelStatus.Text = "Idle time:  " + secondsIdle + "<" + maxIdleSeconds;
            }
        }

        private void RightDown()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
        }

        private void LeftDown()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        }

        private void WheelMove()
        {
            Console.WriteLine("Wheel move {0}", Delta);
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, Delta, UIntPtr.Zero);
        }

        private void mouseMoved()
        { 


            Point pos_current = Cursor.Position;
            if (pos_current == pos_hidden)
            {
                Console.WriteLine("SKIP!");
                return;
            }
                
                    

            //if (pos_current == pos_hidden)
            //    return;

            labelStatus.Text = "Moved!  "+pos_last.ToString()+"<>"+pos_current.ToString();
            //Console.WriteLine("Moved!  " + pos_last.ToString() + "<>" + pos_current.ToString());
            if (boolHidden)
            {
                //move back
                boolHidden = false;
                boolUnhide = true;
            }
            
            lastMovement = DateTime.Now;
            pos_last = pos_current;

        }

        private Point getHiddenPoint()
        {
            Point pt;
            switch (comboBox1.Text)
            {
                case "Bottom Right":
                    pt = new Point(Screen.PrimaryScreen.Bounds.Width - 1, Screen.PrimaryScreen.Bounds.Height - 1);
                    break;
                case "Bottom Left":
                    pt = new Point(0, Screen.PrimaryScreen.Bounds.Height - 1);
                    break;
                case "Top Right":
                    pt = new Point(Screen.PrimaryScreen.Bounds.Width - 1, 0);
                    break;
                default:
                    pt = new Point(0, 0);
                    break;
            }

            return pt;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            //lastMovement = DateTime.Now.AddDays(-1);
            
            notifyIcon1.Text = Application.ProductName;
            labelStatus.Text = "";
            lastMovement = DateTime.Now;
            pos_last = Cursor.Position;

            labelStatus.Text = lastMovement.ToLongDateString();

            comboBox1.SelectedIndex = cornerIndex;
            LoadSettings();
            comboBox1.SelectedIndex = cornerIndex;
            textBoxMaxIdleSeconds.Text = maxIdleSeconds.ToString();

            this.Icon = Properties.Resources.mouse_on;
            notifyIcon1.Icon = Properties.Resources.mouse_on;

            hookProcDelegate = hookProc;
            hook();

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 ab = new AboutBox1();
            ab.ShowDialog();
        }

        private void StartStop()
        {
            boolRunning = !boolRunning;
            enabledToolStripMenuItem.Checked = boolRunning;
            if (buttonStartStop.Text == "&Start")
            {
                buttonStartStop.Text = "&Stop";
                labelStatus.Text = "Running";
            }
            else
            {
                buttonStartStop.Text = "&Start";
                labelStatus.Text = "Stopped";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartStop();
        }


        private void LoadSettings()
        {


            RegistryKey regKey;
            regKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            RegistryKey appRegKey;
            appRegKey = Registry.CurrentUser.OpenSubKey("Software\\" + Application.ProductName, true);

            if (appRegKey == null)
            {
                //Create the key
                Registry.CurrentUser.CreateSubKey("Software\\" + Application.ProductName);
                appRegKey = Registry.CurrentUser.OpenSubKey("Software\\" + Application.ProductName, true);
                if (appRegKey == null)
                {
                    regKey.Close();
                    return;
                }

                //Ask user if they want it to launch auto...
                if (MessageBox.Show("Launch " + Application.ProductName + " at Startup?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    boolStartup = true;
                }
                else
                {
                    boolStartup = false;
                }

            }
            else
            {
                //Get Current Value and set it in the interface
                boolStartup = Convert.ToBoolean(appRegKey.GetValue("RunAtStartup", false));
                maxIdleSeconds = Convert.ToInt32(appRegKey.GetValue("maxIdleSeconds",5));
                cornerIndex = Convert.ToInt32(appRegKey.GetValue("cornerIndex", 0));


            }

            regKey.Close();
            appRegKey.Close();

            SaveRegistrySettings();

        }

        private void SaveRegistrySettings()
        {
            RegistryKey regKey;
            regKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            RegistryKey appRegKey;
            appRegKey = Registry.CurrentUser.OpenSubKey("Software\\" + Application.ProductName, true);

            appRegKey.SetValue("maxIdleSeconds", maxIdleSeconds, RegistryValueKind.DWord);
            appRegKey.SetValue("cornerIndex", comboBox1.SelectedIndex, RegistryValueKind.DWord);
            appRegKey.SetValue("RunAtStartup", boolStartup, RegistryValueKind.DWord);

            //If boolStartup==true, set to run at startup
            if (boolStartup)
            {
                regKey.SetValue(Application.ProductName, Application.ExecutablePath);
            }
            else
            {
                if (regKey.GetValue(Application.ProductName) != null)
                {
                    regKey.DeleteValue(Application.ProductName);
                }
            }



            runAtStartupToolStripMenuItem.Checked = boolStartup;

            regKey.Close();
            appRegKey.Close();


        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveRegistrySettings();
            MessageBox.Show("Settings saved.", "Save Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void enabledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartStop();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            cornerIndex = comboBox1.SelectedIndex;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void runAtStartupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunAtStartupSet();
        }

        private void RunAtStartupSet()
        {
            boolStartup = runAtStartupToolStripMenuItem.Checked;
            SaveRegistrySettings();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            this.Hide();
            this.Opacity = 100;
        }

        private void Form1_Move(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                notifyIcon1.ShowBalloonTip(600, Application.ProductName, Application.ProductName + " running.", ToolTipIcon.Info);
                this.Hide();
            }
            else
            {
                this.Show();
            }
        }


        private void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            ShowMe();
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            ShowMe();
        }

        private void ShowMe()
        {
            this.Show();
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
        }


        private void hook()
        {
            IntPtr hInstance = LoadLibrary("User32");
            hhook = SetWindowsHookEx(WH_MOUSE_LL, hookProcDelegate, hInstance, 0);
        }


        private int hookProc(int nCode, int wParam, ref Msllhookstruct lParam)
        {
            Boolean boolNext = true;
            try
            {
                if (wParam != WM_MOUSEMOVE)
                {
                    //Console.WriteLine(wParam.ToString("X"));
                    String msg = "";
                    if (wParam == WM_MOUSEWHEEL)
                    {
                        Delta = (int)(lParam.MouseData >> 16);
                        boolMouseWheelMove = true;
                        if (Delta > 0)
                        {
                            msg = "Scroll up";
                        }
                        else if (Delta < 0)
                        {
                            msg = "Scroll down";
                        }
                    }
                    else if (wParam == WM_LBUTTONUP)
                    {
                        msg = "Left button up";
                    }
                    else if (wParam == WM_RBUTTONUP)
                    {
                        msg = "Right button up";
                    }
                    else if (wParam == WM_MBUTTONUP)
                    {
                        msg = "Middle button up";
                    }
                    else if (wParam == WM_LBUTTONDOWN)
                    {
                        msg = "Left button down";
                        boolMouseDownLeft = true;
                    }
                    else if (wParam == WM_RBUTTONDOWN)
                    {
                        msg = "Right button down";
                        boolMouseDownRight = true;
                    }
                    else if (wParam == WM_MBUTTONDOWN)
                    {
                        msg = "Middle button down";
                    }
                    Console.WriteLine(msg);
                    if (boolHidden) {
                        boolUnhide = true;
                        boolHidden = false;
                        boolNext = false;
                    }
                        
                }
                else
                {
                    mouseMoved();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (boolNext)
            {
                return CallNextHookEx(hhook, nCode, wParam, ref lParam);
            }
            else
            {
                Console.WriteLine("SKIPPING");
                return -1;
            }


        }

    }
}
