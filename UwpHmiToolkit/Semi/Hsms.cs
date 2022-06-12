using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UwpHmiToolkit.ViewModel;
using UwpHmiToolkit.DataTool;

namespace UwpHmiToolkit.Semi
{
    public class HsmsSetting : AutoBindableBase
    {
        public enum ConnectionMode { Passive, Active };

        public ConnectionMode Mode { get; set; } = ConnectionMode.Passive;

        /// <summary>
        /// Reply Timeout in sec.
        /// </summary>
        public ushort T3 { get; set; } = 30;

        /// <summary>
        /// Connection Separation Timeout in sec.
        /// </summary>
        public ushort T5 { get; set; } = 10;

        /// <summary>
        /// Control Transaction Timeout in sec.
        /// </summary>
        public ushort T6 { get; set; } = 10;

        /// <summary>
        /// Not Selected Timeout in sec.
        /// </summary>
        public ushort T7 { get; set; } = 10;

        /// <summary>
        /// Network Timeout in sec.
        /// </summary>
        public ushort T8 { get; set; } = 10;

        public byte[] DeviceId { get; set; } = new byte[2] { 0x00, 0x00 };

        public string TargetIpAddress { get; } = "127.0.0.1";
        public string LocalIpAddress { get; } = "127.0.0.1";
        public string TargetPort { get; } = "5000";
        public string LocalPort { get; } = "5000";
    }

    public class HsmsMessage
    {
        enum Stypes : byte
        {
            DataMessage = 0,
            SelectReq = 1,
            SelectRsp = 2,
            DeselectReq = 3,
            DeselectRsp = 4,
            LinktestReq = 5,
            LinktestRsp = 6,
            RejectReq = 7,
            SeparateReq = 9,
        };

        public static uint CurrentSystemBytes;

        public uint MessageLength => IsHsmsMessage ? 10 + (uint)MessageText?.Length : 0;

        public ushort DeviceId { get; } = 01;
        public byte Stream { get; } = 01;
        public byte Function { get; } = 01;

        public const byte Ptype = 01;
        public byte Stype { get; } = (byte)Stypes.DataMessage;
        public uint SystemBytes { get; } = 01;
        public byte[] MessageText { get; } = null;

        public bool IsHsmsMessage { get; } = false;

        public byte[] MessageToSend
        {
            get
            {
                if (IsHsmsMessage)
                {
                    return MyTool.CombineBytes(
                        BitConverter.GetBytes(MessageLength),
                        BitConverter.GetBytes(DeviceId),
                        new byte[1] { Stream },
                        new byte[1] { Function },
                        new byte[1] { Ptype },
                        new byte[1] { Stype },
                        BitConverter.GetBytes(SystemBytes),
                        MessageText,
                        new byte[2] { 0x0d, 0x0a }
                         );
                }
                else
                {
                    return null;
                };
            }
        }

        public static HsmsMessage DataMessage(byte s, byte f, byte[] text = null, byte? sourceSystemBytes = null)
            => new HsmsMessage(0, s, f, (byte)Stypes.DataMessage, sourceSystemBytes, text);




        HsmsMessage(ushort deviceId, byte stream, byte function, byte stype, uint? systemBytes, byte[] messageText)
        {
            DeviceId = deviceId;
            Stream = stream;
            Function = function;
            Stype = stype;
            SystemBytes = systemBytes ?? CurrentSystemBytes++;
            MessageText = messageText;
            IsHsmsMessage = true;
        }

        HsmsMessage(byte[] sourceMessage)
        {

        }


    }

}
