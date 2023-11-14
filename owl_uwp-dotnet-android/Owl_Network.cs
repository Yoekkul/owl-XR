using System.Net;
using System.Net.Sockets;
using System.Text;
using StereoKit;
using System.Text.Json;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;

namespace StereoKitApp
{
    internal class Owl_Network
    {
        private static readonly Lazy<Owl_Network> lazy =
        new Lazy<Owl_Network>(() => new Owl_Network());

        public static Owl_Network Instance { get { return lazy.Value; } }

        TcpClient client;
        const int PORT_NUMBER = 10501;
        const int HELLO_PORT = 10502;
        IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Loopback, PORT_NUMBER);

        private Owl_Network()
        {

            TcpClient client = new();
            Find_Local_Rhino(); //FIXME make the user select if they connect locally or remotely

            // Thread trd = new Thread(new ThreadStart(this.Run_Connection));
            // trd.IsBackground = true;
            // trd.Start();

            this.client = client;


        }

        private async void Run_Connection()
        {
            await client.ConnectAsync(remoteIpEndPoint.Address, remoteIpEndPoint.Port);//FIXME HANDLE connection refused by polling multiple times

            while (true)
            {
                NetworkStream stream = client.GetStream();
                string received_buffer = "";


                byte[] byte_header = new byte[Header.header_size];
                stream.Read(byte_header,0, (int)Header.header_size);

                Header header = new Header(byte_header);
                Log.Info(header.data_length.ToString());

                if (header.packet_Type == Packet_Type.Geom)
                {
                    byte[] body_bytes = new byte[header.data_length];
                    ReadExactly(stream, body_bytes, 0, header.data_length); //stream.ReadExactly(body_bytes);
                    received_buffer += System.Text.Encoding.UTF8.GetString(body_bytes);

                    // received_buffer = received_buffer.Replace("\0", string.Empty);
                    // Log.Info(received_buffer);

                    Mesh m = Mesh.Quad;

                    m = Rhino_Geometry_processor.Get_Mesh((Rhino.Geometry.GeometryBase)Rhino.Geometry.GeometryBase.FromJSON(received_buffer));
                    // m = Rhino_Geometry_processor.Get_Mesh_From_JSON(received_buffer);
                    mesh_dictionary[header.block_guid] = m;  //FIXME set mesh id based on sending block id
                }


            }
        }

        Dictionary<int, Mesh> mesh_dictionary = new Dictionary<int, Mesh>();

        public ref Dictionary<int, Mesh> Get_Mesh_Dictionary()
        {
            return ref mesh_dictionary;
        }



        //https://stackoverflow.com/questions/22852781/how-to-do-network-discovery-using-udp-broadcast
        public void Find_Local_Rhino()
        {
            //TODO put this into a thread


            UdpClient client = new UdpClient();
            client.EnableBroadcast = true;

            IPEndPoint broadcast_ip = new IPEndPoint(IPAddress.Broadcast, HELLO_PORT);

            byte[] hello_payload = Encoding.UTF8.GetBytes("Owl says whoo whoo"); //FIXME send actually important data like client identifier

            client.Client.ReceiveTimeout = 1000; //TODO handle connection timed out error

            remoteIpEndPoint = new IPEndPoint(IPAddress.Any, HELLO_PORT);

            int max_retry = 50;
            for (int i = 0; i < max_retry; i++)
            {
                try
                {
                    client.Send(hello_payload, hello_payload.Length, broadcast_ip);
                    byte[] receiveBytes = client.Receive(ref remoteIpEndPoint);
                    Log.Info("Connection successful!");
                    remoteIpEndPoint.Port = PORT_NUMBER;
                    break;

                }
                catch (System.Net.Sockets.SocketException e)
                {
                    if (i == max_retry - 1)
                    {
                        Log.Warn("No connection found!");
                        throw e;
                    }
                    else
                    {
                        Log.Info("Retrying connection");
                    }

                }
            }

            Log.Info("Received connection from " + remoteIpEndPoint.Address.ToString() + ":" + remoteIpEndPoint.Port.ToString());
            Thread trd = new Thread(new ThreadStart(this.Run_Connection));
            trd.IsBackground = true;
            trd.Start();
        }

        //https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/socket-services
        // public void send_data(string message){
        //     byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        //     NetworkStream stream = client.GetStream();
        //     stream.Write(messageBytes);
        //     Console.WriteLine($"Socket client sent message: \"{message}\"");
        // }

        public void send_action(string action)
        {
            //{command:action, action:pinch}
            Response resp = new Response("action", action);

            string message = JsonSerializer.Serialize(resp);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            Header hdr = new Header(messageBytes.Length, 0, Packet_Type.Action);
            try
            {
                NetworkStream stream = client.GetStream();
                stream.Write(hdr.to_bytes(), 0, hdr.to_bytes().Length);
                stream.Write(messageBytes, 0, messageBytes.Length);
                Log.Info("Sent " + message);
            }
            catch (Exception e)
            {
                Log.Warn("Failed to send message");
            }
        }



        // [Serializable]
        // struct DataPacket{
        //     Header header{get; set;}
        //     byte[] data;

        // }

        //---------------------- HELPERS

        public static int ReadExactly(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = stream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

                if (bytesRead == 0)
                {
                    // Handle the case where the connection is closed prematurely
                    throw new IOException("Connection closed before all bytes could be read.");
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }


        public class Response
        {
            public string command { get; set; }
            public string action { get; set; }

            public Response(string command, string action)
            {
                this.command = command;
                this.action = action;
            }
        }


        enum Packet_Type
        {
            Geom,
            Info,
            Position,
            Action,
            Delete

        }

        class Header
        {
            public int data_length;
            public int block_guid;
            public Packet_Type packet_Type;

            public static uint header_size = 12;

            public Header(int data_length, int block_guid, Packet_Type packet_Type)
            {
                this.data_length = data_length;
                this.block_guid = block_guid;
                this.packet_Type = packet_Type;

            }
            public Header(byte[] byte_header)
            {
                Log.Info(BitConverter.ToString(byte_header));
                data_length = BitConverter.ToInt32(byte_header, 0);
                block_guid = BitConverter.ToInt32(byte_header, 4);
                packet_Type = (Packet_Type)BitConverter.ToInt32(byte_header, 8);

            }

            public byte[] to_bytes()
            {
                byte[] packed = new byte[3 * sizeof(int)];

                Array.Copy(BitConverter.GetBytes(data_length), 0, packed, 0 * sizeof(int), sizeof(int));
                Array.Copy(BitConverter.GetBytes(block_guid), 0, packed, 1 * sizeof(int), sizeof(int));
                Array.Copy(BitConverter.GetBytes((int)packet_Type), 0, packed, 2 * sizeof(int), sizeof(int));

                return packed;
            }
        }

        // [StructLayout(LayoutKind.Explicit)]
        // record struct Header{
        //     [FieldOffset(0)]
        //     uint data_length;
        //     [FieldOffset(4)]
        //     uint block_guid;
        //     [FieldOffset(8)]
        //     Packet_Type packet_Type;
        // }

    }
}
