using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynLight.Model.Arduino
{
    public abstract class Arduino
    {
        protected const string querry = "ping";
        protected const string answer = "pong";
        protected static bool setupSuccessful = false;
        public abstract bool Setup();
        public abstract void Send(PayloadType plt, List<byte> data);
    }
}