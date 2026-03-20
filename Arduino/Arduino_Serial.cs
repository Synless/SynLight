using System.Diagnostics;
using System.IO.Ports;

namespace SynLight.Arduino
{
    public class Arduino_Serial : Arduino, IDisposable
    {
        private SerialPort _serialPort;
        private readonly object _lock = new object();
        private bool _disposed = false;
        public string PortName { get; private set; }
        public const int BaudRate  = 115200;

        public Arduino_Serial()
        {
        }
        public void SetPortName(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new ArgumentException("Invalid port name.", nameof(portName));
            }

            PortName = portName;
            ConfigureMainPort();
        }

        public override bool Setup()
        {
            if(!setupSuccessful)
            {
                string[] allPorts = SerialPort.GetPortNames();

                foreach (string portName in allPorts)
                {
                    try
                    {
                        using (var testPort = new SerialPort(portName, BaudRate))
                        {
                            testPort.ReadTimeout = 300;
                            testPort.WriteTimeout = 300;

                            testPort.Open();

                            // DTR line behavior can cause the device to reset when the port is opened
                            Thread.Sleep(2000);

                            testPort.DiscardInBuffer();
                            testPort.DiscardOutBuffer();

                            testPort.Write(querry);

                            Thread.Sleep(100);

                            string response = string.Empty;

                            try
                            {
                                response = testPort.ReadLine();
                            }
                            catch (TimeoutException)
                            {
                                Debug.WriteLine($"[Setup] No response from port {portName}");
                            }

                            if (!string.IsNullOrEmpty(response) && response.Contains(answer))
                            {
                                PortName = portName;
                                ConfigureMainPort();
                                setupSuccessful = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Setup] Port {portName} failed: {ex.Message}");
                    }
                }
            }
            return setupSuccessful;
        }

        private void ConfigureMainPort()
        {
            _serialPort?.Dispose();

            _serialPort = new SerialPort(PortName, BaudRate)
            {
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            _serialPort.Open();
        }

        public override void Send(List<byte> data)
        {
            Send(PayloadType.terminalPayload, data);
        }

        public override void Send(PayloadType plt, List<byte> data)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Arduino_Serial));
            }

            lock (_lock)
            {
                try
                {
                    EnsurePortOpen();
                    _serialPort.Write(data.ToArray(), 0, data.Count);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Send] Error: {ex.Message}");
                }
            }
        }

        private void EnsurePortOpen()
        {
            if (string.IsNullOrEmpty(PortName))
            {
                throw new InvalidOperationException("Device not initialized. Call Setup() first.");
            }

            if (_serialPort == null)
            {
                throw new InvalidOperationException("Serial port not initialized.");
            }

            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_serialPort != null)
                {
                    try
                    {
                        if (_serialPort.IsOpen)
                        {
                            _serialPort.Close();
                        }

                        _serialPort.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Dispose] Error: {ex.Message}");
                    }
                }
            }

            _disposed = true;
        }
    }
}