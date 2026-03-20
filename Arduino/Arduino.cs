using System;
using System.Collections.Generic;
using System.Text;

namespace SynLight.Arduino
{
    public abstract class Arduino
    {
        protected const string querry = "ping";
        protected const string answer = "pong";
        protected volatile bool setupSuccessful = false;
        public abstract bool Setup();
        public abstract void Send(List<byte> data);
        public abstract void Send(PayloadType plt, List<byte> data);
    }
}
