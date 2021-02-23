using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EISA.Model
{
    public class ES
    {
        public string Name { get; }

        public ES(string _name)
        {
            Name = _name;
        }

        public ES Clone()
        {
            return new ES(Name);
        }
    }
}
