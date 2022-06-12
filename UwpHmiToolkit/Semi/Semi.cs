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

        //protected DataReader reader;
        //protected DataWriter writer;

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

#if false

            TcpListener server = null;
            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = 5000;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;

                // Enter the listening loop.
                while (true)
                {
                    ServerMessageUpdate("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also use server.AcceptSocket() here.
                    TcpClient client = await server.AcceptTcpClientAsync();
                    ServerMessageUpdate("Connected!");

                    data = null;

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        
                        ServerMessageUpdate($"Received: {BitConverter.ToString(bytes)}");

                        // Process the data sent by the client.
                        //data = data.ToUpper();

                        //byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                        // Send back a response.
                        //stream.Write(msg, 0, msg.Length);
                        //ServerMessageUpdate($"Sent: {data}");
                        await Task.Delay(3000);
                    }

                    // Shutdown and end connection
                    client.Close();

                    await Task.Delay(3000);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }

            ServerMessageUpdate("\nHit enter to continue...");
            
#endif

#if true
            try
            {
                var streamSocketListener = new Windows.Networking.Sockets.StreamSocketListener();
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
#endif
        }

        private async void StreamSocketListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {


#if false
            byte[] request = new byte[0];
            byte[] bytes1 = null;
            while (true)
            {
                using (DataReader reader = new DataReader(args.Socket.InputStream))
                {
                    //reader.InputStreamOptions = InputStreamOptions.;

                    byte[] buffer = new byte[0];
                    await reader.LoadAsync(1);
                    while (reader.UnconsumedBufferLength > 0)
                    {
                        bytes1 = new byte[reader.UnconsumedBufferLength];
                        reader.ReadBytes(bytes1);
                        request = MyTool.CombineBytes(request, bytes1);
                        ServerMessageUpdate($"Input: {BitConverter.ToString(request)}");
                        await reader.LoadAsync(reader.UnconsumedBufferLength);
                    }
                    reader.DetachStream();
                    
                    if (request != null && request.Length != 0)
                    {
                        ServerMessageUpdate($"Input: {BitConverter.ToString(request)}");
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }
            }

#endif

#if true
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
                        await Task.Delay(500);
                    }
                }
            }
            sender.Dispose();
#endif
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
