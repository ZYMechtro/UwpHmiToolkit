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
                                        SendServer(DataMessageSecondary(request, EncodeSecsII(info)));
                                    }
                                }
                                break;

                            case 2: //On Line Data
                                {
                                    if (CurrentControlState == ControlState.Offline_AttemptOnline)
                                        CurrentControlState = ControlState.Online_Local;
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
                                        || CurrentCommunicationState == CommunicationState.Enable_WaitDelay)
                                    {
                                        CurrentCommunicationState = CommunicationState.Enable_Communicating;
                                        if (request.WBit)
                                        {
                                            var L = new L();
                                            var info = new L();
                                            info.Items.Add(new A(EquipmentInfo.MDLN));
                                            info.Items.Add(new A(EquipmentInfo.SOFTREV));
                                            L.Items.Add(new B(0));
                                            L.Items.Add(info);
                                            SendServer(DataMessageSecondary(request, EncodeSecsII(L)));
                                        }
                                    }
                                }
                                break;

                            case 14: //Establish Communications Request Acknowledge
                                {
                                    int i = 0;
                                    if (CurrentCommunicationState == CommunicationState.Enable_WaitCra
                                        && (lastSentMessageServer.Stream, lastSentMessageServer.Function) == (1, 13))
                                    {
                                        var item = DecodeSecsII(request.MessageText);
                                        if (item is L list && list.Items.Count == 2 && list.Items[0] is B b && b.Items[0] == 0)
                                            CurrentCommunicationState = CommunicationState.Enable_Communicating;
                                    }
                                }
                                break;


                            case 15: //Request OFF-LINE
                                {
                                    CurrentControlState = ControlState.Offline_HostOffline;
                                    if (request.WBit)
                                        SendServer(DataMessageSecondary(request, EncodeSecsII(new B(0))));
                                }
                                break;

                            case 17: //Request ON-LINE
                                {
                                    if (CurrentControlState == ControlState.Offline_HostOffline)
                                        CurrentControlState = ControlState.Online_Local;
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
                                case 25:
                                    {

                                    }
                                    break;

                                case 37:
                                    {

                                    }
                                    break;

                                case 41:
                                    {

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
                CurrentCommunicationState = CommunicationState.Enable_WaitCra;
                var info = new L();
                info.Items.Add(new A(EquipmentInfo.MDLN));
                info.Items.Add(new A(EquipmentInfo.SOFTREV));
                SendServer(DataMessagePrimary(1, 13, EncodeSecsII(info)));
            }

        }




    }
}
