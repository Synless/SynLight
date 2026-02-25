using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

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

        const int ipRange = 8;
        static UdpClient udpClient = new UdpClient(UDPPort);
        int waitTime = 1;
        public override bool Setup()
        {
            byte[] ping = Encoding.ASCII.GetBytes(querry);

            List<byte> ips = new List<byte>();

            byte[] currentIP = GetLocalIPAddress().GetAddressBytes();

            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipProps = networkInterface.GetIPProperties();
                    UnicastIPAddressInformationCollection ipAddresses = ipProps.UnicastAddresses;

                    Console.Write($"IP addresses for {networkInterface.Name}:");
                    foreach (UnicastIPAddressInformation ipAddress in ipAddresses)
                    {
                        ips.Add(ipAddress.Address.GetAddressBytes()[3]);
                        Console.WriteLine(ipAddress.Address);
                    }
                }
            }

            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, UDPPort);
                        byte[] receiveBytes = udpClient.Receive(ref remoteEndPoint);

                        string message = Encoding.ASCII.GetString(receiveBytes);

                        Console.WriteLine($"Received from {remoteEndPoint.Address}:{remoteEndPoint.Port}: {message}");

                        if(message.Contains(answer))
                        {
                            IPAddress = remoteEndPoint.Address;
                            EndPoint = new IPEndPoint(IPAddress, UDPPort);
                            setupSuccessful = true;
                            break;
                        }
                    }
                    catch
                    {
                    }
                }
            });

            for (byte n = 2; n < 255; n++)
            {
                if(ips.Contains(n))
                    continue;

                endPoint = new IPEndPoint(new IPAddress(new byte[4] { currentIP[0], currentIP[1], ipRange, n }), UDPPort);
                Send(PayloadType.ping, new List<byte>(ping));

                System.Threading.Thread.Sleep(waitTime);

                if(setupSuccessful)
                    break;
            }

            if (!setupSuccessful)
                waitTime = Math.Min(50, waitTime + 1);

            return setupSuccessful;
        }
        public override void Send(List<byte> data)
        {
            Send(PayloadType.terminalPayload, data);
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
        public static List<List<byte>> SplitBytes(List<byte> input, int splitNumber)
        {
            int chunkSize = 3 * splitNumber;
            var result = new List<List<byte>>();

            for (int i = 0; i < input.Count; i += chunkSize)
            {
                int currentChunkSize = Math.Min(chunkSize, input.Count - i);
                List<byte> chunk = input.GetRange(i, currentChunkSize);
                result.Add(chunk);
            }

            return result;
        }
        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) { if (ip.AddressFamily == AddressFamily.InterNetwork) { return ip; } }
            return null;
        }
    }
}