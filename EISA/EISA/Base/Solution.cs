using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlTypes;
using System.Text;
using System.Threading.Tasks;
using EISA.Extension;
using EISA.Model;

namespace EISA.Base
{
    public class Solution
    {
        public FCP Fcp { get; }
        public List<Applications> Apps { get; }

        public int Hyperperiod { get; }

        public Solution(Application_model _apps, Architecture_model _arch)
        {
            Fcp = new FCP(_arch);
            Apps = new List<Applications>();
            foreach (Application_model.TaskGraph _app in _apps.Apps)
            {
                Apps.Add(new Applications(_app, getTasks(_apps,_app)));
            }

            Hyperperiod = GetHyperperiod();
        }

        public Solution(FCP _fcp, List<Applications> _apps)
        {
            Fcp = _fcp.Clone();
            Apps = new List<Applications>();
            foreach (Applications _app in _apps)
            {
                Apps.Add(_app.Clone());
            }
            Hyperperiod = GetHyperperiod();
        }

        public int GetHyperperiod()
        {
            int _hyperperiod = 1;

            foreach ( Applications App  in Apps)
            {
                List<int> periods = App.getTaskPeriods();
                foreach (int period in periods)
                {
                    _hyperperiod = (int) Extensions.LeastCommonMultiple(_hyperperiod, period);
                }
                
            }

            return _hyperperiod;
        }

        private List<Application_model.Task> getTasks(Application_model _apps, Application_model.TaskGraph _app)
        {
            List<Application_model.Task> Tasks = new List<Application_model.Task>();
            foreach (Application_model.Runnable runnable in _app.Runnables)
            {
                Tasks.Add(_apps.Tasks.Find(x => (x.Id == runnable.Id)));

            }

            return Tasks;
        }

        public Solution Clone()
        {
            return new Solution(Fcp, Apps);
        }
    }
}
