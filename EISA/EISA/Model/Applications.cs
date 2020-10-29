using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace EISA.Model
{
    public class Applications
    {
        public string Name { get; }
        public bool InOrder { get; }
        public bool CA { get; }
        public List<Task> Tasks { get; }
        public int Cil => Tasks.First().Cil;
        public double Workload => getWorkload();
        public Applications(Application_model.TaskGraph _apps, List<Application_model.Task> _tasks)
        {
            Name = _apps.Name;
            InOrder = _apps.Order;
            CA = _apps.CA;
            Tasks = new List<Task>();
            foreach (Application_model.Task _task in _tasks)
            {
                Tasks.Add(new Task(_task));
            }

        }

        public Applications(string _name, bool _order, bool _ca, List<Task> _tasks)
        {
            Name = _name;
            InOrder = _order;
            CA = _ca;
            Tasks = new List<Task>();
            foreach (Task _task in _tasks)
            {
                Tasks.Add(_task.Clone());
            }
        }

        private double getWorkload()
        {
            double _workload = 0;
            foreach (Task _task in Tasks)
            {
                _workload += _task.Workload;
            }

            return _workload;
        }

        public List<int> getTaskPeriods()
        {
            List<int> periods = new List<int>();
            foreach (Task Task in Tasks)
            {
                periods.Add(Task.Period);
            }

            return periods;
        }

        public Applications Clone()
        {
            return new Applications(Name, InOrder, CA, Tasks);
        }
    }
}
