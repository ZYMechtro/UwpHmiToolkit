using System.Collections.Generic;
using System.Linq;
using static UwpHmiToolkit.Semi.HsmsMessage;
using static UwpHmiToolkit.Semi.SecsII;

namespace UwpHmiToolkit.Semi
{
    public partial class Gem
    {
        const byte onByte = 128, offByte = 0;

        private void HandleDataMessage(HsmsMessage request)
        {
            ServerMessageUpdate($"Input : {request.ToSML()}");
            bool abort = false;

            //Refuse message conditions
            if (CurrentCommunicationState == CommunicationState.Disable)
                abort = true;
            else if (CurrentCommunicationState != CommunicationState.Enable_Communicating)
            {
                if (request.Stream == 9) { }
                else if (request.Stream == 1 && (request.Function == 13 || request.Function == 14)) { }
                else
                    abort = true;
            }
            else if (CurrentControlState != ControlState.Online_Local && CurrentControlState != ControlState.Online_Remote)
            {
                if (request.Stream == 1 && (request.Function == 13 || request.Function == 17)) { }
                else
                    abort = true;
            }

            if (!abort)
            {
                //TODO: Finish functions
                switch (request.Stream)
                {
                    case 1: //Tool Status
                        switch (request.Function)
                        {
                            case 1: //Are You Online?
                                {
                                    if (request.WBit)
                                    {
                                        var info = new L();
                                        info.Items.Add(new A(EquipmentInfo.MDLN));
                                        info.Items.Add(new A(EquipmentInfo.SOFTREV));
                                        SendServer(DataMessageSecondary(request, info));
                                    }
                                }
                                break;

                            case 2: //On Line Data
                                {
                                    if (CurrentControlState == ControlState.Offline_AttemptOnline)
                                        SwitchControlState(ControlState.Online_Local);
                                }
                                break;

                            case 3: //Selected Equipment Status Request
                                if (request.WBit)
                                {
                                    GotDataMessage(request);
                                }
                                break;

                            case 11: //Status Variable Namelist Request
                                if (request.WBit)
                                {
                                    GotDataMessage(request);
                                }
                                break;

                            case 13: //Establish Communications Request
                                {
                                    if (CurrentCommunicationState == CommunicationState.Enable_WaitCrFromHost
                                        || CurrentCommunicationState == CommunicationState.Enable_WaitCra
                                        || CurrentCommunicationState == CommunicationState.Enable_WaitDelay
                                        || CurrentCommunicationState == CommunicationState.Enable_Communicating)
                                    {
                                        SwitchCommState(CommunicationState.Enable_Communicating);
                                        if (request.WBit)
                                        {
                                            var L = new L();
                                            var info = new L();
                                            info.Items.Add(new A(EquipmentInfo.MDLN));
                                            info.Items.Add(new A(EquipmentInfo.SOFTREV));
                                            L.Items.Add(new B(0));
                                            L.Items.Add(info);
                                            SendServer(DataMessageSecondary(request, L));
                                        }
                                    }
                                }
                                break;

                            case 14: //Establish Communications Request Acknowledge
                                {
                                    if (CurrentCommunicationState == CommunicationState.Enable_WaitCra
                                        && (lastSentMessageServer.Stream, lastSentMessageServer.Function) == (1, 13))
                                    {
                                        var item = DecodeSecsII(request.MessageText);
                                        if (item is L list && list.Items.Count == 2 && list.Items[0] is B b && b.Items[0] == 0)
                                            SwitchCommState(CommunicationState.Enable_Communicating);
                                    }
                                }
                                break;


                            case 15: //Request OFF-LINE
                                {
                                    SwitchControlState(ControlState.Offline_HostOffline);
                                    if (request.WBit)
                                        SendServer(DataMessageSecondary(request, new B(0)));
                                }
                                break;

                            case 17: //Request ON-LINE
                                {
                                    if (CurrentControlState == ControlState.Offline_HostOffline
                                        || CurrentControlState == ControlState.Offline_AttemptOnline)
                                    {
                                        SwitchControlState(ControlState.Online_Local);
                                        if (request.WBit)
                                            SendServer(DataMessageSecondary(request, new B(0)));
                                    }
                                    else if (CurrentControlState == ControlState.Online_Local
                                        || CurrentControlState == ControlState.Online_Remote)
                                    {
                                        if (request.WBit)
                                            SendServer(DataMessageSecondary(request, new B(2)));
                                    }
                                }
                                break;

                            default:
                                abort = true; break;
                        }
                        break;

                    case 2: //Tool Control & Diagnostics
                        {
                            switch (request.Function)
                            {
                                case 25: //Loopback Diagnostic Request
                                    {
                                        if (request.WBit
                                            && DecodeSecsII(request.MessageText) is B bs)
                                            SendServer(DataMessageSecondary(request, request.MessageText));
                                    }
                                    break;

                                case 37: //Enable/Disable Event Report
                                    {
                                        if (DecodeSecsII(request.MessageText) is L list
                                            && list.Items.Count() >= 2
                                            && list.Items[0] is TF ceed
                                            && list.Items[1] is L ll)
                                        {
                                            if (ll.Items.Count() == 0)
                                            {
                                                foreach (var ev in Events)
                                                {
                                                    ev.CEED.Items[0] = ceed.Items[0];
                                                }
                                            }
                                            else
                                            {
                                                var ceids = new List<uint>();
                                                foreach (U4 ceid in ll.Items)
                                                {
                                                    ceids.Add(ceid.Items[0]);
                                                }
                                                var matchs = Events.Where(e => ceids.Contains(e.CEID.Items[0]));
                                                foreach (var ev in matchs)
                                                {
                                                    ev.CEED.Items[0] = ceed.Items[0];
                                                }
                                            }

                                            SendServer(DataMessageSecondary(request, new B(0)));
                                        }
                                        else
                                            abort = true;
                                    }
                                    break;

                                case 41: //Host Command Send
                                    {
                                        byte HCACK = 0;
                                        if (DecodeSecsII(request.MessageText) is L list
                                            && !list.IsEmpty
                                            && list.Items[0] is A a
                                            && !a.IsEmpty
                                            && a.GetString.ToUpper() is string cmd)
                                        {
                                            if (CurrentControlState != ControlState.Online_Remote && cmd != "REMOTE" && cmd != "LOCAL")
                                                break;

                                            switch (cmd)
                                            {
                                                case "START":
                                                    if (CurrentProcessingState == ProcessingState.Idle)
                                                        SwitchProcessingState(ProcessingState.Ready);
                                                    else if (CurrentProcessingState == ProcessingState.Ready)
                                                        HCACK = 5;
                                                    else
                                                        HCACK = 2;
                                                    break;
                                                case "STOP":
                                                    if (CurrentProcessingState == ProcessingState.Ready
                                                        || CurrentProcessingState == ProcessingState.PauseOnReady)
                                                        SwitchProcessingState(ProcessingState.Idle);
                                                    else if (CurrentProcessingState == ProcessingState.Idle)
                                                        HCACK = 5;
                                                    else
                                                        HCACK = 2;
                                                    break;
                                                case "PAUSE":
                                                    if (CurrentProcessingState == ProcessingState.Ready)
                                                        SwitchProcessingState(ProcessingState.PauseOnReady);
                                                    else if (CurrentProcessingState == ProcessingState.PauseOnReady)
                                                        HCACK = 5;
                                                    else
                                                        HCACK = 2;
                                                    break;
                                                case "RESUME":
                                                    if (CurrentProcessingState == ProcessingState.PauseOnReady)
                                                        SwitchProcessingState(ProcessingState.Ready);
                                                    else if (CurrentProcessingState == ProcessingState.Ready)
                                                        HCACK = 5;
                                                    else
                                                        HCACK = 2;
                                                    break;
                                                case "REMOTE":
                                                    if (CurrentControlState == ControlState.Online_Local)
                                                        SwitchControlState(ControlState.Online_Remote);
                                                    else if (CurrentControlState == ControlState.Online_Remote)
                                                        HCACK = 5;
                                                    else
                                                        HCACK = 2;
                                                    break;
                                                case "LOCAL":
                                                    if (CurrentControlState == ControlState.Online_Remote)
                                                        SwitchControlState(ControlState.Online_Local);
                                                    else if (CurrentControlState == ControlState.Online_Local)
                                                        HCACK = 5;
                                                    else
                                                        HCACK = 2;
                                                    break;
                                                default:
                                                    HCACK = 1;
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            HCACK = 1;
                                        }

                                        SendServer(DataMessageSecondary(request, RcmdReply(HCACK)));
                                    }
                                    break;


                                default:
                                    abort = true; break;
                            }
                        }
                        break;

                    case 3: //Material Status
                        {
                            switch (request.Function)
                            {
                                default:
                                    abort = true; break;
                            }
                        }
                        break;

                    case 4: //Material Control
                        {
                            switch (request.Function)
                            {
                                default:
                                    abort = true; break;
                            }
                        }
                        break;

                    case 5: //Exception Handling
                        {
                            switch (request.Function)
                            {
                                case 2: //Alarm Report Ack
                                    {
                                        if (DecodeSecsII(request.MessageText) is B ackc5
                                            && !ackc5.IsEmpty)
                                        {
                                            //For spooling
                                        }
                                        else abort = true;
                                    }
                                    break;

                                case 3: //Enable/Disable Alarm Send
                                    {
                                        if (DecodeSecsII(request.MessageText) is L list
                                            && !list.IsEmpty
                                            && list.Length == 2
                                            && list.Items[0] is B aled
                                            && list.Items[1] is U4 alid
                                            && SwitchAlarmEnable(aled, alid))
                                        {
                                            SendServer(DataMessageSecondary(request, new B(0))); //return ACKC5 = 0 ok
                                        }
                                        else
                                            abort = true;
                                    }
                                    break;

                                case 5: //List Alarms Request
                                    {
                                        if (request.WBit)
                                        {
                                            if (DecodeSecsII(request.MessageText) is U4 u4)
                                            {
                                                var list = new L();
                                                if (u4.IsEmpty)
                                                {
                                                    foreach (var alarm in Alarms)
                                                    {
                                                        list.Items.Add(alarm.GetL());
                                                    }
                                                    SendServer(DataMessageSecondary(request, list));
                                                }
                                                else if (u4.Items[0] is uint alidVector)
                                                {
                                                    var alarm = Alarms.Where(al => al.ALID.Items[0] == alidVector);
                                                    if (alarm.Count() > 0)
                                                    {
                                                        list.Items.Add(alarm.First().GetL());
                                                        SendServer(DataMessageSecondary(request, list));
                                                    }
                                                    else
                                                    {
                                                        abort = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;

                                case 7:
                                    {
                                        if (request.WBit)
                                        {
                                            var list = new L();
                                            foreach (var alarm in Alarms)
                                            {
                                                if (alarm.ALED.Items[0] >= 128)
                                                    list.Items.Add(alarm.GetL());
                                            }
                                            SendServer(DataMessageSecondary(request, list));
                                        }
                                    }
                                    break;

                                default:
                                    abort = true; break;
                            }
                        }
                        break;

                    case 6: //Data Collection
                        {
                            switch (request.Function)
                            {
                                case 12:
                                    {
                                        if (DecodeSecsII(request.MessageText) is B ackc6
                                            && !ackc6.IsEmpty)
                                        {
                                            //For spooling
                                        }
                                        else abort = true;
                                    }
                                    break;

                                case 15:
                                    {
                                        if (DecodeSecsII(request.MessageText) is U4 u4
                                            && !u4.IsEmpty
                                            && u4.Items[0] is uint ceid
                                            && Events.FirstOrDefault(ev => ev.CEID.Items[0] == ceid) is GemEvent e)
                                        {
                                            SendServer(DataMessagePrimary(6, 16, e.GetL()));
                                        }
                                        else abort = true;
                                    }
                                    break;

                                default:
                                    abort = true; break;
                            }
                        }
                        break;

                    case 7: //Process Program Management
                        {
                            switch (request.Function)
                            {
                                case 1:
                                    {

                                    }
                                    break;

                                case 2:
                                    {

                                    }
                                    break;


                                case 3:
                                    {

                                    }
                                    break;

                                case 4:
                                    {

                                    }
                                    break;


                                case 5:
                                    {

                                    }
                                    break;

                                case 6:
                                    {

                                    }
                                    break;


                                case 17:
                                    {

                                    }
                                    break;

                                case 19:
                                    {

                                    }
                                    break;

                                case 23:
                                    {

                                    }
                                    break;

                                case 25:
                                    {

                                    }
                                    break;


                                default:
                                    abort = true; break;
                            }
                        }
                        break;

                    default:
                        abort = true; break;
                }
            }


            if (abort && request.WBit)
                SendServer(DataMessageAbort(request));
        }

        private void EstablishComm()
        {
            if (CurrentCommunicationState != CommunicationState.Enable_Communicating)
            {
                SwitchCommState(CommunicationState.Enable_WaitCra);
                var info = new L();
                info.Items.Add(new A(EquipmentInfo.MDLN));
                info.Items.Add(new A(EquipmentInfo.SOFTREV));
                SendServer(DataMessagePrimary(1, 13, info));
            }

        }

        public byte[] RcmdReply(byte hcack)
        {
            var l = new L();
            var l2 = new L();
            l.Items.Add(new B(hcack));
            l.Items.Add(l2);
            return EncodeSecsII(l);
        }


        #region Status

        #endregion /Status


        #region Event

        public List<GemEvent> Events = new List<GemEvent>();

        public class GemEvent
        {
            /// <summary>
            /// An identifier to correlate related messages
            /// </summary>
            public static U2 DATAID = new U2(0);

            /// <summary>
            /// Collection event identifier, GEM requires type Un
            /// </summary>
            public U4 CEID;


            /// <summary>
            /// Collection event or trace enablement, true is enabled
            /// </summary>
            public TF CEED = new TF(true);

            public GemEvent(uint ceid)
            {
                CEID = new U4(ceid);
            }

            public L GetL()
            {
                var list = new L();
                DATAID.Items[0]++;
                list.Items.Add(DATAID); //DATAID
                list.Items.Add(CEID);
                list.Items.Add(new L()); //Add empty list for reports (RPTID, VID, V)
                return list;
            }
        }

        public void SendEventReport(uint ceid)
        {
            if (Events.FirstOrDefault(ev => ev.CEID.Items[0] == ceid) is GemEvent e
                && e.CEED.Items[0] > 0)
                messageListToSend.Add(DataMessagePrimary(6, 11, e.GetL()));
        }

        #endregion /Event


        #region Alarm

        public List<GemAlarm> Alarms = new List<GemAlarm>();

        public class GemAlarm
        {
            /// <summary>
            /// Alarm type ID
            /// </summary>
            public U4 ALID { get; }

            /// <summary>
            /// Alarm text
            /// </summary>
            public A ALTX { get; }

            /// <summary>
            /// Enable/disable alarm, 128 means enable, 0 disable
            /// </summary>
            public B ALED { get; } = new B(128);

            /// <summary>
            /// Alarm code byte, >= 128 alarm is on
            /// </summary>
            public B ALCD { get; } = new B(0);

            public GemAlarm(uint alid, string altx)
            {
                ALID = new U4(alid);
                ALTX = new A(altx);
            }

            public L GetL()
            {
                var list = new L();
                list.Items.Add(ALCD);
                list.Items.Add(ALID);
                list.Items.Add(ALTX);
                return list;
            }

        }

        public void ChangeAlarmState(bool on, uint alid)
        {
            byte newState = on ? onByte : offByte;
            var u4 = new U4(alid);
            var matchItems = Alarms.Where(alarm => alarm.ALID.Items[0] == u4.Items[0]);
            foreach (var item in matchItems)
            {
                if (item.ALCD.Items[0] != newState)
                {
                    item.ALCD.Items[0] = newState;

                    //SendReport if enable (ALED)
                    if (item.ALED.ValueInBytes[0] == onByte)
                        SendAlarmReport(item);
                }
            }
        }

        private void SendAlarmReport(GemAlarm gemAlarm)
        {
            var list = new L();
            list.Items.Add(gemAlarm.ALCD);
            list.Items.Add(gemAlarm.ALID);
            list.Items.Add(gemAlarm.ALTX);
            messageListToSend.Add(DataMessagePrimary(5, 1, list));
        }

        private bool SwitchAlarmEnable(B enable, U4 alid)
        {
            if (alid.Items[0] == 0)
            {
                //Set all alarm
                foreach (var alarm in Alarms)
                {
                    alarm.ALED.Items[0] = enable.Items[0];
                }
                return true;
            }
            else
            {
                var matchItems = Alarms.Where(alarm => alarm.ALID.ValueInBytes == alid.ValueInBytes);
                if (matchItems.Count() > 0)
                {
                    foreach (var alarm in Alarms)
                    {
                        alarm.ALED.Items[0] = enable.Items[0];
                    }
                    return true;
                }
                else
                    return false;
            }
        }


        #endregion /Alarm

    }
}
