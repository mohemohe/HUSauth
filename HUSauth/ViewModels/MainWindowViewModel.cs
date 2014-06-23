using HUSauth.Models;
using Livet;
using Livet.Commands;
using Livet.EventListeners;
using Livet.Messaging.Windows;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace HUSauth.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        /* コマンド、プロパティの定義にはそれぞれ
         *
         *  lvcom   : ViewModelCommand
         *  lvcomn  : ViewModelCommand(CanExecute無)
         *  llcom   : ListenerCommand(パラメータ有のコマンド)
         *  llcomn  : ListenerCommand(パラメータ有のコマンド・CanExecute無)
         *  lprop   : 変更通知プロパティ(.NET4.5ではlpropn)
         *
         * を使用してください。
         *
         * Modelが十分にリッチであるならコマンドにこだわる必要はありません。
         * View側のコードビハインドを使用しないMVVMパターンの実装を行う場合でも、ViewModelにメソッドを定義し、
         * LivetCallMethodActionなどから直接メソッドを呼び出してください。
         *
         * ViewModelのコマンドを呼び出せるLivetのすべてのビヘイビア・トリガー・アクションは
         * 同様に直接ViewModelのメソッドを呼び出し可能です。
         */

        /* ViewModelからViewを操作したい場合は、View側のコードビハインド無で処理を行いたい場合は
         * Messengerプロパティからメッセージ(各種InteractionMessage)を発信する事を検討してください。
         */

        /* Modelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedEventListenerや
         * CollectionChangedEventListenerを使うと便利です。各種ListenerはViewModelに定義されている
         * CompositeDisposableプロパティ(LivetCompositeDisposable型)に格納しておく事でイベント解放を容易に行えます。
         *
         * ReactiveExtensionsなどを併用する場合は、ReactiveExtensionsのCompositeDisposableを
         * ViewModelのCompositeDisposableプロパティに格納しておくのを推奨します。
         *
         * LivetのWindowテンプレートではViewのウィンドウが閉じる際にDataContextDisposeActionが動作するようになっており、
         * ViewModelのDisposeが呼ばれCompositeDisposableプロパティに格納されたすべてのIDisposable型のインスタンスが解放されます。
         *
         * ViewModelを使いまわしたい時などは、ViewからDataContextDisposeActionを取り除くか、発動のタイミングをずらす事で対応可能です。
         */

        /* UIDispatcherを操作する場合は、DispatcherHelperのメソッドを操作してください。
         * UIDispatcher自体はApp.xaml.csでインスタンスを確保してあります。
         *
         * LivetのViewModelではプロパティ変更通知(RaisePropertyChanged)やDispatcherCollectionを使ったコレクション変更通知は
         * 自動的にUIDispatcher上での通知に変換されます。変更通知に際してUIDispatcherを操作する必要はありません。
         */

        private static NotifyIcon notifyIcon;
        public Network network;

        private Version _version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        public string version
        {
            get { return _version.ToString(); }
        }

        public void Initialize()
        {
            ChangeStatusBarString("ネットワーク認証を確認しています");
            
            Settings.Initialize();

            if (Settings.ID != null || Settings.Password != "")
            {
                ID = Settings.ID;
                Password = Settings.Password;

                SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
            }

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = new Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("HUSauth.Views.Resource.HUSauth_tasktray.ico"));
            notifyIcon.Visible = true;

            notifyIcon.DoubleClick += (sender, e) => Restore();

            var cms = new ContextMenuStrip();
            var tsm0 = new ToolStripMenuItem();
            var tsm1 = new ToolStripMenuItem();

            tsm0.Text = "Restore";
            tsm1.Text = "Exit";
            cms.Items.AddRange(new ToolStripMenuItem[] { tsm0, tsm1 });

            tsm0.Click += (sender, e) => Restore();
            tsm1.Click += (sender, e) => Exit();

            notifyIcon.ContextMenuStrip = cms;

            network = new Network();
            var listener = new PropertyChangedEventListener(network);
            listener.RegisterHandler((sender, e) => ViewUpdateHandler(sender, e));
            this.CompositeDisposable.Add(listener);

            network.StartAuthenticationCheckTimer();

            UpdateCheck();
        }

        private async void UpdateCheck() // 本来ならここに書くべきではない気もするけどしょうがないじゃん
        {
            var uc = new UpdateChecker();
            UpdateInfoPack uip = await Task.Run(() => uc.UpdateCheck(version));

            if (uip.UpdateAvailable == true)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show(
                    "新しいバージョンの HUSauth が見つかりました。\n" + uip.CurrentVersion + " -> " + uip.NextVersion + "\n\n配布サイトを開きますか？",
                    "アップデートのお知らせ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("http://ghippos.net/app/husauth.html");
                }
            }
        }

        private async void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (Network.IsAvailable() == true)
            {
                return;
            }

            network.isStarted = false;
            network.StopAuthenticationCheckTimer();

            ChangeStatusBarString("ネットワーク認証を確認しています");

            var IsConnected = false;

            try
            {
                IsConnected = await Task.Run(() => Network.IsAvailable());
            }
            catch
            {
                ChangeStatusBarString("ネットワークに接続されていません");
            }

            if (IsConnected == true)
            {
                ChangeStatusBarString("認証されています");
            }
            else
            {
                await Task.Run(() => {
                    for (int i = 0; i <= 60; i++)
                    {
                        if (i == 60)
                        {
                            ShowNotifyBaloon("自動認証失敗", "ネットワークに接続されていません");
                            return;
                        }

                        if (Network.CheckIPAddress() == true)
                        {
                            break;
                        }

                        Thread.Sleep(1000);
                    }
                });
                
                Login();

                if (IsShowTaskBar == false)
                {
                    ShowNotifyBaloon("自動認証成功", "ネットワークの自動認証に成功しました");
                }
            }

            network.StartAuthenticationCheckTimer();
        }

        #region CloseCommand

        //lvcomn
        private ViewModelCommand _CloseCommand;

        public ViewModelCommand CloseCommand
        {
            get
            {
                if (_CloseCommand == null)
                {
                    _CloseCommand = new ViewModelCommand(Close);
                }
                return _CloseCommand;
            }
        }

        public void Close()
        {
            IsShowTaskBar = false;
            //Messenger.Raise(new WindowActionMessage(WindowAction.Minimize, "Close"));
            Opacity = 0.0; // Alt+Tabで表示されちゃうけどしょうがない

            ShowNotifyBaloon("認証状況の監視中", "右クリックメニューから終了できます");
        }

        #endregion CloseCommand

        #region ExitCommand

        private ViewModelCommand _ExitCommand;

        public ViewModelCommand ExitCommand
        {
            get
            {
                if (_ExitCommand == null)
                {
                    _ExitCommand = new ViewModelCommand(Exit);
                }
                return _ExitCommand;
            }
        }

        public void Exit()
        {
            if (ID != null && Password != null)
            {
                Settings.ID = ID;
                Settings.Password = Password;

                Settings.WriteSettings();
            }
            notifyIcon.Visible = false;
            notifyIcon.Dispose();

            Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));
        }

        #endregion ExitCommand

        #region MinimizeCommand

        private ViewModelCommand _MinimizeCommand;

        public ViewModelCommand MinimizeCommand
        {
            get
            {
                if (_MinimizeCommand == null)
                {
                    _MinimizeCommand = new ViewModelCommand(Minimize);
                }
                return _MinimizeCommand;
            }
        }

        public void Minimize()
        {
            Messenger.Raise(new WindowActionMessage(WindowAction.Minimize, "Minimize"));
        }

        #endregion MinimizeCommand

        #region RestoreCommand

        private ViewModelCommand _RestoreCommand;

        public ViewModelCommand RestoreCommand
        {
            get
            {
                if (_RestoreCommand == null)
                {
                    _RestoreCommand = new ViewModelCommand(Restore);
                }
                return _RestoreCommand;
            }
        }

        public void Restore()
        {
            IsShowTaskBar = true;
            //Messenger.Raise(new WindowActionMessage(WindowAction.Normal, "Normal"));
            //Messenger.Raise(new WindowActionMessage(WindowAction.Active, "Active")); //アクティブにならないんですがそれは
            Opacity = 1.0; // 妥協
        }

        #endregion RestoreCommand

        #region LoginCommand

        private ViewModelCommand _LoginCommand;

        public ViewModelCommand LoginCommand
        {
            get
            {
                if (_LoginCommand == null)
                {
                    _LoginCommand = new ViewModelCommand(Login);
                }
                return _LoginCommand;
            }
        }

        public async void Login()
        {
            var id = ID;
            var password = Password;

            await Task.Run(() =>
            {
                for (int i = 0; i <= 60; i++)
                {
                    if (i == 60)
                    {
                        ShowNotifyBaloon("自動認証失敗", "ネットワークに接続されていません");
                        return;
                    }

                    if (Network.CheckIPAddress() == true)
                    {
                        break;
                    }

                    Thread.Sleep(1000);
                }
            });

            await Task.Run(() => Network.DoAuth(id, password));

            int j = 0;
            while (j < 60)
            {
                var IsConnected = false;

                ChangeStatusBarString("認証中...");


                try
                {
                    IsConnected = await Task.Run(() => Network.IsAvailable());
                }
                catch { }

                if (IsConnected)
                {
                    ChangeStatusBarString("認証されています");
                    UpdateCheck();

                    return;
                }

                await Task.Run(() => Thread.Sleep(1000));
                j++;
            }

            ChangeStatusBarString("認証に失敗しました");
        }

        #endregion LoginCommand

        #region StatusBarString変更通知プロパティ

        //lpropn
        private string _StatusBarString;

        public string StatusBarString
        {
            get
            { return _StatusBarString; }
            set
            {
                if (_StatusBarString == value)
                    return;
                _StatusBarString = value;
                RaisePropertyChanged();
            }
        }

        #endregion StatusBarString変更通知プロパティ

        public void ChangeStatusBarString(string str)
        {
            StatusBarString = str;
        }

        private void ViewUpdateHandler(object sender, PropertyChangedEventArgs e)
        {
            var worker = sender as Network;
            if (e.PropertyName == "NetworkStatusString")
            {
                ChangeStatusBarString(worker.NetworkStatusString);
            }
            if (e.PropertyName == "NetworkStatusBaloonString")
            {
                ShowNotifyBaloon("HUSauth", worker.NetworkStatusBaloonString);
            }
        }

        #region IsShowTaskBar変更通知プロパティ

        private bool _IsShowTaskBar = true;

        public bool IsShowTaskBar
        {
            get
            { return _IsShowTaskBar; }
            set
            {
                if (_IsShowTaskBar == value)
                    return;
                _IsShowTaskBar = value;
                RaisePropertyChanged();
            }
        }

        #endregion IsShowTaskBar変更通知プロパティ

        #region Opacity変更通知プロパティ

        private double _Opacity = 1.0;

        public double Opacity
        {
            get
            { return _Opacity; }
            set
            {
                if (_Opacity == value)
                    return;
                _Opacity = value;
                RaisePropertyChanged();
            }
        }

        #endregion Opacity変更通知プロパティ

        #region ID変更通知プロパティ

        private string _ID;

        public string ID
        {
            get
            { return _ID; }
            set
            {
                if (_ID == value)
                    return;
                _ID = value;
                RaisePropertyChanged();
            }
        }

        #endregion ID変更通知プロパティ

        #region Password変更通知プロパティ

        private string _Password;

        public string Password
        {
            get
            { return _Password; }
            set
            {
                if (_Password == value)
                    return;
                _Password = value;
                RaisePropertyChanged();
            }
        }

        #endregion Password変更通知プロパティ

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