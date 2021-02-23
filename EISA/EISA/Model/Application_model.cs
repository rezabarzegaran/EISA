using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EISA.Model
{
    [XmlRoot("Application")]
    public class Application_model
    {
        [XmlElement("Task")]
        public List<Task> Tasks { get; set; }

        [XmlElement("DAG")]
        public List<TaskGraph> Apps { get; set; }

        [XmlElement("Message")]
        public List<Flow> Flows { get; set; }

        public class Task
        {
            public Task()
            {
                MaxJitter = -1;
            }

            [XmlAttribute("Name")]
            public string Name { get; set; }

            [XmlAttribute("WCET")]
            public int WCET { get; set; }

            [XmlAttribute("Period")]
            public int Period { get; set; }

            [XmlAttribute("Deadline")]
            public int Deadline { get; set; }

            [XmlAttribute("MaxJitter")]
            public int MaxJitter { get; set; }

            [XmlAttribute("CIL")]
            public int Cil { get; set; }

            [XmlAttribute("CpuName")]
            public string CpuName { get; set; }

        }

        public class TaskGraph
        {
            [XmlAttribute("Inorder")]
            public bool Order { get; set; }

            [XmlAttribute("CA")]
            public bool CA { get; set; }

            [XmlAttribute("Name")]
            public string Name { get; set; }

            [XmlElement("Runnable")]
            public List<Runnable> Runnables { get; set; }
        }

        public class Flow
        {
            public Flow()
            {
            }

            [XmlAttribute("Name")]
            public string Name { get; set; }

            [XmlAttribute("Size")]
            public int Size { get; set; }

            [XmlAttribute("Period")]
            public int Period { get; set; }

            [XmlAttribute("Deadline")]
            public int Deadline { get; set; }

            [XmlAttribute("Priority")]
            public int Priority { get; set; }

        }

        public class Runnable
        {
            [XmlAttribute("Name")]
            public string Name { get; set; }

            [XmlAttribute("Type")]
            public string Type { get; set; }
        }
    }
}
