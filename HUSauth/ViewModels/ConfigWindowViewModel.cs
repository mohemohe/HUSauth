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

namespace HUSauth.ViewModels
{
    public class ConfigWindowViewModel : ViewModel
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

        #region プロパティ

        #region ExcludeIP1変更通知プロパティ
        private string _ExcludeIP1;

        public string ExcludeIP1
        {
            get
            { return _ExcludeIP1; }
            set
            {
                if (_ExcludeIP1 == value)
                    return;
                _ExcludeIP1 = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ExcludeIP2変更通知プロパティ
        private string _ExcludeIP2;

        public string ExcludeIP2
        {
            get
            { return _ExcludeIP2; }
            set
            { 
                if (_ExcludeIP2 == value)
                    return;
                _ExcludeIP2 = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ExcludeIP3変更通知プロパティ
        private string _ExcludeIP3;

        public string ExcludeIP3
        {
            get
            { return _ExcludeIP3; }
            set
            { 
                if (_ExcludeIP3 == value)
                    return;
                _ExcludeIP3 = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region AnotherAuthServer変更通知プロパティ
        private string _AnotherAuthServer;

        public string AnotherAuthServer
        {
            get
            { return _AnotherAuthServer; }
            set
            { 
                if (_AnotherAuthServer == value)
                    return;
                _AnotherAuthServer = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region AllowUpdateCheck変更通知プロパティ
        private bool _AllowUpdateCheck;

        public bool AllowUpdateCheck
        {
            get
            { return _AllowUpdateCheck; }
            set
            { 
                if (_AllowUpdateCheck == value)
                    return;
                _AllowUpdateCheck = value;

                if (value == true)
                {
                    AllowAutoUpdate_IsEnable = true;
                }
                else
                {
                    AllowAutoUpdate_IsEnable = false;
                }

                RaisePropertyChanged();
            }
        }
        #endregion

        #region AllowAutoUpdate変更通知プロパティ
        private bool _AllowAutoUpdate;

        public bool AllowAutoUpdate
        {
            get
            { return _AllowAutoUpdate; }
            set
            { 
                if (_AllowAutoUpdate == value)
                    return;
                _AllowAutoUpdate = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region AllowAutoUpdate_IsEnable変更通知プロパティ
        private bool _AllowAutoUpdate_IsEnable;

        public bool AllowAutoUpdate_IsEnable
        {
            get
            { return _AllowAutoUpdate_IsEnable; }
            set
            { 
                if (_AllowAutoUpdate_IsEnable == value)
                    return;
                _AllowAutoUpdate_IsEnable = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #endregion

        public void Initialize()
        {
            ReadSettings();
        }

        private void ReadSettings()
        {
            ExcludeIP1 = Settings.ExcludeIP1;
            ExcludeIP2 = Settings.ExcludeIP2;
            ExcludeIP3 = Settings.ExcludeIP3;
            AnotherAuthServer = Settings.AnotherAuthServer;
            AllowUpdateCheck = Settings.AllowUpdateCheck;
            AllowAutoUpdate = Settings.AllowAutoUpdate;
        }

        private void WriteSettings()
        {
            Settings.ExcludeIP1 = ExcludeIP1;
            Settings.ExcludeIP2 = ExcludeIP2;
            Settings.ExcludeIP3 = ExcludeIP3;
            Settings.AnotherAuthServer = AnotherAuthServer;
            Settings.AllowUpdateCheck = AllowUpdateCheck;
            Settings.AllowAutoUpdate = AllowAutoUpdate;
        }

        #region OKCommand
        private ViewModelCommand _OKCommand;

        public ViewModelCommand OKCommand
        {
            get
            {
                if (_OKCommand == null)
                {
                    _OKCommand = new ViewModelCommand(OK);
                }
                return _OKCommand;
            }
        }

        public void OK()
        {
            WriteSettings();
            Close();
        }
        #endregion
        
        #region CancelCommand
        private ViewModelCommand _CancelCommand;

        public ViewModelCommand CancelCommand
        {
            get
            {
                if (_CancelCommand == null)
                {
                    _CancelCommand = new ViewModelCommand(Cancel);
                }
                return _CancelCommand;
            }
        }

        public void Cancel()
        {
            Close();
        }
        #endregion

        #region ApplyCommand
        private ViewModelCommand _ApplyCommand;

        public ViewModelCommand ApplyCommand
        {
            get
            {
                if (_ApplyCommand == null)
                {
                    _ApplyCommand = new ViewModelCommand(Apply);
                }
                return _ApplyCommand;
            }
        }

        public void Apply()
        {
            WriteSettings();
        }
        #endregion

        public void Close()
        {
            Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));
        }
    }
}
