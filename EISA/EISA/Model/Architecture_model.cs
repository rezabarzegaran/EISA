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

            [XmlAttribute("Name")]
            public string Name { get; set; }

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

        [XmlElement("SW")]
        public List<SW> SWs { get; set; }

        public class SW
        {
            public SW()
            {
            }

            [XmlAttribute("Name")]
            public string Name { get; set; }

        }

        [XmlElement("ES")]
        public List<ES> ESs { get; set; }

        public class ES
        {
            public ES()
            {
            }

            [XmlAttribute("Name")]
            public string Name { get; set; }

        }

        [XmlElement("Route")]
        public List<Route> Routes { get; set; }

        public class Route
        {
            public Route()
            {
            }

            [XmlAttribute("Id")]
            public int Id { get; set; }

            [XmlElement("node")]
            public List<string> Nodes { get; set; }

            [XmlElement("flow")]
            public List<string> Flows { get; set; }

        }
    }
}
