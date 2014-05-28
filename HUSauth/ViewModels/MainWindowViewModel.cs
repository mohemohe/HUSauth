using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using HUSauth.Models;
using System.Threading.Tasks;
using System.Threading;
using Livet.Behaviors.ControlBinding;
using System.Security;
using Microsoft.Win32;

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

        public async void Initialize()
        {
            ChangeStatusBarString("ネットワーク認証を確認しています");

            Settings.Initialize();

            if (Settings.ID != null || Settings.Password != "")
            {
                ID = Settings.ID;
                Password = Settings.Password;

                SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
            }

            var IsConnected = false;

            try
            {
                IsConnected = await Task.Run(() => Network.AuthenticationCheck());
            }
            catch { }

            if (IsConnected)
            {
                ChangeStatusBarString("認証されています");
            }
            else
            {
                ChangeStatusBarString("認証されていません");
            }
        }

        private async void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (Network.IsAvailable() == true)
            {
                return;
            }

            ChangeStatusBarString("ネットワーク認証を確認しています");

            var IsConnected = false;

            try
            {
                IsConnected = await Task.Run(() => Network.AuthenticationCheck());
            }
            catch
            {
                ChangeStatusBarString("ネットワークに接続されていません");
            }

            if (IsConnected)
            {
                ChangeStatusBarString("認証されています");
                return;
            }
            else
            {
                Login();
            }
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
            if (ID != null && Password != null)
            {
                //if (Settings.ID == null && Settings.Password == null)
                //{
                    Settings.ID = ID;
                    Settings.Password = Password;
                //}

                Settings.WriteSettings();
            }
            Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));
        }
        #endregion


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
        #endregion



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

            //if (Settings.ID == null || Settings.Password == null)
            //{
            //    //Settings.ID = ID;
            //    id = ID;

            //    //Settings.Password = Password;
            //    password = Password;
            //}

            await Task.Run(() => Network.DoAuth(id, password));

            int i = 0;
            while (i < 12)
            {
                var IsConnected = await Task.Run(() => Network.AuthenticationCheck());
                if (IsConnected)
                {
                    ChangeStatusBarString("認証されています");
                    return;
                }
                else
                {
                    ChangeStatusBarString("認証中...");
                }

                await Task.Run(() => Thread.Sleep(5000));
            }

            ChangeStatusBarString("認証に失敗しました");
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
        #endregion

        public void ChangeStatusBarString(string str)
        {
            StatusBarString = str;
        }


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
        #endregion


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
        #endregion


    }
}
