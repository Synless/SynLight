using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;

namespace SynLight.Model.Arduino
{
    public class Arduino_UDP_manual : Arduino
    {
        protected static Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp);

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
        public Arduino_UDP_manual()
        {
        }
        public override bool Setup()
        {
            return true;
        }

        public override void Send(PayloadType plt, List<byte> data)
        {
            try
            {
                if(EndPoint != null)
                {
                    data.Insert(0, (byte)plt);
                    sock.SendTo(data.ToArray(), endPoint);
                }
            }
            catch
            {
            }
        }
        /*public override void Send(List<byte> data)
        {
            try
            {
                if (endPoint == null || IPAddress == null)
                {
                    Console.WriteLine("WLED endpoint not set.");
                    return;
                }

                // WLED requires raw RGB data over UDP to port 21324
                IPEndPoint wledEndPoint = new IPEndPoint(IPAddress, 21324);

                int len = data.Count;
                if (len > 0)
                {
                    List<byte> wled_data = new List<byte>();
                    wled_data.Add(4);

                    byte high = (byte)(len >> 8);
                    byte low = (byte)(len % 255);

                    wled_data.Add(high);
                    wled_data.Add(low);

                    foreach (byte b in data)
                    {
                        wled_data.Add((byte)b);
                    }

                    sock.SendTo(wled_data.ToArray(), wledEndPoint);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending data to WLED: {ex.Message}");
            }
        }*/

        public void Send(IPAddress _IPAddress, PayloadType plt, List<byte> data)
        {
            try
            {
                IPAddress = _IPAddress;
                EndPoint = new IPEndPoint(IPAddress, UDPPort);

                data.Insert(0, (byte)plt);
                sock.SendTo(data.ToArray(), endPoint);
            }
            catch
            {
            }
        }
    }
}