using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SynLight.Model
{
    public class AutoNodeMCU : ModelBase
    {
        #region Variables
        protected static Socket sock;
        protected static List<Socket> sockList;
        protected static IPEndPoint endPoint;
        protected static IPAddress arduinoIP;
        protected static int UDP_Port = 8787;
        protected static UdpClient Client;        

        protected static string querry = "ping";
        protected static string answer = "pong"; //a0
        protected static bool connected = false;
        #endregion  

        private bool single = true;
        
        public AutoNodeMCU()
        {
            querry = Properties.Settings.Default.querry;
            answer = Properties.Settings.Default.answer;
            if (init())
            {
                byte[] ping = System.Text.Encoding.ASCII.GetBytes(querry);
                Client.BeginReceive(new AsyncCallback(recv), null);
                for (byte n = 0; n < 255; n++)
                {
                    endPoint = new IPEndPoint(new IPAddress(new byte[4] { 192, 168, 0, n }), UDP_Port);
                    try { sock.SendTo(ping, endPoint); }
                    catch { }
                    System.Threading.Thread.Sleep(Properties.Settings.Default.UDPwaitTime);
                    if (connected)
                    {
                        if (!single)
                            sockList.Add(sock);
                        else
                            break;
                    }
                    endPoint = new IPEndPoint(new IPAddress(new byte[4] { 192, 168, 1, n }), UDP_Port);
                    try { sock.SendTo(ping, endPoint); }
                    catch { }
                    System.Threading.Thread.Sleep(Properties.Settings.Default.UDPwaitTime);
                    if (connected)
                    {
                        if (!single)
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
            Client.BeginReceive(new AsyncCallback(recv), null);
            if (System.Text.Encoding.UTF8.GetString(received).Contains(answer))
            {
                connected = true;
            }
        }
        public bool init()
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sockList = new List<Socket>();
            arduinoIP = new IPAddress(new byte[4] { 192, 168, 0, 14 });
            UDP_Port = 8787;
            Client = new UdpClient(0);
            // WOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOW
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
        ~AutoNodeMCU()
        {
            Client.Close();
        }
    }
}
