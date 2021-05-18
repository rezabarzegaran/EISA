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
        private List<IntVar> Costs;
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
            Costs = new List<IntVar>();
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
                FoundSolutions.Add(new Result(Problem, FlowIntVarMap, TaskIntVarMap, TaskCoreMap));
                DateTime SolutionDate = DateTime.Now;
                long SolutionDur = (SolutionDate - StartDate).Milliseconds;
                Console.WriteLine("Solution found in " + SolutionDur.ToString() + ", with total cost= " + Costs[0].Value() + " , QoC= " + Costs[4].Value() + " , FrameEx = " + Costs[5].Value() + " , JobEx = " + Costs[6].Value());
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
            IntVar cost1 = FrameJitter();
            IntVar cost2 = JobJitter();
            IntVar cost3 = FlowE2E();
            IntVar cost4 = QoC();
            IntVar cost5 = FrameExtensibility();
            IntVar cost6 = JobExtensibility();
            IntVar FinalCost = (cost4 + cost5 + cost6).Var();
            //IntVar FinalCost = cost6;

            Costs.Add(FinalCost);
            Costs.Add(cost1);
            Costs.Add(cost2);
            Costs.Add(cost3);
            Costs.Add(cost4);
            Costs.Add(cost5);
            Costs.Add(cost6);


        }
        private void AddLimits()
        {
            int hours = 0;
            int minutes = 5;
            int dur = (hours * 3600 + minutes * 60) * 1000;
            var Timelimit = solver.MakeTimeLimit(dur);
            var SolutionLimit = solver.MakeSolutionsLimit(10);
            SearchMonitor[] searchVar = new SearchMonitor[3];
            //searchVar[0] = costVar;
            //Other Limits
            searchVar[0] = Timelimit;
            searchVar[1] = SolutionLimit;
            searchVar[2] = solver.MakeMinimize(Costs[0],1);
            //searchVar[3] = solver.MakeConstantRestart(500);
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

                for (int i = 0; i < (kSW2Int.Keys.Count - 1); i++)
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
                var k1element = kSW2Int.ElementAt(kSW2Int.Keys.Count - 1).Value;

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
                            solver.Add((kkk.Value + k.TransmitTime) <= vv[kkk.Key]);
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
            AllInputsArrived();
            AllTasksExecuted();
        }

        private void AllInputsArrived()
        {
            foreach (Applications _app in Problem.Apps.FindAll(x => x.CA == true))
            {
                foreach (Flow k in _app.Flows.FindAll(x => x.Type == Flow.Flow_Type.Input))
                {
                    var kFrame = FlowIntVarMap[k].ElementAt(FlowIntVarMap[k].Keys.Count - 1).Value;

                    foreach (KeyValuePair<int,IntVar> kkFrame in kFrame)
                    {
                        foreach (Task _task in _app.Tasks)
                        {
                            IntVar taskStart = (TaskIntVarMap[_task].ElementAt(kkFrame.Key).Value +
                                              kkFrame.Key * _task.Period).Var();
                            IntVar flowEnd = (kkFrame.Value + kkFrame.Key * k.Period + k.TransmitTime).Var();

                            solver.Add(flowEnd<= taskStart);
                        }
                    }
                }
            }

        }

        private void AllTasksExecuted()
        {
            foreach (Applications _app in Problem.Apps.FindAll(x => x.CA == true))
            {
                foreach (Flow k in _app.Flows.FindAll(x => x.Type == Flow.Flow_Type.Output))
                {
                    var kFrame = FlowIntVarMap[k].ElementAt(0).Value;

                    foreach (KeyValuePair<int, IntVar> kkFrame in kFrame)
                    {
                        foreach (Task _task in _app.Tasks)
                        {
                            IntVar taskEnd = (TaskIntVarMap[_task].ElementAt(kkFrame.Key).Value +
                                             kkFrame.Key * _task.Period + _task.WCET).Var();
                            IntVar flowStart = (kkFrame.Value + kkFrame.Key * k.Period).Var();
                            solver.Add(flowStart >= taskEnd);
                        }
                    }
                }
            }
        }

        // Const Functions
        private IntVar FrameJitter()
        {
            IntVar framestartjitter = null;
            IntVar frameendjitter = null;
            IntVar framejitter = null;

            foreach (KeyValuePair<Flow, Dictionary<string, Dictionary<int, IntVar>>> kFlow in FlowIntVarMap)
            {

                Dictionary<int, IntVar> kFrameStart = kFlow.Value.ElementAt(0).Value;
                Dictionary<int, IntVar> kFrameEnd = kFlow.Value.ElementAt(kFlow.Value.Keys.Count - 1).Value;

                IntVar SingleFrameStartJitter = null;

                for (int i = 0; i < kFrameStart.Keys.Count; i++)
                {
                    for (int j = 0; j < kFrameStart.Keys.Count; j++)
                    {
                        if (SingleFrameStartJitter != null)
                        {
                            SingleFrameStartJitter = (SingleFrameStartJitter + solver.MakeAbs(solver.MakeAbs(kFrameStart[i]).Var() -
                                solver.MakeAbs(kFrameStart[j]).Var()).Var()).Var();
                        }
                        else
                        {
                            SingleFrameStartJitter = solver.MakeAbs(solver.MakeAbs(kFrameStart[i]).Var() - solver.MakeAbs(kFrameStart[j]).Var()).Var();
                        }
                    }
                }

                if (framestartjitter != null)
                {
                    framestartjitter = (framestartjitter + (SingleFrameStartJitter / (kFrameStart.Keys.Count ^ 2)).Var()).Var();
                }
                else
                {
                    framestartjitter = (SingleFrameStartJitter / (kFrameStart.Keys.Count ^ 2)).Var();
                }



                IntVar SingleFrameEndJitter = null;

                for (int i = 0; i < kFrameEnd.Keys.Count; i++)
                {
                    for (int j = 0; j < kFrameEnd.Keys.Count; j++)
                    {
                        if (SingleFrameEndJitter != null)
                        {
                            SingleFrameEndJitter = (SingleFrameEndJitter + solver.MakeAbs(solver.MakeAbs(kFrameEnd[i]).Var() -
                                                                              solver.MakeAbs(kFrameEnd[j]).Var()).Var()).Var();
                        }
                        else
                        {
                            SingleFrameEndJitter = solver.MakeAbs(solver.MakeAbs(kFrameEnd[i]).Var() - solver.MakeAbs(kFrameEnd[j]).Var()).Var();
                        }
                    }
                }

                if (frameendjitter != null)
                {
                    frameendjitter = (frameendjitter + (SingleFrameEndJitter / (kFrameEnd.Keys.Count ^ 2)).Var()).Var();
                }
                else
                {
                    frameendjitter = (SingleFrameEndJitter / (kFrameEnd.Keys.Count ^ 2)).Var();
                }

            }

            framestartjitter = (framestartjitter / FlowIntVarMap.Keys.Count).Var();
            frameendjitter = (frameendjitter / FlowIntVarMap.Keys.Count).Var();


            framejitter = (framestartjitter * 1 + frameendjitter * 1).Var();

            return framejitter;
        }

        private IntVar JobJitter()
        {
            IntVar jobjitter = null;

            foreach (KeyValuePair<Task,Dictionary<int, IntVar>> kTask in TaskIntVarMap)
            {
                IntVar SingleJobJitter = null;
                for (int i = 0; i < kTask.Value.Keys.Count; i++)
                {
                    for (int j = 0; j < kTask.Value.Keys.Count; j++)
                    {
                        if (SingleJobJitter != null)
                        {
                            SingleJobJitter = (SingleJobJitter + solver.MakeAbs(solver.MakeAbs(kTask.Value[i]).Var() -
                                                                          solver.MakeAbs(kTask.Value[j]).Var()).Var()).Var();
                        }
                        else
                        {
                            SingleJobJitter = solver.MakeAbs(solver.MakeAbs(kTask.Value[i]).Var() - solver.MakeAbs(kTask.Value[j]).Var()).Var();
                        }
                    }
                }

                if (jobjitter != null)
                {
                    jobjitter = (jobjitter + (SingleJobJitter / (kTask.Value.Keys.Count ^ 2)).Var()).Var();
                }
                else
                {
                    jobjitter = (SingleJobJitter / (kTask.Value.Keys.Count ^ 2)).Var();
                }

            }

            jobjitter = (jobjitter / TaskIntVarMap.Keys.Count).Var();

            return jobjitter;
        }

        private IntVar FlowE2E()
        {
            IntVar e2e = null;

            foreach (KeyValuePair<Flow, Dictionary<string, Dictionary<int, IntVar>>> kFlow in FlowIntVarMap)
            {
                Dictionary<int, IntVar> kFrameStart = kFlow.Value.ElementAt(0).Value;
                Dictionary<int, IntVar> kFrameEnd = kFlow.Value.ElementAt(kFlow.Value.Keys.Count - 1).Value;

                for (int i = 0; i < kFrameStart.Keys.Count; i++)
                {

                    if (e2e != null)
                    {
                        e2e = (e2e + solver.MakeAbs(solver.MakeAbs(kFrameEnd[i]).Var() -
                                                    solver.MakeAbs(kFrameStart[i]).Var()).Var()).Var();
                    }
                    else
                    {
                        e2e = solver.MakeAbs(solver.MakeAbs(kFrameEnd[i]).Var() - solver.MakeAbs(kFrameStart[i]).Var()).Var();
                    }
                }
            }

            e2e = (e2e / FlowIntVarMap.Keys.Count).Var();

            return e2e;
        }

        private IntVar QoC()
        {
            IntVar sensorJitter = null;
            IntVar sensorDelay = null;
            IntVar actuatorJitter = null;
            IntVar actuatorDelay = null;
            IntVar slack = null;
            IntVar qoc = null;
            foreach (Applications _app in Problem.Apps.FindAll(x => x.CA == true))
            {
                foreach (Flow k in _app.Flows.FindAll(x => x.Type == Flow.Flow_Type.Input))
                {
                    var kSensorStartFrame = FlowIntVarMap[k].ElementAt(0).Value;
                    var kSensorEndFrame = FlowIntVarMap[k].ElementAt(FlowIntVarMap[k].Keys.Count - 1).Value;


                    IntVar SingleSensorJitter = null;
                    IntVar SingleSensorDelay = null;
                    IntVar SingleSlack = null;
                    foreach (KeyValuePair<int, IntVar> kkFrame1 in kSensorStartFrame)
                    {
                        if (SingleSensorDelay != null)
                        {
                            SingleSensorDelay = (SingleSensorDelay + solver.MakeAbs(solver.MakeAbs(kSensorEndFrame[kkFrame1.Key]) -
                                                                              solver.MakeAbs(kkFrame1.Value)).Var()).Var();
                        }
                        else
                        {
                            SingleSensorDelay = solver.MakeAbs(solver.MakeAbs(kSensorEndFrame[kkFrame1.Key]) -
                                                               solver.MakeAbs(kkFrame1.Value)).Var();
                        }
                        foreach (KeyValuePair<int, IntVar> kkFrame2 in kSensorStartFrame)
                        {
                            if (SingleSensorJitter != null)
                            {
                                SingleSensorJitter = (SingleSensorJitter + solver.MakeAbs(solver.MakeAbs(kkFrame1.Value) - solver.MakeAbs(kkFrame2.Value)).Var()).Var();
                            }
                            else
                            {
                                SingleSensorJitter = solver
                                    .MakeAbs(solver.MakeAbs(kkFrame1.Value) - solver.MakeAbs(kkFrame2.Value)).Var();
                            }
                        }
                    }

                    SingleSensorJitter = (SingleSensorJitter / (kSensorStartFrame.Keys.Count ^ 2)).Var();
                    SingleSensorDelay = (SingleSensorDelay / kSensorStartFrame.Keys.Count).Var();
                    if (sensorJitter != null)
                    {
                        sensorJitter = (sensorJitter + SingleSensorJitter).Var();
                    }
                    else
                    {
                        sensorJitter = SingleSensorJitter;
                    }

                    if (sensorDelay != null)
                    {
                        sensorDelay = (sensorDelay + SingleSensorDelay).Var();
                    }
                    else
                    {
                        sensorDelay = SingleSensorDelay;
                    }




                    foreach (KeyValuePair<int, IntVar> kkFrame1 in kSensorEndFrame)
                    {
                        foreach (Flow kOut in _app.Flows.FindAll(x => x.Type == Flow.Flow_Type.Output))
                        {
                            var kActuatorStartFrame = FlowIntVarMap[kOut].ElementAt(0).Value;
                            foreach (KeyValuePair<int, IntVar> kkFrame2 in kActuatorStartFrame)
                            {
                                if (SingleSlack != null)
                                {
                                    SingleSlack = (SingleSlack + (k.Period - solver.MakeAbs(solver.MakeAbs(kActuatorStartFrame[kkFrame1.Key]) -
                                                                    solver.MakeAbs(kkFrame1.Value)).Var()).Var()).Var();
                                }
                                else
                                {
                                    SingleSlack = (k.Period - solver.MakeAbs(solver.MakeAbs(kActuatorStartFrame[kkFrame1.Key]) -
                                                                             solver.MakeAbs(kkFrame1.Value)).Var()).Var();
                                }
                            }
                        }
                    }

                    SingleSlack = (SingleSlack / _app.Flows.Count).Var();

                    if (slack != null)
                    {
                        slack = (slack + SingleSlack).Var();
                    }
                    else
                    {
                        slack = SingleSlack;
                    }
                }

                foreach (Flow k in _app.Flows.FindAll(x => x.Type == Flow.Flow_Type.Output))
                {
                    var kActuatorStartFrame = FlowIntVarMap[k].ElementAt(0).Value;

                    var kActuatorEndFrame = FlowIntVarMap[k].ElementAt(FlowIntVarMap[k].Keys.Count - 1).Value;

                    IntVar SingleActuatorJitter = null;
                    IntVar SingleActuatorDelay = null;

                    foreach (KeyValuePair<int, IntVar> kkFrame1 in kActuatorEndFrame)
                    {
                        if (SingleActuatorDelay != null)
                        {
                            SingleActuatorDelay = (SingleActuatorDelay + solver.MakeAbs(solver.MakeAbs(kkFrame1.Value) -
                                                                            solver.MakeAbs(
                                                                                kActuatorStartFrame[kkFrame1.Key]))
                                .Var()).Var();
                        }
                        else
                        {
                            SingleActuatorDelay = solver.MakeAbs(solver.MakeAbs(kkFrame1.Value) -
                                                                 solver.MakeAbs(kActuatorStartFrame[kkFrame1.Key])).Var();
                        }
                        foreach (KeyValuePair<int, IntVar> kkFrame2 in kActuatorEndFrame)
                        {
                            if (SingleActuatorJitter != null)
                            {
                                SingleActuatorJitter = (SingleActuatorJitter + solver.MakeAbs(solver.MakeAbs(kkFrame1.Value) - solver.MakeAbs(kkFrame2.Value)).Var()).Var();
                            }
                            else
                            {
                                SingleActuatorJitter = solver
                                    .MakeAbs(solver.MakeAbs(kkFrame1.Value) - solver.MakeAbs(kkFrame2.Value)).Var();
                            }
                        }
                    }


                    SingleActuatorJitter = (SingleActuatorJitter / (kActuatorStartFrame.Keys.Count ^ 2)).Var();
                    SingleActuatorDelay = (SingleActuatorDelay / kActuatorStartFrame.Keys.Count).Var();
                    if (actuatorJitter != null)
                    {
                        actuatorJitter = (actuatorJitter + SingleActuatorJitter).Var();
                    }
                    else
                    {
                        actuatorJitter = SingleActuatorJitter;
                    }

                    if (actuatorDelay != null)
                    {
                        actuatorDelay = (actuatorDelay + SingleActuatorDelay).Var();
                    }
                    else
                    {
                        actuatorDelay = SingleActuatorDelay;
                    }
                }

                if (sensorJitter != null)
                {
                    sensorJitter = (sensorJitter / _app.Flows.FindAll(x => x.Type == Flow.Flow_Type.Input).Count).Var();
                }
                if (actuatorJitter != null)
                {
                    actuatorJitter = (actuatorJitter / _app.Flows.FindAll(x => x.Type == Flow.Flow_Type.Output).Count).Var();
                }
                if (sensorDelay != null)
                {
                    sensorDelay = (sensorDelay / _app.Flows.FindAll(x => x.Type == Flow.Flow_Type.Input).Count).Var();
                }
                if (actuatorDelay != null)
                {
                    actuatorDelay = (actuatorDelay / _app.Flows.FindAll(x => x.Type == Flow.Flow_Type.Output).Count).Var();
                }




                if (qoc != null)
                {
                    qoc = (qoc + actuatorDelay + sensorDelay + actuatorJitter + sensorJitter + slack).Var();
                }
                else
                {
                    qoc = (actuatorDelay + sensorDelay + actuatorJitter + sensorJitter + slack).Var();
                }
            }

            return qoc;
        }

        private IntVar FrameExtensibility()
        {
            IntVar extensibility = null;
            

            foreach (KeyValuePair<Flow, Dictionary<string, Dictionary<int, IntVar>>> k in FlowIntVarMap)
            {
                var kSW2Int = k.Value;
                foreach (KeyValuePair<Flow, Dictionary<string, Dictionary<int, IntVar>>> v in FlowIntVarMap)
                {

                    var vSW2Int = v.Value;

                        foreach (KeyValuePair<string, Dictionary<int, IntVar>> kk in kSW2Int)
                        {
                            var kkInt2Var = kk.Value;

                            foreach (KeyValuePair<string, Dictionary<int, IntVar>> vv in vSW2Int)
                            {
                                if (kk.Key == vv.Key)
                                {
                                    int counter = 0;
                                    IntVar SingleFrameExtensibility = null;

                                    var vvInt2Var = vv.Value;
                                    foreach (KeyValuePair<int, IntVar> kkk in kkInt2Var)
                                    {
                                        
                                        IntVar kOffset = (kkk.Value + (kkk.Key * k.Key.Period)).Var();
                                        IntVar kEnd = (kOffset + k.Key.TransmitTime).Var();

                                        IntVar SingleExtensibility = null;
                                        foreach (KeyValuePair<int, IntVar> vvv in vvInt2Var)
                                        {
                                            IntVar vOffset = (vvv.Value + (vvv.Key * v.Key.Period)).Var();
                                            IntVar vEnd = (vOffset + v.Key.TransmitTime).Var();

                                            
                                            if ((vvv.Key != kkk.Key) || (k.Key != v.Key))
                                            {
                                                if (SingleExtensibility != null)
                                                {
                                                    //extensibility = (extensibility + solver.MakeAbs(vEnd - kOffset) +
                                                        //solver.MakeAbs(kEnd - vOffset)).Var();
                                                    IntVar aVar = solver.MakeMin(solver.MakeAbs(vEnd - kOffset),
                                                        solver.MakeAbs(kEnd - vOffset)).Var();
                                                    SingleExtensibility = solver.MakeMin(SingleExtensibility, aVar).Var();
                                                }
                                                else
                                                {
                                                //extensibility = (solver.MakeAbs(vEnd - kOffset) + solver.MakeAbs(kEnd - vOffset)).Var();
                                                    SingleExtensibility = solver.MakeMin(solver.MakeAbs(vEnd - kOffset),
                                                        solver.MakeAbs(kEnd - vOffset)).Var();

                                                }
                                            }
                                        }

                                        if (SingleFrameExtensibility != null)
                                        {
                                            SingleFrameExtensibility =
                                                (SingleFrameExtensibility + SingleExtensibility).Var();
                                            counter++;
                                        }   
                                        else
                                        {
                                            SingleFrameExtensibility = SingleExtensibility;
                                            counter++;
                                        }
                                    }

                                    SingleFrameExtensibility = (SingleFrameExtensibility / counter).Var();
                                    if (extensibility != null)
                                    {
                                        extensibility = (extensibility + Problem.Hyperperiod - SingleFrameExtensibility).Var();
                                        counter++;
                                    }
                                    else
                                    {
                                        extensibility = (Problem.Hyperperiod - SingleFrameExtensibility).Var();
                                        counter++;
                                    }


                            }
                            }
                        }

                }
            }



           // extensibility = (extensibility / counter).Var();

            return extensibility;
        }

        private IntVar JobExtensibility()
        {
            IntVar extensibility = null;
            
            foreach (KeyValuePair<Task, Dictionary<int, IntVar>> k in TaskIntVarMap)
            {

                IntVar TaskMinEx = null;
                int counter = 0;
                foreach (KeyValuePair<Task, Dictionary<int, IntVar>> v in TaskIntVarMap)
                {
                    IntVar kCore = TaskCoreMap[k.Key];
                    IntVar vCore = TaskCoreMap[v.Key];
                    IntVar CoreVar = (kCore == vCore).Var();


                    foreach (KeyValuePair<int, IntVar> kk in k.Value)
                    {
                        IntVar JobMinEx = null;
                        IntVar kOffset = (kk.Value + (kk.Key * k.Key.Period)).Var();
                        IntVar kEnd = (kOffset + k.Key.WCET).Var();

                        foreach (KeyValuePair<int, IntVar> vv in v.Value)
                        {
                            if ((vv.Key != kk.Key) || (k.Key != v.Key))
                            {

                                IntVar vOffset = (vv.Value + (vv.Key * v.Key.Period)).Var();
                                IntVar vEnd = (vOffset + v.Key.WCET).Var();
                                IntVar aVar = (CoreVar * solver.MakeMin(solver.MakeAbs(vEnd - kOffset),
                                solver.MakeAbs(kEnd - vOffset)).Var()).Var();

                                if (JobMinEx != null)
                                {
                                    JobMinEx = solver.MakeMin(JobMinEx, aVar).Var();
                                }
                                else
                                {
                                    JobMinEx = aVar;
                                }

                            }

                        }

                        if (TaskMinEx != null)
                        {
                            TaskMinEx = (TaskMinEx + Problem.Hyperperiod - JobMinEx).Var();
                            counter++;
                        }
                        else
                        {
                            TaskMinEx = (Problem.Hyperperiod - JobMinEx).Var();
                            counter++;
                        }


                    }

                }

                TaskMinEx = (TaskMinEx / counter).Var();

                if (extensibility != null)
                {
                    extensibility = (extensibility + TaskMinEx).Var();
                }
                else
                {
                    extensibility = TaskMinEx;
                }
            }

            //extensibility = (extensibility / counter).Var();
            //extensibility = (Problem.Hyperperiod - extensibility).Var();
            return extensibility;
        }

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

            DecisionBuilder db1 = solver.MakePhase(TaskMappingVars.ToArray(), Choose_first_unbound, Assign_random);
            DecisionBuilder db2 = solver.MakePhase(TaskOffsets.ToArray(), Choose_first_unbound, Assign_random);
            DecisionBuilder db3 = solver.MakePhase(FlowOffsets.ToArray(), Choose_first_unbound, Assign_random);

            db = solver.Compose(db1, db2, db3);
        }
        private DecisionBuilder getDecision()
        {
            return db;
        }





    }
}
