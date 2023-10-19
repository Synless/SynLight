using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SynLight.Model.Arduino
{
    public class Arduino_UDP : Arduino
    {
        protected static Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp);
        protected static List<Socket> sockList;
        protected static UdpClient Client = new UdpClient(udpPort);

        private IPAddress ipAddress = null;
        public IPAddress IPAddress
        {
            get
            {
                return ipAddress;
            }
            set
            {
                ipAddress = value;
            }
        }
        private static int udpPort = 8787;
        public static int UDPPort
        {
            get
            {
                return udpPort;
            }
            set
            {
                udpPort = value;
            }
        }

        private IPEndPoint endPoint = null;
        public IPEndPoint EndPoint
        {
            get
            {
                return endPoint;
            }
            set
            {
                endPoint = value;
            }
        }
        public Arduino_UDP()
        {
        }
        ~Arduino_UDP()
        {
            try
            {
                Client.Close();
            }
            catch
            {
            }
        }
        public override bool Setup()
        {
            byte[] ping = Encoding.ASCII.GetBytes(querry);

            byte[] currentIP = GetLocalIPAddress().GetAddressBytes();

            Client.BeginReceive(new AsyncCallback(Recv), null);

            for (byte n = 2; n < 255; n++)
            {
                if(n == currentIP[3])
                    continue;

                endPoint = new IPEndPoint(new IPAddress(new byte[4] { currentIP[0], currentIP[1], currentIP[2], n }), UDPPort);

                Send(PayloadType.ping, new List<byte>(ping));

                System.Threading.Thread.Sleep(10);

                if(setupSuccessful)
                    break;
            }

            return setupSuccessful;
        }

        public override void Send(PayloadType plt, List<byte> data)
        {
            try
            {
                data.Insert(0, (byte)plt);
                sock.SendTo(data.ToArray(), endPoint);
            }
            catch
            {
            }
        }
        private void Recv(IAsyncResult res)
        {
            endPoint = new IPEndPoint(IPAddress.Any, UDPPort);
            byte[] received = Client.EndReceive(res, ref endPoint);
            if (Encoding.UTF8.GetString(received).Contains(answer))
            {
                Client.BeginReceive(new AsyncCallback(Recv), null);
                ipAddress = endPoint.Address;
                setupSuccessful = true;
            }
        }
        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) { if (ip.AddressFamily == AddressFamily.InterNetwork) { return ip; } }
            return null;
        }
    }
}