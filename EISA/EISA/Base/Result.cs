using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EISA.Model;
using Google.OrTools.ConstraintSolver;
using Google.OrTools.LinearSolver;
using jdk.nashorn.@internal.runtime.regexp.joni.exception;
using Attribute = com.sun.tools.javac.code.Attribute;

namespace EISA.Base
{
    public class Result
    {
        // Variables
        public int Hyperperiod { get;}
        public List<Stream> Streams;
        public List<VTask> Tasks;
        private Solution Problem;
        public List<string> Links;
        public List<string> Cores;

        //Constructor
        public Result(Solution solution, Dictionary<Flow, Dictionary<string, Dictionary<int, IntVar>>> FlowIntVarMap, Dictionary<Task, Dictionary<int, IntVar>> TaskIntVarMap, Dictionary<Task, IntVar> TaskCoreMap)
        {
            Hyperperiod = solution.Hyperperiod;
            Problem = solution.Clone();
            Streams = new List<Stream>();
            Tasks = new List<VTask>();
            foreach (KeyValuePair<Flow, Dictionary<string, Dictionary<int, IntVar>>> k in FlowIntVarMap)
            {

                Streams.Add(new Stream(k.Key, k.Value, Problem.RouteMap[k.Key.Name]));
            }

            Links = Problem.getLinks();

            foreach (KeyValuePair<Task, Dictionary<int, IntVar>> k in TaskIntVarMap)
            {
                Tasks.Add(new VTask(k.Key, k.Value, TaskCoreMap[k.Key]));
            }

            Cores = Problem.getCores();

        }

        //Classes

        public class Stream
        {
            public string Name;
            private List<Link> Links;
            public int Priority;
            private int E2E;
            private int Jitter;
            private int N_instances = 0;
            public Stream(Flow flow, Dictionary<string, Dictionary<int, IntVar>> FlowMap, List<string> routing)
            {
                Name = flow.Name;
                Priority = flow.Priority;
                N_instances = flow.N_Instances;
                Links = new List<Link>();
                foreach (KeyValuePair<string, Dictionary<int, IntVar>> k in FlowMap)
                {
                    Links.Add(new Link(k.Key, routing[routing.FindIndex(x => x == k.Key) + 1], flow.TransmitTime, flow.Period, k.Value));
                }

                E2E = getMaxE2E();
            }

            private int getMaxE2E()
            {
                int E2E = 0;
                for (int i = 0; i < N_instances; i++)
                {
                    int TempE2E = (Links[Links.Count - 1].getFrames()[i].getClose()) -
                                  (Links[0].getFrames()[i].getOpen());
                    if (E2E <= TempE2E)
                    {
                        E2E = TempE2E;
                    }
                }

                return E2E;
            }

            public int getE2E()
            {
                return E2E;
            }

            private int getStartJitter()
            {
                int jitter = 0;
                for (int i = 0; i < N_instances; i++)
                {
                    int Tempjitter = (Links[Links.Count - 1].getFrames()[i].getClose()) -
                                  (Links[0].getFrames()[i].getOpen());

                }

                return E2E;
            }

            public List<Link> getLinks()
            {
                return Links;
            }
            public class Link
            {
                private List<Frame> Frames;
                private string Source;
                private string Destination;
                public Link(string source, string destination, int t_time, int period, Dictionary<int, IntVar> VarVector)
                {
                    Source = source;
                    Destination = destination;
                    Frames = new List<Frame>();
                    foreach (KeyValuePair<int, IntVar> k in VarVector)
                    {
                        int _start = (int) k.Value.Value() + k.Key * period;
                        Frames.Add(new Frame(_start, t_time));
                    }
                }

                public class Frame
                {
                    private int Start;
                    private int End;
                    public Frame(int start, int t_time)
                    {
                        Start = start;
                        End = Start + t_time;
                    }
                    public int getOpen()
                    {
                        return Start;
                    }
                    public int getClose()
                    {
                        return End;
                    }

                }

                public string getName()
                {
                    return Source + ":" + Destination;
                }

                public List<Frame> getFrames()
                {
                    return Frames;
                }



            }


        }

        public class VTask
        {
            private string mapCore = null;
            public string Name = null;
            private List<Job> Jobs;
            public VTask(EISA.Model.Task task, Dictionary<int, IntVar> Jobmap, IntVar coremap)
            {
                Name = task.Name;
                mapCore = task.FN_Name + ":" + coremap.Value().ToString();
                Jobs = new List<Job>();
                foreach (KeyValuePair<int, IntVar> k in Jobmap)
                {
                    Jobs.Add(new Job((int) k.Value.Value() + k.Key * task.Period, task.WCET));
                }
            }

            public string getMap()
            {
                return mapCore;
            }

            public List<Job> getJobs()
            {
                return Jobs;
            }

            public class Job
            {
                private int Start;
                private int End;
                public Job(int start, int t_time)
                {
                    Start = start;
                    End = Start + t_time;
                }
                public int getStart()
                {
                    return Start;
                }
                public int getEnd()
                {
                    return End;
                }
            }
        }
    }
}
