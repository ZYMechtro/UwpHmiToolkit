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

        protected StreamSocket tcpSocketClient, tcpSocketServer;
        protected Stream inputStreamClient, outputStreamClient;
        protected Stream inputStreamServer, outputStreamServer;

        bool waitingReplyClient = false;
        bool waitingReplyServer = false;
        List<HsmsMessage> messageListToSend = new List<HsmsMessage>();

        HsmsMessage lastSentMessageServer, lastSentMessageClient;


        CancellationTokenSource ctsServer, ctsClient;


        #region StateModel
        public HsmsState CurrentHsmsState { get; private set; } = HsmsState.NotConnected;
        public CommunicationState CurrentCommunicationState { get; private set; } = CommunicationState.Disable;
        public ControlState CurrentControlState { get; private set; } = ControlState.Offline_HostOffline;
        public ProcessingState CurrentProcessingState { get; private set; } = ProcessingState.Idle;
        public SpoolingState CurrentSpoolingState { get; private set; } = SpoolingState.SpoolInactive;

        public async void SwitchHsmsState(HsmsState newState) => await UpdateWithUI(() => CurrentHsmsState = newState);
        public async void SwitchCommState(CommunicationState newState) => await UpdateWithUI(() => CurrentCommunicationState = newState);
        public async void SwitchControlState(ControlState newState) => await UpdateWithUI(() => CurrentControlState = newState);
        public async void SwitchProcessingState(ProcessingState newState) => await UpdateWithUI(() => CurrentProcessingState = newState);
        public async void SwitchSpoolState(SpoolingState newState) => await UpdateWithUI(() => CurrentSpoolingState = newState);
        #endregion /StateModel


        public Gem(HsmsSetting hsmsSetting, CoreDispatcher coreDispatcher)
        {
            this.HsmsSetting = hsmsSetting;
            this.dispatcher = coreDispatcher;
        }

        public async void Start()
        {
            StartServer();

            await Task.Delay(500);

            //StartClient();
        }

        public void Stop()
        {
            ctsServer?.Cancel();
            tcpSocketClient?.Dispose();
        }

        private async void StartServer()
        {
            inputStreamServer?.Dispose();
            outputStreamServer?.Dispose();

            ctsServer = new CancellationTokenSource();
            try
            {
                SwitchHsmsState(HsmsState.NotConnected);
                SwitchCommState(CommunicationState.Enable_WaitCrFromHost);
                var streamSocketListener = new StreamSocketListener();
                streamSocketListener.ConnectionReceived += this.StreamSocketListener_ConnectionReceived;
                await streamSocketListener.BindServiceNameAsync(HsmsSetting.LocalPort);

                ServerMessageUpdate("Server is listening...");
            }
            catch (Exception ex)
            {
                SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                if (webErrorStatus == SocketErrorStatus.AddressAlreadyInUse)
                {
                    ServerMessageUpdate(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
                }
            }
        }

        private async void StreamSocketListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            if (HsmsSetting.Mode == HsmsSetting.ConnectionMode.Passive)
            {
                int bytesQtyWaiting = 0;
                byte[] inputStack = new byte[0];

                if (CurrentHsmsState != HsmsState.Selected)
                {
                    if (CurrentHsmsState == HsmsState.NotConnected)
                    {
                        SwitchHsmsState(HsmsState.NotSelected);
                        ServerMessageUpdate($"New Connect coming, State: [NotSelected]");
                    }

                    using (var inputStream = args.Socket.InputStream.AsStreamForRead())
                    {
                        var outputStream = args.Socket.OutputStream.AsStreamForWrite();
                        outputStreamServer = outputStream;
                        await UpdateWithUI(() => { SetupTimerServer(); });
                        bool separate = false;
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
                                                    ServerMessageUpdate($"Input : {request.ToSML()}");
                                                    if (CurrentHsmsState == HsmsState.NotSelected)
                                                    {
                                                        SendServer(ControlMessageSecondary(request, STypes.SelectRsp));
                                                        SwitchHsmsState(HsmsState.Selected);
                                                    }
                                                    else
                                                        SendServer(RejectControlMessage(request, 1));
                                                }
                                                break;

                                            case STypes.LinktestReq:
                                                {
                                                    ServerMessageUpdate($"Input : {request.ToSML()}");
                                                    if (CurrentHsmsState == HsmsState.Selected)
                                                        SendServer(ControlMessageSecondary(request, STypes.LinktestRsp));
                                                    else
                                                        SendServer(RejectControlMessage(request, 2));
                                                }
                                                break;

                                            case STypes.DeselectReq:
                                                {
                                                    ServerMessageUpdate($"Input : {request.ToSML()}");
                                                    if (CurrentHsmsState == HsmsState.Selected)
                                                    {
                                                        SendServer(ControlMessageSecondary(request, STypes.DeselectRsp));
                                                        SwitchHsmsState(HsmsState.NotSelected);
                                                    }
                                                    else
                                                        SendServer(RejectControlMessage(request, 3));
                                                }
                                                break;

                                            case STypes.SeparateReq:
                                                {
                                                    ServerMessageUpdate($"Input : {request.ToSML()}");
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
                                            ServerMessageUpdate($"Input error: {BitConverter.ToString(source)}");
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    if (ctsServer.IsCancellationRequested)
                                    {
                                        ctsServer = new CancellationTokenSource();
                                        break;
                                    }
                                    switch (CurrentHsmsState)
                                    {
                                        case HsmsState.NotSelected:
                                            await Task.Delay(HsmsSetting.T7 * 1000);
                                            if (CurrentHsmsState == HsmsState.NotSelected)
                                            {
                                                ServerMessageUpdate($"NotSelected timeout (T7), State: [NotConnected]");
                                                SwitchHsmsState(HsmsState.NotConnected);
                                                separate = true;
                                            }
                                            break;
                                        case HsmsState.Selected:
                                            if (waitingReplyServer)
                                            {
                                                await Task.Delay(HsmsSetting.T3 * 1000);
                                                if (waitingReplyServer)
                                                    ServerMessageUpdate($"Server wait reply timeout.");
                                            }
                                            break;

                                        default:
                                            break;
                                    }
                                    await Task.Delay(1000);
                                }
                            }
                            catch (Exception ex)
                            {
                                separate = true;
                            }
                        }
                    }
                    SwitchHsmsState(HsmsState.NotConnected);
                    SwitchControlState(ControlState.Offline_HostOffline);
                    SwitchCommState(CommunicationState.Enable_WaitCrFromHost);
                    ServerMessageUpdate("Server is listening...");
                }
            }
        }

        private async void StartClient()
        {
            if (HsmsSetting.Mode == HsmsSetting.ConnectionMode.Active && CurrentHsmsState == HsmsState.NotConnected)
            {
                tcpSocketClient?.Dispose();
                new TcpClient(HsmsSetting.TargetIpAddress, int.Parse(HsmsSetting.TargetPort)).Close();

                try
                {
                    tcpSocketClient = new Windows.Networking.Sockets.StreamSocket();
                    var hostName = new HostName(HsmsSetting.LocalIpAddress);
                    ClientMessageUpdate("Client is trying to connect...");
                    await tcpSocketClient.ConnectAsync(hostName, HsmsSetting.TargetPort);
                    ClientMessageUpdate("Client connected");
                    outputStreamClient = tcpSocketClient.OutputStream.AsStreamForWrite();
                    outputStreamClient.WriteTimeout = HsmsSetting.T8 * 1000;
                    inputStreamClient = tcpSocketClient.InputStream.AsStreamForRead();
                    await UpdateWithUI(() => { SetupTimerClient(); });

                    CurrentHsmsState = HsmsState.NotSelected;
                    StartListenClient();
                }
                catch (Exception ex)
                {
                    SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                    ClientMessageUpdate(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
                }
                finally
                {
                    tcpSocketClient?.Dispose();
                    new TcpClient(HsmsSetting.TargetIpAddress, int.Parse(HsmsSetting.TargetPort)).Close();
                }
            }
            else
            {
                if (HsmsSetting.Mode != HsmsSetting.ConnectionMode.Active)
                    ClientMessageUpdate("Connection Mode Is Not Active");
                else
                    ClientMessageUpdate($"Connection Status Not Allow: {CurrentHsmsState}");
            }

        }

        private async void StartListenClient()
        {

        }


        bool sendingServer, sendingClient;
        public async void SendClient(HsmsMessage hsmsMessage)
        {
            if (tcpSocketClient != null && outputStreamClient != null)
            {
                try
                {
                    var msg = hsmsMessage.MessageToSend;
                    await outputStreamClient.WriteAsync(msg, 0, msg.Length);
                    await outputStreamClient.FlushAsync();
                    ClientMessageUpdate(string.Format($"Sent : {BitConverter.ToString(msg)}"));

                    inputStreamClient = tcpSocketClient.InputStream.AsStreamForRead();
                    byte[] buffer = new byte[4096];
                    var length = await inputStreamClient.ReadAsync(buffer, 0, 4096);
                    if (length > 0)
                    {
                        var source = new byte[length];
                        Array.Copy(buffer, 0, source, 0, length);
                        ClientMessageUpdate($"Input: {BitConverter.ToString(source)}");
                    }

                }
                catch
                {
                    tcpSocketClient?.Dispose();
                    ClientMessageUpdate(string.Format($"Clent send fail..."));
                }

            }
        }

        public async void SendServer(HsmsMessage hsmsMessage)
        {
            if (outputStreamServer != null)
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
                            await outputStreamServer.WriteAsync(msg, 0, msg.Length);
                            await outputStreamServer.FlushAsync();
                            lastSentMessageServer = hsmsMessage;
                            sended = true;
                            ServerMessageUpdate(string.Format($"Sent : " + hsmsMessage.ToSML()));
                        }
                    }
                    if (hsmsMessage.WBit)
                    {
                        //waitingReplyServer = true;
                    }
                }
                catch
                {
                    ServerMessageUpdate(string.Format($"Server send fail..."));
                }
                finally
                {
                    sendingServer = false;
                }
            }
        }

        public void AddMessageToSend(HsmsMessage hsmsMessage) => messageListToSend.Add(hsmsMessage);

        public delegate void ServerMessageHandler(string message);
        public event ServerMessageHandler ServerMessageUpdate;

        public delegate void ClientMessageHandler(string message);
        public event ClientMessageHandler ClientMessageUpdate;

        //public delegate void GotMessageHandler(HsmsMessage request);
        //public event GotMessageHandler GotMessage;

        public delegate HsmsMessage GotMessageNeedsReplyHandler(HsmsMessage request);
        public event GotMessageNeedsReplyHandler GotDataMessage;

        //public delegate void RcmdHandler(HsmsMessage request,string rcmd);
        //public event RcmdHandler RcmdRequest;

        DispatcherTimer timerServer, timerClient;

        private async void SetupTimerServer()
        {
            messageListToSend.Clear();
            if (timerServer != null)
                timerServer.Tick -= TimerServer_Tick;
            await UpdateWithUI(() =>
            {
                timerServer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 500) };
                timerServer.Tick += TimerServer_Tick;
            });
            await UpdateWithUI(() =>
            {
                timerServer.Start();
            });
        }

        private void TimerServer_Tick(object sender, object e)
        {
            timerServer.Stop();
            for (int i = 0; i < messageListToSend.Count; i++)
            {
                SendServer(messageListToSend[i]);
                Task.Delay(100);
            }
            messageListToSend.Clear();
            timerServer.Start();
        }

        private async void SetupTimerClient()
        {
            messageListToSend.Clear();
            if (timerClient != null)
                timerClient.Tick -= TimerClient_Tick;
            await UpdateWithUI(() =>
            {
                timerClient = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 500) };
                timerClient.Tick += TimerClient_Tick;
            });
            await UpdateWithUI(() =>
            {
                timerClient.Start();
            });
        }

        private void TimerClient_Tick(object sender, object e)
        {
            timerClient.Stop();
            for (int i = 0; i < messageListToSend.Count; i++)
            {
                SendClient(messageListToSend[i]);
                Task.Delay(100);
            }
            messageListToSend.Clear();
            timerClient.Start();
        }

    }



}
