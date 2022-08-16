using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UwpHmiToolkit.ViewModel;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace UwpHmiToolkit.Protocol
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ApplicationLayerProtocol
    {
        Unknown = 0,
        McProtocol,
        Modbus,
        EthernetIP,
    };

    [JsonConverter(typeof(StringEnumConverter))]
    public enum TransmissionLayerProtocol { UDP = 0, TCP = 1 }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CommunicationCode { Binary = 0, ASCII = 1 }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ModbusMethod { RTU = 0, ASCII = 1, TCP = 2 }


    public abstract class EthernetProtocolBase : BindableBase
    {
        protected DatagramSocket udpSocket;
        protected StreamSocket tcpSocket;
        protected Stream outputStream, inputStream;
        protected StreamWriter streamWriter;
        protected StreamReader streamReader;

        /// <summary>
        /// Try to open a connection, returns true if succeed and returns false if fail.
        /// </summary>
        /// <returns></returns>
        public abstract Task<bool> TryConnectAsync(); //override in inherited class, because udp / tcp use different methods.

        /// <summary>
        /// Try to disconnect a connection, returns true if succeed and returns false if fail.
        /// </summary>
        /// <returns></returns>
        public abstract void TryDisconnect(); //override in inherited class

        protected virtual async Task<bool> UdpConnect()
        {
            udpSocket = new DatagramSocket();
            try
            {
                var localIp = GetLocalIp(HostName.CanonicalName);
                if (localIp is null)
                {
                    CommunicationError("No cable connections.");
                    return false;
                }
                await udpSocket.ConnectAsync(HostName, Port);
                lock (udpSocket)
                {
                    outputStream = udpSocket.OutputStream.AsStreamForWrite();
                }
                return true;
            }
            catch (Exception ex)
            {
                CommunicationError(ex.Message);
                return false;
            }
        }

        protected virtual async void UdpDisconnect()
        {
            if (udpSocket != null)
            {
                try
                {
                   await udpSocket.CancelIOAsync();
                }
                catch { }

                lock (udpSocket)
                {

                    udpSocket?.Dispose();
                    udpSocket = null;
                }
            }

        }


        protected virtual async Task<bool> TcpConnect()
        {
            throw new NotImplementedException();
        }

        protected virtual async Task TcpDisconnect()
        {
            throw new NotImplementedException();
        }


        public abstract Task CommunicateAsync();

        protected string GetLocalIp(string targetIp)
        {
            var match = targetIp.Substring(0, targetIp.LastIndexOf('.') + 1);
            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp?.NetworkAdapter == null) return null;
            var hns = NetworkInformation.GetHostNames();

            var b = hns.Any(hn =>
                     hn.Type == HostNameType.Ipv4 &&
                     hn.IPInformation?.NetworkAdapter != null &&
                     //hn.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId &&
                     hn.CanonicalName.Contains(match));
            if (!b) return null;
            var hostname =
                hns.First(hn =>
                    hn.Type == HostNameType.Ipv4 &&
                    hn.IPInformation?.NetworkAdapter != null &&
                    //hn.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId &&
                    hn.CanonicalName.Contains(match));

            return hostname?.CanonicalName;
        }

        /// <summary>
        /// Interval of refresh in ms.
        /// </summary>
        public ushort RefreshInterval { get; }

        public HostName HostName { get; }

        public string IP => HostName.CanonicalName;

        public string Port { get; }

        public ushort Timeout { get; }

        public int SendDelay { get; set; }

        public bool IsReadonly { get; set; }

        public delegate void OnlineStateChangeHandler();
        public event OnlineStateChangeHandler OnlineStateChanged;
        protected virtual void RaiseOnlineStateChange() => OnlineStateChanged?.Invoke();

        protected virtual void ChangeOnlineStatus(bool b)
        {
            isOnline = b;
            OnPropertyChanged("IsOnline");
            RaiseOnlineStateChange();
        }

        protected bool isOnline;
        public bool IsOnline => isOnline;

        protected readonly List<Device> devicesToMonitor = new List<Device>();

        protected readonly List<DeviceToWrite> devicesToWrite = new List<DeviceToWrite>();

        protected readonly Dictionary<ButtonBase, BitDevice> holdPairs = new Dictionary<ButtonBase, BitDevice>();

        protected bool needToRefreshReading;

        protected DispatcherTimer dispatcherTimer;

        protected readonly Windows.UI.Core.CoreDispatcher CurrentDispatcher;


        public virtual void Start() => dispatcherTimer?.Start();

        public virtual void Stop() => dispatcherTimer?.Stop();

        protected virtual void SetupTimer()
        {
            if (dispatcherTimer != null)
            {
                dispatcherTimer?.Stop();
                dispatcherTimer.Tick -= DispatcherTimer_Tick;
            }

            dispatcherTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, RefreshInterval) };
            dispatcherTimer.Tick += DispatcherTimer_Tick;
        }

        protected virtual async void DispatcherTimer_Tick(object sender, object e)
        {
            dispatcherTimer.Stop();

            if (!IsOnline)
            {
                TryDisconnect();
                await Task.Delay(30 * RefreshInterval);
                await TryConnectAsync();
            }

            if (IsOnline)
                await CommunicateAsync();

            dispatcherTimer.Start();
        }


        #region Device I/O Action

        public virtual void AddRead(Device device)
        {
            if (!devicesToMonitor.Any(d => d.Name == device.Name))
                devicesToMonitor.Add(device);

            needToRefreshReading = true;
        }

        public virtual void RemoveRead(Device device)
        {
            devicesToMonitor.RemoveAll(d => d.Name == device.Name);

            needToRefreshReading = true;
        }

        public virtual void AddReads(IEnumerable<Device> devices)
        {
            var result = from device in devices
                         where device is null
                         select nameof(device);
            if (result.Count() > 0)
                throw new ArgumentNullException();

            devicesToMonitor.AddRange(devices.Where(d => !devicesToMonitor.Any(m => m.Name == d.Name)));

            needToRefreshReading = true;
        }

        public virtual void RemoveReads()
        {
            devicesToMonitor.Clear();
        }

        public virtual void ReveresBit(BitDevice bitDevice) => devicesToWrite.Add(new BitToWrite(bitDevice, !bitDevice.Value));

        public virtual void SetBit(BitDevice bitDevice) => devicesToWrite.Add(new BitToWrite(bitDevice, true));

        public virtual void ResBit(BitDevice bitDevice) => devicesToWrite.Add(new BitToWrite(bitDevice, false));

        public virtual void SetMomentory(BitDevice bitDevice)
        {
            devicesToWrite.Add(new BitToWrite(bitDevice, true));
            devicesToWrite.Add(new BitToWrite(bitDevice, false));
        }

        public virtual void HoldBit(BitDevice bitDevice, RepeatButton repeatButton)
        {
            holdPairs.TryAdd(repeatButton, bitDevice);
        }

        public virtual void WriteWord(WordDevice wordDevice, int newValue) => devicesToWrite.Add(_ = new WordToWrite(wordDevice, newValue));

        public virtual void WriteDevices(IEnumerable<DeviceToWrite> devices) => devicesToWrite.AddRange(devices);

        #endregion /Device I/O Action


        public delegate void CommunicationErrorHandler(string message);
        public event CommunicationErrorHandler CommunicationError;
        protected virtual void RaiseCommError(string message)
        {
            CommunicationError?.Invoke(message);
        }

        protected EthernetProtocolBase(ProtocolSettingBase setting, Windows.UI.Core.CoreDispatcher dispatcher)
        {
            this.HostName = new HostName(setting.Ip);
            this.Port = setting.Port;
            this.RefreshInterval = setting.RefreshInterval;
            this.Timeout = setting.Timeout;
            this.CurrentDispatcher = dispatcher;
            this.SendDelay = setting.SendDelay;
        }

        #region DeviceModel

        public abstract class Device : AutoBindableBase
        {
            protected uint address;
            public uint Address => address;

            protected string deviceType;
            public string DeviceType => deviceType;

            public abstract void DecodeValue(byte[] valueInBytes);
            public abstract string Name { get; }

            public override string ToString() => Name;
        }

        public abstract class BitDevice : Device
        {
            public uint Channel => address / 0x10;
            public bool Value { get; set; }
            public override void DecodeValue(byte[] valueInBytes)
            {

                var length = valueInBytes.Length;
                if (length > 0 && length <= 2)
                {
                    var bs = new byte[2];
                    Array.Copy(valueInBytes, bs, length);
                    this.Value = (BitConverter.ToUInt16(bs, 0) & (1 << (int)(address % 0x10))) != 0;
                }
                else
                    throw new ArgumentOutOfRangeException($"ValueBytes Length Error: {length}.");
            }
        }

        public abstract class WordDevice : Device
        {
            public int Value { get; set; }

            protected double? upperLimit, lowerLimit;
            public bool HasLimit => upperLimit is double u && lowerLimit is double l && u > l;
            public double UpperLimit => upperLimit ?? double.MaxValue;
            public double LowerLimit => lowerLimit ?? double.MinValue;

            protected bool asDoubleWords, asFloat;
            public bool AsDoubleWords => asDoubleWords;
            public bool AsFloat => asFloat;
            public bool Use2Channels => asFloat || asDoubleWords;

            public uint decimalPointPositon;
            public uint DecimalPointPositon => decimalPointPositon;

            public override void DecodeValue(byte[] valueInBytes)
            {
                byte[] vs = new byte[4];
                var length = valueInBytes.Length;
                if (length == 2 || length == 4)
                {
                    Array.Copy(valueInBytes, vs, AsDoubleWords ? 4 : 2);
                    Value = BitConverter.ToInt32(vs, 0);
                }
                else
                    throw new ArgumentOutOfRangeException($"ValueBytes Length Error: {length}.");
            }
        }

        public abstract class DeviceToWrite
        {
            public Device Device;
        }

        public class BitToWrite : DeviceToWrite
        {
            public bool Value;

            public BitToWrite(BitDevice bitDevice, bool newValue)
            {
                Device = bitDevice;
                Value = newValue;
            }
        }

        public class WordToWrite : DeviceToWrite
        {
            public int Value;
            public WordToWrite(WordDevice wordDevice, int newValue)
            {
                Device = wordDevice;
                Value = newValue;
            }
        }

        #endregion /DeviceModel
    }

    public abstract class ProtocolSettingBase : AutoBindableBase
    {
        public string Ip { get; set; }

        public string Port { get; set; }

        public ushort RefreshInterval { get; set; }

        /// <summary>
        /// Timeout in ms;
        /// </summary>
        public ushort Timeout { get; set; }

        public ushort SendDelay { get; set; }

    }


}
