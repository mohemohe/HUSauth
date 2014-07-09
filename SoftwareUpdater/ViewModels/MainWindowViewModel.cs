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

using SoftwareUpdater.Models;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace SoftwareUpdater.ViewModels
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

        Model m = new Model();
        UpdateInfoPack uip;
        string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public void Initialize()
        {
            var ui = new UpdateInfomation();
            uip = ui.GetUpdateInfomation();

            Name = uip.AppricationName;
            Infomation = uip.Infomation;

            var listener = new PropertyChangedEventListener(m);
            listener.RegisterHandler((sender, e) => UpdateHandler(sender, e));
            this.CompositeDisposable.Add(listener);

            if (uip.CanAutoUpdate == true)
            {
                Update();
            }
            else
            {
                CantUpdate();
            }
        }

        public void Update()
        {
            AddLog("Update to " + uip.AvailableVersion + " .");

            AddLog("Downloading '" + Path.GetFileName(uip.DownloadURL) + "' .");

            if (Directory.Exists(appPath + @"\tmp\") == false)
            {
                Directory.CreateDirectory(appPath + @"\tmp\");
            }
            m.download(uip.DownloadURL, appPath + @"\tmp\");

            
        }

        public async void CantUpdate()
        {
            AddLog("Can't auto update to " + uip.AvailableVersion + " .");
            AddLog("Please download zipped application, and overwrite manually.");
            AddLog("");
            AddLog("Auto updater will soon open the default web browser...");

            await Task.Run(() =>    
            {
                Thread.Sleep(3000);
                Process.Start(uip.DownloadURL);
                Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));
            });
        }

        private async void UpdateHandler(object sender, PropertyChangedEventArgs e)
        {
            var worker = sender as Model;
            if (e.PropertyName == "Downloaded")
            {
                AddLog("Downloaded!");
                AddLog("UnZipping...");
                m.UnZip(appPath + @"\tmp\tmp.zip", appPath + @"\tmp\");
            }
            if (e.PropertyName == "UnZipped")
            {
                AddLog("UnZipped!");
                AddLog("Copying files...");
                m.FileCopy(appPath + @"\tmp\", appPath);
            }
            if (e.PropertyName == "CopiedFileName")
            {
                AddLog("Copied: " + worker.CopiedFileName);
            }
            if (e.PropertyName == "Copied")
            {
                AddLog("Update complete!");
                AddLog("Delete temporary files...");
                m.DeleteDir(appPath + @"\tmp\");
            }
            if (e.PropertyName == "Deleted")
            {
                AddLog("delete complete!");
                AddLog("Restarting...");
                m.Restart(Path.Combine(appPath, Name + ".exe"));
            }
            if (e.PropertyName == "Restarted")
            {
                AddLog("Restarted!");
                AddLog("");
                AddLog("This updater will exit in 5sec...");

                await Task.Run(() => 
                {
                    Thread.Sleep(5000);
                    Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));
                });
                            }
            if (e.PropertyName == "AnotherLogMessage")
            {
                AddLog(worker.AnotherLogMessage);
            }
        }

        #region Name変更通知プロパティ
        private string _Name;

        public string Name
        {
            get
            { return _Name; }
            set
            { 
                if (_Name == value)
                    return;
                _Name = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Infomation変更通知プロパティ
        private string _Infomation;

        public string Infomation
        {
            get
            { return _Infomation; }
            set
            { 
                if (_Infomation == value)
                    return;
                _Infomation = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Log変更通知プロパティ
        private string _Log = "";

        public string Log
        {
            get
            { return _Log; }
            set
            { 
                if (_Log == value)
                    return;
                _Log = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        public void AddLog(string str)
        {
            string currentLog = Log;

            if (currentLog == "")
            {
                Log = str;
            }
            else
            {
                Log = currentLog + "\n" + str;
            }
        }
    }
}
