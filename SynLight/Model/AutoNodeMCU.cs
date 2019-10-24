 using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace SynLight.Model
{
    public class AutoNodeMCU : ModelBase
    {
        #region Variables
        protected Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp);
        protected List<Socket> sockList;
        protected IPEndPoint endPoint;
        protected IPAddress nodeMCU;
        protected int UDPPort = 8787; //Must match the next line UdpClient port and the ESP listenning port
        protected UdpClient Client= new UdpClient(8787);

        protected readonly string querry = "ping";
        protected readonly string answer = "pong"; //a0
        #endregion  

        protected static bool staticConnected = false;
        private readonly bool multipleESP = false;
        
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

                if (Init())
                {
                    byte[] ping = System.Text.Encoding.ASCII.GetBytes(querry); //"ping" -> [p,i,n,g]
                    Client.BeginReceive(new AsyncCallback(Recv), null); //Subscribe to the even "data received" : recv

                    for (byte n = 1; n < 254; n++) //Checking local IP from 1 to 254
                    {
                        endPoint = new IPEndPoint(new IPAddress(new byte[4] { 192, 168, 0, n }), UDPPort); //Try with 192.168.0.N
                        try { SendPayload(PayloadType.ping, new List<byte>(ping)); }
                        catch { }
                        System.Threading.Thread.Sleep(10); //Wait for a short while for the ESP to answer and give the time to 'recv ' to trigger

                        if (staticConnected) //If we received "pong"
                        {
                            if (multipleESP) //Can be added to a list of ESP, maybe supported in the futur
                                sockList.Add(sock);
                            else //We found the single ESP, the current IP address if the ESP IP address, just return;
                                break;
                        }

                        endPoint = new IPEndPoint(new IPAddress(new byte[4] { 192, 168, 1, n }), UDPPort); //Try with 192.168.1.N
                        try { SendPayload(PayloadType.ping, new List<byte>(ping)); }
                        catch { }
                        System.Threading.Thread.Sleep(10);

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
        }

        private void Recv(IAsyncResult res)
        {
            endPoint = new IPEndPoint(IPAddress.Any, 8787);
            byte[] received = Client.EndReceive(res, ref endPoint);
            if (System.Text.Encoding.UTF8.GetString(received).Contains(answer))
            {
                Client.BeginReceive(new AsyncCallback(Recv), null);
                staticConnected = true;
            }
        }
        public bool Init()
        {
            //This function may become a check for the manual IP
            //For now, no check is being done when using manual IP address for the ESP

            return true;
        }
        protected enum PayloadType
        {
            ping = 0,
            fixedColor = 1,
            multiplePayload = 2,
            terminalPayload = 3,
        }
        private bool once = true;
        protected void SendPayload(PayloadType plt, List<byte> payload)
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
        protected void SendPayload(PayloadType plt, List<byte> payload, EndPoint edp)
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
        protected void SendPayload(PayloadType plt, byte r=0)
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
        protected void SendPayload(PayloadType plt, byte r = 0, byte g = 0, byte b = 0)
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