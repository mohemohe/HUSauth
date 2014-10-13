using HUSauth.Helpers;
using HUSauth.Models;
using HUSauth.Views;
using Livet;
using Livet.Commands;
using Livet.EventListeners;
using Livet.Messaging;
using Livet.Messaging.Windows;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

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

        private readonly Version _version = Assembly.GetExecutingAssembly().GetName().Version;
        private readonly string _appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private Network _network;

        public string Version
        {
            get { return _version.ToString(); }
        }

// ReSharper disable once UnusedMember.Global
        public void Initialize()
        {
            ChangeStatusBarString("ネットワーク認証を確認しています");

            Settings.Initialize();

            if (Settings.ID != null || Settings.Password != "")
            {
                ID = Settings.ID;
                Password = Settings.Password;

                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            }

            _network = new Network();
            var listener = new PropertyChangedEventListener(_network);
            listener.RegisterHandler(ViewUpdateHandler);
            CompositeDisposable.Add(listener);

            _network.StartAuthenticationCheckTimer();

            if (_network.IsAvailable())
            {
                UpdateCheck();
            }
        }

        private async void UpdateCheck() // 本来ならここに書くべきではない気もするけどしょうがないじゃん
        {
            if (Settings.AllowUpdateCheck == false)
            {
                return;
            }

            var uc = new UpdateChecker();
            UpdateInfoPack uip = await Task.Run(() => uc.UpdateCheck(Version));

            if (uip.UpdateAvailable)
            {
                if (File.Exists(Path.Combine(_appPath, "SoftwareUpdater.exe")))
                {
                    if (Settings.AllowAutoUpdate)
                    {
                        if (ID != null && Password != null)
                        {
                            Settings.ID = ID;
                            Settings.Password = Password;

                            Settings.WriteSettings();
                        }

                        Process.Start(Path.Combine(_appPath, "SoftwareUpdater.exe"));

                        NotifyIconHelper.MainWindowExit();
                        return;
                    }
                }

                MessageBoxResult result = MessageBox.Show(
                    "新しいバージョンの HUSauth が見つかりました。\n" + uip.CurrentVersion + " -> " + uip.AvailableVersion +
                    "\n\nダウンロードしますか？",
                    "アップデートのお知らせ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(uip.DownloadURL);
                }
            }
        }

        private async void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (_network.IsAvailable())
            {
                return;
            }

            _network.IsStarted = false;
            _network.StopAuthenticationCheckTimer();

            ChangeStatusBarString("ネットワーク認証を確認しています");

            bool isConnected = false;

            try
            {
                isConnected = await Task.Run(() => _network.IsAvailable());
            }
            catch
            {
                ChangeStatusBarString("ネットワークに接続されていません");
            }

            if (isConnected)
            {
                ChangeStatusBarString("認証されています");
            }
            else
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i <= 60; i++)
                    {
                        if (i == 60)
                        {
                            NotifyIconHelper.ShowNotifyBaloon("自動認証失敗", "ネットワークに接続されていません");
                            return;
                        }

                        if (_network.CheckIPAddress())
                        {
                            break;
                        }

                        Thread.Sleep(1000);
                    }
                });

                Login();

                if (IsShowTaskBar == false)
                {
                    NotifyIconHelper.ShowNotifyBaloon("自動認証成功", "ネットワークの自動認証に成功しました");
                }
            }

            _network.StartAuthenticationCheckTimer();
        }

        public void ChangeStatusBarString(string str)
        {
            StatusBarString = str;
        }

        private void ViewUpdateHandler(object sender, PropertyChangedEventArgs e)
        {
            var worker = sender as Network;
            if (worker == null) throw new ArgumentNullException();

            if (e.PropertyName == "NetworkStatusString")
            {
                ChangeStatusBarString(worker.NetworkStatusString);
            }
            if (e.PropertyName == "NetworkStatusBaloonString")
            {
                NotifyIconHelper.ShowNotifyBaloon("HUSauth", worker.NetworkStatusBaloonString);
            }
        }

        #region IsShowTaskBar変更通知プロパティ

        private bool _IsShowTaskBar = true;

        public bool IsShowTaskBar
        {
            get { return _IsShowTaskBar; }
            set
            {
                if (_IsShowTaskBar == value)
                    return;
                _IsShowTaskBar = value;
                RaisePropertyChanged();
            }
        }

        #endregion IsShowTaskBar変更通知プロパティ

        #region ID変更通知プロパティ

        private string _ID;

        public string ID
        {
            get { return _ID; }
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
            get { return _Password; }
            set
            {
                if (_Password == value)
                    return;
                _Password = value;
                RaisePropertyChanged();
            }
        }

        #endregion Password変更通知プロパティ

        #region CloseCommand

        //lvcomn
        private ViewModelCommand _CloseWindowCommand;

        public ViewModelCommand CloseWindowCommand
        {
            get
            {
                if (_CloseWindowCommand == null)
                {
                    _CloseWindowCommand = new ViewModelCommand(CloseWindow);
                }
                return _CloseWindowCommand;
            }
        }

        public void CloseWindow()
        {
            if (ID != null && Password != null)
            {
                Settings.ID = ID;
                Settings.Password = Password;

                Settings.WriteSettings();
            }

            //Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));
            NotifyIconHelper.MainWindowClose();
        }

        #endregion CloseCommand

        #region MinimizeCommand

        private ViewModelCommand _MinimizeWindowCommand;

        public ViewModelCommand MinimizeWindowCommand
        {
            get
            {
                if (_MinimizeWindowCommand == null)
                {
                    _MinimizeWindowCommand = new ViewModelCommand(MinimizeWindow);
                }
                return _MinimizeWindowCommand;
            }
        }

        public void MinimizeWindow()
        {
            Messenger.Raise(new WindowActionMessage(WindowAction.Minimize, "Minimize"));
        }

        #endregion MinimizeCommand

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
            ChangeStatusBarString("認証中...");
            LoginButtonIsEnabled = false;

            string id = ID;
            string password = Password;

            await Task.Run(() =>
            {
                for (int i = 0; i <= 60; i++)
                {
                    if (i == 60)
                    {
                        NotifyIconHelper.ShowNotifyBaloon("自動認証失敗", "ネットワークに接続されていません");
                        return;
                    }

                    if (_network.CheckIPAddress())
                    {
                        break;
                    }

                    Thread.Sleep(1000);
                }
            });

            await Task.Run(() =>
            {
                try
                {
                    _network.DoAuth(id, password);
                }
                catch (NullException e)
                {
                    Messenger.Raise(new InformationMessage(e.Message, "error", "Information"));
                }
            });

            int j = 0;
            while (j < 60)
            {
                bool isConnected = false;

                try
                {
                    isConnected = await Task.Run(() => _network.IsAvailable());
                }
                catch
                {
                }

                if (isConnected)
                {
                    ChangeStatusBarString("認証されています");

                    try
                    {
                        UpdateCheck();
                    }
                    catch { }

                    LoginButtonIsEnabled = true;

                    return;
                }

                await Task.Run(() => Thread.Sleep(1000));
                j++;
            }

            ChangeStatusBarString("認証に失敗しました");
            LoginButtonIsEnabled = true;
        }

        #endregion LoginCommand

        #region LoginButtonIsEnabled変更通知プロパティ

        private bool _LoginButtonIsEnabled = true;

        public bool LoginButtonIsEnabled
        {
            get { return _LoginButtonIsEnabled; }
            set
            {
                if (_LoginButtonIsEnabled == value)
                    return;
                _LoginButtonIsEnabled = value;
                RaisePropertyChanged();
            }
        }

        #endregion LoginButtonIsEnabled変更通知プロパティ

        #region ConfigCommand

        private ViewModelCommand _ConfigCommand;

        public ViewModelCommand ConfigCommand
        {
            get
            {
                if (_ConfigCommand == null)
                {
                    _ConfigCommand = new ViewModelCommand(Config);
                }
                return _ConfigCommand;
            }
        }

        public void Config()
        {
            var cw = new ConfigWindow();
            cw.ShowDialog();
        }

        #endregion ConfigCommand

        #region AboutCommand

        private ViewModelCommand _AboutCommand;

        public ViewModelCommand AboutCommand
        {
            get
            {
                if (_AboutCommand == null)
                {
                    _AboutCommand = new ViewModelCommand(About);
                }
                return _AboutCommand;
            }
        }

        public void About()
        {
            var aw = new AboutWindow();
            aw.ShowDialog();
        }

        #endregion AboutCommand

        #region StatusBarString変更通知プロパティ

        //lpropn
        private string _StatusBarString;

        public string StatusBarString
        {
            get { return _StatusBarString; }
            set
            {
                if (_StatusBarString == value)
                    return;
                _StatusBarString = value;
                RaisePropertyChanged();
            }
        }

        #endregion StatusBarString変更通知プロパティ
    }
}