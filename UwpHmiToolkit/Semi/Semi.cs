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

namespace UwpHmiToolkit.Semi
{
    public class Semi : AutoBindableBase
    {
        public enum DataItemType : ushort
        {
            List = 0b0000_00,
            Binary = 0b0010_00,
            Boolean = 0b0010_01,
            ASCII = 0b0100_00,
            JIS8 = 0b0100_01,
            I8 = 0b0110_00,
            I1 = 0b0110_01,
            I2 = 0b0110_10,
            I4 = 0b0111_00,
            F8 = 0b1000_00,
            F4 = 0b1001_00,
            U8 = 0b1010_00,
            U1 = 0b1010_01,
            U2 = 0b1010_10,
            U4 = 0b1011_00,
        }

        private HsmsSetting HsmsSetting { get; set; }

        protected StreamSocket tcpSocketServer, tcpSocketClient;
        protected Stream inputStream, outputStream;

        protected StreamReader reader;
        protected StreamWriter writer;

        //protected StreamWriter streamWriter;
        //protected StreamReader streamReader;

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
            tcpSocketServer?.Dispose();
            tcpSocketClient?.Dispose();
        }


        private async void StartServer()
        {
            try
            {
                var streamSocketListener = new Windows.Networking.Sockets.StreamSocketListener();
                streamSocketListener.ConnectionReceived += this.StreamSocketListener_ConnectionReceived;
                streamSocketListener.Control.KeepAlive = true;
                await streamSocketListener.BindServiceNameAsync(HsmsSetting.LocalPort);

                ServerMessageUpdate("server is listening...");
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
            string request;
            using (var streamReader = new StreamReader(args.Socket.InputStream.AsStreamForRead()))
            {
                while (true)
                {
                    request = await streamReader.ReadLineAsync();
                    if (request != null)
                        ServerMessageUpdate($"Input: {request}");
                    else
                    {
                        await Task.Delay(10000);
                    }
                }
            }
            sender.Dispose();
        }

        private async void StartClient()
        {
            TcpClient client = new TcpClient(HsmsSetting.TargetIpAddress, int.Parse(HsmsSetting.TargetPort));
            client.Close();

            tcpSocketClient?.Dispose();
            try
            {
                tcpSocketClient = new Windows.Networking.Sockets.StreamSocket();
                var hostName = new HostName(HsmsSetting.LocalIpAddress);
                ClientMessageUpdate("client is trying to connect...");
                await tcpSocketClient.ConnectAsync(hostName, HsmsSetting.TargetPort);
                ClientMessageUpdate("client connected");

            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                ClientMessageUpdate(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }



        }
        string request = "Hello world!";
        public async void Send(byte[] hsmsMessage)
        {
            if (tcpSocketClient != null)
            {
                outputStream = tcpSocketClient.OutputStream.AsStreamForWrite();
                writer = new StreamWriter(outputStream);
                request += "0";
                await writer.WriteLineAsync(request);
                await writer.FlushAsync();

                ClientMessageUpdate(string.Format("client sent the request: \"{0}\"", request));
            }

            if (tcpSocketClient != null && false)
            {

                // Send a request to the echo server.
                outputStream = tcpSocketClient.OutputStream.AsStreamForWrite();
                //await outputStream.WriteAsync(hsmsMessage, 0, hsmsMessage.Length);
                //await outputStream.FlushAsync();


                //ClientMessageUpdate(string.Format("client sent the request: \"{0}, Port {1} to {2} \"", BitConverter.ToString(hsmsMessage), tcpSocketClient.Information.LocalPort, tcpSocketClient.Information.RemotePort));


                //Send a request to the echo server.
                string request = "Hello, World!";
                //writer = new StreamWriter(outputStream);
                // await writer.WriteLineAsync(request);
                //await writer.FlushAsync();

                ClientMessageUpdate(string.Format("client sent the request: \"{0}\"", request));
            }
        }


        public delegate void ServerMessageHandler(string message);
        public event ServerMessageHandler ServerMessageUpdate;

        public delegate void ClientMessageHandler(string message);
        public event ClientMessageHandler ClientMessageUpdate;
    }



}
