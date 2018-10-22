 using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SynLight.Model
{
    public class AutoNodeMCU : ModelBase
    {
        #region Variables
        protected static Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp);
        protected static List<Socket> sockList;
        protected static IPEndPoint endPoint;
        protected static IPAddress nodeMCU;
        protected static int UDP_Port = 8787; //Must match the next line UdpClient port and the ESP listenning port
        protected static UdpClient Client= new UdpClient(8787);

        protected static string querry = "ping";
        protected static string answer = "pong"; //a0
        #endregion  

        protected static bool staticConnected = false;
        private readonly bool multipleESP = false;
        
        public AutoNodeMCU()
        {
            /*
             * Was previously used at the creation of the object, meaning at startup, but it was blocking the view from behind displayed
             * Now the FindNodeMCU is called after the program has started, you can even see the [Trying to connect] state on the tittle-bar
             */
            //FindNodeMCU();

        }
        public void FindNodeMCU()
        {
            staticConnected = false; //If we are here, it means we haven't found the ESP. But if 'staticConnected' is true we should not be here too so ...

            querry = Properties.Settings.Default.querry;
            answer = Properties.Settings.Default.answer;

            if (init())
            {
                byte[] ping = System.Text.Encoding.ASCII.GetBytes(querry); //"ping" -> [p,i,n,g]
                Client.BeginReceive(new AsyncCallback(recv), null); //Subscribe to the even "data received" : recv

                for (byte n = 1; n < 254; n++) //Checking local IP from 1 to 254
                {
                    endPoint = new IPEndPoint(new IPAddress(new byte[4] { 192, 168, 0, n }), UDP_Port); //Try with 192.168.0.N
                    try { SendPayload(PayloadType.ping, new List<byte>(ping)); }
                    catch { }
                    System.Threading.Thread.Sleep(Properties.Settings.Default.UDPwaitTime); //Wait for a short while for the ESP to answer and give the time to 'recv ' to trigger

                    //DEBUG PURPOSE
                    //staticConnected = true;
                    //return;
                    //END DEBUG

                    if (staticConnected) //If we received "pong"
                    {
                        if (multipleESP) //Can be added to a list of ESP, maybe supported in the futur
                            sockList.Add(sock);
                        else //We found the single ESP, the current IP address if the ESP IP address, just return;
                            break;
                    }
                    endPoint = new IPEndPoint(new IPAddress(new byte[4] { 192, 168, 1, n }), UDP_Port); //Try with 192.168.1.N
                    try { SendPayload(PayloadType.ping, new List<byte>(ping)); }
                    catch { }
                    System.Threading.Thread.Sleep(Properties.Settings.Default.UDPwaitTime);
                    if (staticConnected)
                    {
                        if (multipleESP)
                            sockList.Add(sock);
                        else
                            break;
                    }
                }
            }
        }
        private static void recv(IAsyncResult res)
        {
            endPoint = new IPEndPoint(IPAddress.Any, 8787);
            byte[] received = Client.EndReceive(res, ref endPoint);
            if (System.Text.Encoding.UTF8.GetString(received).Contains(answer))
            {
                Client.BeginReceive(new AsyncCallback(recv), null);
                staticConnected = true;
            }
        }
        public bool init()
        {
            //This function may become a check for the manual IP
            //For now, no check is being done when using manual IP address for the ESP

            /*
            nodeMCU = IPAddress.Parse(subLine[1]);
            endPoint = new IPEndPoint(nodeMCU, UDP_Port);
            Tittle = "Synlight - " + subLine[1];
            staticConnected = true;
             */

            return true;

            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            nodeMCU = new IPAddress(new byte[4] { 192, 168, 0, 14 });
            UDP_Port = 8787;
            Client = new UdpClient(0);
            try
            {
                Client = new UdpClient(UDP_Port);
                return true;
            }
            catch
            {
                return false;
            }
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