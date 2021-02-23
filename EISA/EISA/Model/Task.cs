using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EISA.Model
{
    public class Task
    {
        public string Name { get;}

        public int WCET { get;}
        public int RelWCET { get; private set; }

        public int Period { get;}

        public int Deadline { get;}

        public int MaxJitter { get;}

        public int Cil { get;}

        public string FN_Name { get;}

        public int Hyperperiod { get; private set; }

        public int N_Instances { get; private set; }

        public double Workload => (double) WCET / Period;
        public Task(Application_model.Task _task)
        {
            Name = _task.Name;
            WCET = _task.WCET;
            Period = _task.Period;
            Deadline = _task.Deadline;
            MaxJitter = _task.MaxJitter;
            Cil = _task.Cil;
            FN_Name = _task.CpuName;

        }

        public Task(string _name, int _wcet, int _period, int _deadline, int _maxjitter, int _cil, string _fn_name, int _hyperperiod)
        {
            Name = _name;
            WCET = _wcet;
            Period = _period;
            Deadline = _deadline;
            MaxJitter = _maxjitter;
            Cil = _cil;
            FN_Name = _fn_name;
            SetHyperperiod(_hyperperiod);
            SetN_Instances();
        }

        public void SetRelWCET(int gcd)
        {
            RelWCET = WCET / gcd;
        }
        public void Init(int _hyperperiod)
        {
            SetHyperperiod(_hyperperiod);
            SetN_Instances();
        }

        private void SetHyperperiod(int val)
        {
            Hyperperiod = val;
        }

        private void SetN_Instances()
        {
            N_Instances = Hyperperiod / Period;
        }

        public Task Clone()
        {
            return new Task(Name, WCET,Period,Deadline,MaxJitter,Cil,FN_Name, Hyperperiod);
        }
    }
}
