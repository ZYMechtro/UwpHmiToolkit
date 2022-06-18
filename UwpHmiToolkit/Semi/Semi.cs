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
using UwpHmiToolkit.DataTool;
using System.Threading;
using static UwpHmiToolkit.Semi.HsmsMessage;

namespace UwpHmiToolkit.Semi
{
    public class Semi : AutoBindableBase
    {

        private HsmsSetting HsmsSetting { get; set; }

        protected StreamSocket tcpSocketClient, tcpSocketServer;
        protected Stream inputStreamClient, outputStreamClient;
        protected Stream inputStreamServer, outputStreamServer;

        bool waitingReplyClient = false;
        bool waitingReplyServer = false;


        CancellationTokenSource cts;

        public State CurrentState { get; private set; }

        private CoreDispatcher Dispatcher;

        public Semi(HsmsSetting hsmsSetting)
        {
            this.HsmsSetting = hsmsSetting;
        }

        public async void Start()
        {
            StartServer();

            await Task.Delay(500);

            //StartClient();
        }

        public void Stop()
        {
            cts?.Cancel();
            tcpSocketClient?.Dispose();
        }

        private async void StartServer()
        {
            cts = new CancellationTokenSource();
            try
            {
                var streamSocketListener = new StreamSocketListener();
                streamSocketListener.ConnectionReceived += this.StreamSocketListener_ConnectionReceived;
                streamSocketListener.Control.KeepAlive = true;
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
            if (CurrentState != State.Selected)
            {
                if (CurrentState == State.NotConnected)
                {
                    CurrentState = State.NotSelected;
                    ServerMessageUpdate($"New Connect coming, State: [NotSelected]");
                }

                using (var inputStream = args.Socket.InputStream.AsStreamForRead())
                {
                    var outputStream = args.Socket.OutputStream.AsStreamForWrite();
                    bool separate = false;
                    while (!separate)
                    {
                        byte[] buffer = new byte[4096];

                        try
                        {
                            var length = await inputStream.ReadAsync(buffer, 0, 4096);
                            if (length > 0)
                            {
                                var source = new byte[length];
                                Array.Copy(buffer, 0, source, 0, length);

                                if (TryParseHsms(source, out var request))
                                {
                                    switch (request.SType)
                                    {
                                        case STypes.SelectReq:
                                            if (CurrentState == State.NotSelected)
                                            {
                                                ServerSend(outputStream, ControlMessageSecondary(request, STypes.SelectRsp));
                                                CurrentState = State.Selected;
                                            }
                                            else
                                                ServerSend(outputStream, RejectControlMessage(request, 1));
                                            break;
                                        case STypes.LinktestReq:
                                            if (CurrentState == State.Selected)
                                                ServerSend(outputStream, ControlMessageSecondary(request, STypes.LinktestRsp));
                                            else
                                                ServerSend(outputStream, RejectControlMessage(request, 2));
                                            break;
                                        case STypes.DeselectReq:
                                            if (CurrentState == State.Selected)
                                            {
                                                ServerSend(outputStream, ControlMessageSecondary(request, STypes.DeselectRsp));
                                                CurrentState = State.NotSelected;
                                            }
                                            else
                                                ServerSend(outputStream, RejectControlMessage(request, 3));
                                            break;

                                        case STypes.SeparateReq:
                                            separate = true;
                                            break;

                                        case STypes.DataMessage:
                                            HandleDataMessageServer();
                                            break;

                                        default: break;
                                    }
                                }
                                else
                                {
                                    //Echo
                                    using (StreamWriter writer = new StreamWriter(outputStream))
                                    {
                                        await writer.WriteLineAsync("Message received is not HSMS message. Bye.");
                                        break;
                                    }
                                }


                                ServerMessageUpdate($"Input: {BitConverter.ToString(source)}");
                            }
                            else
                            {
                                if (cts.IsCancellationRequested)
                                {
                                    break;
                                }
                                switch (CurrentState)
                                {
                                    case State.NotSelected:
                                        await Task.Delay(HsmsSetting.T7 * 1000);
                                        if (CurrentState == State.NotSelected)
                                        {
                                            ServerMessageUpdate($"NotSelected timeout (T7), State: [NotConnected]");
                                            CurrentState = State.NotConnected;
                                            separate = true;
                                        }
                                        break;
                                    case State.Selected:
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
                        catch { }
                    }
                }
                CurrentState = State.NotConnected;
            }
            sender.Dispose();
        }

        private async void StartClient()
        {
            tcpSocketClient?.Dispose();
            TcpClient client = new TcpClient(HsmsSetting.TargetIpAddress, int.Parse(HsmsSetting.TargetPort));
            client.Close();

            try
            {
                tcpSocketClient = new Windows.Networking.Sockets.StreamSocket();
                tcpSocketClient.Control.KeepAlive = true;
                var hostName = new HostName(HsmsSetting.LocalIpAddress);
                ClientMessageUpdate("Client is trying to connect...");
                await tcpSocketClient.ConnectAsync(hostName, HsmsSetting.TargetPort);
                ClientMessageUpdate("Client connected");
                outputStreamClient = tcpSocketClient.OutputStream.AsStreamForWrite();
                outputStreamClient.WriteTimeout = HsmsSetting.T8 * 1000;

            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                ClientMessageUpdate(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }

        public async void ClientSend(HsmsMessage hsmsMessage)
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

        public async void ServerSend(Stream outputStream, HsmsMessage hsmsMessage)
        {
            if (outputStream != null)
            {
                try
                {
                    var msg = hsmsMessage.MessageToSend;
                    await outputStream.WriteAsync(msg, 0, msg.Length);
                    await outputStream.FlushAsync();
                    ServerMessageUpdate(string.Format($"Sent : {BitConverter.ToString(msg)}"));
                }
                catch
                {
                    outputStream?.Dispose();
                    ServerMessageUpdate(string.Format($"Server send fail..."));
                }


            }
        }


        private void HandleDataMessageServer()
        {

        }

        private void HandleDataMessageClient()
        {
            throw new NotImplementedException();
        }



        public delegate void ServerMessageHandler(string message);
        public event ServerMessageHandler ServerMessageUpdate;

        public delegate void ClientMessageHandler(string message);
        public event ClientMessageHandler ClientMessageUpdate;
    }



}
