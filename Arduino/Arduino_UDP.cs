using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SynLight.Arduino
{
    public class Arduino_UDP : Arduino, IDisposable
    {
        private readonly Socket _sendSocket;
        private readonly UdpClient _receiveClient;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task? _receiveTask;

        private bool _disposed = false;

        public IPAddress? DeviceAddress { get; private set; }
        public IPEndPoint? DeviceEndPoint { get; private set; }
        public static int Port { get; set; } = DefaultPort;

        private const int DefaultPort = 8787;
        private const int MaxPayloadSize = 489;

        public Arduino_UDP()
        {
            _sendSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            _sendSocket.EnableBroadcast = true;

            _receiveClient = new UdpClient(Port);
            _receiveClient.Client.ReceiveTimeout = 250;
        }

        public override bool Setup()
        {
            StartReceiveLoop();
            SendDiscoveryPing();

            bool found = SpinWait.SpinUntil(() => setupSuccessful, 2000);

            return found;
        }

        private void StartReceiveLoop()
        {
            if (_receiveTask != null && !_receiveTask.IsCompleted)
            {
                return;
            }

            _receiveTask = Task.Run(ReceiveLoop);
        }

        private void ReceiveLoop()
        {
            if(!setupSuccessful)
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        IPEndPoint remote = new IPEndPoint(IPAddress.Any, Port);
                        byte[] data = _receiveClient.Receive(ref remote);

                        string message = Encoding.ASCII.GetString(data);

                        Debug.WriteLine($"[UDP] {remote.Address}: {message}");

                        if (message.Contains(answer))
                        {
                            DeviceAddress = remote.Address;
                            DeviceEndPoint = new IPEndPoint(DeviceAddress, Port);

                            setupSuccessful = true;
                            break;
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.TimedOut)
                        {
                            continue;
                        }

                        Debug.WriteLine($"[Receive] Socket error: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Receive] Error: {ex.Message}");
                    }
                }
            }
        }

        private void SendDiscoveryPing()
        {
            IPEndPoint broadcast = new IPEndPoint(IPAddress.Broadcast, Port);

            byte[] payload = Encoding.ASCII.GetBytes(querry);

            byte[] buffer = new byte[payload.Length + 1];
            buffer[0] = (byte)PayloadType.ping;

            Array.Copy(payload, 0, buffer, 1, payload.Length);

            _sendSocket.SendTo(buffer, broadcast);
        }
        public void SetDeviceAddress(IPAddress ipAddress)
        {
            DeviceAddress = ipAddress;
            updateEndPoint();
        }
        public void SetDevicePort(int port)
        {
            Port = port;
            updateEndPoint();
        }
        private void updateEndPoint()
        {
            try
            {
                DeviceEndPoint = new IPEndPoint(DeviceAddress, Port);
                setupSuccessful = true;
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"[UpdateEndPoint] Error: {ex.Message}");
            }
        }

        public override void Send(List<byte> data)
        {
            if (data == null || data.Count == 0)
            {
                return;
            }

            int offset = 0;

            while (offset < data.Count)
            {
                int chunkSize = Math.Min(MaxPayloadSize, data.Count - offset);

                byte[] chunk = new byte[chunkSize];
                data.CopyTo(offset, chunk, 0, chunkSize);

                PayloadType type;

                if (offset + chunkSize >= data.Count)
                {
                    type = PayloadType.terminalPayload;
                }
                else
                {
                    type = PayloadType.multiplePayload;
                }

                Send(type, chunk);

                offset += chunkSize;
            }
        }

        public override void Send(PayloadType type, List<byte> data)
        {
            if (DeviceEndPoint == null)
            {
                throw new InvalidOperationException("Device not initialized.");
            }

            byte[] buffer = new byte[data.Count + 1];
            buffer[0] = (byte)type;

            data.CopyTo(0, buffer, 1, data.Count);

            _sendSocket.SendTo(buffer, DeviceEndPoint);
        }

        private void Send(PayloadType type, byte[] data)
        {
            try
            {
                if (DeviceEndPoint == null)
                {
                    return;
                }

                byte[] buffer = new byte[data.Length + 1];
                buffer[0] = (byte)type;

                Array.Copy(data, 0, buffer, 1, data.Length);

                _sendSocket.SendTo(buffer, DeviceEndPoint);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Send] Error: {e.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _cts.Cancel();

            try
            {
                _receiveClient.Close();
                _receiveClient.Dispose();

                _sendSocket.Close();
                _sendSocket.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Dispose] Error: {ex.Message}");
            }

            _disposed = true;
        }
    }
}