using System.Text.Json;
using BRB.Core.Common.Exceptions;
using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Entities.Billing.Enum;
using Core.Services.Billing.Click.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Services.Billing.Click;

[Injectable]
public class ClickService(
    IOptions<ClickConfig> config,
    AppDbContext appDbContext,
    OrderService orderService,
    ILogger<ClickService> logger)
{
    public async Task<ClickResponse?> HandleAsync(ClickRequest request)
    {
        logger.LogInformation($"Request from click: {JsonSerializer.Serialize(request)}");

        var response = request.Action switch
        {
            0 => await Prepare(request),
            1 => await Complete(request),
            _ => new ClickResponse
            {
                Error = ClickErrorType.ActionNotFound,
                ErrorNote = "Action not found"
            }
        };

        logger.LogInformation($"Response to click: {JsonSerializer.Serialize(response)}");

        return response;
    }

    private async Task<ClickResponse?> Prepare(ClickRequest request)
    {
        var transaction = uint.TryParse(request.OrderId, out var orderId)
            ? await FindByOrderId(orderId)
            : null;

        uint prepareId = 0;
        uint confirmId = 0;

        if (transaction is not null)
        {
            prepareId = transaction.Id;
            confirmId = transaction.Id;
        }

        var response = await CheckRequest(request);

        response.ClickTransId = request.ClickTransId;
        response.OrderId = request.OrderId;
        response.MerchantPrepareId = prepareId;
        response.MerchantConfirmId = confirmId;

        if (transaction is null)
        {
            response.Error = ClickErrorType.UserDoesNotExist;
            response.ErrorNote = "Order does not exist";

            return response;
        }

        if (response.Error == ClickErrorType.Success)
        {
            transaction.State = EnumClickTransactionState.Waiting;
            appDbContext.ClickTransactions.Update(transaction);
            await appDbContext.SaveChangesAsync();
        }

        return response;
    }

    private async Task<ClickResponse?> Complete(ClickRequest request)
    {
        var transaction = uint.TryParse(request.OrderId, out var transactionId)
            ? await FindByOrderId(transactionId)
            : null;

        uint prepareId = 0;
        uint confirmId = 0;

        if (transaction is not null)
        {
            prepareId = transaction.Id;
            confirmId = transaction.Id;
        }

        var response = await CheckRequest(request);

        response.ClickTransId = request.ClickTransId;
        response.OrderId = request.OrderId;
        response.MerchantPrepareId = prepareId;
        response.MerchantConfirmId = confirmId;


        if (transaction is null)
        {
            response.Error = ClickErrorType.UserDoesNotExist;
            response.ErrorNote = "User does not exist";

            return response;
        }

        if (request.Error is < 0 &&
            response.Error is not (ClickErrorType.AlreadyPaid or ClickErrorType.TransactionCancelled))
        {
            transaction.State = EnumClickTransactionState.Rejected;
            appDbContext.ClickTransactions.Update(transaction);
            await appDbContext.SaveChangesAsync();

            response.Error = ClickErrorType.TransactionCancelled;
            response.ErrorNote = "Transaction cancelled";
            return response;
        }

        if (response.Error == ClickErrorType.Success)
        {
            transaction.State = EnumClickTransactionState.Confirmed;
            appDbContext.ClickTransactions.Update(transaction);

            var result = await orderService
                .AcceptPaymentAsync(transaction.OrderId);

            if (!result)
            {
                return new ClickResponse()
                {
                    Error = ClickErrorType.TransactionCancelled,
                    ErrorNote = "Transaction cancelled"
                };
            }

            await appDbContext.SaveChangesAsync();
        }

        return response;
    }

    private async Task<ClickResponse> CheckRequest(ClickRequest request)
    {
        if (CheckPossibleDataInRequest(request))
            return new ClickResponse
            {
                Error = ClickErrorType.ErrorInRequestFromClick,
                ErrorNote = "Error in request from click"
            };

        if (!Authorize(request))
            return new ClickResponse
            {
                Error = ClickErrorType.SignCheckFailed,
                ErrorNote = "SIGN CHECK FAILED!"
            };

        var transaction = await FindByOrderId(long.Parse(request.OrderId!));

        if (transaction is null)
            return new ClickResponse()
            {
                Error = ClickErrorType.UserDoesNotExist,
                ErrorNote = "Order does not exist"
            };

        if (request.Action == 1)
        {
            transaction = await FindByOrderId(request.MerchantPrepareId!.Value);
            if (transaction is null)
                return new ClickResponse()
                {
                    Error = ClickErrorType.TransactionDoesNotExist,
                    ErrorNote = "Transaction does not exist",
                };
        }

        if (transaction.State == EnumClickTransactionState.Confirmed)
            return new ClickResponse()
            {
                Error = ClickErrorType.AlreadyPaid,
                ErrorNote = "Already paid",
            };

        var requestedAmount = request.Amount!.Value * 100;
        if (Math.Abs(requestedAmount - transaction.Amount) > 1m)
            return new ClickResponse()
            {
                Error = ClickErrorType.IncorrectParameterAmount,
                ErrorNote = "Incorrect parameter amount"
            };

        if (transaction.State == EnumClickTransactionState.Rejected)
            return new ClickResponse()
            {
                Error = ClickErrorType.TransactionCancelled,
                ErrorNote = "Transaction cancelled"
            };

        return new ClickResponse()
        {
            Error = ClickErrorType.Success,
            ErrorNote = "Success",
        };
    }

    private bool CheckPossibleDataInRequest(ClickRequest request)
    {
        return !(request.ClickTransId is not null &&
                 request.ServiceId is not null &&
                 request.ClickPayDocId is not null &&
                 request.OrderId is not null &&
                 long.TryParse(request.OrderId, out _) &&
                 request.Amount is not null &&
                 request.Action is not null &&
                 request.Error is not null &&
                 request.ErrorNote is not null &&
                 request.SignTime is not null &&
                 request.SignString is not null) ||
               request is { Action: 1, MerchantPrepareId: null };
    }

    private bool Authorize(ClickRequest request)
    {
        var unHashedSignStr = $"{request.ClickTransId}{request.ServiceId}{config.Value.SecretKey}{request.OrderId}" +
                              (request.Action == 1 ? request.MerchantPrepareId.ToString() : "") +
                              $"{request.Amount}{request.Action}{request.SignTime}";

        return ClickMd5Helper.VerifyMd5Hash(unHashedSignStr, request.SignString!);
    }

    private async Task<ClickTransaction?> FindByOrderId(long orderId) =>
        await appDbContext.ClickTransactions
            .Include(x => x.Order)
            .SingleOrDefaultAsync(ct => ct.OrderId == orderId);

    private async Task<ClickTransaction?> FindById(uint id) =>
        await appDbContext.ClickTransactions
            .SingleOrDefaultAsync(ct => ct.Id == id);

    public async Task<string> MakeClickPaymentLink(long orderId, decimal amount)
    {
        var transaction = await appDbContext.ClickTransactions
            .FirstOrDefaultAsync(x => x.OrderId == orderId) ?? throw new NotFoundException("Transaction not found");

        amount /= 100; //convert to sum

        return
            $"https://my.click.uz/services/pay?service_id={config.Value.ServiceId}&merchant_id={config.Value.MerchantId}&amount={amount}&transaction_param={transaction.OrderId}";
    }

    public async Task<long> CreateTransaction(Order order)
    {
        var transaction = new ClickTransaction()
        {
            Amount = order.Amount,
            OrderId = order.Id,
            State = EnumClickTransactionState.Input,
            Token = Guid.NewGuid().ToString(),
            Total = order.Amount,
        };

        transaction = appDbContext.ClickTransactions.Add(transaction).Entity;
        await appDbContext.SaveChangesAsync();

        return transaction.Id;
    }
}