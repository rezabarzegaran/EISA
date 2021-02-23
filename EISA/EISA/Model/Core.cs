using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EISA.Model
{
    public class Core
    {
        public int Id { get; }
        public string Parent_Name { get; }
        public int Macrotick { get; }
        public double Utilization { get; set; }
        public Core(int _id, string _parent_name, int _macrotick)
        {
            Id = _id;
            Parent_Name = _parent_name;
            Macrotick = _macrotick;
            Utilization = 0;
        }

        public Core(int _id, string _parent_name, int _macrotick, bool a)
        {
            Id = _id;
            Parent_Name = _parent_name;
            Macrotick = _macrotick;
            Utilization = 0;
        }

        public bool isSchedulable(Task t)
        {
            if ((Utilization + t.Workload) <= 1)
                return true;
            return false;
        }

        public void AssignTask(Task t)
        {
            Utilization += t.Workload;
        }

        public Core Clone()
        {
            return new Core(Id, Parent_Name, Macrotick, true);
        }

    }
}
