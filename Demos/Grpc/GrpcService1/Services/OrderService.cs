using Basics;
using Basics.Grpc;
using Grpc.Core;

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