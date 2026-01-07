using System.Text.Json;
using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Entities.Billing.Enum;
using Core.Entities.Billing.Payme;
using Core.Services.Billing.Payme.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace Core.Services.Billing.Payme;

[Injectable]
public class PaymeService(AppDbContext dbContext, IOptions<PaymeConfig> config)
{
    public bool CheckFodValidRequestFromPayme(string authToken)
    {
        try
        {
            var basicAuth = Convert.ToString(Convert.FromBase64String(authToken));
            var parts = basicAuth!.Split(":");

            if (parts.Length != 2) return false;

            if (parts[0] != config.Value.Login || parts[1] != config.Value.Password) return false;

            return true;
        }
        catch (Exception e)
        {
            Log.Error("Invalid request from payme: {Error}", e.Message);
            return false;
        }
    }

    public async Task<BaseResponseDto> HandleAsync(BaseRequest request, string authToken)
    {
#if !DEBUG
        if (!CheckFodValidRequestFromPayme(authToken))
        {
            return new ErrorResponseDto()
            {
                Error = ResponseErrors.Unathorized,
                Id = request.Id
            };
        }
#endif

        var methodInfo = this.GetType().GetMethod(request.Method);

        if (methodInfo is null)
            return new ErrorResponseDto()
            {
                Error = ResponseErrors.MethodNotFound,
                Id = request.Id
            };

        var parameters = methodInfo.GetParameters();

        var parameter = parameters.FirstOrDefault();

        if (parameter is null)
            return new ErrorResponseDto()
            {
                Error = ResponseErrors.InternalError,
                Id = request.Id
            };

        var paramValue = request.Params.Deserialize(parameter.ParameterType,
            new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

        try
        {
            var resultTask = (Task<BaseResponseDto>)methodInfo.Invoke(this, [paramValue])!;
            var result = await resultTask;
            if (result is ErrorResponseDto error)
                error.Id = request.Id;


            return result;
        }
        catch (Exception e)
        {
            Log.Error("Error while invoking method {Method} with params {Params}\n{Error}", request.Method,
                JsonSerializer.Serialize(request.Params), e);
            return new ErrorResponseDto()
            {
                Error = ResponseErrors.InternalError,
                Id = request.Id
            };
        }
    }

    public async Task<BaseResponseDto> CheckPerformTransaction(CheckPerformTransactionDto dto)
    {
        var orderId = long.Parse(dto.Account.OrderId);

        var order = await dbContext.Orders.FirstOrDefaultAsync(x =>
            x.Id == orderId && x.Status == EnumOrderStatus.Pending);


        if (order is null)
            return new ErrorResponseDto()
            {
                Error = ResponseErrors.OrderNotFound,
            };


        if (order.Amount != dto.Amount)
            return new ErrorResponseDto()
            {
                Error = ResponseErrors.WrongAmount,
            };

        var transaction = dbContext.PaymeTransactions.FirstOrDefault(x => x.OrderId == orderId);

        if (transaction is null)
            return new ErrorResponseDto()
            {
                Error = ResponseErrors.OrderNotFound,
            };

        return new ResultResponseDto<AllowResultDto>() { Result = new AllowResultDto() { Allow = true } };
    }

    public async Task CreateTransaction(Order order)
    {
        var paymeTransaction = new PaymeTransaction()
            { OrderId = order.Id, Amount = order.Amount, Status = EnumPaymeTransactionStatus.Created };

        paymeTransaction = dbContext.PaymeTransactions.Add(paymeTransaction).Entity;
        await dbContext.SaveChangesAsync();
    }
}