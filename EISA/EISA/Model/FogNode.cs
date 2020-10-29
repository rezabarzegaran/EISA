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
        public int Id { get; }
        public Core[] Cores { get; }
        public double Utilization => getUtilization();

        public FogNode(int _id, List<Architecture_model.Cpu.Core> _cores)
        {
            Id = _id;
            Cores = new Core[_cores.Count];
            for (int i = 0; i < _cores.Count; i++)
            {
                Cores[i] = new Core(_cores[i].Id, Id , _cores[i].MacroTick);
            }

        }

        public FogNode(int _id, Core[] _cores)
        {
            Id = _id;
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
            return new FogNode(Id, Cores);
        }
    }
}
