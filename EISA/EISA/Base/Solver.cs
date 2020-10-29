using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        }

        private void InitialMapping()
        {

        }

    }
}
