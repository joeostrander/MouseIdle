using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
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
        static DateTime lastMovement;
        static bool boolHidden = false;
        static bool boolRunning = true;
        static bool boolStartup = false;
        static int cornerIndex = 0;

        public Form1()
        {
            InitializeComponent();
            this.Text = Application.ProductName;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!boolRunning)
                return;

            int secs;
            Int32.TryParse(textBoxMaxIdleSeconds.Text, out secs);
            if (secs < 5)
            {
                secs = 60;
            }
            maxIdleSeconds = secs;

            Point pos_current = Cursor.Position;
            if (pos_current != pos_last)
            {
                labelStatus.Text = "Moved!  "+pos_last.ToString()+"<>"+pos_current.ToString();
                //Console.WriteLine("Moved!  " + pos_last.ToString() + "<>" + pos_current.ToString());
                if (boolHidden)
                {
                    //move back
                    Cursor.Position = pos_original;
                    this.Icon = Properties.Resources.mouse_on;
                    notifyIcon1.Icon = Properties.Resources.mouse_on;
                }
                boolHidden = false;
                lastMovement = DateTime.Now;
                pos_last = pos_current;
            }
            else
            {
                //Console.WriteLine("max idle {0}", maxIdleSeconds);
                TimeSpan ts = new TimeSpan();
                ts = DateTime.Now - lastMovement;
                int secondsIdle = (int)ts.TotalSeconds;
                if (secondsIdle >= maxIdleSeconds)
                {
                    if (!boolHidden)
                    {
                        labelStatus.Text = "Idle.";
                        boolHidden = true;
                        Point pos_new = getHiddenPoint();
                        pos_last = pos_new;
                        pos_original = pos_current;
                        Cursor.Position = pos_new;
                        Console.WriteLine("HIDE IT!!!");
                        this.Icon = Properties.Resources.mouse_off;
                        notifyIcon1.Icon = Properties.Resources.mouse_off;

                    }
                }
                else
                {
                    labelStatus.Text = "Idle time:  " + secondsIdle + "<"+maxIdleSeconds;
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
    }
}
