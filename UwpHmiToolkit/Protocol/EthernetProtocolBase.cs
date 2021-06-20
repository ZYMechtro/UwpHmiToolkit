using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UwpHmiToolkit.ViewModel;
using Windows.Networking;
using Windows.UI.Xaml;

namespace UwpHmiToolkit.Protocol
{
    /// <summary>
    /// 
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProtocolType
    {
        Unknown = 0,
        McProtocolUdp,
        McProtocolTcp,

        ModbusRtu,
        ModbusAscii,

        EthernetIP,
    };


    public abstract class EthernetProtocolBase
    {
        public abstract Task<bool> TryConnect(); //override in inherited class, because udp / tcp use different methods.
        public abstract Task<bool> TryDisconnect(); //override in inherited class

        /// <summary>
        /// Interval of refresh in ms.
        /// </summary>
        public ushort RefreshInterval;

        /// <summary>
        /// Period of Reconnect.
        /// </summary>
        public ushort ReconnectPeriod;

        HostName HostName { get; }

        public string IP { get => this.HostName.IPInformation.ToString(); }

        public string Port { get; }

        private bool isOnline;
        public bool IsOnline { get => isOnline; }

        public List<Device> DevicesToMonitor { get; }

        public List<DeviceToWrite> DevicesToWrite { get; }

        private DispatcherTimer dispatcherTimer;

        public virtual void Start() => dispatcherTimer.Start();

        public virtual void Stop() => dispatcherTimer.Stop();

        private void SetupTimer()
        {
            dispatcherTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, RefreshInterval) };
            dispatcherTimer.Tick += DispatcherTimer_Tick;
        }

        private async void DispatcherTimer_Tick(object sender, object e)
        {
            dispatcherTimer.Stop();

            if (!isOnline)
                isOnline = await TryConnect();

            //TODO: Universal  Read / Write procdure

            dispatcherTimer.Start();
        }

        public virtual void ReveresBit(BitDevice bitDevice) => DevicesToWrite.Add(new BitToWrite(bitDevice, !bitDevice.Value));
        public virtual void SetBit(BitDevice bitDevice) => DevicesToWrite.Add(new BitToWrite(bitDevice, true));
        public virtual void ResBit(BitDevice bitDevice) => DevicesToWrite.Add(new BitToWrite(bitDevice, false));

        public virtual void SetMomentory(BitDevice bitDevice)
        {
            DevicesToWrite.Add(new BitToWrite(bitDevice, true));
            DevicesToWrite.Add(new BitToWrite(bitDevice, false));
        }

        public delegate void CommunicationErrorHandler(string message); //TODO: Create ErrorInfo Class.
        public event CommunicationErrorHandler CommunicationError; //TODO: Consider->turn off isOnline when error happened?
    }

    public abstract class TcpProtocol : EthernetProtocolBase
    {

    }

    public abstract class UdpProtocol : EthernetProtocolBase
    {

    }


    public abstract class ProtocolSettingBase : AutoBindableBase
    {

        public string IP { get; set; }
        public string Port { get; set; }

        /// <summary>
        /// Timeout in ms;
        /// </summary>
        public ushort Timeout { get; set; }
    }


    public abstract class Device : AutoBindableBase
    {
        protected uint address;
        public abstract void DecodeValue(byte[] valueInBytes);
    }

    public abstract class BitDevice : Device
    {
        private byte subAddress;
        public bool Value { get; set; }

        protected BitDevice(uint address, byte subAddress)
        {
            this.address = address;
            if (subAddress >= 0x0 && subAddress <= 0xf)
                this.subAddress = subAddress;
            else
                throw new ArgumentOutOfRangeException($"Argument of subAddress out of range 0 ~ 15 : ({subAddress})");
        }

        public override void DecodeValue(byte[] valueInBytes)
        {
            var length = valueInBytes.Length;
            if (length > 0 && length <= 2)
            {
                var bs = new byte[2];
                Array.Copy(valueInBytes, bs, length);
                this.Value = (BitConverter.ToUInt16(bs, 0) & (1 << subAddress)) != 0;
            }
            else
                throw new ArgumentOutOfRangeException($"ValueBytes Length Error: {length}.");
        }

        //public void RefreshValue(byte channelValue)
        //{
        //    var i = subAddress % 8;
        //    this.Value = (channelValue & (1 << i)) != 0;
        //}
    }

    public abstract class WordDevice : Device
    {
        public int Value { get; set; }

        private double? upperLimit, lowerLimit;
        public bool HasLimit => upperLimit is double u && lowerLimit is double l && u > l;

        private bool UseDoubleWords;

        public override void DecodeValue(byte[] valueInBytes)
        {
            byte[] vs = new byte[4];
            var length = valueInBytes.Length;
            if (length == 2 || length == 4)
            {
                Array.Copy(valueInBytes, vs, UseDoubleWords ? 4 : 2);
                Value = BitConverter.ToInt32(vs, 0);
            }
            else
                throw new ArgumentOutOfRangeException($"ValueBytes Length Error: {length}.");
        }
    }


    public abstract class DeviceToWrite
    {

    }


    public class BitToWrite : DeviceToWrite
    {
        BitDevice device;
        private bool value;

        public BitToWrite(BitDevice bitDevice, bool newValue)
        {
            device = bitDevice;
            value = newValue;
        }
    }

    public class WordToWrite : DeviceToWrite
    {
        private WordDevice device;
        private int value;
        public WordToWrite(WordDevice wordDevice, int newValue)
        {
            device = wordDevice;
            value = newValue;
        }
    }

}
