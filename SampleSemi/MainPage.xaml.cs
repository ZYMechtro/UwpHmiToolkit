using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UwpHmiToolkit.Semi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static UwpHmiToolkit.Semi.HsmsMessage;
using static UwpHmiToolkit.Semi.SecsII;

namespace SampleSemi
{
    public sealed partial class MainPage : Page
    {
        private HsmsSetting hsmsSetting = new HsmsSetting();
        private Gem MySemi;
        public MainPage()
        {
            this.InitializeComponent();
            MySemi = new Gem(hsmsSetting, this.Dispatcher);
            MySemi.ServerMessageUpdate += MySemi_ServerMessageUpdate;
            MySemi.ClientMessageUpdate += MySemi_ClientMessageUpdate;

            MySemi.Start();
        }

        private async void MySemi_ClientMessageUpdate(string message)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ClientListBox.Items.Insert(0, message);
            });
        }

        private async void MySemi_ServerMessageUpdate(string message)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ServerListBox.Items.Insert(0, message);
            });
        }

        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            MySemi.Start();
        }

        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            MySemi.Stop();
        }

        private void Button_Send_Click(object sender, RoutedEventArgs e)
        {
            var bs = MySemi.RcmdReply(5);
            
            ClientListBox.Items.Insert(0, DecodeSecsII(bs).ToSML(0));
        
        }

    }
}
