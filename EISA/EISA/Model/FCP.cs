using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EISA.Model
{
    public class FCP
    {
        public FogNode[] FNs { get; }




        public FCP(Architecture_model _arch)
        {
            FNs = new FogNode[_arch.Cpus.Count];
            for (int i = 0; i < _arch.Cpus.Count; i++)
            {
                FNs[i] = new FogNode(_arch.Cpus[i].Id, _arch.Cpus[i].Cores);
            }
            
        }

        public FCP(FogNode[] _fns)
        {
            FNs = new FogNode[_fns.Length];
            for (int i = 0; i < _fns.Length; i++)
            {
                FNs[i] = _fns[i].Clone();
            }

        }

        public int getNoFNs()
        {
            return FNs.Length;
        }

        public FCP Clone()
        {
            return new FCP(FNs);
        }
    }
}
