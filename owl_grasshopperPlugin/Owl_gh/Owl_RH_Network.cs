using Rhino;
using Rhino.FileIO;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OwlVR
{
    public delegate void EventHandlerDelegate(string message);


    //https://csharpindepth.com/Articles/Singleton
    internal class Owl_RH_Network
    {
        private static readonly Lazy<Owl_RH_Network> lazy =
        new Lazy<Owl_RH_Network>(() => new Owl_RH_Network());

        public static Owl_RH_Network Instance { get { return lazy.Value; } }

        TcpListener listener;
        IPEndPoint srcIpEndPoint = new IPEndPoint(IPAddress.Any, 10501);
        // List<TcpClient> connected_clients;
        ConcurrentDictionary<TcpClient,TcpClient> connected_clients;
        // private readonly object listLock = new object();

        private Owl_RH_Network(){
            listener = new TcpListener(srcIpEndPoint);
            connected_clients = new ConcurrentDictionary<TcpClient, TcpClient>();

            //Always listen for new connections. On new connection request add client to list of connected clients
            Thread trd = new Thread(new ThreadStart(this.Accept_Connections));
            trd.IsBackground = true;
            trd.Start();
            
            // Responds to local discovery requests. Can connect to local network requests
            Thread udp_trd = new Thread(new ThreadStart(this.UDP_Search_Responder));
            udp_trd.IsBackground = true;
            udp_trd.Start();

            // Receive data sent from headset (Such as positon/rotation/inputs) 
            Thread rcv_trd = new Thread(new ThreadStart(this.Receive_Json));
            rcv_trd.IsBackground = true;
            rcv_trd.Start();
        }

        private void UDP_Search_Responder(){
            IPEndPoint srcIpEndPoint = new IPEndPoint(IPAddress.Any, 10502);
            UdpClient udp_listener = new UdpClient(srcIpEndPoint);

            while(true){
                IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] received = udp_listener.Receive(ref remoteIpEndPoint);

                //TODO check received is whoo whoo
                byte[] response = Encoding.UTF8.GetBytes("The Owl has awoken");

                udp_listener.Send(response, response.Length, remoteIpEndPoint);

            }
        }

        //Continuosly listens for new clients attempting to connect
        private async void Accept_Connections(){
            while (true){
                listener.Start();
                Task<TcpClient> accept_task = listener.AcceptTcpClientAsync();

                TcpClient client = await accept_task;
                connected_clients[client] = client;
                //FIXME send all required data to clinet
            }

        }

        //---------------------------------------------------------------------------

        public event EventHandlerDelegate Command_Received_EVT;

        private void Receive_Json(){
            //1. receive data
            //2. Notify registered components that new data is avaiable
            while(true){
                // lock(listLock){ //Ensures that no item is deleted while we are receiving data
                    foreach (TcpClient client in connected_clients.Values){

                        try{
                            NetworkStream stream = client.GetStream();
                            while(stream.DataAvailable){//FIXME ensure we only return when all the data is read!
                                byte[] byte_header = new byte[Header.header_size];
                                stream.Read(byte_header,0,(int)Header.header_size);
                                Header header = new Header(byte_header);


                                byte[] buffer = new byte[header.data_length];
                                stream.Read(buffer, 0, buffer.Length);  //FIXME READ EXACTLY!!!!
                                // RhinoApp.OutputDebugString();
                                // Debug.Print(System.Text.Encoding.UTF8.GetString(buffer));
                                Response resp = JsonSerializer.Deserialize<Response>(System.Text.Encoding.UTF8.GetString(buffer));
                                Command_Received_EVT?.Invoke(resp.action);
                            }
                        }catch(Exception){

                        }
                    }

                // }
            }
        }


        List<TcpClient> just_diconnected_clients = new List<TcpClient>();
        public async void Send_Json(string json){
            byte[] msg = Encoding.UTF8.GetBytes(json.ToCharArray());

            Header header = new Header(msg.Length, 0, Packet_Type.Geom);
            byte[] header_bytes = header.to_bytes();


            //TODO send data via thread
            foreach (TcpClient client in connected_clients.Values){
                
                NetworkStream stream = client.GetStream();
                try{
                    stream.Write(header_bytes, 0, header_bytes.Length);
                    Task write = stream.WriteAsync(msg, 0, msg.Length);
                    await write;
                }catch(IOException){
                    just_diconnected_clients.Add(client);   //TODO ensure this is only done if we actually have a disconnection
                }
            }

            foreach (TcpClient client in just_diconnected_clients){
                connected_clients.TryRemove(client, out _);
            }

            if(just_diconnected_clients.Count >0){
                just_diconnected_clients.Clear();

            }
        }

        //----------------------------------------------------------------------

        class Response{
            public string command { get; set; }
            public string action { get; set; }
        }

        enum Packet_Type{
            Geom,
            Info,
            Position,
            Action,
            Delete

        }

        class Header{
            public int data_length;
            int block_guid;
            Packet_Type packet_Type;

            public static uint header_size = 12;

            public Header(int data_length, int block_guid, Packet_Type packet_Type){
                this.data_length = data_length;
                this.block_guid = block_guid;
                this.packet_Type = packet_Type;

            }
            public Header(byte[] byte_header){
                data_length = byte_header[0];
                block_guid = byte_header[4];
                packet_Type = (Packet_Type)byte_header[8];

            }

            public byte[] to_bytes(){
                byte[] packed = new byte[3 * sizeof(int)];

                Array.Copy(BitConverter.GetBytes(data_length), 0, packed, 0 * sizeof(int), sizeof(int));
                Array.Copy(BitConverter.GetBytes(block_guid), 0, packed, 1 * sizeof(int), sizeof(int));
                Array.Copy(BitConverter.GetBytes((int)packet_Type), 0, packed, 2 * sizeof(int), sizeof(int));

                return packed;
            }
        }

    }
}
