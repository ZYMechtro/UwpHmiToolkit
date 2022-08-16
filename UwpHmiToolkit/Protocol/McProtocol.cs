using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls.Primitives;

namespace UwpHmiToolkit.Protocol.McProtocol
{
    public class McProtocol : EthernetProtocolBase
    {
        public TransmissionLayerProtocol TransmissionLayerProtocol { get; }
        public CommunicationCode Code { get; }

        public static readonly Dictionary<string, byte> DeviceTypeCodePairs = new Dictionary<string, byte>()
        {
            { "SM", 0x91 }, //Special Relay
            { "SD", 0xa9 }, //Special Register
            { "X", 0x9c }, //Input
            { "Y", 0x9d }, //Output
            { "M", 0x90 }, //Internal Relay
            { "L", 0x92 }, //Latch Relay
            { "F", 0x93 }, //Annunciator
            { "V", 0x94 }, //Edge relay
            { "B", 0xa0 }, //Link Relay
            { "D", 0xa8 }, //Data Register
            { "W", 0xb4 }, //Link Register

            { "TS", 0xc1 }, //Timer-Contact
            { "TC", 0xc0 }, //Timer-Coil
            { "TN", 0xc2 }, //Timer-CurrentValue
            { "LTS", 0x51 }, //Long Timer-Contact
            { "LTC", 0x50 }, //Long Timer-Coil
            { "LTN", 0x52 }, //Long Timer-CurrentValue

            { "STS", 0xc7 }, //RetentiveTimer-Contact
            { "STC", 0xc6 }, //RetentiveTimer-Coil
            { "STN", 0xc8 }, //RetentiveTimer-CurrentValue
            { "LSTS", 0x59 }, //Long RetentiveTimer-Contact
            { "LSTC", 0x58 }, //Long RetentiveTimer-Coil
            { "LSTN", 0x5a }, //Long RetentiveTimer-CurrentValue

            { "CS", 0xc4 }, //Counter-Contact
            { "CC", 0xc3 }, //Counter-Coil
            { "CN", 0xc5 }, //Counter-CurrentValue
            { "LCS", 0x55 }, //Long Counter-Contact
            { "LCC", 0x54 }, //Long Counter-Coil
            { "LCN", 0x56 }, //Long Counter-CurrentValue

            { "SB", 0xa1 }, //Link special relay
            { "SW", 0xb5 }, //Link special register
            { "DX", 0xa2 }, //Direct access input
            { "DY", 0xa3 }, //Direct access output

            { "Z", 0xcc }, //Index register
            { "LZ", 0x62 }, //Long index register
            { "R", 0xaf }, //File register: Block switching method
            { "ZR", 0xb0 }, //File register: Serial number access method
            { "RD", 0x2c }, //Refresh data register
        };

        protected readonly byte[] currentCmdSerialNo = new byte[2] { 0, 0 };
        protected readonly Dictionary<int, int> unhandledSerialIndexPairs = new Dictionary<int, int>();

        protected const byte netWorkNo = 0x00, pcNo = 0xff, reqDestUnitStationNo = 0x00;

        public override async Task<bool> TryConnectAsync()
        {
            if (TransmissionLayerProtocol == TransmissionLayerProtocol.UDP)
            {
                if (!(udpSocket is null))
                    udpSocket.MessageReceived -= UdpSocket_MessageReceived;
                var result = await this.UdpConnect();
                if (result)
                {
                    ChangeOnlineStatus(true);
                    udpSocket.MessageReceived += UdpSocket_MessageReceived;
                    SetupTimer();
                    this.Start();
                }
                else
                {
                    return false;
                }

                return result;
            }
            else
            {
                return false;
                //TODO: TCP
            }

        }

        public override void TryDisconnect()
        {
            ChangeOnlineStatus(false);
            if (TransmissionLayerProtocol == TransmissionLayerProtocol.UDP)
            {
                if (udpSocket != null)
                    udpSocket.MessageReceived -= UdpSocket_MessageReceived;
                UdpDisconnect();
            }
            else
            {

            }
        }

        public override async Task CommunicateAsync()
        {
            //Writing Actions
            if (!IsReadonly)
            {
                if (holdPairs.Count > 0)
                {
                    var listToRemove = new List<RepeatButton>();
                    foreach (RepeatButton pbs in holdPairs.Keys)
                    {
                        if (pbs.IsPressed)
                        {
                            devicesToWrite.Add(new BitToWrite(holdPairs[pbs], true));
                        }
                        else
                        {
                            devicesToWrite.Add(new BitToWrite(holdPairs[pbs], false));
                            listToRemove.Add(pbs);
                        }
                    }
                    foreach (RepeatButton pbs in listToRemove)
                    {
                        if (holdPairs.ContainsKey(pbs))
                            holdPairs.Remove(pbs);
                    }
                }

                if (devicesToWrite.Count > 0)
                {
                    var current = new List<DeviceToWrite>();
                    var splited = new List<List<DeviceToWrite>> { current };
                    Type lastType = null;
                    string lastDeviceName = "";
                    foreach (var dw in devicesToWrite)
                    {
                        if (lastType != null && (dw.Device.GetType() != lastType || dw.Device.Name == lastDeviceName))
                        {
                            current = new List<DeviceToWrite>();
                            splited.Add(current);
                        }
                        current.Add(dw);
                        lastType = dw.Device.GetType();
                        lastDeviceName = dw.Device.Name;
                    }

                    foreach (var list in splited)
                    {
                        await SendCommand(AddFrame4E(RandomWrite(list)));
                    }
                    devicesToWrite.Clear();
                }
            }
            else if (IsReadonly)
            {
                devicesToWrite.Clear();
            }

            //TODO: Customized Tasks (Future)

            //Reading Actions
            if (needToRefreshReading)
            {
                PrepareReadingCmds();
                needToRefreshReading = false;
            }

            if (devicesToMonitor.Count == 0)
            {
                await SendCommand(AddFrame4E(loopTestCommand));
            }
            else if (devicesToMonitor.Count > 0)
            {
                if (unhandledSerialIndexPairs.Count > 0)
                {
                    RaiseCommError($"LostResponseData,count: {unhandledSerialIndexPairs.Count}.");
                    await Task.Delay(Timeout);
                    if (unhandledSerialIndexPairs.Count > 0)
                    {
                        unhandledSerialIndexPairs.Clear();
                        ChangeOnlineStatus(false);
                    }
                }
                else
                {
                    RaiseCommError("");
                }

                if (IsOnline)
                {
                    for (int i = 0; i < splitedReadingCmds.Count; i++)
                    {
                        await SendCommand(AddFrame4E(splitedReadingCmds[i], i)); await Task.Delay(SendDelay);
                    }
                }

            }
        }

        private readonly Dictionary<Device, int> deviceIndexPairs = new Dictionary<Device, int>();
        private readonly List<byte[]> splitedReadingCmds = new List<byte[]>();
        private List<Dictionary<Device, int>> splitedDeviceWaitingForValue;
        private IEnumerable<McBitDevice> bitDevices;
        private IEnumerable<McWordDevice> wordDevices;
        private IEnumerable<McWordDevice> dwordDevices;

        private void PrepareReadingCmds()
        {
            bitDevices = from bd in devicesToMonitor.OfType<McBitDevice>()
                         orderby bd.DeviceType, bd.Address
                         select bd;

            wordDevices = from wd in devicesToMonitor.OfType<McWordDevice>()
                          where !wd.Use2Channels
                          orderby wd.DeviceType, wd.Address
                          select wd;

            dwordDevices = from dwd in devicesToMonitor.OfType<McWordDevice>()
                           where dwd.Use2Channels
                           orderby dwd.DeviceType, dwd.Address
                           select dwd;

            string lastType = "";
            uint lastChannel = 0;
            int index = 0;

            deviceIndexPairs.Clear();
            if (bitDevices.Count() > 0)
            {
                foreach (var b in bitDevices)
                {
                    if ((lastType != b.DeviceType && lastType != "") || lastChannel != b.Channel)
                        index++;

                    deviceIndexPairs.Add(b, index);

                    lastType = b.DeviceType;
                    lastChannel = b.Channel;
                }
            }
            if (wordDevices.Count() > 0)
            {
                foreach (var w in wordDevices)
                {
                    index++;
                    deviceIndexPairs.Add(w, index);
                }
            }
            if (dwordDevices.Count() > 0)
            {
                foreach (var dw in dwordDevices)
                {
                    index++;
                    deviceIndexPairs.Add(dw, index);
                }
            }
            splitedDeviceWaitingForValue = SplitDicitionay(deviceIndexPairs).ToList();

            splitedReadingCmds.Clear();
            foreach (var dict in splitedDeviceWaitingForValue)
            {
                var list = dict.Keys.ToList();
                byte ch1 = 0;
                byte ch2 = 0;
                index = 0;
                lastType = "";
                lastChannel = 0;
                var length = (dict.Values.Max() % randomReadsMaxCount + 1) * 4;
                var bytes = new byte[length];
                foreach (var d in list)
                {
                    if (d is McBitDevice b)
                    {
                        if (lastType != b.DeviceType || lastChannel != b.Channel)
                        {
                            var deviceCode = b.GetDeviceCodeForRead();
                            Array.Copy(deviceCode, 0, bytes, index, deviceCode.Length);
                            index += deviceCode.Length;
                            ch1++;
                        }
                        lastType = b.DeviceType;
                        lastChannel = b.Channel;
                    }
                    if (d is McWordDevice w && !w.Use2Channels)
                    {
                        var deviceCode = w.GetDeviceCodeForRead();
                        Array.Copy(deviceCode, 0, bytes, index, deviceCode.Length);
                        index += deviceCode.Length;
                        ch1++;
                    }
                    if (d is McWordDevice dw && dw.Use2Channels)
                    {
                        var deviceCode = dw.GetDeviceCodeForRead();
                        Array.Copy(deviceCode, 0, bytes, index, deviceCode.Length);
                        index += deviceCode.Length;
                        ch2++;
                    }
                }
                var cmd = new byte[6] { 0x03, 0x04, 0x00, 0x00, ch1, ch2 }.Concat(bytes).ToArray();

                splitedReadingCmds.Add(cmd);
            }


        }

        private async void UdpSocket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            using (var dataReader = args.GetDataReader())
            {
                byte[] response = new byte[dataReader.UnconsumedBufferLength];
                dataReader.ReadBytes(response);
                if (IsMcResponse(response, out byte[] serialNumber, out byte[] endcode, out byte[] data))
                {
                    ushort sN = BitConverter.ToUInt16(serialNumber, 0);
                    //Registered reading action
                    if (unhandledSerialIndexPairs.ContainsKey(sN))
                    {
                        var i = 0;
                        string lastType = "";
                        uint lastChannel = 0;
                        foreach (var device in splitedDeviceWaitingForValue[unhandledSerialIndexPairs[sN]].Keys.ToList())
                        {
                            await CurrentDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                if (device is McBitDevice b)
                                {
                                    if (b.DeviceType == lastType && b.Channel == lastChannel)
                                    {
                                        i -= 2;
                                    }
                                    var value = new byte[2];
                                    Array.Copy(data, i, value, 0, 2);
                                    i += 2;
                                    b.DecodeValue(value);
                                    lastType = b.DeviceType;
                                    lastChannel = b.Channel;
                                }
                                else if (device is McWordDevice w && !w.Use2Channels)
                                {
                                    var value = new byte[2];
                                    Array.Copy(data, i, value, 0, 2);
                                    i += 2;
                                    w.DecodeValue(value);
                                }
                                else if (device is McWordDevice dw && dw.Use2Channels)
                                {
                                    var value = new byte[4];
                                    Array.Copy(data, i, value, 0, 4);
                                    i += 4;
                                    dw.DecodeValue(value);
                                }

                            });
                        }
                        lock (unhandledSerialIndexPairs)
                            unhandledSerialIndexPairs.Remove(sN);
                    }
                    else
                    {
                        if (!Enumerable.SequenceEqual(endcode, new byte[2] { 0, 0 }))
                        {
                            byte[] cmd, subCmd;
                            string cmds = "", subCmds = "";
                            if (data.Length > 6)
                            {
                                cmd = new byte[2] { data[6], data[7] };
                                cmds = BitConverter.ToString(cmd);
                                if (data.Length > 8)
                                {
                                    subCmd = new byte[2] { data[8], data[9] };
                                    subCmds = BitConverter.ToString(subCmd);
                                }
                            }
                            RaiseCommError($"Response Error: {BitConverter.ToString(endcode)}, Command:{cmds}, SubCommand:{cmds}.");
                        }
                    }
                }
            }
        }

        private bool IsMcResponse(byte[] source, out byte[] seiralNumber, out byte[] endcode, out byte[] responseData)
        {
            responseData = null;
            seiralNumber = null;
            endcode = null;

            //CheckSubHeader is d4 00 xx yy 00 00
            if (source.Length > 15 && source[0] == 0xd4 && source[1] == 0x00 && source[4] == 0x00 && source[5] == 0x00)
            {
                seiralNumber = new byte[2] { source[2], source[3] };

                var accessRoute = source.Skip(6).Take(5).ToArray();
                endcode = source.Skip(13).Take(2).ToArray(); //Index 14~15 is length of return data

                if (accessRoute[0] == netWorkNo
                    && accessRoute[1] == pcNo
                    && accessRoute[2] == 0xff
                    && accessRoute[3] == 0x03
                    && accessRoute[4] == reqDestUnitStationNo
                    && endcode[0] == 0x00
                    && endcode[1] == 0x00)
                {
                    responseData = source.Skip(15).ToArray();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private const int randomReadsMaxCount = 192;

        private static IEnumerable<List<T>> SplitList<T>(List<T> devices, int size = randomReadsMaxCount)
        {
            for (int i = 0; i < devices.Count; i += size)
            {
                yield return devices.GetRange(i, Math.Min(size, devices.Count - i));
            }
        }

        //TODO: Confirm
        private static IEnumerable<Dictionary<T, int>> SplitDicitionay<T>(Dictionary<T, int> totalDevices, int size = randomReadsMaxCount)
        {
            var j = totalDevices.Max(d => d.Value) / size;
            for (int i = 0; i <= j; i++)
            {
                var devices = from pair in totalDevices
                              where pair.Value >= i * size
                              && pair.Value < (i + 1) * size
                              orderby pair.Value
                              select pair;
                yield return devices.ToDictionary(p => p.Key, p => p.Value);
            }
        }


        #region Commands

        private async Task SendCommand(byte[] request)
        {
            await Task.Delay(SendDelay);
            try
            {
                if (outputStream != null)
                {
                    await outputStream.WriteAsync(request, 0, request.Length);
                    await outputStream.FlushAsync();
                }
            }
            catch { }
        }

        private byte[] AddFrame4E(byte[] commandContent) => AddSubHeader(commandContent, null);

        private byte[] AddFrame4E(byte[] commandContent, int index) => AddSubHeader(commandContent, index);

        private byte[] AddSubHeader(byte[] commandContent, int? index)
        {
            byte[] a = new byte[5] { netWorkNo, pcNo, 0xff, 0x03, reqDestUnitStationNo };
            byte[] cpuMonitorTimer = new byte[2] { 0x10, 0x00 };
            int length = 2 + commandContent.Length;

            if (8194 - 13 < length)
                throw new ArgumentOutOfRangeException("Command length over 8194 byte.");

            byte[] l = new byte[2] { BitConverter.GetBytes(length)[0], BitConverter.GetBytes(length)[1] };
            var t = a.Concat(l).Concat(cpuMonitorTimer).Concat(commandContent).ToArray();

            byte[] b = new byte[] { 0x54, 0x00 };
            byte[] c = new byte[] { 0x00, 0x00 };
            var result = b.Concat(currentCmdSerialNo).Concat(c).Concat(t).ToArray();

            if (index is int i)
            {
                var s = BitConverter.ToUInt16(currentCmdSerialNo, 0);
                lock (unhandledSerialIndexPairs)
                    unhandledSerialIndexPairs.Add(s, i);
            }

            if (++currentCmdSerialNo[0] == 0)
                ++currentCmdSerialNo[1];

            return result;
        }

        private readonly byte[] loopTestCommand = new byte[8] { 0x19, 0x06, 0x0, 0x0, 0x02, 0x00, 0x4f, 0x4b };

        private byte[] RandomWrite(List<DeviceToWrite> writes)
        {
            if (!(writes.All(device => device.Device is McBitDevice) || writes.All(device => device.Device is McWordDevice)))
            {
                throw new ArgumentException("Devices of Writelist is type-mixed or null.");
            }
            else
            {
                if (writes.First().Device is McBitDevice)
                {
                    var count = BitConverter.GetBytes(writes.Count)[0];
                    var commandHeader = new byte[5] { 0x02, 0x14, 0x01, 0x00, count };
                    var array = new byte[count * 5];
                    for (int i = 0; i < count; i++)
                    {
                        var dw = writes[i] as BitToWrite;
                        Array.Copy((dw.Device as McBitDevice).GetDeviceCodeForWrite(), 0, array, i * 5, 4);
                        array[i * 5 + 4] = (byte)(dw.Value ? 1 : 0);
                    }
                    return commandHeader.Concat(array).ToArray();
                }
                else
                {
                    var ordered = from dw in writes.OfType<WordToWrite>()
                                  orderby (dw.Device as McWordDevice).Use2Channels descending
                                  select dw;
                    byte count1 = 0, count2 = 0;
                    var array = new List<byte>();
                    foreach (var d in ordered)
                    {
                        if (!(d.Device as McWordDevice).Use2Channels)
                        {
                            count1++;
                            var address = (d.Device as McWordDevice).GetDeviceCodeForWrite();
                            var value = BitConverter.GetBytes(d.Value).Take(2);
                            array = array.Concat(address).Concat(value).ToList();
                        }
                        else
                        {
                            count2++;
                            var address = (d.Device as McWordDevice).GetDeviceCodeForWrite();
                            var value = BitConverter.GetBytes(d.Value);
                            array = array.Concat(address).Concat(value).ToList();
                        }
                    }
                    var commandHeader = new byte[6] { 0x02, 0x14, 0x00, 0x00, count1, count2 };
                    return commandHeader.Concat(array).ToArray();
                }
            }
        }

        #endregion /Commands


        #region Devices

        interface IMcDevices
        {
            byte[] GetDeviceCodeForRead();
            byte[] GetDeviceCodeForWrite();
        }

        public class McBitDevice : BitDevice, IMcDevices
        {
            public override string Name => deviceType + "0x" + address.ToString("X");

            public byte[] GetDeviceCodeForRead()
            {
                var b = new byte[4];
                var addressbyteArray = BitConverter.GetBytes(Channel * 0x10);
                b[0] = addressbyteArray[0];
                b[1] = addressbyteArray[1];
                b[2] = addressbyteArray[2];
                b[3] = DeviceTypeCodePairs[DeviceType];
                return b;
            }
            public byte[] GetDeviceCodeForWrite()
            {
                var b = new byte[4];
                var addressbyteArray = BitConverter.GetBytes(address);
                b[0] = addressbyteArray[0];
                b[1] = addressbyteArray[1];
                b[2] = addressbyteArray[2];
                b[3] = DeviceTypeCodePairs[DeviceType];
                return b;
            }

            public McBitDevice(string name)
            {
                if (IsNameFormatCorrect(name, out string type, out uint address) && DeviceTypeCodePairs.ContainsKey(type))
                {
                    deviceType = type;
                    this.address = address;
                }
                else
                {
                    throw new ArgumentException($"Create McBitDevice Fail: {name}.");
                }
            }

        }

        public class McWordDevice : WordDevice, IMcDevices
        {
            public override string Name => deviceType + address;

            public byte[] GetDeviceCodeForRead()
            {
                var b = new byte[4];
                var addressbyteArray = BitConverter.GetBytes(Address);
                b[0] = addressbyteArray[0];
                b[1] = addressbyteArray[1];
                b[2] = addressbyteArray[2];
                b[3] = DeviceTypeCodePairs[DeviceType];
                return b;
            }

            public byte[] GetDeviceCodeForWrite() => GetDeviceCodeForRead();

            public McWordDevice(string name)
            {
                if (IsNameFormatCorrect(name, out var type, out var address) && DeviceTypeCodePairs.ContainsKey(type))
                {
                    deviceType = type;
                    this.address = address;
                }
                else
                {
                    throw new ArgumentException($"Create McWordDevice Fail: {name}.");
                }
            }

            public McWordDevice(string name, double? upperLimit = null, double? lowerLimit = null, bool asDouble = false, bool asFloat = false, uint decimalPointPosition = 0) : this(name)
            {
                this.upperLimit = upperLimit;
                this.lowerLimit = lowerLimit;
                this.asDoubleWords = asDouble || asFloat;
                this.asFloat = asFloat;
                this.decimalPointPositon = decimalPointPosition;
            }
        }

        static bool IsNameFormatCorrect(string name, out string type, out uint address)
        {
            int indexA = -1, indexD = -1;
            string aParts = "", dParts = "";
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsLetter(c) && ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
                {
                    if (indexA < 0)
                        indexA = i;
                    if (indexD < 0)
                        aParts += c;
                    else
                        break;
                }
                else if (indexA > -1 && char.IsDigit(c) && (c >= '0' && c <= '9'))
                {
                    if (indexD < 0)
                        indexD = i;
                    if (indexD > -1)
                        dParts += c;
                }
                else
                    break;
            }

            if (name.Length == aParts.Length + dParts.Length && uint.TryParse(dParts, out address))
            {
                type = aParts;
                return true;
            }
            else
            {
                type = null;
                address = 0;
                return false;
            }

        }

        #endregion /Devices


        public McProtocol(McSetting setting, Windows.UI.Core.CoreDispatcher dispatcher) : base(setting, dispatcher)
        {
            this.TransmissionLayerProtocol = setting.TransmissionLayerProtocol;
            this.Code = setting.Code;
        }

    }

    public class McSetting : ProtocolSettingBase
    {
        public TransmissionLayerProtocol TransmissionLayerProtocol { get; set; }

        public CommunicationCode Code { get; set; }
    }

}
