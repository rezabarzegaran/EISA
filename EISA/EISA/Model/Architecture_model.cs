using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EISA.Model
{
    [XmlRoot("Architecture")]
    public class Architecture_model
    {
        [XmlElement("Cpu")]
        public List<Cpu> Cpus { get; set; }

        public class Cpu
        {
            public Cpu()
            {
            }

            [XmlAttribute("Id")]
            public int Id { get; set; }

            [XmlElement("Core")]
            public List<Core> Cores { get; set; }

            public class Core
            {
                public Core()
                {
                }

                [XmlAttribute("Id")]
                public int Id { get; set; }

                [XmlAttribute("MacroTick")]
                public int MacroTick { get; set; }

                [XmlAttribute("CF")]
                public double CF { get; set; }
            }
        }
    }
}
