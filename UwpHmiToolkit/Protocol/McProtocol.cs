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

        byte[] currentCmdSerialNo = new byte[2] { 0, 0 };
        Dictionary<int, int> unhandledSerialIndexPairs = new Dictionary<int, int>();

        const byte netWorkNo = 0x00, pcNo = 0xff, reqDestUnitStationNo = 0x00;

        public override async Task<bool> TryConnectAsync()
        {
            if (TransmissionLayerProtocol == TransmissionLayerProtocol.UDP)
            {
                if (!(udpSocket is null))
                    udpSocket.MessageReceived -= UdpSocket_MessageReceived;
                var result = await this.UdpConnect();
                if (result)
                {
                    isOnline = true;
                    udpSocket.MessageReceived += UdpSocket_MessageReceived;
                    SetupTimer();
                    this.Start();
                }

                return result;
            }
            else
            {
                return false;
                //TODO: TCP
            }

        }

        public override async Task TryDisconnectAsync()
        {
            this.Stop();
            isOnline = false;
            if (TransmissionLayerProtocol == TransmissionLayerProtocol.UDP)
            {
                udpSocket.MessageReceived -= UdpSocket_MessageReceived;
                await UdpDisconnect();
            }
            else
            {

            }
        }

        bool needToRefreshReading;
        protected override void RefreshReadCommand() => needToRefreshReading = true;


        public override async Task CommunicateAsync()
        {
            //Writing Actions
            if (!IsReadonly && devicesToWrite.Count > 0)
            {
                //TODO: Improved Write-Commands
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
                await SendCommand(ToMcCommand4E(loopTestCommand));
            }
            else if (devicesToMonitor.Count > 0)
            {
                if (unhandledSerialIndexPairs.Count > 0)
                    RaiseCommError($"LostResponseData,count: {unhandledSerialIndexPairs.Count}.");

                unhandledSerialIndexPairs.Clear();
                for (int i = 0; i < splitedReadingCmds.Count; i++)
                {
                    await SendCommand(ToMcCommand4E(splitedReadingCmds[i], i));
                }
            }
        }



        private readonly Dictionary<Device, int> deviceIndexPairs = new Dictionary<Device, int>();
        private List<Dictionary<Device, int>> splitedDeviceWaitingForValue;
        private readonly List<byte[]> splitedReadingCmds = new List<byte[]>(), responseData = new List<byte[]>();
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
                    deviceIndexPairs.Add(b, index);

                    if ((lastType != b.DeviceType || lastChannel != b.Channel)
                        && index != 0)
                        index++;

                    lastType = b.DeviceType;
                    lastChannel = b.Channel;
                }
            }
            if (wordDevices.Count() > 0)
            {
                foreach (var w in wordDevices)
                {
                    deviceIndexPairs.Add(w, index);
                    index++;
                }
            }
            if (dwordDevices.Count() > 0)
            {
                foreach (var dw in dwordDevices)
                {
                    deviceIndexPairs.Add(dw, index);
                    index++;
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

        private void UdpSocket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            using (var dataReader = args.GetDataReader())
            {
                byte[] response = new byte[dataReader.UnconsumedBufferLength];
                dataReader.ReadBytes(response);
                if (IsMcResponse(response, out var serialNumber, out var endcode, out var data))
                {
                    var s = BitConverter.ToUInt16(serialNumber, 0);
                    //Registered reading action
                    if (unhandledSerialIndexPairs.ContainsKey(s))
                    {
                        var i = unhandledSerialIndexPairs[s];
                        foreach (var device in splitedDeviceWaitingForValue[i].Keys.ToList())
                        {
                            if (device is McBitDevice b)
                            {
                                var value = new byte[2];
                                Array.Copy(data, deviceIndexPairs[b] % randomReadsMaxCount, value, 0, 2);
                                b.DecodeValue(value);
                            }
                            else if (device is McWordDevice w && !w.Use2Channels)
                            {
                                var value = new byte[2];
                                Array.Copy(data, deviceIndexPairs[w] % randomReadsMaxCount, value, 0, 2);
                                w.DecodeValue(value);
                            }
                            else if (device is McWordDevice dw && dw.Use2Channels)
                            {
                                var value = new byte[4];
                                Array.Copy(data, deviceIndexPairs[dw] % randomReadsMaxCount, value, 0, 2);
                                dw.DecodeValue(value);
                            }
#if DEBUG
                            var str = device is McWordDevice a ? a.Value.ToString() : (device as McBitDevice).Value.ToString();
                            RaiseCommError($"Read {device.Name}: {str}");
#endif
                        }
                        unhandledSerialIndexPairs.Remove(s);
                    }
                    else
                    {
                        if (!Enumerable.SequenceEqual(endcode, new byte[2] { 0, 0 }))
                        {
                            RaiseCommError($"Response Error: {BitConverter.ToString(endcode)}");
                        }
                    }
                }


            }
            //TODO: Handle received message

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

        const int randomReadsMaxCount = 192;
        private static IEnumerable<List<T>> SplitList<T>(List<T> devices, int size = randomReadsMaxCount)
        {
            for (int i = 0; i < devices.Count; i += size)
            {
                yield return devices.GetRange(i, Math.Min(size, devices.Count - i));
            }
        }

        private static IEnumerable<Dictionary<T, int>> SplitDicitionay<T>(Dictionary<T, int> totalDevices, int size = randomReadsMaxCount)
        {
            for (int i = 0; i < totalDevices.Count; i += size)
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
            await outputStream.WriteAsync(request, 0, request.Length);
            await outputStream.FlushAsync();
        }

        private byte[] ToMcCommand4E(byte[] commandContent) => AddSubHeader(PackRequestData(commandContent), null);
        private byte[] ToMcCommand4E(byte[] commandContent, int index) => AddSubHeader(PackRequestData(commandContent), index);

        private static byte[] PackRequestData(byte[] commandContent)
        {
            byte[] a = new byte[5] { netWorkNo, pcNo, 0xff, 0x03, reqDestUnitStationNo };
            byte[] cpuMonitorTimer = new byte[2] { 0x10, 0x00 };
            int length = 2 + commandContent.Length;

            if (8194 - 13 < length)
                throw new ArgumentOutOfRangeException("Command length over 8194 byte.");

            byte[] l = new byte[2] { BitConverter.GetBytes(length)[0], BitConverter.GetBytes(length)[1] };
            var t = a.Concat(l).Concat(cpuMonitorTimer).Concat(commandContent).ToArray();
            return t;
        }


        private byte[] AddSubHeader(byte[] text, int? index)
        {
            byte[] a = new byte[] { 0x54, 0x00 };
            byte[] b = new byte[] { 0x00, 0x00 };
            var t = a.Concat(currentCmdSerialNo).Concat(b).Concat(text).ToArray();

            if (index is int i)
            {
                var s = BitConverter.ToUInt16(currentCmdSerialNo, 0);
                unhandledSerialIndexPairs.Add(s, i);
                Debug.WriteLine($"Send:{currentCmdSerialNo[0]},{currentCmdSerialNo[1]}");
            }

            if (++currentCmdSerialNo[0] == 0)
                ++currentCmdSerialNo[1];

            return t;
        }

        private readonly byte[] loopTestCommand = new byte[8] { 0x19, 0x06, 0x0, 0x0, 0x02, 0x00, 0x4f, 0x4b };


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
                if (IsNameFormatCorrect(name, out var type, out var address) && DeviceTypeCodePairs.ContainsKey(type))
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

            public McWordDevice(string name, double? upperLimit, double? lowerLimit) : this(name)
            {
                this.upperLimit = upperLimit;
                this.lowerLimit = lowerLimit;
            }


        }


        static bool IsNameFormatCorrect(string name, out string type, out uint address)
        {
            int indexA = -1, indexD = -1;
            string aParts = "", dParts = "";
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (Char.IsLetter(c) && (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
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


        public McProtocol(McSetting setting) : base(setting)
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
