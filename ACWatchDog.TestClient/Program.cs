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
                AppMessage msg = AppMessage.New("TestClient", 5, false);
                AppMessage msg2 = Client.Send(msg);
                Console.WriteLine("from server: pool size: " + ((msg2?.PoolSize).ToString() ?? ""));
                Thread.Sleep(1000);
            }
        }
    }
}
