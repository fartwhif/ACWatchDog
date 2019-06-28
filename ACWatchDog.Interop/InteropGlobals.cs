using System.Collections.Generic;

namespace ACWatchDog.Interop
{
    public sealed class InteropGlobals
    {
        private InteropGlobals() { }
        public static InteropGlobals Instance => Nested.instance;
        public static Queue<AppMessage> Queue { get => Nested.instance.queue; set => Nested.instance.queue = value; }
        public static object QueueLocker => Nested.instance.queueLocker;
        private Queue<AppMessage> queue = new Queue<AppMessage>();
        private object queueLocker = new object();
        private class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
            }
            internal static readonly InteropGlobals instance = new InteropGlobals();
        }
    }
}
