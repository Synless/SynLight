using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace SynLight.Model.Arduino
{
    public class Arduino_Serial : Arduino
    {
        public SerialPort Arduino = new SerialPort();
        public string PortName
        {
            get
            {
                return Arduino.PortName;
            }
            set
            {
                Arduino.PortName = value;
            }
        }
        public int BaudRate
        {
            get
            {
                return Arduino.BaudRate;
            }
            set
            {
                Arduino.BaudRate = value;
            }
        }
        public bool isTurboEnabled = true;

        public Arduino_Serial() { }

        ~Arduino_Serial()
        {
            try
            {
                if (Arduino.IsOpen) { Arduino.Close(); }
            }
            catch
            {
            }
        }
        public override bool Setup()
        {
            BaudRate = 115200;

            string[] allPorts = SerialPort.GetPortNames();

            if (allPorts.Length > 0)
            {
                foreach(string portname in allPorts)
                {
                    try
                    {
                        PortName = portname;

                        Arduino.Open();

                        Arduino.Write(querry);
                        Thread.Sleep(100);
                        string tmp_answer = Arduino.ReadLine();

                        Arduino.Close();

                        if (tmp_answer.Contains(answer))
                        {
                            setupSuccessful = true;
                            break;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return setupSuccessful;
        }

        public override void Send(PayloadType plt, List<byte> data)
        {
            try
            {
                if(!Arduino.IsOpen) { Arduino.Open(); }

                Arduino.Write(data.ToArray(), 0, data.Count);

                if (Arduino.IsOpen && !isTurboEnabled) { Arduino.Close(); }
            }
            catch
            {
            }
        }
    }
}