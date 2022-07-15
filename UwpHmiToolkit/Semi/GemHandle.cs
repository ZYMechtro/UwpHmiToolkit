using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UwpHmiToolkit.ViewModel;
using Windows.Networking;
using System.Net.Sockets;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using System.Net;
using static UwpHmiToolkit.DataTools.DataTool;
using System.Threading;
using static UwpHmiToolkit.Semi.HsmsMessage;
using static UwpHmiToolkit.Semi.SecsII;

namespace UwpHmiToolkit.Semi
{
    public partial class Gem
    {
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
                                    GotMessageNeedsReply(request);
                                }
                                break;

                            case 11: //Status Variable Namelist Request
                                if (request.WBit)
                                {
                                    GotMessageNeedsReply(request);
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
                                        if (request.WBit)
                                            GotMessageNeedsReply(request);
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
                                case 2:
                                    {

                                    }
                                    break;


                                case 3:
                                    {

                                    }
                                    break;

                                case 5:
                                    {

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

                                    }
                                    break;

                                case 15:
                                    {

                                    }
                                    break;

                                case 19:
                                    {

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


            if (abort)
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


    }
}
