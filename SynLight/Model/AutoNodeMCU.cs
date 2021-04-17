using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SynLight.Model
{
    public class AutoNodeMCU : ModelBase
    {
        #region Variables
        protected static Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp);
        protected static List<Socket> sockList;
        protected static IPEndPoint endPoint;
        protected static IPAddress nodeMCU;
        protected static int UDPPort = 8787; //Must match the next line UdpClient port and the ESP listenning port
        protected static UdpClient Client= new UdpClient(8787);

        protected static readonly string querry = "ping";
        protected static readonly string answer = "pong"; //a0
        #endregion  

        private bool staticConnected = false;
        public bool StaticConnected
        {
            get
            {
                return staticConnected;
            }
            set
            {
                staticConnected = value;
                OnPropertyChanged(nameof(StaticConnected));
            }
        }


        public void FindNodeMCU()
        {
            if (!StaticConnected)
            {
                byte[] ping = Encoding.ASCII.GetBytes(querry);

                byte[] currentIP = GetLocalIPAddress().GetAddressBytes();

                Client.BeginReceive(new AsyncCallback(Recv), null);
                
                for (byte n = 2; n < 254; n++)
                {
                    if (n == currentIP[3])
                        continue;
                    
                    endPoint = new IPEndPoint(new IPAddress(new byte[4] { currentIP[0], currentIP[1], currentIP[2], n }), UDPPort);
                    
                    SendPayload(PayloadType.ping, new List<byte>(ping));                   

                    System.Threading.Thread.Sleep(10);

                    if (StaticConnected)
                        break;
                }
            }
        }
        private void Recv(IAsyncResult res)
        {
            endPoint = new IPEndPoint(IPAddress.Any, 8787);
            byte[] received = Client.EndReceive(res, ref endPoint);
            if (Encoding.UTF8.GetString(received).Contains(answer))
            {
                Client.BeginReceive(new AsyncCallback(Recv), null);
                nodeMCU = endPoint.Address;
                StaticConnected = true;
            }
        }
        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip;
            }
            return null;
        }
        protected enum PayloadType
        {
            ping = 0,
            fixedColor = 1,
            multiplePayload = 2,
            terminalPayload = 3,
        }
        protected static void SendPayload(PayloadType plt, List<byte> payload)
        {
            payload.Insert(0, (byte)plt);
            payload.Insert(0, (byte)('A')); //magic number #1, helps eliminate the junk that is broadcasted on the network

            sock.SendTo(payload.ToArray(), endPoint);
        }
        /*protected static void SendPayload(PayloadType plt, List<byte> payload, EndPoint edp)
        {
            payload.Insert(0, (byte)plt);
            payload.Insert(0, (byte)('A'));

            sock.SendTo(payload.ToArray(), edp);
        }*/
        protected static void SendPayload(PayloadType plt, byte r=0)
        {
            List<byte> payload = new List<byte>();
            payload.Insert(0, r);
            payload.Insert(0, r);
            payload.Insert(0, r);
            payload.Insert(0, (byte)plt);
            payload.Insert(0, (byte)('A'));

            sock.SendTo(payload.ToArray(), endPoint);
        }
        /*protected static void SendPayload(PayloadType plt, byte r = 0, byte g = 0, byte b = 0)
        {
            List<byte> payload = new List<byte>();
            payload.Insert(0, b);
            payload.Insert(0, g);
            payload.Insert(0, r);
            payload.Insert(0, (byte)plt);
            payload.Insert(0, (byte)('A'));

            sock.SendTo(payload.ToArray(), endPoint);
        }*/
        ~AutoNodeMCU()
        {
            Client.Close();
        }
    }
}