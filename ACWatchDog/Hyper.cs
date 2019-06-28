using ACWatchDog.Interop;
using System.Diagnostics;

namespace ACWatchDog
{
    internal class Hyper
    {
        public AppMessage Register { get; set; }
        public Stopwatch TimeSinceRcvd { get; set; }
        public Stopwatch TimeSinceTriggered { get; set; }
    }
}
