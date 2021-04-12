 using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

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

        protected static bool staticConnected = false;
        
        public AutoNodeMCU()
        {
            //Disabled while working in static IP mode
            //FindNodeMCU();
        }
        public void FindNodeMCU()
        {
            if (!staticConnected)
            {
                staticConnected = false; //If we are here, it means we haven't found the ESP. But if 'staticConnected' is true we should not be here too so ...
                
                
                byte[] ping = Encoding.ASCII.GetBytes(querry);
                //Client.BeginReceive(new AsyncCallback(Recv), null);

                byte[] currentIP = GetLocalIPAddress().GetAddressBytes();
                Client.BeginReceive(new AsyncCallback(Recv), null); //Subscribe to the even "data received" : recv
                
                for (byte n = 2; n < 254; n++) //Checking local IP from 1 to 254
                {
                    if (n == currentIP[3])
                        continue;
                    
                    endPoint = new IPEndPoint(new IPAddress(new byte[4] { currentIP[0], currentIP[1], currentIP[2], n }), UDPPort);

                    if(n == 175)
                    {

                    }

                    try
                    {
                        SendPayload(PayloadType.ping, new List<byte>(ping));
                    }
                    catch { }
                    System.Threading.Thread.Sleep(8);

                    if (staticConnected)
                    {
                        break;
                    }
                }
            }
        }
        private static void Recv(IAsyncResult res)
        {
            endPoint = new IPEndPoint(IPAddress.Any, 8787);
            byte[] received = Client.EndReceive(res, ref endPoint);
            if (Encoding.UTF8.GetString(received).Contains(answer))
            {
                Client.BeginReceive(new AsyncCallback(Recv), null);
                nodeMCU = endPoint.Address;
                staticConnected = true;
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

            try
            {
                sock.SendTo(payload.ToArray(), endPoint);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

            //for (int n = 2; n < payload.Count-2;n+=3)
            //{
            //    int i = payload[n] + payload[n+1] + payload[n+2];
            //    Console.Write(i.ToString() + "|");
            //}
            //Console.WriteLine();
        }
        protected static void SendPayload(PayloadType plt, List<byte> payload, EndPoint edp)
        {
            payload.Insert(0, (byte)plt);
            payload.Insert(0, (byte)('A')); //magic number #1, helps eliminate the junk that is broadcasted on the network

            try
            {
                sock.SendTo(payload.ToArray(), edp);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

            //for (int n = 2; n < payload.Count-2;n+=3)
            //{
            //    int i = payload[n] + payload[n+1] + payload[n+2];
            //    Console.Write(i.ToString() + "|");
            //}
            //Console.WriteLine();
        }
        protected static void SendPayload(PayloadType plt, byte r=0)
        {
            List<byte> payload = new List<byte>();
            payload.Insert(0, r);
            payload.Insert(0, r);
            payload.Insert(0, r);
            payload.Insert(0, (byte)plt);
            payload.Insert(0, (byte)('A')); //magic number #1, helps eliminate the junk that is broadcasted on the network

            try //If terminating without the ESP being found
            {
                sock.SendTo(payload.ToArray(), endPoint);
            }
            catch { }
        }
        protected static void SendPayload(PayloadType plt, byte r = 0, byte g = 0, byte b = 0)
        {
            List<byte> payload = new List<byte>();
            payload.Insert(0, b);
            payload.Insert(0, g);
            payload.Insert(0, r);
            payload.Insert(0, (byte)plt);
            payload.Insert(0, (byte)('A')); //magic number #1, helps eliminate the junk that is broadcasted on the network

            sock.SendTo(payload.ToArray(), endPoint);
        }
        ~AutoNodeMCU()
        {
            Client.Close();
        }
    }
}