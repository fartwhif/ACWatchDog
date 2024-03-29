﻿using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ACWatchDog.Interop
{
    public class AppMessage
    {
        public enum TriggerType
        {
            Canary // if time since msg > X then trigger
        }
        public AppMessage() { }
        public bool DecalInject { get; set; } //C2S
        public string CmdLine { get; set; } //C2S
        public string ExePath { get; set; } //C2S
        public string AppName { get; set; } //C2S
        public int ProcessId { get; set; } //C2S
        public int PoolSize { get; set; } //S2C
        public int DelinquencyTime { get; set; } = 300; //C2S
        public TriggerType Trigger { get; set; } //C2S
        public static AppMessage FromBytes(byte[] input)
        {
            string inputStr = Encoding.UTF8.GetString(input);
            AppMessage msg = JsonConvert.DeserializeObject<AppMessage>(inputStr);
            return msg;
        }
        public byte[] ToBytes()
        {
            string sMsg = JsonConvert.SerializeObject(this);
            byte[] bMsg = Encoding.UTF8.GetBytes(sMsg);
            return bMsg;
        }
        public static AppMessage New(string appName, int delinquencyTime, bool DecalInject)
        {
            using (Process proc = Process.GetCurrentProcess())
            {
                return new AppMessage()
                {
                    DecalInject = DecalInject,
                    ProcessId = proc.Id,
                    AppName = appName,
                    DelinquencyTime = delinquencyTime,
                    ExePath = proc.MainModule.FileName,
                    CmdLine = GetCommandLineOfProcess(proc)
                };
            }
        }

        /// <summary>
        /// https://stackoverflow.com/a/46006415/6620171
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        private static string GetCommandLineOfProcess(Process proc)
        {
            // max size of a command line is USHORT/sizeof(WCHAR), so we are going
            // just allocate max USHORT for sanity's sake.
            StringBuilder sb = new StringBuilder(0xFFFF);
            switch (IntPtr.Size)
            {
                case 4: GetProcCmdLine32((uint)proc.Id, sb, (uint)sb.Capacity); break;
                case 8: GetProcCmdLine64((uint)proc.Id, sb, (uint)sb.Capacity); break;
            }
            return sb.ToString();
        }
        [DllImport("ProcCmdLine32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetProcCmdLine")]
        private static extern bool GetProcCmdLine32(uint nProcId, StringBuilder sb, uint dwSizeBuf);
        [DllImport("ProcCmdLine64.dll", CharSet = CharSet.Unicode, EntryPoint = "GetProcCmdLine")]
        private static extern bool GetProcCmdLine64(uint nProcId, StringBuilder sb, uint dwSizeBuf);
    }
}
