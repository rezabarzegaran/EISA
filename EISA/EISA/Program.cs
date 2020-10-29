using EISA.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EISA.Base;
using EISA.Model;

namespace EISA
{
    class Program
    {
        static void Main(string[] args)
        {
            string appsInput = @"Data\Tasks.xml";
            string archInput = @"Data\Config.xml";
            (Application_model apps, Architecture_model arch) scheme = DataLoader.Load(appsInput, archInput);
            Solution Problem = new Solution(scheme.apps, scheme.arch);
            Solver solver = new Solver(Problem);
            solver.Run();

        }
    }
}
