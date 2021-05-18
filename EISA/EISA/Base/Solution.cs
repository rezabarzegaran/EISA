using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlTypes;
using System.Text;
using EISA.Extension;
using EISA.Model;

namespace EISA.Base
{
    public class Solution
    {
        //Variabels
        public FCP Fcp { get; }
        public List<Applications> Apps { get; }
        public int Hyperperiod { get; private set; }
        public int TaskMicrotick { get; private set; }
        public int FlowMicrotick { get; private set; }
        public Dictionary<Task, Core> TaskMap;
        public Dictionary<string, List<string>> RouteMap;

        //Constructor
        public Solution(Application_model _apps, Architecture_model _arch)
        {
            Fcp = new FCP(_arch);
            Apps = new List<Applications>();
            foreach (Application_model.TaskGraph _app in _apps.Apps)
            {
                Apps.Add(new Applications(_app, getTasks(_apps,_app), getInputFlows(_apps, _app), getOutputFlows(_apps, _app)));
            }
            TaskMap = new Dictionary<Task, Core>();
            RouteMap = new Dictionary<string, List<string>>();
            foreach (Applications _app in Apps)
            {
                foreach (Flow _flow in _app.Flows)
                {
                    List<string> _nodes = getRoute(_arch, _flow);
                    if (_nodes != null)
                    {
                        RouteMap.Add(_flow.Name, _nodes);
                    }
                }
            }
            Initialize();
        }

        public Solution(FCP _fcp, List<Applications> _apps, Dictionary<Task, Core> _taskmap, Dictionary<string, List<string>> _routemap)
        {
            Fcp = _fcp.Clone();
            Apps = new List<Applications>();
            foreach (Applications _app in _apps)
            {
                Apps.Add(_app.Clone());
            }
            TaskMap = new Dictionary<Task, Core>();
            foreach (KeyValuePair<Task, Core> k in _taskmap)
            {
                TaskMap.Add(k.Key, k.Value);
            }
            RouteMap = new Dictionary<string, List<string>>();

            foreach (KeyValuePair<string, List<string>> k in _routemap)
            {
                RouteMap.Add(k.Key, k.Value);
            }
            Initialize();
        }

        //Internal Functions
        private void Initialize()
        {
            Hyperperiod = GetHyperperiod();
            SetRel(getGCDtasks(), getGCDflows());
            Apps.ForEach(x => x.Init(Hyperperiod));
            SortApps();
        }
        public int GetHyperperiod()
        {
            int _hyperperiod = 1;

            foreach ( Applications App  in Apps)
            {
                List<int> taskperiods = App.getTaskPeriods();
                List<int> flowperiods = App.getFlowPeriods();
                foreach (int period in taskperiods)
                {
                    _hyperperiod = (int) Extensions.LeastCommonMultiple(_hyperperiod, period);
                }
                foreach (int period in flowperiods)
                {
                    _hyperperiod = (int)Extensions.LeastCommonMultiple(_hyperperiod, period);
                }

            }
            return _hyperperiod;
        }
        private int getGCDflows()
        {
            int _gcd = -1;
            foreach (Applications _app in Apps)
            {
                foreach (Flow _flow in _app.Flows)
                {
                    if (_gcd == -1)
                    {
                        _gcd = _flow.Size;
                    }
                    else
                    {
                        _gcd = (int)Extensions.GreatestCommonFactor(_gcd, _flow.Size);

                    }
                }
            }

            return _gcd;
        }
        private int getGCDtasks()
        {
            int _gcd = -1;
            foreach (Applications _app in Apps)
            {
                foreach (Task _task in _app.Tasks)
                {
                    if (_gcd == -1)
                    {
                        _gcd = _task.WCET;
                    }
                    else
                    {
                        _gcd = (int)Extensions.GreatestCommonFactor(_gcd, _task.WCET);

                    }
                }
            }
            return _gcd;
        }
        private void SetRel(int task_gcd, int flow_gcd)
        {


            TaskMicrotick = task_gcd;
            FlowMicrotick = flow_gcd;
        }
        private void SortApps()
        {
            Apps.Sort((a, b) => (b.Cil.CompareTo(a.Cil)));
        }
        private List<Application_model.Task> getTasks(Application_model _apps, Application_model.TaskGraph _app)
        {
            List<Application_model.Task> Tasks = new List<Application_model.Task>();
            foreach (Application_model.Runnable runnable in _app.Runnables)
            {
                Application_model.Task Selected_Task = _apps.Tasks.Find(x => (x.Name == runnable.Name));
                if(Selected_Task != null)
                {
                    Tasks.Add(Selected_Task);
                }
                

            }
            return Tasks;
        }
        private List<Application_model.Flow> getInputFlows(Application_model _apps, Application_model.TaskGraph _app)
        {
            List<Application_model.Flow> Flows = new List<Application_model.Flow>();
            foreach (Application_model.Runnable runnable in _app.Runnables)
            {
                Application_model.Flow Selected_Flow = _apps.Flows.Find(x => (x.Name == runnable.Name));
                if ((Selected_Flow != null) && (runnable.Type == "Input"))
                {
                    Flows.Add(Selected_Flow);
                }

            }
            return Flows;
        }
        private List<Application_model.Flow> getOutputFlows(Application_model _apps, Application_model.TaskGraph _app)
        {
            List<Application_model.Flow> Flows = new List<Application_model.Flow>();
            foreach (Application_model.Runnable runnable in _app.Runnables)
            {
                Application_model.Flow Selected_Flow = _apps.Flows.Find(x => (x.Name == runnable.Name));
                if ((Selected_Flow != null)&&(runnable.Type == "Output"))
                {
                    Flows.Add(Selected_Flow);
                }

            }
            return Flows;
        }
        private List<string> getRoute(Architecture_model _arch, Flow _flow)
        {
            List<string> nodes = new List<string>();
            Architecture_model.Route SelectedRoute = _arch.Routes.Find(x => x.Flows.Contains(_flow.Name));

            if (SelectedRoute != null)
            {
                foreach (string _node in SelectedRoute.Nodes)
                {
                    nodes.Add(_node);
                }
            }
            return nodes;
        }
        public List<Flow> getAllFlows()
        {
            List<Flow> flows = new List<Flow>();
            foreach (Applications _app in Apps)
            {
                foreach (Flow _flow in _app.Flows)
                {
                    if (!flows.Contains(_flow))
                    {
                        flows.Add(_flow);
                    }
                }
            }

            return flows;
        }
        public List<Task> getAllTasks()
        {
            List<Task> tasks = new List<Task>();
            foreach (Applications _app in Apps)
            {
                foreach (Task _task in _app.Tasks)
                {
                    if (!tasks.Contains(_task))
                    {
                        tasks.Add(_task);
                    }
                }
            }
            return tasks;
        }

        public List<string> getLinks()
        {
            List<string> links = new List<string>();
            foreach (KeyValuePair<string, List<string>> k in RouteMap)
            {
                for (int i = 0; i < (k.Value.Count - 1); i++)
                {
                    string link = k.Value[i] + ":" + k.Value[i + 1];
                    if (!links.Contains(link))
                    {
                        links.Add(link);
                    }
                }
            }

            return links;
        }

        public List<string> getCores()
        {
            List<string> cores = new List<string>();
            foreach (var Fn in Fcp.FNs)
            {
                foreach (Core core in Fn.Cores)
                {
                    string _core = Fn.Name + ":" + core.Id.ToString();
                    if (!cores.Contains(_core))
                    {
                        cores.Add(_core);
                    }
                }
            }

            return cores;
        }

        //Clone Function
        public Solution Clone()
        {
            return new Solution(Fcp, Apps, TaskMap, RouteMap);
        }
    }
}
