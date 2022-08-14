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
using Windows.UI.Xaml;

namespace UwpHmiToolkit.Semi
{
    public partial class Gem : AutoBindableBase
    {
        private HsmsSetting HsmsSetting { get; set; }

        public EquipmentInfo EquipmentInfo { get; set; } = new EquipmentInfo();

        protected StreamSocket tcpSocket; //tcpSocketClient
        //protected Stream inputStreamClient, outputStreamClient;
        protected Stream inputStream, outputStream;

        //bool waitingReplyClient = false;
        bool waitingReply = false;
        readonly List<HsmsMessage> messageListToSend = new List<HsmsMessage>();

        HsmsMessage lastSentMessage; //, lastSentMessageClient;

        CancellationTokenSource cts; //, ctsClient;


        #region StateModel
        public HsmsState CurrentHsmsState { get; private set; } = HsmsState.NotConnected;
        public CommunicationState CurrentCommunicationState { get; private set; } = CommunicationState.Disable;
        public ControlState CurrentControlState { get; private set; } = ControlState.Offline_HostOffline;
        public ProcessingState CurrentProcessingState { get; private set; } = ProcessingState.Idle;
        public SpoolingState CurrentSpoolingState { get; private set; } = SpoolingState.SpoolInactive;

        public async void SwitchHsmsState(HsmsState newState) => await UpdateWithUI(() => CurrentHsmsState = newState);
        public async void SwitchCommState(CommunicationState newState) => await UpdateWithUI(() => CurrentCommunicationState = newState);
        public async void SwitchControlState(ControlState newState)
        {
            await UpdateWithUI(() => CurrentControlState = newState);
            switch (newState)
            {
                case ControlState.Offline_HostOffline:
                case ControlState.Offline_EqpOffline:
                    SendEventReport(22);
                    break;

                case ControlState.Online_Local:
                    SendEventReport(8);
                    break;

                case ControlState.Online_Remote:
                    SendEventReport(9);
                    break;

            }
        }

        public async void SwitchProcessingState(ProcessingState newState) => await UpdateWithUI(() => CurrentProcessingState = newState);
        public async void SwitchSpoolState(SpoolingState newState) => await UpdateWithUI(() => CurrentSpoolingState = newState);
        #endregion /StateModel


        public Gem(HsmsSetting hsmsSetting, CoreDispatcher coreDispatcher)
        {
            this.HsmsSetting = hsmsSetting;
            this.dispatcher = coreDispatcher;
        }

        public void Start()
        {
            cts?.Cancel();

            if (HsmsSetting.Mode == HsmsSetting.ConnectionMode.Passive)
                StartServer();
            else
                StartClient();
        }

        public void Stop()
        {
            cts?.Cancel();
        }

        private async void StartServer()
        {
            tcpSocket?.Dispose();

            cts = new CancellationTokenSource();
            try
            {
                SwitchHsmsState(HsmsState.NotConnected);
                SwitchCommState(CommunicationState.Enable_WaitCrFromHost);
                var streamSocketListener = new StreamSocketListener();
                streamSocketListener.ConnectionReceived += this.StreamSocketListener_ConnectionReceived;
                await streamSocketListener.BindServiceNameAsync(HsmsSetting.LocalPort);

                MessageRecord("Server is listening...");
            }
            catch (Exception ex)
            {
                SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                if (webErrorStatus == SocketErrorStatus.AddressAlreadyInUse)
                {
                    MessageRecord(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
                }
            }
        }

        private void StreamSocketListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            if (HsmsSetting.Mode == HsmsSetting.ConnectionMode.Passive)
            {
                if (CurrentHsmsState != HsmsState.Selected)
                {
                    if (CurrentHsmsState == HsmsState.NotConnected)
                    {
                        SwitchHsmsState(HsmsState.NotSelected);
                        MessageRecord($"A new connection is coming, State: [NotSelected]");
                    }
                    tcpSocket = args.Socket;
                    inputStream = tcpSocket.InputStream.AsStreamForRead();
                    outputStream = tcpSocket.OutputStream.AsStreamForWrite();

                    StartListening();

                    MessageRecord("Server is listening...");
                }
            }
        }

        private async void StartClient()
        {
            if (HsmsSetting.Mode == HsmsSetting.ConnectionMode.Active && CurrentHsmsState == HsmsState.NotConnected)
            {
                try
                {
                    tcpSocket?.Dispose();
                    //new TcpClient(HsmsSetting.TargetIpAddress, int.Parse(HsmsSetting.TargetPort)).Close();

                    tcpSocket = new StreamSocket();
                    //var localHoat = new HostName(HsmsSetting.LocalIpAddress);
                    var targetHost = new HostName(HsmsSetting.TargetIpAddress);
                    //EndpointPair endpointPair = new EndpointPair(localHoat, "5000", targetHost, "5000");
                    MessageRecord("Client is trying to connect...");
                    await tcpSocket.ConnectAsync(targetHost, HsmsSetting.TargetPort);
                    //await tcpSocket.ConnectAsync(endpointPair);
                    MessageRecord("Client connected");
                    outputStream = tcpSocket.OutputStream.AsStreamForWrite();
                    inputStream = tcpSocket.InputStream.AsStreamForRead();
                    if (CurrentHsmsState != HsmsState.Selected)
                    {
                        if (CurrentHsmsState == HsmsState.NotConnected)
                        {
                            SwitchHsmsState(HsmsState.NotSelected);
                            MessageRecord($"A new connection is established, State: [NotSelected]");
                            messageListToSend.Add(ControlMessagePrimary(STypes.SelectReq));
                        }
                    }

                    StartListening();
                }
                catch (Exception ex)
                {
                    SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                    MessageRecord(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
                }
            }
            else
            {
                if (HsmsSetting.Mode != HsmsSetting.ConnectionMode.Active)
                    MessageRecord("Connection Mode Is Not Active");
                else
                    MessageRecord($"Connection Status Not Allow: {CurrentHsmsState}");
            }

        }

        private async void StartListening()
        {
            int bytesQtyWaiting = 0;
            byte[] inputStack = new byte[0];

            await UpdateWithUI(() => { SetupTimer(); });
            bool separate = false;
            cts = new CancellationTokenSource();
            while (!separate)
            {
                byte[] buffer = new byte[7_995_148];

                try
                {
                    var length = await inputStream.ReadAsync(buffer, 0, 7_995_148);

                    if (length > 0)
                    {
                        var source = new byte[length];
                        Array.Copy(buffer, 0, source, 0, length);
                        inputStack = bytesQtyWaiting > 0 ? CombineBytes(inputStack, source) : source;

                        var messageIsHsms = TryParseHsms(inputStack, out var request, ref bytesQtyWaiting);
                        if (messageIsHsms)
                        {
                            switch (request.SType)
                            {
                                case STypes.SelectReq:
                                    {
                                        MessageRecord($"Input : {request.ToSML()}");
                                        if (CurrentHsmsState == HsmsState.NotSelected)
                                        {
                                            Send(ControlMessageSecondary(request, STypes.SelectRsp));
                                            SwitchHsmsState(HsmsState.Selected);
                                        }
                                        else
                                            Send(RejectControlMessage(request, 1));
                                    }
                                    break;

                                case STypes.SelectRsp:
                                    {
                                        MessageRecord($"Input : {request.ToSML()}");
                                        if (CurrentHsmsState == HsmsState.NotSelected)
                                        {
                                            SwitchHsmsState(HsmsState.Selected);
                                            SwitchCommState(CommunicationState.Enable_WaitCrFromHost);
                                        }
                                        else
                                            Send(RejectControlMessage(request, 1));
                                    }
                                    break;


                                case STypes.LinktestReq:
                                    {
                                        MessageRecord($"Input : {request.ToSML()}");
                                        if (CurrentHsmsState == HsmsState.Selected)
                                            Send(ControlMessageSecondary(request, STypes.LinktestRsp));
                                        else
                                            Send(RejectControlMessage(request, 2));
                                    }
                                    break;

                                case STypes.DeselectReq:
                                    {
                                        MessageRecord($"Input : {request.ToSML()}");
                                        if (CurrentHsmsState == HsmsState.Selected)
                                        {
                                            Send(ControlMessageSecondary(request, STypes.DeselectRsp));
                                            SwitchHsmsState(HsmsState.NotSelected);
                                        }
                                        else
                                            Send(RejectControlMessage(request, 3));
                                    }
                                    break;

                                case STypes.DeselectRsp:
                                    {
                                        MessageRecord($"Input : {request.ToSML()}");
                                        if (CurrentHsmsState == HsmsState.Selected)
                                        {
                                            SwitchHsmsState(HsmsState.NotSelected);
                                        }
                                        else
                                            Send(RejectControlMessage(request, 3));
                                    }
                                    break;

                                case STypes.SeparateReq:
                                    {
                                        MessageRecord($"Input : {request.ToSML()}");
                                        separate = true;
                                    }
                                    break;

                                case STypes.DataMessage:
                                    if (bytesQtyWaiting == 0)
                                    {
                                        HandleDataMessage(request);
                                        inputStack = new byte[0];
                                    }
                                    break;

                                default: break;
                            }
                        }
                        else
                        {
                            bytesQtyWaiting = 0;

                            //Echo
                            using (StreamWriter writer = new StreamWriter(outputStream))
                            {
                                await writer.WriteLineAsync("Message received is not HSMS message. Bye.");
                                MessageRecord($"Input error: {BitConverter.ToString(source)}");
                                break;
                            }
                        }
                    }

                    switch (CurrentHsmsState)
                    {
                        case HsmsState.NotSelected:
                            await Task.Delay(HsmsSetting.T7 * 1000);
                            if (CurrentHsmsState == HsmsState.NotSelected)
                            {
                                MessageRecord($"NotSelected timeout (T7), State: [NotConnected]");
                                SwitchHsmsState(HsmsState.NotConnected);
                                separate = true;
                            }
                            break;
                        case HsmsState.Selected:
                            if (waitingReply)
                            {
                                await Task.Delay(HsmsSetting.T3 * 1000);
                                if (waitingReply)
                                    MessageRecord($"Server wait reply timeout.");
                            }
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    separate = true;
                    if (cts.IsCancellationRequested)
                    {
                        Send(ControlMessagePrimary(STypes.SeparateReq));
                        await Task.Delay(1000);
                    }
                }
            }
            SwitchHsmsState(HsmsState.NotConnected);
            SwitchControlState(ControlState.Offline_HostOffline);
            SwitchCommState(CommunicationState.Enable_WaitCrFromHost);
        }

        bool sendingServer; //, sendingClient;

        //public async void SendClient(HsmsMessage hsmsMessage)
        //{
        //    if (tcpSocket != null && outputStream != null)
        //    {
        //        try
        //        {
        //            var msg = hsmsMessage.MessageToSend;
        //            await outputStreamClient.WriteAsync(msg, 0, msg.Length);
        //            await outputStreamClient.FlushAsync();
        //            ClientMessageUpdate(string.Format($"Sent : {BitConverter.ToString(msg)}"));

        //            inputStreamClient = tcpSocketClient.InputStream.AsStreamForRead();
        //            byte[] buffer = new byte[4096];
        //            var length = await inputStreamClient.ReadAsync(buffer, 0, 4096);
        //            if (length > 0)
        //            {
        //                var source = new byte[length];
        //                Array.Copy(buffer, 0, source, 0, length);
        //                ClientMessageUpdate($"Input: {BitConverter.ToString(source)}");
        //            }

        //        }
        //        catch
        //        {
        //            tcpSocketClient?.Dispose();
        //            ClientMessageUpdate(string.Format($"Clent send fail..."));
        //        }

        //    }
        //}

        public async void Send(HsmsMessage hsmsMessage)
        {
            if (outputStream != null) //&&
                                      //(
                                      //    hsmsMessage.SType != STypes.DataMessage ||
                                      //    (hsmsMessage.SType == STypes.DataMessage && CurrentCommunicationState == CommunicationState.Enable_Communicating)
                                      //)
                                      //)
            {
                bool sended = false;
                try
                {
                    var msg = hsmsMessage.MessageToSend;
                    while (!sended)
                    {
                        if (sendingServer)
                            await Task.Delay(50);
                        else
                        {
                            sendingServer = true;
                            await outputStream.WriteAsync(msg, 0, msg.Length);
                            await outputStream.FlushAsync();
                            lastSentMessage = hsmsMessage;
                            sended = true;
                            MessageRecord(string.Format($"Sent : " + hsmsMessage.ToSML()));
                        }
                    }
                    if (hsmsMessage.WBit)
                    {
                        //waitingReply = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageRecord(string.Format($"Send fail..."));
                }
                finally
                {
                    sendingServer = false;
                }
            }
        }

        //public void AddMessageToSend(HsmsMessage hsmsMessage) => messageListToSend.Add(hsmsMessage);

        public delegate void ServerMessageHandler(string message);
        public event ServerMessageHandler MessageRecord;

        //public delegate void ClientMessageHandler(string message);
        //public event ClientMessageHandler ClientMessageUpdate;

        //public delegate void GotMessageHandler(HsmsMessage request);
        //public event GotMessageHandler GotMessage;

        public delegate HsmsMessage GotMessageNeedsReplyHandler(HsmsMessage request);
        public event GotMessageNeedsReplyHandler GotDataMessage;

        //public delegate void RcmdHandler(HsmsMessage request,string rcmd);
        //public event RcmdHandler RcmdRequest;

        DispatcherTimer timer;

        private async void SetupTimer()
        {
            if (timer != null)
                timer.Tick -= TimerServer_Tick;
            await UpdateWithUI(() =>
            {
                timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 500) };
                timer.Tick += TimerServer_Tick;
            });
            await UpdateWithUI(() =>
            {
                timer.Start();
            });
        }

        private void TimerServer_Tick(object sender, object e)
        {
            timer.Stop();
            for (int i = 0; i < messageListToSend.Count; i++)
            {
                Send(messageListToSend[i]);
                Task.Delay(100);
            }
            messageListToSend.Clear();
            timer.Start();
        }


        //private async void SetupTimerClient()
        //{
        //    messageListToSend.Clear();
        //    if (timerClient != null)
        //        timerClient.Tick -= TimerClient_Tick;
        //    await UpdateWithUI(() =>
        //    {
        //        timerClient = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 500) };
        //        timerClient.Tick += TimerClient_Tick;
        //    });
        //    await UpdateWithUI(() =>
        //    {
        //        timerClient.Start();
        //    });
        //}

        //private void TimerClient_Tick(object sender, object e)
        //{
        //    timerClient.Stop();
        //    for (int i = 0; i < messageListToSend.Count; i++)
        //    {
        //        SendClient(messageListToSend[i]);
        //        Task.Delay(100);
        //    }
        //    messageListToSend.Clear();
        //    timerClient.Start();
        //}

    }



}
