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
        public List<Flow> Flows { get; }
        public int Cil => Tasks.First().Cil;
        public double Workload => getWorkload();
        public Applications(Application_model.TaskGraph _apps, List<Application_model.Task> _tasks, List<Application_model.Flow> _In_flows, List<Application_model.Flow> _Out_flows)
        {
            Name = _apps.Name;
            InOrder = _apps.Order;
            CA = _apps.CA;
            Tasks = new List<Task>();
            foreach (Application_model.Task _task in _tasks)
            {
                Tasks.Add(new Task(_task));
            }
            Flows = new List<Flow>();
            foreach (Application_model.Flow _flow in _In_flows)
            {
                Flows.Add(new Flow(_flow, Flow.Flow_Type.Input)) ;
            }
            foreach (Application_model.Flow _flow in _Out_flows)
            {
                Flows.Add(new Flow(_flow, Flow.Flow_Type.Output));
            }

        }

        public Applications(string _name, bool _order, bool _ca, List<Task> _tasks, List<Flow> _flows)
        {
            Name = _name;
            InOrder = _order;
            CA = _ca;
            Tasks = new List<Task>();
            foreach (Task _task in _tasks)
            {
                Tasks.Add(_task.Clone());
            }
            Flows = new List<Flow>();
            foreach (Flow _flow in _flows)
            {
                Flows.Add(_flow.Clone());
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
        public List<int> getFlowPeriods()
        {
            List<int> periods = new List<int>();
            foreach (Flow _flow in Flows)
            {
                periods.Add(_flow.Period);
            }

            return periods;
        }

        public void Init(int val)
        {
            Flows.ForEach(x => x.Init(val));
            Tasks.ForEach(x => x.Init(val));
        }


        public Applications Clone()
        {
            return new Applications(Name, InOrder, CA, Tasks, Flows);
        }
    }
}
