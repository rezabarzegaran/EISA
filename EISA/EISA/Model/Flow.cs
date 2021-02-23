using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EISA.Model
{
    public class Flow
    {
        public string Name { get; }

        public int Size { get; }

        public int RelSize { get; private set; }

        public int Period { get; }

        public int Deadline { get; }

        public int Priority { get; }
        public int TransmitTime { get; }

        public int Hyperperiod { get; private set; }

        public int N_Instances { get; private set; }

        public enum Flow_Type
        {
            Input,
            Output
        }

        public Flow_Type Type { get; }
        public Flow(Application_model.Flow _flow, Flow_Type _type)
        {
            Name = _flow.Name;
            Size = _flow.Size;
            Period = _flow.Period;
            Deadline = _flow.Deadline;
            Priority = _flow.Priority;
            Type = _type;
            TransmitTime = (int)Math.Ceiling((Size + 42) * 0.08);


        }

        public Flow(string _name, int _size, int _period, int _deadline, int _priority, Flow_Type _type, int _hyperperiod)
        {
            Name = _name;
            Size = _size;
            Period = _period;
            Deadline = _deadline;
            Priority = _priority;
            Type = _type;
            TransmitTime = (int)Math.Ceiling((Size + 42) * 0.08);
            SetHyperperiod(_hyperperiod);
            SetN_Instances();
        }

        public void SetRelSize(int gcd)
        {
            RelSize = Size / gcd;
        }

        public void Init(int _hyperperiod)
        {
            SetHyperperiod(_hyperperiod);
            SetN_Instances();
        }

        private void SetHyperperiod(int val)
        {
            Hyperperiod = val;
        }

        private void SetN_Instances()
        {
            N_Instances = Hyperperiod / Period;
        }
        public Flow Clone()
        {
            return new Flow(Name, Size, Period, Deadline, Priority, Type, Hyperperiod);
        }
    }
}
