using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EISA.Base;

namespace EISA.Interface
{
    public class Unloader
    {
        private SVGDumper svg;
        public Unloader()
        {
            svg = new SVGDumper();
        }

        public void Save(List<Result> _results)
        {

            int counter = 0;
            foreach (Result _result in _results)
            {
                string Filename = "Result_" + counter.ToString() + ".svg";
                svg.Save(_result, Filename);
                counter++;
            }
        }
    }
}
