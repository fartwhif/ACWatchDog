using ACWatchDog.Interop;
using System;
using System.Threading;

namespace ACWatchDog.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                var msg = AppMessage.New();
                msg.DelinquencyTime = 5;//5 seconds
                AppMessage msg2 = Client.Send(msg);
                Console.WriteLine("from server: pool size: " + msg?.PoolSize.ToString());
                Thread.Sleep(1000);
            }
        }
    }
}
