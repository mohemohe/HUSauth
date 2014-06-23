using Livet.EventListeners.WeakEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HUSauth.Views
{
    /* 
     * ViewModelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedWeakEventListenerや
     * CollectionChangedWeakEventListenerを使うと便利です。独自イベントの場合はLivetWeakEventListenerが使用できます。
     * クローズ時などに、LivetCompositeDisposableに格納した各種イベントリスナをDisposeする事でイベントハンドラの開放が容易に行えます。
     *
     * WeakEventListenerなので明示的に開放せずともメモリリークは起こしませんが、できる限り明示的に開放するようにしましょう。
     */

    /// <summary>
    /// ConfigWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public ConfigWindow()
        {
            InitializeComponent();
        }

        // View内で完結するからViewに書かせてお願いしますなんでもしますから

        private void AnotherAuthServer_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AnotherAuthServer.Text != "")
            {
                AnotherAuthServer.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            }
            else
            {
                AnotherAuthServer.Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
            }
        }

        private void ExcludeIP1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ExcludeIP1.Text != "")
            {
                ExcludeIP1.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            }
            else
            {
                ExcludeIP1.Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
            }
        }

        private void ExcludeIP2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ExcludeIP2.Text != "")
            {
                ExcludeIP2.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            }
            else
            {
                ExcludeIP2.Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
            }
        }

        private void ExcludeIP3_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ExcludeIP3.Text != "")
            {
                ExcludeIP3.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            }
            else
            {
                ExcludeIP3.Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
            }
        }
    }
}