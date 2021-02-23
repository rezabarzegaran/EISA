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
        public Switch[] SWs { get; }
        public ES[] ESs { get; }




        public FCP(Architecture_model _arch)
        {
            FNs = new FogNode[_arch.Cpus.Count];
            for (int i = 0; i < _arch.Cpus.Count; i++)
            {
                FNs[i] = new FogNode(_arch.Cpus[i].Name, _arch.Cpus[i].Cores);
            }

            SWs = new Switch[_arch.SWs.Count];
            for (int i = 0; i < _arch.SWs.Count; i++)
            {
                SWs[i] = new Switch(_arch.SWs[i].Name);
            }


            ESs = new ES[_arch.ESs.Count];
            for (int i = 0; i < _arch.ESs.Count; i++)
            {
                ESs[i] = new ES(_arch.ESs[i].Name);
            }

        }

        public FCP(FogNode[] _fns, Switch[] _sws, ES[] _ess)
        {
            FNs = new FogNode[_fns.Length];
            for (int i = 0; i < _fns.Length; i++)
            {
                FNs[i] = _fns[i].Clone();
            }

            SWs = new Switch[_sws.Length];
            for (int i = 0; i < _sws.Length; i++)
            {
                SWs[i] = _sws[i].Clone();
            }

            ESs = new ES[_ess.Length];
            for (int i = 0; i < _ess.Length; i++)
            {
                ESs[i] = _ess[i].Clone();
            }

        }

        public int getNoFNs()
        {
            return FNs.Length;
        }

        public FCP Clone()
        {
            return new FCP(FNs, SWs, ESs);
        }
    }
}
