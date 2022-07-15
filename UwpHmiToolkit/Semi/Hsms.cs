using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UwpHmiToolkit.ViewModel;
using UwpHmiToolkit.DataTools;
using static UwpHmiToolkit.DataTools.DataTool;
using static UwpHmiToolkit.Semi.SecsII;


namespace UwpHmiToolkit.Semi
{
    public class HsmsMessage
    {
        #region Field

        /// <summary>
        /// Session Types of HSMS.
        /// </summary>
        public enum STypes : byte
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

        /// <summary>
        /// HSMS-SS State Machine
        /// </summary>
        public enum HsmsState { NotConnected, NotSelected, Selected }

        /// <summary>
        /// SystemByte for sending, Will generate a random value while create.
        /// </summary>
        public static uint CurrentSystemBytes = (uint)new Random().Next();

        /// <summary>
        /// Message length include HSMS header(10)
        /// </summary>
        public uint MessageLength => 10 + (MessageText is null ? 0 : (uint)MessageText.Length);

        /// <summary>
        /// DeviceID of this, for recognize, normally is 0.
        /// </summary>
        public ushort DeviceId { get; } = 0;

        /// <summary>
        /// Expect a reply.
        /// </summary>
        public bool WBit { get; } = false;

        /// <summary>
        /// Stream code of HSMS.
        /// </summary>
        public byte Stream { get; } = 0;

        /// <summary>
        /// Function code of HSMS.
        /// When value is odd, is primary message (means request or active)
        /// When value is even, is secondary message (means reply)
        /// </summary>
        public byte Function { get; } = 0;

        /// <summary>
        /// Presentation Type, Normally 0, means SECS-II message encoding, 
        /// 1-127: reserved for subsidiary standards,
        /// 128-255: not used.
        /// </summary>
        public const byte PType = 0;

        /// <summary>
        /// Session Type, <seealso cref="STypes"/>
        /// </summary>
        public STypes SType { get; } = STypes.DataMessage;

        /// <summary>
        /// Last received SystemByte for reply.
        /// </summary>
        public uint SystemBytes { get; } = 0;

        /// <summary>
        /// SECS-II format items in bytes.
        /// </summary>
        public byte[] MessageText { get; } = null;

        /// <summary>
        /// Pack this into bytes to send.
        /// </summary>
        public byte[] MessageToSend
        {
            get
            {
                byte[] bsLength = BitConverter.GetBytes(MessageLength);
                byte[] bsId = BitConverter.GetBytes(DeviceId);
                byte[] bsSb = BitConverter.GetBytes(SystemBytes);

                ReverseIfLittleEndian(bsLength);
                ReverseIfLittleEndian(bsId);
                ReverseIfLittleEndian(bsSb);

                return CombineBytes(
                    bsLength,
                    bsId,
                    new byte[1] { WBit ? (byte)(Stream | 0b10000000) : Stream },
                    new byte[1] { Function },
                    new byte[1] { PType },
                    new byte[1] { (byte)SType },
                    bsSb,
                    MessageText
                    );
            }
        }


        const ushort deviceId = 0;
        const ushort controlMessageId = 0xffff;

        #endregion /Field

        public static HsmsMessage DataMessagePrimary(byte s, byte f, byte[] text = null, byte? sourceSystemBytes = null)
            => new HsmsMessage(deviceId, (byte)(s | 0b10000000), f, STypes.DataMessage, null, text);

        public static HsmsMessage DataMessagePrimary(byte s, byte f, SecsDataBase secsData = null, byte? sourceSystemBytes = null)
           => new HsmsMessage(deviceId, (byte)(s | 0b10000000), f, STypes.DataMessage, null, EncodeSecsII(secsData));

        public static HsmsMessage DataMessageSecondary(HsmsMessage request, byte[] text = null)
            => new HsmsMessage(request.DeviceId, request.Stream, (byte)(request.Function + 1), STypes.DataMessage, request.SystemBytes, text);

        public static HsmsMessage DataMessageSecondary(HsmsMessage request, SecsDataBase secsData = null)
    => new HsmsMessage(request.DeviceId, request.Stream, (byte)(request.Function + 1), STypes.DataMessage, request.SystemBytes, EncodeSecsII(secsData));


        public static HsmsMessage DataMessageAbort(HsmsMessage request, byte[] text = null)
                    => new HsmsMessage(request.DeviceId, request.Stream, 0, STypes.DataMessage, request.SystemBytes, text);

        public static HsmsMessage ControlMessagePrimary(STypes sType)
            => new HsmsMessage(controlMessageId, 0, 0, sType, CurrentSystemBytes);

        public static HsmsMessage ControlMessageSecondary(HsmsMessage request, STypes sType)
           => new HsmsMessage(request.DeviceId, 0, 0, sType, request.SystemBytes);

        public static HsmsMessage RejectControlMessage(HsmsMessage request, byte reasonCode)
           => new HsmsMessage(request.DeviceId, (byte)request.SType, reasonCode, STypes.RejectReq, request.SystemBytes);

        public static bool TryParseHsms(byte[] sourceMessage, out HsmsMessage hsmsMessage, ref int waitBytesQty)
        {
            if (sourceMessage != null
                && sourceMessage.Length >= 14)
            {
                byte[] lengthByte = new byte[4] { sourceMessage[3], sourceMessage[2], sourceMessage[1], sourceMessage[0] };
                var lengthOfMessage = BitConverter.ToInt32(lengthByte, 0);
                if (lengthOfMessage >= sourceMessage.Length - 4)
                {
                    waitBytesQty = lengthOfMessage - (sourceMessage.Length - 4);
                    var id = BitConverter.ToUInt16(new byte[2] { sourceMessage[5], sourceMessage[4] }, 0);
                    uint sb = BitConverter.ToUInt32(new byte[4] { sourceMessage[13], sourceMessage[12], sourceMessage[11], sourceMessage[10] }, 0);
                    if (sourceMessage.Length > 14)
                    {
                        byte[] data = new byte[sourceMessage.Length - 14];
                        Array.Copy(sourceMessage, 14, data, 0, sourceMessage.Length - 14);
                        hsmsMessage = new HsmsMessage(id, sourceMessage[6], sourceMessage[7], (STypes)sourceMessage[9], sb, data);
                        return true;
                    }
                    hsmsMessage = new HsmsMessage(id, sourceMessage[6], sourceMessage[7], (STypes)sourceMessage[9], sb);
                    return true;
                }
            }
            hsmsMessage = null;
            return false;
        }

        HsmsMessage(ushort deviceId, byte stream, byte function, STypes stype, uint? systemBytes, byte[] messageText = null)
        {
            const byte b = 0b0111_1111;
            DeviceId = deviceId;
            WBit = stream >= b;
            Stream = (byte)(stream & b);
            Function = function;
            SType = stype;
            SystemBytes = systemBytes ?? CurrentSystemBytes++;
            MessageText = messageText;
        }

        public string ToSML()
        {
            if (SType == STypes.DataMessage)
            {
                var r = $"\nS{Stream}F{Function}";
                if (WBit)
                    r += "R";
                r += "\n";
                r += MessageText == null ? "" : DecodeSecsII(MessageText).ToSML(0);
                return r;
            }
            return SType.ToString();
        }
    }

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

}
