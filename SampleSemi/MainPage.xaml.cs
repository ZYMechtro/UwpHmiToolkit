using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
            MySemi.Events.Add(new Gem.GemEvent(1));
            MySemi.Events.Add(new Gem.GemEvent(2));
            MySemi.Events.Add(new Gem.GemEvent(3));
            MySemi.Alarms.Add(new Gem.GemAlarm(10, "Alarm10"));
            MySemi.Alarms.Add(new Gem.GemAlarm(11, "Alarm11"));
            MySemi.Alarms.Add(new Gem.GemAlarm(12, "Alarm12"));

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

        private async void Button_Send_Click(object sender, RoutedEventArgs e)
        {
            MySemi.SendEventReport(1);
            await Task.Delay(3000);
            MySemi.SendEventReport(2);
        }

    }
}
