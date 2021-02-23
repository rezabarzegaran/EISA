using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using EISA.Model;
using Google.OrTools.ConstraintSolver;
using IntVar = Google.OrTools.ConstraintSolver.IntVar;

namespace EISA.Base
{
    public class OptiSolver
    {
        //Variables
        private Solution Problem;
        Solver solver = new Solver("EISA");
        private Dictionary<Flow, Dictionary<string, Dictionary<int, IntVar>>> FlowIntVarMap;
        private Dictionary<Task, Dictionary<int, IntVar>> TaskIntVarMap;
        private Dictionary<Task, IntVar> TaskCoreMap;
        private DecisionBuilder db;
        private const int Choose_Random = 3;
        private const int Choose_first_unbound = 2;
        private const int Assign_random = 4;
        private const int Assign_min_val = 2;

        //Constructor
        public OptiSolver(Solution _problem)
        {
            Problem = _problem.Clone();
            FlowIntVarMap = new Dictionary<Flow, Dictionary<string, Dictionary<int, IntVar>>>();
            TaskIntVarMap = new Dictionary<Task, Dictionary<int, IntVar>>();
            TaskCoreMap = new Dictionary<Task, IntVar>();
        }

        //Internal Function
        public void Init()
        {
            Initialize();
            AddConstraints();
            AddCosts();
            AddDecision();
            AddLimits();
        }

        public List<Result> Run()
        {
            List<Result> FoundSolutions = new List<Result>();
            DateTime StartDate = DateTime.Now;
            while (solver.NextSolution())
            {
                FoundSolutions.Add(new Result(Problem, FlowIntVarMap));
                DateTime SolutionDate = DateTime.Now;
                long SolutionDur = (SolutionDate - StartDate).Milliseconds;
                Console.WriteLine("Solution found in " + SolutionDur.ToString());
            }



            // Statistics
            Console.WriteLine("Solutions: " + solver.Solutions().ToString());
            Console.WriteLine("Failures: " + solver.Failures().ToString());
            Console.WriteLine("Branches: " + solver.Branches().ToString());
            Console.WriteLine("Wall time: " + solver.WallTime().ToString());

            return FoundSolutions;

        }

        //Solver Functions
        private void Initialize()
        {
            SetFlowVariables();
            SetTaskVariables();
        }
        private void SetFlowVariables()
        {
            foreach (Flow _flow in Problem.getAllFlows())
            {
                Dictionary<string, Dictionary<int, IntVar>> N2I = new Dictionary<string, Dictionary<int, IntVar>>();
                foreach (string node in Problem.RouteMap[_flow.Name])
                {

                    if (Problem.RouteMap[_flow.Name].IndexOf(node) < (Problem.RouteMap[_flow.Name].Count - 1))
                    {
                        Dictionary<int, IntVar> I2Int = new Dictionary<int, IntVar>();

                        for (int i = 0; i < _flow.N_Instances; i++)
                        {
                            IntVar offset = solver.MakeIntVar(0, (Problem.Hyperperiod / Problem.FlowMicrotick), (_flow.Name + node + i.ToString()));
                            I2Int.Add(i, offset);
                        }
                        N2I.Add(node, I2Int);
                    }

                }
                FlowIntVarMap.Add(_flow, N2I);
            }
        }
        private void SetTaskVariables()
        {
            foreach (Task _task in Problem.getAllTasks())
            {
                Dictionary<int, IntVar> I2Int = new Dictionary<int, IntVar>();
                for (int i = 0; i < _task.N_Instances; i++)
                {
                    IntVar offset = solver.MakeIntVar(0, (_task.Period - _task.WCET), (_task.Name + i.ToString()));
                    I2Int.Add(i, offset);
                }
                if (!TaskIntVarMap.ContainsKey(_task))
                {
                    TaskIntVarMap.Add(_task, I2Int);

                }

                FogNode FN = Problem.Fcp.FNs.ToList().Find(x => x.Name == _task.FN_Name);
                if (FN != null)
                {
                    IntVar CoreMapping = solver.MakeIntVar(0, (FN.Cores.Length - 1), _task.Name);
                    if (!TaskCoreMap.ContainsKey(_task))
                    {
                        TaskCoreMap.Add(_task, CoreMapping);

                    }

                }
                


            }

        }
        private void AddConstraints()
        {
            AddFlowConstraints();
            AddTaskConstraints();
            AddJointTaskFlowConstraints();
        }
        private void AddCosts()
        {

        }
        private void AddLimits()
        {
            int hours = 0;
            int minutes = 1;
            int dur = (hours * 3600 + minutes * 60) * 1000;
            var Timelimit = solver.MakeTimeLimit(dur);
            var SolutionLimit = solver.MakeSolutionsLimit(2);
            SearchMonitor[] searchVar = new SearchMonitor[2];
            //searchVar[0] = costVar;
            //Other Limits
            searchVar[0] = Timelimit;
            searchVar[1] = SolutionLimit;
            //searchVar[2] = solver.makeConstantRestart(500);
            solver.NewSearch(getDecision(), searchVar);
            Console.WriteLine("Initiated");

        }

        // Flow Constraints
        private void AddFlowConstraints()
        {
            LinkNoOverlap();
            FlowRouting();
            FlowDeadline();
            FlowIsolation();
            FlowControlPrecedence();
        }
        private void LinkNoOverlap()
        {
            foreach (KeyValuePair<Flow, Dictionary<string, Dictionary<int, IntVar>>> k in FlowIntVarMap)
            {
                var kSW2Int = k.Value;
                foreach (KeyValuePair<Flow, Dictionary<string, Dictionary<int, IntVar>>> v in FlowIntVarMap)
                {
                    if (!k.Key.Equals(v.Key))
                    {
                        var vSW2Int = v.Value;

                        foreach (KeyValuePair<string, Dictionary<int, IntVar>> kk in kSW2Int)
                        {
                            var kkInt2Var = kk.Value;

                            foreach (KeyValuePair<string, Dictionary<int, IntVar>> vv in vSW2Int)
                            {
                                if (kk.Key == vv.Key)
                                {
                                    var vvInt2Var = vv.Value;
                                    foreach (KeyValuePair<int, IntVar> kkk in kkInt2Var)
                                    {
                                        IntVar kOffset = (kkk.Value + (kkk.Key * k.Key.Period)).Var();

                                        foreach (KeyValuePair<int, IntVar> vvv in vvInt2Var)
                                        {
                                            IntVar vOffset = (vvv.Value + (vvv.Key * v.Key.Period)).Var();
                                            IntVar aVar = solver.MakeIsGreaterOrEqualVar(vOffset, (kOffset + k.Key.TransmitTime).Var());
                                            IntVar bVar = solver.MakeIsGreaterOrEqualVar(kOffset, (vOffset + v.Key.TransmitTime).Var());
                                            solver.Add((aVar + bVar) == 1);


                                        }
                                    }
                                }
                            }
                        }

                    }
                }
            }
        }
        private void FlowRouting()
        {
            foreach (KeyValuePair<Flow, Dictionary<string, Dictionary<int, IntVar>>> k in FlowIntVarMap)
            {
                var kSW2Int = k.Value;

                for (int i = 0; i < (kSW2Int.Count - 2); i++)
                {
                    var k1element = kSW2Int.ElementAt(i).Value;
                    var k2element = kSW2Int.ElementAt(i + 1).Value;

                    foreach (KeyValuePair<int, IntVar> kk in k1element)
                    {
                        solver.Add((kk.Value + k.Key.TransmitTime) <= k2element[kk.Key]);
                    }
                }
            }
        }
        private void FlowDeadline()
        {
            foreach (KeyValuePair<Flow, Dictionary<string, Dictionary<int, IntVar>>> k in FlowIntVarMap)
            {
                var kSW2Int = k.Value;
                var k1element = kSW2Int.ElementAt(kSW2Int.Count - 1).Value;

                foreach (KeyValuePair<int, IntVar> kk in k1element)
                {
                    solver.Add((kk.Value + k.Key.TransmitTime) <= k.Key.Deadline);
                }
            }
        }
        private void FlowIsolation()
        {
            foreach (Switch SW in Problem.Fcp.SWs)
            {
                foreach (KeyValuePair<Flow, Dictionary<string, Dictionary<int, IntVar>>> k in FlowIntVarMap)
                {
                    if (k.Value.ContainsKey(SW.Name))
                    {
                        foreach (KeyValuePair<Flow, Dictionary<string, Dictionary<int, IntVar>>> v in FlowIntVarMap)
                        {
                            if (v.Value.ContainsKey(SW.Name) && (!k.Key.Equals(v.Key)) && (k.Key.Priority == v.Key.Priority))
                            {
                                var kPreInt2Var = k.Value.ElementAt(k.Value.Keys.ToList().IndexOf(SW.Name) - 1).Value;
                                var vPreInt2Var = v.Value.ElementAt(v.Value.Keys.ToList().IndexOf(SW.Name) - 1).Value;

                                foreach (KeyValuePair<int, IntVar> kk in kPreInt2Var)
                                {
                                    IntVar kOffset = (kk.Value + (kk.Key * k.Key.Period)).Var();

                                    foreach (KeyValuePair<int, IntVar> vv in vPreInt2Var)
                                    {
                                        IntVar vOffset = (vv.Value + (vv.Key * v.Key.Period)).Var();
                                        IntVar aVar = solver.MakeIsGreaterOrEqualVar(vOffset, (kOffset + k.Key.TransmitTime).Var());
                                        IntVar bVar = solver.MakeIsGreaterOrEqualVar(kOffset, (vOffset + v.Key.TransmitTime).Var());

                                        solver.Add((aVar + bVar) == 1);


                                    }
                                }

                            }
                        }
                    }
                }
            }
        }
        private void FlowControlPrecedence()
        {
            foreach (Applications _app in Problem.Apps.FindAll(x => x.CA == true))
            {
                foreach (Flow k in _app.Flows.FindAll(x => x.Type == Flow.Flow_Type.Input))
                {
                    var kk = FlowIntVarMap[k].ElementAt(FlowIntVarMap[k].Keys.Count - 1).Value;
                    foreach (Flow v in _app.Flows.FindAll(x => x.Type == Flow.Flow_Type.Output))
                    {
                        var vv = FlowIntVarMap[v].ElementAt(0).Value;
                        foreach (KeyValuePair<int, IntVar> kkk in kk)
                        {
                            solver.Add(kkk.Value <= (vv[kkk.Key] + v.TransmitTime));
                        }
                    }
                }
            }
        }
        // Task Constraints
        private void AddTaskConstraints()
        {
            CoreUtilization();
            TasksinOrderedApps();
            NoTaskOverlaps();
            TaskDeadline();
        }
        private void CoreUtilization()
        {
            foreach (FogNode fn in Problem.Fcp.FNs)
            {
                foreach (Core core in fn.Cores)
                {
                    IntVar utilization = null;
                    foreach (KeyValuePair<Task, IntVar> k in TaskCoreMap)
                    {
                        if (k.Key.FN_Name == fn.Name)
                        {
                            long workload = (long) k.Key.Workload * 100;
                            if (utilization == null)
                            {
                                utilization = ((k.Value == core.Id).Var() * workload).Var();
                            }
                            else
                            {
                                utilization = (utilization + ((k.Value == core.Id).Var() * workload).Var()).Var();
                            }
                        }
                    }

                    solver.Add(utilization < 100);

                }
            }
        }
        private void TasksinOrderedApps()
        {
            foreach (Applications _app in Problem.Apps.FindAll(x => x.CA == true))
            {
                for (int i = 0; i < (_app.Tasks.Count - 1); i++)
                {
                    Task first = _app.Tasks[i];
                    var firstN2Var = TaskIntVarMap[first];
                    for (int k = 0; k < first.N_Instances; k++)
                    {
                        IntVar firstOffset = firstN2Var[k];
                        for (int j = (i + 1); j < _app.Tasks.Count; j++)
                        {
                            Task second = _app.Tasks[j];
                            var secondN2Var = TaskIntVarMap[second];
                            for (int l = 0; l < second.N_Instances; l++)
                            {
                                IntVar secondOffset = secondN2Var[l];
                                solver.Add((firstOffset + first.WCET) <=(secondOffset));
                            }

                        }

                    }

                }   
            }
        }
        private void NoTaskOverlaps()
        {
            foreach (KeyValuePair<Task, Dictionary<int, IntVar>> k in TaskIntVarMap)
            {
                foreach (KeyValuePair<Task, Dictionary<int, IntVar>> v in TaskIntVarMap)
                {
                    if (!k.Key.Equals(v.Key))
                    {
                        IntVar kCore = TaskCoreMap[k.Key];
                        IntVar vCore = TaskCoreMap[v.Key];
                        IntVar CoreVar = (kCore != vCore).Var();


                        foreach (KeyValuePair<int, IntVar> kk in k.Value)
                        {
                            IntVar kOffset = (kk.Value + (kk.Key * k.Key.Period)).Var();
                            foreach (KeyValuePair<int, IntVar> vv in v.Value)
                            {
                                IntVar vOffset = (vv.Value + (vv.Key * v.Key.Period)).Var();
                                IntVar aVar = solver.MakeIsGreaterOrEqualVar(vOffset,(kOffset + k.Key.WCET).Var());
                                IntVar bVar = solver.MakeIsGreaterOrEqualVar(kOffset,(vOffset + v.Key.WCET).Var());
                                IntVar cVar = ((aVar + bVar) == 1).Var();
                                solver.Add((cVar + CoreVar)>=1);
                            }
                        }
                    }
                }
            }

        }
        private void TaskDeadline()
        {
            foreach (Applications _app in Problem.Apps)
            {
                foreach (Task _task in _app.Tasks)
                {
                    var N2Var = TaskIntVarMap[_task];
                    for (int i = 0; i < _task.N_Instances; i++)
                    {
                        IntVar termination = (N2Var[i] + _task.WCET).Var();
                        solver.Add(termination <= _task.Deadline);

                    }

                }
            }
        }

        // Joint Task and Flow Contraints
        private void AddJointTaskFlowConstraints()
        {

        }

        // Const Functions


        //Decision Vriables
        private void AddDecision()
        {
            List<IntVar> TaskMappingVars = new List<IntVar>();
            List<IntVar> TaskOffsets = new List<IntVar>();
            List<IntVar> FlowOffsets = new List<IntVar>();

            foreach (KeyValuePair<Task, IntVar> k in TaskCoreMap)
            {
                TaskMappingVars.Add(k.Value);
            }

            foreach (KeyValuePair<Task, Dictionary<int, IntVar>> k in TaskIntVarMap)
            {
                foreach (KeyValuePair<int, IntVar> v in k.Value)
                {
                    TaskOffsets.Add(v.Value);
                }
            }

            foreach (KeyValuePair<Flow, Dictionary<string, Dictionary<int, IntVar>>> k in FlowIntVarMap)
            {
                foreach (KeyValuePair<string, Dictionary<int, IntVar>> kk in k.Value)
                {
                    foreach (KeyValuePair<int, IntVar> kkk in kk.Value)
                    {
                        FlowOffsets.Add(kkk.Value);
                    }
                }
            }

            DecisionBuilder db1 = solver.MakePhase(TaskMappingVars.ToArray(), 3, 4);
            DecisionBuilder db2 = solver.MakePhase(TaskOffsets.ToArray(), 3, 4);
            DecisionBuilder db3 = solver.MakePhase(FlowOffsets.ToArray(), 3, 4);

            db = solver.Compose(db1, db2, db3);
        }
        private DecisionBuilder getDecision()
        {
            return db;
        }





    }
}
