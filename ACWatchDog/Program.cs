using ACWatchDog.Interop;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace ACWatchDog
{
    class Program
    {
        private static Stopwatch LatestTrigger = null;
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
                        if (msg.DelinquencyTime < 10)
                        {
                            msg.DelinquencyTime = 10;
                        }
                        KeyValuePair<int, Hyper> register = Pool.FirstOrDefault(k => k.Key == msg.ProcessId);
                        if (register.Value == null && string.IsNullOrEmpty(InjectDllPath))
                        {
                            continue;
                        }
                        if (register.Value == null)
                        {
                            Pool[msg.ProcessId] = new Hyper() { Register = msg, TimeSinceRcvd = Stopwatch.StartNew(), TimeSinceRegistered = Stopwatch.StartNew() };
                        }
                        else
                        {
                            Hyper hyper = register.Value;
                            hyper.Register = msg;
                            hyper.TimeSinceRcvd = Stopwatch.StartNew();
                            hyper.TimeSinceTriggered = null;
                        }
                        server.PoolSize = Pool.Count;
                    }
                }
                CheckPool();
                Display();
            }
        }
        private static void Display()
        {
            Console.CursorTop = 0;
            Console.CursorLeft = 0;
            Console.Clear();
            IOrderedEnumerable<Hyper> items = Pool.Select(k => k.Value).OrderByDescending(k => k.TimeSinceRegistered.ElapsedTicks);
            foreach (Hyper item in items)
            {
                Console.WriteLine(item.ToString());
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
            if ((LatestTrigger?.Elapsed.TotalSeconds ?? int.MaxValue) < 30)
            {
                return;
            }
            List<KeyValuePair<int, Hyper>> delinquents = Pool.Where(
                k => k.Value.Register.Trigger == AppMessage.TriggerType.Canary &&
                k.Value.TimeSinceRcvd.Elapsed.TotalSeconds > k.Value.Register.DelinquencyTime &&
                (k.Value.TimeSinceTriggered == null || (k.Value.TimeSinceTriggered?.Elapsed.TotalSeconds ?? 0) > k.Value.Register.DelinquencyTime)).ToList();
            if (!delinquents.Any())
            {
                return;
            }
            KeyValuePair<int, Hyper> hyper = delinquents.First();
            Pool.Remove(hyper.Value.Register.ProcessId);
            hyper.Value.TriggerCount++;
            LatestTrigger = Stopwatch.StartNew();
            AppMessage register = hyper.Value.Register;
            KillProcById(register.ProcessId);
            Thread.Sleep(5000);
            string exePath = register.ExePath;
            string cmdLin = register.CmdLine;
            if (cmdLin.StartsWith($"\"{exePath}\""))
            {
                cmdLin = cmdLin.Substring(exePath.Length + 2).Trim();
            }
            int newPid = StartProc(exePath, cmdLin, register.DecalInject);
            if (newPid > 0)
            {
                int oldPid = register.ProcessId;
                register.ProcessId = newPid;
                Pool[newPid] = hyper.Value;
                Pool[newPid].Register = register;
                hyper.Value.TimeSinceTriggered = Stopwatch.StartNew();
            }
        }
        private static int StartProc(string exe, string cmdLin, bool decalInject)
        {
            if (decalInject)
            {
                string injectDll = InjectDllPath;
                if (string.IsNullOrEmpty(injectDll))
                {
                    return -1;
                }
                else
                {
                    int pid = -1;
                    if (Injector.RunSuspendedCommaInjectCommaAndResume(exe, cmdLin, out pid, injectDll, "DecalStartup"))
                    {
                        return pid;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            else
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = cmdLin,
                    WorkingDirectory = Path.GetDirectoryName(exe)
                };
                Process newProc = Process.Start(psi);
                return newProc.Id;
            }
        }
        private static string _InjectDllPath = null;
        private static string InjectDllPath
        {
            get
            {
                if (_InjectDllPath != null)
                {
                    return _InjectDllPath;
                }
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Decal\Agent"))
                    {
                        if (key != null)
                        {
                            object o = key.GetValue("AgentPath");
                            if (o != null)
                            {
                                string decalProgramPath = o as string;
                                if (!string.IsNullOrEmpty(decalProgramPath))
                                {
                                    string injectPath = Path.Combine(decalProgramPath, "Inject.dll");
                                    if (File.Exists(injectPath))
                                    {
                                        _InjectDllPath = injectPath;
                                        return injectPath;
                                    }
                                    else
                                    {
                                        _InjectDllPath = "";
                                    }
                                }
                                else
                                {
                                    _InjectDllPath = "";
                                }
                            }
                            else
                            {
                                _InjectDllPath = "";
                            }
                        }
                        else
                        {
                            _InjectDllPath = "";
                        }
                    }
                }
                catch (Exception) { _InjectDllPath = ""; }
                return null;
            }
        }
    }
}
