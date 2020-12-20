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



            //Unary gRPC Example
            OrderRequest req = new OrderRequest { Id = 0 };
            var reply = await client.GetOrderAsync(req);

            foreach (var order in reply.Orders)
            {
                Console.WriteLine($"Order {order.Id}:");
                foreach (var detail in order.Details)
                {
                    Console.WriteLine("Order Details: " + detail);
                }
            }



            //Server Streaming gRPC Example
            var tokenSource = new CancellationTokenSource();
            var n = 0;
            Empty request = new Empty();
            using var callSrv = client.GetOrdersServerStream(request);
            try
            {
                await foreach (var response in callSrv.ResponseStream.ReadAllAsync(tokenSource.Token))
                {
                    //Console.WriteLine($"Order {n + 1}:");
                    foreach (var order in response.Orders)
                    {
                        Console.WriteLine($"Order {order.Id}:");
                        foreach (var detail in order.Details)
                        {
                            Console.WriteLine("Order Details: " + detail);
                        }
                    }
                    if (++n == 3)
                    {
                        tokenSource.Cancel();
                    }
                }
            }
            catch (RpcException e) when (e.Status.StatusCode == StatusCode.Cancelled)
            {
                Console.WriteLine("Streaming was cancelled from the client!");
            }



            //Client Streaming gRPC Example
            using var callClnt = client.GetOrdersClientStream(deadline: DateTime.UtcNow.AddSeconds(5));

            try
            {
                for (var i = 1; i <= 4; i++)
                {
                    await callClnt.RequestStream.WriteAsync(new OrderRequest { Id = i });
                }
                await callClnt.RequestStream.CompleteAsync();

                var res = await callClnt;
                foreach (var order in res.Orders)
                {
                    Console.WriteLine($"Order {order.Id}:");
                    foreach (var detail in order.Details)
                    {
                        Console.WriteLine("Order Details: " + detail);
                    }
                }
            }
            catch (RpcException e) when (e.Status.StatusCode == StatusCode.DeadlineExceeded)
            {
                Console.WriteLine("Deadline exceeded!");
            }


            Console.ReadKey();
        }
    }
}
