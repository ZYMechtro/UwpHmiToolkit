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
using static SampleApp.KenyenceDeviceToMcConvert;

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

        public McProtocol Machine1;
        public McProtocol.McWordDevice DM100, DM200, EM100, EM200, W0, WFF;
        public McProtocol.McBitDevice R0, R515, MR0, MR515, B0, BFF;


        public MainPage()
        {
            Machine1 = new McProtocol(Setting1, Dispatcher) { IsReadonly = false };

            CreateDevices(); //Create devices before UI bindings them.

            this.InitializeComponent();

            Machine1.CommunicationError += Machine1_CommunicationError;
        }

        private void CreateDevices()
        {
            DM100 = DM(100);
            DM200 = DM(200);
            EM100 = EM(100);
            EM200 = EM(200);
            W0 = W(0, asDouble: true);
            WFF = W(0xff, asFloat: true);

            R0 = R(0, 0);
            R515 = R(5, 0xf);
            MR0 = MR(0, 0);
            MR515 = MR(5, 0xf);
            B0 = B(0);
            BFF = B(0xff);

            Machine1.AddRead(DM100);
            Machine1.AddRead(DM200);
            Machine1.AddRead(EM100);
            Machine1.AddRead(EM200);
            Machine1.AddRead(W0);
            Machine1.AddRead(WFF);
            Machine1.AddRead(R0);
            Machine1.AddRead(R515);
            Machine1.AddRead(MR0);
            Machine1.AddRead(MR515);
            Machine1.AddRead(B0);
            Machine1.AddRead(BFF);
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

        private void Pb_DM100_Click(object sender, RoutedEventArgs e) => Machine1.WriteWord(DM100, DM100.Value + 1);
        private void Pb_DM200_Click(object sender, RoutedEventArgs e) => Machine1.WriteWord(DM200, DM200.Value + 1);
        private void Pb_EM100_Click(object sender, RoutedEventArgs e) => Machine1.WriteWord(EM100, EM100.Value + 1);
        private void Pb_EM200_Click(object sender, RoutedEventArgs e) => Machine1.WriteWord(EM200, EM200.Value + 1);
        private void Pb_W0_Click(object sender, RoutedEventArgs e) => Machine1.WriteWord(W0, W0.Value + 1);
        private void Pb_WFF_Click(object sender, RoutedEventArgs e) => Machine1.WriteWord(WFF, WFF.Value + 1);

        private void Pb_R0_Click(object sender, RoutedEventArgs e) => Machine1.ReveresBit(R0);
        private void Pb_R515_Click(object sender, RoutedEventArgs e) => Machine1.ReveresBit(R515);
        private void Pb_MR0_Click(object sender, RoutedEventArgs e) => Machine1.ReveresBit(MR0);
        private void Pb_MR515_Click(object sender, RoutedEventArgs e) => Machine1.ReveresBit(MR515);
        private void Pb_B0_Click(object sender, RoutedEventArgs e) => Machine1.ReveresBit(B0);
        private void Pb_BFF_Click(object sender, RoutedEventArgs e) => Machine1.ReveresBit(BFF);


        #endregion /PushButtons

        private class ViewModel : AutoBindableBase
        {
            public ObservableCollection<string> Messages { get; set; } = new ObservableCollection<string>();
        }

    }

    public static class KenyenceDeviceToMcConvert
    {
        private static string GetBitAddress(int address, int subAddress) => ((address * 0x10) + subAddress).ToString();

        //For R, MR, LR, CR, Keyence use "R0000 to R1999_15" form to indicate address.
        public static McProtocol.McBitDevice R(int address, int subAddress)
        {
            return new McProtocol.McBitDevice($"X{GetBitAddress(address, subAddress)}");
        }

        public static McProtocol.McBitDevice MR(int address, int subAddress)
        {
            return new McProtocol.McBitDevice($"M{GetBitAddress(address, subAddress)}");
        }

        public static McProtocol.McBitDevice LR(int address, int subAddress)
        {
            return new McProtocol.McBitDevice($"L{GetBitAddress(address, subAddress)}");
        }

        public static McProtocol.McBitDevice CR(int address, int subAddress)
        {
            return new McProtocol.McBitDevice($"SM{GetBitAddress(address, subAddress)}");
        }

        public static McProtocol.McBitDevice B(int address) => new McProtocol.McBitDevice($"B{address}");

        public static McProtocol.McWordDevice CM(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false)
        {
            return new McProtocol.McWordDevice($"S{address}", upperLimit, lowerLimit, asDouble, asFloat);
        }

        public static McProtocol.McWordDevice DM(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false)
        {
            return new McProtocol.McWordDevice($"D{address}", upperLimit, lowerLimit, asDouble, asFloat);
        }

        public static McProtocol.McWordDevice EM(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false)
        {
            return new McProtocol.McWordDevice($"D{address + 100000}", upperLimit, lowerLimit, asDouble, asFloat);
        }

        public static McProtocol.McWordDevice FM(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false)
        {
            return new McProtocol.McWordDevice($"R{address}", upperLimit, lowerLimit, asDouble, asFloat);
        }

        public static McProtocol.McWordDevice ZF(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false)
        {
            return new McProtocol.McWordDevice($"ZR{address}", upperLimit, lowerLimit, asDouble, asFloat);
        }

        public static McProtocol.McWordDevice W(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false)
        {
            return new McProtocol.McWordDevice($"W{address}", upperLimit, lowerLimit, asDouble, asFloat);
        }

        public static McProtocol.McWordDevice Tcurrent(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false)
        {
            return new McProtocol.McWordDevice($"TN{address}", upperLimit, lowerLimit, asDouble, asFloat);
        }

        public static McProtocol.McBitDevice Tcontact(int address)
        {
            return new McProtocol.McBitDevice($"TS{address}");
        }

        public static McProtocol.McWordDevice Ccurrent(int address, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false)
        {
            return new McProtocol.McWordDevice($"CN{address}", upperLimit, lowerLimit, asDouble, asFloat);
        }

        public static McProtocol.McBitDevice Ccontact(int address)
        {
            return new McProtocol.McBitDevice($"CS{address}");
        }
    }

}
