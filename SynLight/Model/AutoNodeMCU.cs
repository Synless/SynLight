using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Linq;

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
        protected static UdpClient Client = new UdpClient(8787);

        protected static readonly string querry = "ping";
        protected static readonly string answer = "pong"; //a0

        protected static bool UseComPort = false;
        protected static SerialPort nodeMCU_com = new SerialPort();
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
                if (!UseComPort)
                {
                    byte[] ping = Encoding.ASCII.GetBytes(querry);

                    byte[] currentIP = GetLocalIPAddress().GetAddressBytes();

                    Client.BeginReceive(new AsyncCallback(Recv), null);

                    for (byte n = 2; n < 255; n++)
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
                else
                {
                    List<string> allPorts = SerialPort.GetPortNames().ToList();

                    if (allPorts.Count == 0)
                    {
                        //MessageBox.Show("No COM port to send payload to. Exiting.");
                        return;
                        //Environment.Exit(0);
                    }
                    else if (allPorts.Count == 1)
                    {
                        nodeMCU_com.PortName = allPorts[0];
                    }
                    else
                    {
                        if (!allPorts.Contains(nodeMCU_com.PortName))
                        {
                            //MessageBox.Show("Port " + nodeMCU_com.PortName + " not found. Using port " + allPorts[0] + ".");
                            //nodeMCU_com.PortName = allPorts[0];
                        }
                    }

                    nodeMCU_com.BaudRate = 115200;
                    nodeMCU_com.Open();
                    StaticConnected = true;
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
            try
            {
                if (!UseComPort)
                {
                    payload.Insert(0, (byte)plt);
                    payload.Insert(0, (byte)('A')); //magic number #1, helps eliminate the junk that is broadcasted on the network
                    sock.SendTo(payload.ToArray(), endPoint);
                }
                else
                {
                    nodeMCU_com.Write(payload.ToArray(), 0, payload.Count);
                }
            }
            catch
            {
            }
        }
        protected static void SendPayload(PayloadType plt, byte r = 0)
        {
            try
            {
                List<byte> payload = new List<byte>();

                payload.Insert(0, r);
                payload.Insert(0, r);
                payload.Insert(0, r);

                if (!UseComPort)
                {
                    payload.Insert(0, (byte)plt);
                    payload.Insert(0, (byte)('A'));
                    sock.SendTo(payload.ToArray(), endPoint);
                }
                else
                {
                    nodeMCU_com.Write(payload.ToArray(), 0, payload.Count);
                }
            }
            catch
            {
            }
        }
        ~AutoNodeMCU()
        {
            if (!UseComPort)
                Client.Close();
            else
            {
                if (nodeMCU_com.IsOpen) { nodeMCU_com.Close(); }
            }
        }
    }
}