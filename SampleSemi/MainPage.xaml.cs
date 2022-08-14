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
            hsmsSetting.LocalIpAddress = "192.168.1.105";
            hsmsSetting.LocalPort = "5000";
            hsmsSetting.TargetIpAddress = "192.168.1.106";
            hsmsSetting.TargetPort = "5000";
            MySemi = new Gem(hsmsSetting, this.Dispatcher);
            MySemi.Events.Add(new Gem.GemEvent(1));
            MySemi.Events.Add(new Gem.GemEvent(2));
            MySemi.Events.Add(new Gem.GemEvent(3));
            MySemi.Alarms.Add(new Gem.GemAlarm(10, "Alarm10"));
            MySemi.Alarms.Add(new Gem.GemAlarm(11, "Alarm11"));
            MySemi.Alarms.Add(new Gem.GemAlarm(12, "Alarm12"));
            MySemi.Statuses.Add(new Gem.GemStatus(1, new F4(0.354f), "Ratio", "%"));
            MySemi.Statuses.Add(new Gem.GemStatus(2, new U4(154), "Count"));

            MySemi.MessageRecord += MySemi_ServerMessageUpdate;

            //MySemi.Start();
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
            MySemi.Stop();

            MySemi.Start();
        }

        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            MySemi.Stop();
        }

        private void Button_Send_Click(object sender, RoutedEventArgs e)
        {
            MySemi.EstablishComm();
        }

        private void Pb_Role_Click(object sender, RoutedEventArgs e)
        {
            hsmsSetting.Mode = hsmsSetting.Mode == HsmsSetting.ConnectionMode.Passive
                ? HsmsSetting.ConnectionMode.Active
                : HsmsSetting.ConnectionMode.Passive;
        }
    }
}
