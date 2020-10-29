using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EISA.Model
{
    public class Task
    {
        public int Id { get;}

        public int WCET { get;}

        public int Period { get;}

        public int Deadline { get;}

        public int MaxJitter { get;}

        public int Cil { get;}

        public int CpuId { get;}

        public double Workload => (double) WCET / Period;
        public Task(Application_model.Task _task)
        {
            Id = _task.Id;
            WCET = _task.WCET;
            Period = _task.Period;
            Deadline = _task.Deadline;
            MaxJitter = _task.MaxJitter;
            Cil = _task.Cil;
            CpuId = _task.CpuId;

        }

        public Task(int _id, int _wcet, int _period, int _deadline, int _maxjitter, int _cil, int _cpuid)
        {
            Id = _id;
            WCET = _wcet;
            Period = _period;
            Deadline = _deadline;
            MaxJitter = _maxjitter;
            Cil = _cil;
            CpuId = _cpuid;
        }

        public Task Clone()
        {
            return new Task(Id, WCET,Period,Deadline,MaxJitter,Cil,CpuId);
        }
    }
}
