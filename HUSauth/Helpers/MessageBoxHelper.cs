using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HUSauth.Helpers
{
    public class MessageBoxPack
    {
        public string messageBoxText { get; set; }
        public string caption { get; set; }
        public MessageBoxButton button { get; set; }
        public MessageBoxImage icon { get; set; }

        public MessageBoxPack(string messageBoxText)
        {
            this.messageBoxText = messageBoxText;
        }

        public MessageBoxPack(string messageBoxText, string caption)
        {
            this.messageBoxText = messageBoxText;
            this.caption = caption;
        }

        public MessageBoxPack(string messageBoxText, string caption, MessageBoxButton button)
        {
            this.messageBoxText = messageBoxText;
            this.caption = caption;
            this.button = button;
        }

        public MessageBoxPack(string messageBoxText, string caption, MessageBoxImage icon)
        {
            this.messageBoxText = messageBoxText;
            this.caption = caption;
            this.icon = icon;
        }

        public MessageBoxPack(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            this.messageBoxText = messageBoxText;
            this.caption = caption;
            this.button = button;
            this.icon = icon;
        }

        public MessageBoxPack(MessageBoxPack mbp)
        {
            this.messageBoxText = mbp.messageBoxText;
            this.caption = mbp.caption;
            this.button = mbp.button;
            this.icon = mbp.icon;
        }
    }

    internal static class MessageBoxHelper
    {
        #region ロック制御

        private static bool _lock { get; set; }

        private static bool GetLockState()
        {
            return _lock;
        }

        private static void Lock()
        {
            _lock = true;
        }

        private static void Unlock()
        {
            _lock = false;
        }

        #endregion

        private static Queue<MessageBoxPack> MessageBoxQueue = new Queue<MessageBoxPack>();

        /// <summary>
        /// キューに送られたとおりに非同期でメッセージボックスを表示します
        /// </summary>
        /// <param name="mbp">パック済みの内容</param>
        public static void AddMessageBoxQueue(MessageBoxPack mbp)
        {
            MessageBoxQueue.Enqueue(mbp);

            if (GetLockState() == false)
            {
                ShowMessageBoxAsync();
            }
        }

        private static async void ShowMessageBoxAsync()
        {
            await Task.Run(() => 
            {
                Lock();

                while (true)
                {
                    var mbp = new MessageBoxPack(MessageBoxQueue.Dequeue());
                    var result = System.Windows.MessageBox.Show(mbp.messageBoxText, mbp.caption, mbp.button, mbp.icon);

                    
                    if(MessageBoxQueue.Count == 0)
                    {
                        break;
                    }
                }

                Unlock();
            });
        }
    }
}
