using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UwpHmiToolkit.ViewModel;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.UI.Core;

namespace UwpHmiToolkit.Semi
{
    public partial class Semi : AutoBindableBase
    {
        public enum DataItemType
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

        protected StreamSocket tcpSocket;

        protected StreamWriter streamWriter;
        protected StreamReader streamReader;

        private CoreDispatcher Dispatcher;

        public Semi(HsmsSetting hsmsSetting)
        {
            this.HsmsSetting = hsmsSetting;
        }

        public async void Start()
        {
            StartServer();
            //await Task.Delay(500);
            //StartClient();
        }

        public void Stop() => tcpSocket?.Dispose();

        private async void StartServer()
        {
            try
            {
                var streamSocketListener = new Windows.Networking.Sockets.StreamSocketListener();

                // The ConnectionReceived event is raised when connections are received.
                streamSocketListener.ConnectionReceived += this.StreamSocketListener_ConnectionReceived;

                // Start listening for incoming TCP connections on the specified port. You can specify any port that's not currently in use.
                await streamSocketListener.BindServiceNameAsync(HsmsSetting.LocalPort);
                //await streamSocketListener.BindEndpointAsync(new HostName("localhost"), HsmsSetting.LocalPort);

                //this.serverListBox.Items.Add("server is listening...");
                ServerMessageUpdate("server is listening...");
            }
            catch (Exception ex)
            {
                SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
                //this.serverListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
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
                request = await streamReader.ReadLineAsync();
            }

            // Echo the request back as the response.
            using (Stream outputStream = args.Socket.OutputStream.AsStreamForWrite())
            {
                using (var streamWriter = new StreamWriter(outputStream))
                {
                    await streamWriter.WriteLineAsync(request);
                    await streamWriter.FlushAsync();
                }
            }

            sender.Dispose();

            ServerMessageUpdate($"server got some message...: {request}");

        }

        private async void StartClient()
        {
            try
            {
                // Create the StreamSocket and establish a connection to the echo server.
                using (var streamSocket = new Windows.Networking.Sockets.StreamSocket())
                {
                    // The server hostname that we will be establishing a connection to. In this example, the server and client are in the same process.
                    var hostName = new HostName(HsmsSetting.LocalIpAddress);

                    ClientMessageUpdate("client is trying to connect...");

                    //await streamSocket.ConnectAsync(hostName, HsmsSetting.TargetPort);
                    await streamSocket.ConnectAsync(hostName, "5000");

                    ClientMessageUpdate("client connected");

                    // Send a request to the echo server.
                    string request = "Hello, World!";
                    using (Stream outputStream = streamSocket.OutputStream.AsStreamForWrite())
                    {
                        using (var streamWriter = new StreamWriter(outputStream))
                        {
                            await streamWriter.WriteLineAsync(request);
                            await streamWriter.FlushAsync();
                        }
                    }

                    ClientMessageUpdate(string.Format("client sent the request: \"{0}\"", request));

                    // Read data from the echo server.
                    string response;
                    using (Stream inputStream = streamSocket.InputStream.AsStreamForRead())
                    {
                        using (StreamReader streamReader = new StreamReader(inputStream))
                        {
                            response = await streamReader.ReadLineAsync();
                        }
                    }

                    ClientMessageUpdate(string.Format("client received the response: \"{0}\" ", response));
                }

                ClientMessageUpdate("client closed its socket");
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                ClientMessageUpdate(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }



        public delegate void ServerMessageHandler(string message);
        public event ServerMessageHandler ServerMessageUpdate;

        public delegate void ClientMessageHandler(string message);
        public event ClientMessageHandler ClientMessageUpdate;
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
