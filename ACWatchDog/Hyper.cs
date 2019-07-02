using ACWatchDog.Interop;
using System;
using System.Diagnostics;

namespace ACWatchDog
{
    internal class Hyper
    {
        public AppMessage Register { get; set; }
        public Stopwatch TimeSinceRcvd { get; set; }
        public Stopwatch TimeSinceTriggered { get; set; }
        public Stopwatch TimeSinceRegistered { get; set; }
        public TimeSpan TimeUntilRestart => TimeSpan.FromSeconds(Register.DelinquencyTime) - TimeSinceRcvd?.Elapsed ?? TimeSpan.Zero;
        public int TriggerCount { get; set; }
        public override string ToString()
        {
            string strTrigCnt = (TriggerCount > 0) ? $" R: {TriggerCount}" : "";
            string tur = $" T{TimeUntilRestart.Negate()}";
            return $"{Register.AppName} PID: {Register.ProcessId}{tur}{strTrigCnt}";
        }
    }
}
