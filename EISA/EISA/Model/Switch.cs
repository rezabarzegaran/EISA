using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EISA.Model
{
    public class Switch
    {
        public string Name { get; }
        public List<Port> Ports;

        public Switch(string _name)
        {
            Name = _name;
            Ports = new List<Port>();
        }

        public Switch Clone()
        {
            return new Switch(Name);
        }

        public class Port
        {
            private const int N_Qs = 8;
            Queue[] queues = new Queue[N_Qs];
            public Port()
            {

            }
        }


        public class Queue
        {
            public Queue()
            {

            }
        }
    }
}
