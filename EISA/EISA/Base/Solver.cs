using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EISA.Model;

namespace EISA.Base
{
    public class Solver
    {
        private Solution Problem;
        public Solver(Solution _problem)
        {
            Problem = _problem.Clone();
        }

        public void Run()
        {
            InitialMapping();
            Initialize();

        }

        private void InitialMapping()
        {
            foreach (Applications App in Problem.Apps)
            {
                FogNode LowUtilFN = Problem.Fcp.FNs.ToList().OrderBy(x => x.Utilization).First();
                foreach (var Task in App.Tasks)
                {
                    Core LowUtilCore = LowUtilFN.Cores.ToList().OrderBy(x => x.Utilization).First();
                    if (LowUtilCore.isSchedulable(Task))
                    {   
                        Problem.TaskMap.Add(Task,LowUtilCore);
                        LowUtilCore.AssignTask(Task);
                    }
                    else
                        throw new System.InvalidOperationException("Unschedulabe");
                }
            }
        }

        private void Initialize()
        {

        }

    }
}
