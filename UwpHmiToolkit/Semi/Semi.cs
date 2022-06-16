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

namespace UwpHmiToolkit.Semi
{
    public class Semi : AutoBindableBase
    {

        private HsmsSetting HsmsSetting { get; set; }

        protected StreamSocket tcpSocketClient;
        protected Stream inputStream, outputStream;

        CancellationTokenSource cts;


        private CoreDispatcher Dispatcher;

        public Semi(HsmsSetting hsmsSetting)
        {
            this.HsmsSetting = hsmsSetting;
        }

        public async void Start()
        {
            StartServer();

            await Task.Delay(500);

            StartClient();
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

            using (Stream inputStream = args.Socket.InputStream.AsStreamForRead())
            {
                while (true)
                {
                    byte[] buffer = new byte[4096];

                    var length = await inputStream.ReadAsync(buffer, 0, 4096);
                    if (length > 0)
                    {
                        var request = new byte[length];
                        Array.Copy(buffer, 0, request, 0, length);
                        ServerMessageUpdate($"Input: {BitConverter.ToString(request)}");
                    }
                    else
                    {
                        if (cts.IsCancellationRequested)
                        {
                            break;
                        }
                        await Task.Delay(10000);
                    }
                }
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
                var hostName = new HostName(HsmsSetting.LocalIpAddress);
                ClientMessageUpdate("Client is trying to connect...");
                await tcpSocketClient.ConnectAsync(hostName, HsmsSetting.TargetPort);
                ClientMessageUpdate("Client connected");

            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                ClientMessageUpdate(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }

        public async void Send(byte[] hsmsMessage)
        {
            if (tcpSocketClient != null)
            {
                outputStream = tcpSocketClient.OutputStream.AsStreamForWrite();
                
                await outputStream.WriteAsync(hsmsMessage, 0, hsmsMessage.Length);
                await outputStream.FlushAsync();

                ClientMessageUpdate(string.Format($"Sent : {BitConverter.ToString(hsmsMessage)}"));
            }
        }


        public delegate void ServerMessageHandler(string message);
        public event ServerMessageHandler ServerMessageUpdate;

        public delegate void ClientMessageHandler(string message);
        public event ClientMessageHandler ClientMessageUpdate;
    }



}
