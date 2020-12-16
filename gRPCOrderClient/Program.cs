using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using V1;

namespace gRPCOrderClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Order.OrderClient(channel);


            OrderRequest req = new OrderRequest { Id = 0 };
            var reply = await client.GetOrderAsync(req);



            var tokenSource = new CancellationTokenSource();
            var n = 0;
            Empty request = new Empty();
            using var call = client.GetOrdersServerStream(request);
            try
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync(tokenSource.Token))
                {
                    Console.WriteLine("Order Details: " + response.Details);
                    if (++n == 5)
                    {
                        tokenSource.Cancel();
                    }
                }
            }
            catch (RpcException e) when (e.Status.StatusCode == StatusCode.Cancelled)
            {
                Console.WriteLine("Streaming was cancelled from the client!");
            }


            Console.ReadKey();
        }
    }
}
