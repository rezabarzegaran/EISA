using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EISA.Model;
using Google.OrTools.ConstraintSolver;
using Google.OrTools.LinearSolver;

namespace EISA.Base
{
    public class Result
    {
        // Variables
        public int Hyperperiod { get;}
        public List<Stream> Streams;
        private Solution Problem;

        //Constructor
        public Result(Solution solution, Dictionary<Flow, Dictionary<string, Dictionary<int, IntVar>>> FlowIntVarMap)
        {
            Hyperperiod = solution.Hyperperiod;
            Problem = solution.Clone();
            Streams = new List<Stream>();
            foreach (KeyValuePair<Flow, Dictionary<string, Dictionary<int, IntVar>>> k in FlowIntVarMap)
            {

                Streams.Add(new Stream(k.Key, k.Value, Problem.RouteMap[k.Key.Name]));
            }
            
        }

        //Classes

        public class Stream
        {
            private string Name;
            private List<Link> Links;
            private int Priority;
            private int E2E;
            private int Jitter;
            public Stream(Flow flow, Dictionary<string, Dictionary<int, IntVar>> FlowMap, List<string> routing)
            {
                Name = flow.Name;
                Priority = flow.Priority;
                Links = new List<Link>();
                foreach (KeyValuePair<string, Dictionary<int, IntVar>> k in FlowMap)
                {
                    Links.Add(new Link(k.Key, routing[routing.FindIndex(x => x == k.Key) + 1], flow.TransmitTime, flow.Period, k.Value));
                }
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

                }

            }


        }
    }
}
