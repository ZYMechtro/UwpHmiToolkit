using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UwpHmiToolkit.Protocol.McProtocol;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static SampleApp.PlcTools.KenyenceDeviceToMcConvert;

namespace SampleApp
{
    public sealed partial class Page1 : Page
    {
        List<UwpHmiToolkit.Protocol.EthernetProtocolBase.Device> devicesThisPage;
        McProtocol.McWordDevice DM100, DM200, EM100, EM200, W0, WFE;
        McProtocol.McBitDevice R0, R515, MR515, B0, BFF;
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
            MR515 = MR(5, 0xf);
            B0 = B(0);
            BFF = B(0xff);

            devicesThisPage = new List<UwpHmiToolkit.Protocol.EthernetProtocolBase.Device>()
            {
                DM100, DM200, EM100, EM200, W0, WFE,
                R0, R515, MR515, B0, BFF,
            };

        }

        public Page1()
        {
            CreateDevices();
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            MainPage.Current.ChangeReadMonitorDevices(devicesThisPage);
        }

        private void Pb_DM100_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.WriteWord(DM100, DM100.Value + 1);
        private void Pb_DM200_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.WriteWord(DM200, DM200.Value + 1);
        private void Pb_EM100_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.WriteWord(EM100, EM100.Value + 1);
        private void Pb_EM200_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.WriteWord(EM200, EM200.Value + 1);
        private void Pb_W0_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.WriteWord(W0, W0.Value + 1);
        private void Pb_WFE_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.WriteWord(WFE, BitConverter.SingleToInt32Bits(BitConverter.Int32BitsToSingle(WFE.Value) + 1.1f));

        private void Pb_R0_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.SetMomentory(R0);
        private void Pb_R515_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.ReveresBit(R515);

        private void Pb_MR515_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.ReveresBit(MR515);
        private void Pb_B0_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.ReveresBit(B0);
        private void Pb_BFF_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.ReveresBit(BFF);

    }
}
