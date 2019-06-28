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
        private static readonly Dictionary<int, Hyper> Pool = new Dictionary<int, Hyper>();
        static void Main(string[] args)
        {
            Server server = new Server();
            server.Start();
            while (true)
            {
                Thread.Sleep(1000);
                lock (InteropGlobals.QueueLocker)
                {
                    while (InteropGlobals.Queue.Count > 0)
                    {
                        AppMessage msg = InteropGlobals.Queue.Dequeue();
                        if (!Pool.Keys.Contains(msg.ProcessId))
                        {
                            Log($"Registered App: PID: {msg.ProcessId} Name: {msg.AppName}");
                        }
                        Pool[msg.ProcessId] = new Hyper() { Register = msg, TimeSinceRcvd = Stopwatch.StartNew() };
                        server.PoolSize = Pool.Count;
                    }
                }
                CheckPool();
            }
        }

        private static void Log(string what)
        {
            Console.WriteLine($"[{DateTime.Now.ToString()}] " + what);
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
            List<KeyValuePair<int, Hyper>> delinquents = Pool.Where(
                k => k.Value.Register.Trigger == AppMessage.TriggerType.Canary &&
                k.Value.TimeSinceRcvd.Elapsed.TotalSeconds > 5 &&
                k.Value.TimeSinceTriggered == null || (k.Value.TimeSinceTriggered?.Elapsed.TotalSeconds ?? 0) > 5).ToList();
            foreach (KeyValuePair<int, Hyper> hyper in delinquents)
            {
                Pool.Remove(hyper.Value.Register.ProcessId);
                hyper.Value.TimeSinceTriggered = Stopwatch.StartNew();

                AppMessage register = hyper.Value.Register;
                ProcessStartInfo psi = new ProcessStartInfo();

                KillProcById(register.ProcessId);

                string exePath = register.ExePath;
                string cmdLin = register.CmdLine;
                if (cmdLin.StartsWith($"\"{exePath}\""))
                {
                    cmdLin = cmdLin.Substring(exePath.Length + 2).Trim();
                }

                psi.FileName = exePath;
                psi.Arguments = cmdLin;

                Process newProc = Process.Start(psi);
                var oldPid = register.ProcessId;
                register.ProcessId = newProc.Id;
                Pool[newProc.Id] = hyper.Value;

                Log($"Restarted App: PID: {oldPid} => {register.ProcessId} Name: {register.AppName}");
            }
        }
    }
}
