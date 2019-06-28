using ACWatchDog.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ACWatchDog
{
    class Program
    {
        private class Leash
        {
            public AppMessage Bark { get; set; }
            public Stopwatch TimeSinceBark { get; set; }
            public Stopwatch TimeSinceTriggered { get; set; }
        }
        private static readonly Dictionary<int, Leash> Pool = new Dictionary<int, Leash>();
        static void Main(string[] args)
        {
            Server server = new Server();
            server.Start();
            while (true)
            {
                Thread.Sleep(1000);
                lock (InteropGlobals.BarkLocker)
                {
                    while (InteropGlobals.Barking.Count > 0)
                    {
                        AppMessage bark = InteropGlobals.Barking.Dequeue();
                        Pool[bark.ProcessId] = new Leash() { Bark = bark, TimeSinceBark = Stopwatch.StartNew() };
                        server.PoolSize = Pool.Count;
                    }
                }
                CheckPool();
            }
        }

        private static bool KillProcById(int id)
        {
            try
            {
                Process[] procs = Process.GetProcesses();
                foreach (Process proc in procs)
                {
                    if (proc.Id == id)
                    {
                        proc.Kill();
                        return true;
                    }
                }
                return false;
            }
            catch (Exception) { return false; }
        }

        private static void CheckPool()
        {
            List<KeyValuePair<int, Leash>> delinquents = Pool.Where(
                k => k.Value.Bark.Trigger == AppMessage.TriggerType.Canary &&
                k.Value.TimeSinceBark.Elapsed.TotalSeconds > 5 &&
                k.Value.TimeSinceTriggered == null || (k.Value.TimeSinceTriggered?.Elapsed.TotalSeconds ?? 0) > 5).ToList();
            foreach (KeyValuePair<int, Leash> leash in delinquents)
            {
                Pool.Remove(leash.Value.Bark.ProcessId);
                leash.Value.TimeSinceTriggered = Stopwatch.StartNew();

                AppMessage bark = leash.Value.Bark;
                ProcessStartInfo psi = new ProcessStartInfo();

                KillProcById(bark.ProcessId);

                string exePath = bark.ExePath;
                string cmdLin = bark.CmdLine;
                if (cmdLin.StartsWith($"\"{exePath}\""))
                {
                    cmdLin = cmdLin.Substring(exePath.Length + 2).Trim();
                }

                psi.FileName = exePath;
                psi.Arguments = cmdLin;

                Process newProc = Process.Start(psi);
                bark.ProcessId = newProc.Id;
                Pool[newProc.Id] = leash.Value;
            }
        }
    }
}
