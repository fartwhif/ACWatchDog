using CSharpTest.Net.RpcLibrary;
using System;

namespace ACWatchDog.Interop
{
    public class Client
    {
        public static AppMessage Send(AppMessage message)
        {
            try
            {
                Guid iid = new Guid(Constants.ACWatchDogInteropId);
                using (RpcClientApi client = new RpcClientApi(iid, RpcProtseq.ncalrpc, null, Constants.ACWatchDogInteropEndpoint))
                //using (var client = new RpcClientApi(iid, RpcProtseq.ncacn_ip_tcp, null, @"18081"))
                {
                    // Provide authentication information (not nessessary for LRPC)
                    client.AuthenticateAs(RpcClientApi.Self);
                    // Send the request and get a response
                    return AppMessage.FromBytes(client.Execute(message.ToBytes()));
                }
            }
            catch (Exception) { return null; }
        }
    }
}
