using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EISA.Model
{
    public class FogNode
    {
        public string Name { get; }
        public Core[] Cores { get; }
        public double Utilization => getUtilization();

        public FogNode(string _name, List<Architecture_model.Cpu.Core> _cores)
        {
            Name = _name;
            Cores = new Core[_cores.Count];
            for (int i = 0; i < _cores.Count; i++)
            {
                Cores[i] = new Core(_cores[i].Id, Name , _cores[i].MacroTick);
            }

        }

        public FogNode(string _name, Core[] _cores)
        {
            Name = _name;
            Cores = new Core[_cores.Length];
            for (int i = 0; i < _cores.Length; i++)
            {
                Cores[i] = _cores[i].Clone();
            }
        }

        private double getUtilization()
        {
            double utilization = 0;
            for (int i = 0; i < Cores.Length; i++)
            {
                utilization += Cores[i].Utilization;
            }

            return utilization / Cores.Length;
        }

        public FogNode Clone()
        {
            return new FogNode(Name, Cores);
        }
    }
}
