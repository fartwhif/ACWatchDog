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
                AppMessage msg = Client.Send(AppMessage.New());
                Console.WriteLine("from server: pool size: " + msg?.PoolSize.ToString());
                Thread.Sleep(1000);
            }
        }
    }
}
