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

        protected StreamSocket tcpSocket;

        protected Stream inputStream, outputStream;

        bool waitingReply = false;
        readonly List<HsmsMessage> messageListToSend = new List<HsmsMessage>();

        HsmsMessage lastSentMessage;

        CancellationTokenSource cts;


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
        public async void SwitchProcessState(ProcessingState newState) => await UpdateWithUI(() => CurrentProcessingState = newState);
        public async void SwitchSpoolState(SpoolingState newState) => await UpdateWithUI(() => CurrentSpoolingState = newState);
        #endregion /StateModel


        public Gem(HsmsSetting hsmsSetting, CoreDispatcher coreDispatcher)
        {
            this.HsmsSetting = hsmsSetting;
            this.dispatcher = coreDispatcher;
        }

        public async void Start()
        {
            cts?.Cancel();

            if (HsmsSetting.Mode == HsmsSetting.ConnectionMode.Passive)
                await StartServer();
            else
                await StartClient();
        }

        public void Stop()
        {
            SeperateReq();
        }

        private async Task StartServer()
        {
            tcpSocket?.Dispose();

            cts = new CancellationTokenSource();
            try
            {
                SwitchHsmsState(HsmsState.NotConnected);
                SwitchCommState(CommunicationState.Enable_WaitCrFromHost);
                var streamSocketListener = new StreamSocketListener();
                streamSocketListener.ConnectionReceived += this.StreamSocketListener_ConnectionReceived;
                streamSocketListener.Control.NoDelay = true;
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

        private async void StreamSocketListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
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

                    await StartListening();

                }
            }
        }

        private async Task StartClient()
        {
            if (HsmsSetting.Mode == HsmsSetting.ConnectionMode.Active && CurrentHsmsState == HsmsState.NotConnected)
            {
                try
                {
                    tcpSocket?.Dispose();

                    tcpSocket = new StreamSocket();
                    var targetHost = new HostName(HsmsSetting.TargetIpAddress);
                    MessageRecord("Client is trying to connect...");
                    await tcpSocket.ConnectAsync(targetHost, HsmsSetting.TargetPort);
                    MessageRecord("Client connected");
                    outputStream = tcpSocket.OutputStream.AsStreamForWrite();
                    inputStream = tcpSocket.InputStream.AsStreamForRead();
                    if (CurrentHsmsState != HsmsState.Selected)
                    {
                        if (CurrentHsmsState == HsmsState.NotConnected)
                        {
                            SwitchHsmsState(HsmsState.NotSelected);
                            SwitchCommState(CommunicationState.Enable_WaitCra);
                            MessageRecord($"A new connection is established, State: [NotSelected]");
                            messageListToSend.Add(ControlMessagePrimary(STypes.SelectReq));
                        }
                    }

                    await StartListening();
                    tcpSocket?.Dispose();
                    tcpSocket = null;
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

        private async Task StartListening()
        {
            const int bufferSize = 7_995_148;
            //const int bufferSize = 4096;
            int bytesQtyWaiting = 0;
            byte[] inputStack = new byte[0];

            await UpdateWithUI(() => { SetupTimer(); });
            bool separate = false;
            cts = new CancellationTokenSource();
            while (!separate)
            {
                byte[] buffer = new byte[bufferSize];

                try
                {
                    var length = await inputStream.ReadAsync(buffer, 0, bufferSize, cts.Token);

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
                    else
                        await Task.Delay(50);

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
                }
            }
            SwitchHsmsState(HsmsState.NotConnected);
            if (cts.IsCancellationRequested)
            {
                SwitchControlState(ControlState.Offline_EqpOffline);
                SwitchCommState(CommunicationState.Disable);
            }
            else
            {
                SwitchControlState(ControlState.Offline_HostOffline);
                SwitchCommState(CommunicationState.Enable_WaitCrFromHost);
            }
        }

        public void EstablishComm()
        {
            if (CurrentHsmsState == HsmsState.Selected
                && CurrentCommunicationState != CommunicationState.Enable_Communicating)
            {
                SwitchCommState(CommunicationState.Enable_WaitCra);
                var info = new L();
                info.Items.Add(new A(EquipmentInfo.MDLN));
                info.Items.Add(new A(EquipmentInfo.SOFTREV));
                messageListToSend.Add(DataMessagePrimary(1, 13, info));
            }

        }

        public void SeperateReq()
        {
            if (tcpSocket != null)
            {
                Send(ControlMessagePrimary(STypes.SeparateReq));
            }
            SwitchHsmsState(HsmsState.NotConnected);
            SwitchCommState(CommunicationState.Disable);
            if (cts != null && cts.IsCancellationRequested)
                SwitchControlState(ControlState.Offline_EqpOffline);
            else
                SwitchControlState(ControlState.Offline_HostOffline);

            cts?.Cancel();
            tcpSocket?.Dispose();
            tcpSocket = null;
        }

        bool sendingServer;

        public async void Send(HsmsMessage hsmsMessage)
        {
            if (outputStream != null)
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
                            await outputStream.WriteAsync(msg, 0, msg.Length, cts.Token);
                            await outputStream.FlushAsync(cts.Token);
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
                catch (OperationCanceledException oce)
                {
                    MessageRecord(string.Format($"Sending be cancelled..."));

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

        public delegate void ServerMessageHandler(string message);
        public event ServerMessageHandler MessageRecord;

        public delegate HsmsMessage GotMessageNeedsReplyHandler(HsmsMessage request);
        public event GotMessageNeedsReplyHandler GotDataMessage;

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
    }

}
