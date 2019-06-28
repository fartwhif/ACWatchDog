using CSharpTest.Net.RpcLibrary;
using System;
using System.Threading;

namespace ACWatchDog.Interop
{
    public class Server
    {
        public int PoolSize { get; set; }
        private bool @continue;
        public void Stop()
        {
            @continue = false;
        }
        public void Start()
        {
            Thread serverThread = new Thread(new ThreadStart(ServerLoop));
            serverThread.SetApartmentState(ApartmentState.MTA);
            serverThread.Name = "RPC Server";
            serverThread.Start();
        }
        private void ServerLoop()
        {
            Guid iid = new Guid(Constants.ACWatchDogInteropId);
            using (RpcServerApi server = new RpcServerApi(iid, 100, ushort.MaxValue, allowAnonTcp: false))
            {
                // Add an endpoint so the client can connect, this is local-host only:
                server.AddProtocol(RpcProtseq.ncalrpc, Constants.ACWatchDogInteropEndpoint, 100);
                // Add the types of authentication we will accept
                server.AddAuthentication(RpcAuthentication.RPC_C_AUTHN_GSS_NEGOTIATE);
                // Subscribe the code to handle requests on this event:
                server.OnExecute += Server_OnExecute;
                // Start Listening 
                server.StartListening();
                @continue = true;
                while (@continue)
                {
                    Thread.Sleep(100);
                }
                server.StopListening();
            }
        }
        private byte[] Server_OnExecute(IRpcClientInfo client, byte[] input)
        {
            lock (InteropGlobals.BarkLocker)
            {
                InteropGlobals.Barking.Enqueue(AppMessage.FromBytes(input));
            }
            return new AppMessage() { PoolSize = PoolSize }.ToBytes();
        }
    }
}
