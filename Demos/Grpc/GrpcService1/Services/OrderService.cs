using Grpc.Core;
using GrpcService1;
using Microsoft.AspNetCore.Components;

namespace GrpcService1.Services
{
    public class OrderService : Order.OrderBase
    {

        public override Task<OrderResult> Pay(OrderParam param, ServerCallContext context)
        {
            return Task.FromResult(new OrderResult
            {
                Status = "Pay Success . " + param.OrderId,
            });
        }
    }
}