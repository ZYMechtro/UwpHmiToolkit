using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UwpHmiToolkit.Protocol.McProtocol;
using UwpHmiToolkit.ViewModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using static SampleApp.PlcTools.KenyenceDeviceToMcConvert;

namespace SampleApp
{

    public sealed partial class MainPage : Page
    {
        private readonly ViewModel ViewModel1 = new ViewModel();
        public McSetting Setting1 = new McSetting()
        {
            Ip = "192.168.1.10",
            Port = "5000",
            TransmissionLayerProtocol = UwpHmiToolkit.Protocol.TransmissionLayerProtocol.UDP,
            Code = UwpHmiToolkit.Protocol.CommunicationCode.Binary,
            RefreshInterval = 200,
            Timeout = 1000,
            SendDelay = 3,
        };
        public static MainPage Current;

        public McProtocol Machine1;


        public MainPage()
        {
            Current = this;
            Machine1 = new McProtocol(Setting1, this.Dispatcher) { IsReadonly = false };
            Machine1.CommunicationError += Machine1_CommunicationError;
            this.Loaded += MainPage_Loaded;
            this.InitializeComponent();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            Frame1.Navigate(typeof(Page1));
        }

        public void ChangeReadMonitorDevices(List<UwpHmiToolkit.Protocol.EthernetProtocolBase.Device> pageDevices)
        {
            if (Machine1 != null)
            {
                Machine1.RemoveReads();
                Machine1.AddReads(pageDevices);
            }
        }

        private async void Machine1_CommunicationError(string message)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ViewModel1.Messages.Insert(0, message);
            });
        }


        #region PushButtons

        private async void Pb_Connect_Click(object sender, RoutedEventArgs e)
        {
            await Machine1.TryConnectAsync();
        }

        private async void Pb_Disconnect_Click(object sender, RoutedEventArgs e)
        {
            await Machine1.TryDisconnectAsync();
        }

        private void Pb_SwitchPage1_Click(object sender, RoutedEventArgs e) => Frame1.Navigate(typeof(Page1));

        private void Pb_SwitchPage2_Click(object sender, RoutedEventArgs e) => Frame1.Navigate(typeof(Page2));

        #endregion /PushButtons


        private class ViewModel : AutoBindableBase
        {
            public ObservableCollection<string> Messages { get; set; } = new ObservableCollection<string>();
        }

    }

}
