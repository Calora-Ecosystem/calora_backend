using System.Text;
using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Extensions;
using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Services.Billing.Rc.Contracts;
using Microsoft.Extensions.Options;

namespace Core.Services.Billing.Rc;

public class RcService(AppDbContext dbContext, OrderService orderService, IOptions<RcConfig> options)
{
    public async Task HandleRequest(RcRequest request)
    {
        var orderId = long.Parse(request.Event.SubscriberAttribute.OrderId.Value);
        await orderService.AcceptPaymentAsync(orderId);
    }

    public void ValidateAuthentication(string? authorization)
    {
        if (authorization.IsNullOrEmpty())
            throw new UnauthorizedException();

        var equals = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Value.Login}:{options.Value.Password}"))
            .Equals(authorization, StringComparison.InvariantCultureIgnoreCase);

        if (!equals) throw new UnauthorizedException();
    }
}