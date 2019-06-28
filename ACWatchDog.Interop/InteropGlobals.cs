using System.Collections.Generic;

namespace ACWatchDog.Interop
{
    public sealed class InteropGlobals
    {
        private InteropGlobals() { }
        public static InteropGlobals Instance => Nested.instance;
        public static Queue<AppMessage> Barking { get => Nested.instance.barking; set => Nested.instance.barking = value; }
        public static object BarkLocker => Nested.instance.barkLocker;
        private Queue<AppMessage> barking = new Queue<AppMessage>();
        private object barkLocker = new object();
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
