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
        public int Parent_Id { get; }
        public int Macrotick { get; }
        public double Utilization { get; }
        public Core(int _id, int _parent_id, int _macrotick)
        {
            Id = _id;
            Parent_Id = _parent_id;
            Macrotick = _macrotick;
            Utilization = 0;
        }

        public Core(int _id, int _parent_id, int _macrotick, bool a)
        {
            Id = _id;
            Parent_Id = _parent_id;
            Macrotick = _macrotick;
            Utilization = 0;
        }

        public Core Clone()
        {
            return new Core(Id, Parent_Id, Macrotick, true);
        }
    }
}
