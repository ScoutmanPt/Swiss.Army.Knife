using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Profile;
using System.Windows.Forms;

namespace Scoutmans.SwissArmyKnife
{
    public partial class MainWindow : Form
    {

        private KnifeUtils _helper = null;

        public MainWindow()
        {
            InitializeComponent();
            this.CenterToScreen();
            this.Icon = Properties.Resources.Default;
            this.SystemTrayIcon.Icon = Properties.Resources.Default;
            this.SystemTrayIcon.Text = KnifeUtils.APP_TITLE;
            this.SystemTrayIcon.MouseClick+=SystemTrayIcon_MouseClick;
            this.SystemTrayIcon.Visible = true;
            this.SystemTrayIcon.ContextMenuStrip= null;

            this.Text = this.SystemTrayIcon.Text + " Settings";

            this.Resize += WindowResize;
            this.FormClosing += WindowClosing;

            _helper = new KnifeUtils(this);
           
            this.WindowState = FormWindowState.Minimized;

        }

        private void SystemTrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            this.SystemTrayIcon.ContextMenuStrip = _helper.GetContextMenu(ucShortCuts1.imageListBig, true);
            this.SystemTrayIcon.ContextMenuStrip.Show(Cursor.Position);
            this.SystemTrayIcon.ContextMenuStrip.BringToFront();
        }

        

        private void ContextMenuExit(object sender, EventArgs e)
        {
            this.SystemTrayIcon.Visible = false;
            Application.Exit();
            Environment.Exit(0);
        }
      


        private void WindowResize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void WindowClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            _helper.GetContextMenu(ucShortCuts1.imageListBig, false);
            Debug.WriteLine("Menu Cached on exit");
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (KnifeUtils.StartupApp)
                {

                    key.SetValue(KnifeUtils.APP_TITLE, Application.ExecutablePath);
                }
                else
                {
                    key.DeleteValue(KnifeUtils.APP_TITLE, false);
                }
            }
            catch
            {

            }
            

        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ContextMenuExit(sender, e);
        }

        private void ShowLiveMenu(Control sender, bool cached)
        {
            string msgNotCached = "Menu will build based on present configuration. \nProcess can take a bit to execute, but at the end, all settings will be cached for future use.\n\nProceed?";
            string msgCache = "Menu will build based on cached values. \nIf no cache is present , the process can take a bit to execute, but at the end, settings are cached for future use.\n\nProceed?";
            string msg = msgNotCached;
            if (cached)
            {
                msg = msgCache;
            }
            DialogResult rest = MessageBox.Show(msg, KnifeUtils.APP_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (rest == DialogResult.No) return;
            this.Cursor = Cursors.Hand;
            Control btnSender = (Control)sender;
            Point ptLowerLeft = new Point(0, btnSender.Height);
            ptLowerLeft = btnSender.PointToScreen(ptLowerLeft);
            this.Cursor = Cursors.WaitCursor;
            ctMenuStrip= _helper.GetContextMenu(ucShortCuts1.imageListBig, cached);
            ctMenuStrip.Show(ptLowerLeft);
            this.Cursor = Cursors.Default;
            MessageBox.Show("Menu built and cached!", KnifeUtils.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            ShowLiveMenu((Control)sender, chkMenuCache.Checked);
        }

        
    }
}
