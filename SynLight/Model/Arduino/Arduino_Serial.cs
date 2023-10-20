using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
        public bool isTurboEnabled = false;

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
                try
                {
                    //More code to be written here
                    //Free pass for now
                    PortName = allPorts[0];
                    //Arduino.Open();
                    setupSuccessful = true;
                }
                catch
                {
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

                //To comment?
                if (Arduino.IsOpen && !isTurboEnabled) { Arduino.Close(); }
            }
            catch
            {
            }
        }
    }
}