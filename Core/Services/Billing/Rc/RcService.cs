using System.Text;
using BRB.Core.Common.Extensions;
using Core.Services.Billing.Rc.Exceptions;
using Core.Brokers.DbContext;
using Core.Services.Billing.Rc.Contracts;
using Microsoft.Extensions.Options;

namespace Core.Services.Billing.Rc;

public class RcService(AppDbContext dbContext, OrderService orderService, IOptions<RcConfig> options)
{
    public async Task HandleRequest(RcRequest request)
    {
        if (request.Event.SubscriberAttributes.OrderId is null)
            throw new RcOrderIdRequiredException();

        var orderId = long.Parse(request.Event.SubscriberAttributes.OrderId.Value);
        await orderService.AcceptPaymentAsync(orderId);
    }

    public void ValidateAuthentication(string? authorization)
    {
        if (authorization.IsNullOrEmpty())
            throw new RcInvalidAuthException();

        var parts = authorization!.Split();

        if (parts.Length != 2) throw new RcInvalidAuthException();

        if (!parts[0].Equals("Basic", StringComparison.InvariantCultureIgnoreCase)) throw new RcInvalidAuthException();

        var equals = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Value.Login}:{options.Value.Password}"))
            .Equals(parts[1], StringComparison.InvariantCultureIgnoreCase);

        if (!equals) throw new RcInvalidAuthException();
    }
}