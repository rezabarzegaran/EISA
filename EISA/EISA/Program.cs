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
            // Input Data
            string appsInput = @"Data\Tasks.xml";
            string archInput = @"Data\Config.xml";
            List<Result> Results;

            // Data Loader
            (Application_model apps, Architecture_model arch) scheme = DataLoader.Load(appsInput, archInput);
            Solution Problem = new Solution(scheme.apps, scheme.arch);
            OptiSolver solver = new OptiSolver(Problem);

            //Running
            solver.Init();
            Results = solver.Run();

            //Data Unloader
            Unloader dataUnloader = new Unloader();
            dataUnloader.Save(Results);

            //End Key
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

        }
    }
}
