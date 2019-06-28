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
                AppMessage bark = Client.Send(AppMessage.New());
                Console.WriteLine("from server: pool size: " + bark?.PoolSize.ToString());
                Thread.Sleep(1000);
            }
        }
    }
}
