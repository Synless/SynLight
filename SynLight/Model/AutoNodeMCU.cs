using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO.Ports;

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

        protected static SerialPort serial = new SerialPort();

        protected static string querry;// = "ping";
        protected static string answer;// = "pong"; //a0
        //protected static bool connected = false;
        #endregion  

        protected enum ConnectionType
        {
            Wifi, ManualWifi, Serial, Disconnected
        }
        protected static ConnectionType connection = ConnectionType.Disconnected;

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
                    if (connection == ConnectionType.Wifi)
                    {
                        if (!single)
                            sockList.Add(sock);
                        else
                            return;
                    }
                    endPoint = new IPEndPoint(new IPAddress(new byte[4] { 192, 168, 1, n }), UDP_Port);
                    try { sock.SendTo(ping, endPoint); }
                    catch { }
                    System.Threading.Thread.Sleep(Properties.Settings.Default.UDPwaitTime);
                    if (connection == ConnectionType.Wifi)
                    {
                        if (!single)
                            sockList.Add(sock);
                        else
                            return;
                    }
                }
            }
            if (connection == ConnectionType.Disconnected)
            {
                string[] Ports = SerialPort.GetPortNames();
                if(Ports.Length>0)
                {
                    if (Ports[0] != "COM1")
                    {
                        serial.BaudRate = Properties.Settings.Default.serialBaudRate; 

                        foreach (string port in Ports)
                        {
                            serial.PortName = port;
                            try
                            {
                                if(!serial.IsOpen)
                                {
                                    serial.Open();
                                    System.Threading.Thread.Sleep(1);
                                }
                                serial.WriteLine(Properties.Settings.Default.querry);
                                System.Threading.Thread.Sleep(20);
                                string answer = serial.ReadExisting().Replace("\n", string.Empty).Replace("\r", string.Empty);
                                System.Threading.Thread.Sleep(20);
                                serial.Close();                                
                                if (answer == Properties.Settings.Default.answer)
                                {
                                    connection = ConnectionType.Serial;
                                    return;
                                }
                            }
                            catch
                            {
                            }
                        }
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
                connection = ConnectionType.Wifi;
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
