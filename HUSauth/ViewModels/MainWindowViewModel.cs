using HUSauth.Helpers;
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

            network = new Network();
            var listener = new PropertyChangedEventListener(network);
            listener.RegisterHandler((sender, e) => ViewUpdateHandler(sender, e));
            this.CompositeDisposable.Add(listener);

            network.StartAuthenticationCheckTimer();

            if (network.IsAvailable() == true)
            {
                UpdateCheck();
            }
        }

        private async void UpdateCheck() // 本来ならここに書くべきではない気もするけどしょうがないじゃん
        {
            var uc = new UpdateChecker();
            UpdateInfoPack uip = await Task.Run(() => uc.UpdateCheck(version));

            if (uip.UpdateAvailable == true)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show(
                    "新しいバージョンの HUSauth が見つかりました。\n" + uip.CurrentVersion + " -> " + uip.AvailableVersion + "\n\nダウンロードしますか？",
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
            if (network.IsAvailable() == true)
            {
                return;
            }

            network.isStarted = false;
            network.StopAuthenticationCheckTimer();

            ChangeStatusBarString("ネットワーク認証を確認しています");

            var IsConnected = false;

            try
            {
                IsConnected = await Task.Run(() => network.IsAvailable());
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
                            NotifyIconHelper.ShowNotifyBaloon("自動認証失敗", "ネットワークに接続されていません");
                            return;
                        }

                        if (network.CheckIPAddress() == true)
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

            network.StartAuthenticationCheckTimer();
        }

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
            var id = ID;
            var password = Password;

            await Task.Run(() =>
            {
                for (int i = 0; i <= 60; i++)
                {
                    if (i == 60)
                    {
                        NotifyIconHelper.ShowNotifyBaloon("自動認証失敗", "ネットワークに接続されていません");
                        return;
                    }

                    if (network.CheckIPAddress() == true)
                    {
                        break;
                    }

                    Thread.Sleep(1000);
                }
            });

            await Task.Run(() => network.DoAuth(id, password));

            int j = 0;
            while (j < 60)
            {
                var IsConnected = false;

                ChangeStatusBarString("認証中...");


                try
                {
                    IsConnected = await Task.Run(() => network.IsAvailable());
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
            var cw = new Views.ConfigWindow();
            cw.ShowDialog();
        }
        #endregion

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
            var aw = new Views.AboutWindow();
            aw.ShowDialog();
        }
        #endregion


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
                NotifyIconHelper.ShowNotifyBaloon("HUSauth", worker.NetworkStatusBaloonString);
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
    }
}