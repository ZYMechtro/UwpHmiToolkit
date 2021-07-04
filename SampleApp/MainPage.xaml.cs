using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using UwpHmiToolkit.Protocol.McProtocol;
using UwpHmiToolkit.ViewModel;
using System.Collections.ObjectModel;

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
            RefreshInterval = 0,
            Timeout = 1000,
            SendDelay = 2,
        };

        public McProtocol Machine1;
        public McProtocol.McWordDevice DM100;

        public MainPage()
        {
            this.InitializeComponent();

            Machine1 = new McProtocol(Setting1);
            Machine1.CommunicationError += Machine1_CommunicationError;
            DM100 = DM(100);
            Machine1.AddRead(DM100);
        }

        private async void Machine1_CommunicationError(string message)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ViewModel1.Messages.Insert(0, message);
            });
        }

        private class ViewModel : AutoBindableBase
        {
            public ObservableCollection<string> Messages { get; set; } = new ObservableCollection<string>();
        }

        private async void Pb_Connect_Click(object sender, RoutedEventArgs e)
        {
            await Machine1.TryConnectAsync();
        }

        private async void Pb_Disconnect_Click(object sender, RoutedEventArgs e)
        {
            await Machine1.TryDisconnectAsync();
        }


        public McProtocol.McWordDevice DM(int address) => new McProtocol.McWordDevice($"D{address}");

    }
}
