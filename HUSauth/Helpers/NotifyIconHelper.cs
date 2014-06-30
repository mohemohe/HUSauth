using HUSauth.Models;
using HUSauth.ViewModels;
using HUSauth.Views;
using Livet;
using Livet.EventListeners;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HUSauth.Helpers
{
    public class NotifyIconHelper
    {
        private static NotifyIcon notifyIcon;
        private static MainWindow mw;

        public static void Initialize()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = new Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("HUSauth.Views.Resource.HUSauth_tasktray.ico"));
            notifyIcon.Visible = true;

            notifyIcon.DoubleClick += (sender, e) => MainWindowOpen();

            var cms = new ContextMenuStrip();
            var tsm0 = new ToolStripMenuItem();
            var tsm1 = new ToolStripMenuItem();

            tsm0.Text = "Restore";
            tsm1.Text = "Exit";
            cms.Items.AddRange(new ToolStripMenuItem[] { tsm0, tsm1 });

            tsm0.Click += (sender, e) => MainWindowOpen();
            tsm1.Click += (sender, e) =>
            {
                MainWindowClose();
                MainWindowExit();
            };

            notifyIcon.ContextMenuStrip = cms;

            mw = new Views.MainWindow();
            MainWindowOpen();
        }

        public static void Dispose()
        {
            notifyIcon.Dispose();
        }

        private static void MainWindowOpen()
        {
            mw.Show();
        }

        public static void MainWindowClose()
        {
            mw.Hide();
            ShowNotifyBaloon("認証状況の監視中", "右クリックメニューから終了できます");
        }

        public static void MainWindowExit()
        {
            mw.Close();
        }

        #region public static void ShowNotifyBaloon()

        public static void ShowNotifyBaloon(string title, string body)
        {
            if (notifyIcon.Visible == true && notifyIcon.Icon != null)
            {
                notifyIcon.BalloonTipTitle = title;
                notifyIcon.BalloonTipText = body;

                notifyIcon.ShowBalloonTip(10000);
            }
        }

        public static void ShowNotifyBaloon(string title, string body, int timeout)
        {
            if (notifyIcon.Visible == true && notifyIcon.Icon != null)
            {
                notifyIcon.BalloonTipTitle = title;
                notifyIcon.BalloonTipText = body;

                notifyIcon.ShowBalloonTip(timeout);
            }
        }

        public static void ShowNotifyBaloon(string title, string body, ToolTipIcon icon, int timeout)
        {
            if (notifyIcon.Visible == true && notifyIcon.Icon != null)
            {
                notifyIcon.BalloonTipTitle = title;
                notifyIcon.BalloonTipText = body;
                notifyIcon.BalloonTipIcon = icon;

                notifyIcon.ShowBalloonTip(timeout);
            }
        }

        #endregion public static void ShowNotifyBaloon()
    }
}
