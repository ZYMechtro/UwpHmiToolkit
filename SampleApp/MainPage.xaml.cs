using System;
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

        public McProtocol Machine1;
        public McProtocol.McWordDevice DM100, DM200, EM100, EM200, W0, WFE;
        public McProtocol.McBitDevice R0, R515, MR515, B0, BFF;
        public McProtocol.McBitDevice MR0, MR1, MR2, MR3, MR4, MR5, MR6, MR7, MR8, MR9, MR10, MR11, MR12, MR13, MR14, MR15;


        public MainPage()
        {
            Machine1 = new McProtocol(Setting1, this.Dispatcher) { IsReadonly = false };

            CreateDevices(); //Create devices before UI bindings them, or you can use DataTemplate.

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
            WFE = W(0xfe, asFloat: true);

            R0 = R(0, 0);
            R515 = R(5, 0xf);
            MR0 = MR(0, 0);
            MR1 = MR(0, 1);
            MR2 = MR(0, 2);
            MR3 = MR(0, 3);
            MR4 = MR(0, 4);
            MR5 = MR(0, 5);
            MR6 = MR(0, 6);
            MR7 = MR(0, 7);
            MR8 = MR(0, 8);
            MR9 = MR(0, 9);
            MR10 = MR(0, 10);
            MR11 = MR(0, 11);
            MR12 = MR(0, 12);
            MR13 = MR(0, 13);
            MR14 = MR(0, 14);
            MR15 = MR(0, 15);
            MR515 = MR(5, 0xf);
            B0 = B(0);
            BFF = B(0xff);

            Machine1.AddRead(DM100);
            Machine1.AddRead(DM200);
            Machine1.AddRead(EM100);
            Machine1.AddRead(EM200);
            Machine1.AddRead(W0);
            Machine1.AddRead(WFE);
            Machine1.AddRead(R0);
            Machine1.AddRead(R515);
            Machine1.AddRead(MR0);
            Machine1.AddRead(MR1);
            Machine1.AddRead(MR2);
            Machine1.AddRead(MR3);
            Machine1.AddRead(MR4);
            Machine1.AddRead(MR5);
            Machine1.AddRead(MR6);
            Machine1.AddRead(MR7);
            Machine1.AddRead(MR8);
            Machine1.AddRead(MR9);
            Machine1.AddRead(MR10);
            Machine1.AddRead(MR11);
            Machine1.AddRead(MR12);
            Machine1.AddRead(MR13);
            Machine1.AddRead(MR14);
            Machine1.AddRead(MR15);
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
        private void Pb_WFE_Click(object sender, RoutedEventArgs e) => Machine1.WriteWord(WFE, BitConverter.SingleToInt32Bits(BitConverter.Int32BitsToSingle(WFE.Value) + 1.1f));

        private void Pb_R0_Click(object sender, RoutedEventArgs e) => Machine1.SetMomentory(R0);
        private void Pb_R515_Click(object sender, RoutedEventArgs e) => Machine1.ReveresBit(R515);
        private void Pb_MR0_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR0, sender as RepeatButton);
        private void Pb_MR1_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR1, sender as RepeatButton);
        private void Pb_MR2_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR2, sender as RepeatButton);
        private void Pb_MR3_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR3, sender as RepeatButton);
        private void Pb_MR4_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR4, sender as RepeatButton);
        private void Pb_MR5_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR5, sender as RepeatButton);
        private void Pb_MR6_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR6, sender as RepeatButton);
        private void Pb_MR7_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR7, sender as RepeatButton);
        private void Pb_MR8_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR8, sender as RepeatButton);
        private void Pb_MR9_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR9, sender as RepeatButton);
        private void Pb_MR10_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR10, sender as RepeatButton);
        private void Pb_MR11_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR11, sender as RepeatButton);
        private void Pb_MR12_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR12, sender as RepeatButton);
        private void Pb_MR13_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR13, sender as RepeatButton);
        private void Pb_MR14_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR14, sender as RepeatButton);
        private void Pb_MR15_Click(object sender, RoutedEventArgs e) => Machine1.HoldBit(MR15, sender as RepeatButton);
        private void Pb_MR515_Click(object sender, RoutedEventArgs e) => Machine1.ReveresBit(MR515);
        private void Pb_B0_Click(object sender, RoutedEventArgs e) => Machine1.ReveresBit(B0);
        private void Pb_BFF_Click(object sender, RoutedEventArgs e) => Machine1.ReveresBit(BFF);


        #endregion /PushButtons


        private class ViewModel : AutoBindableBase
        {
            public ObservableCollection<string> Messages { get; set; } = new ObservableCollection<string>();
        }

    }

}
