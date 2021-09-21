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

    public sealed partial class Page2 : Page
    {
        List<UwpHmiToolkit.Protocol.EthernetProtocolBase.Device> devicesThisPage;
        McProtocol.McBitDevice MR0, MR1, MR2, MR3, MR4, MR5, MR6, MR7, MR8, MR9, MR10, MR11, MR12, MR13, MR14, MR15;
        private void CreateDevices()
        {
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

            devicesThisPage = new List<UwpHmiToolkit.Protocol.EthernetProtocolBase.Device>()
            {
                MR0, MR1, MR2, MR3, MR4, MR5, MR6, MR7, MR8, MR9, MR10, MR11, MR12, MR13, MR14, MR15
            };
        }

        public Page2()
        {
            CreateDevices();
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            MainPage.Current.ChangeReadMonitorDevices(devicesThisPage);
        }


        private void Pb_MR0_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR0, sender as RepeatButton);
        private void Pb_MR1_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR1, sender as RepeatButton);
        private void Pb_MR2_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR2, sender as RepeatButton);
        private void Pb_MR3_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR3, sender as RepeatButton);
        private void Pb_MR4_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR4, sender as RepeatButton);
        private void Pb_MR5_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR5, sender as RepeatButton);
        private void Pb_MR6_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR6, sender as RepeatButton);
        private void Pb_MR7_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR7, sender as RepeatButton);
        private void Pb_MR8_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR8, sender as RepeatButton);
        private void Pb_MR9_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR9, sender as RepeatButton);
        private void Pb_MR10_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR10, sender as RepeatButton);
        private void Pb_MR11_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR11, sender as RepeatButton);
        private void Pb_MR12_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR12, sender as RepeatButton);
        private void Pb_MR13_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR13, sender as RepeatButton);
        private void Pb_MR14_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR14, sender as RepeatButton);
        private void Pb_MR15_Click(object sender, RoutedEventArgs e) => MainPage.Current.Machine1.HoldBit(MR15, sender as RepeatButton);


    }
}
