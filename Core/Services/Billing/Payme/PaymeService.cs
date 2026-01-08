using System.Data;
using System.Text.Json;
using BRB.Core.Common.Exceptions;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Entities.Billing.Enum;
using Core.Entities.Billing.Payme;
using Core.Services.Billing.Payme.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Core.Services.Billing.Payme;

[Injectable]
public class PaymeService(AppDbContext dbContext, IOptions<PaymeConfig> config)
{
    public bool CheckFodValidRequestFromPayme(string authToken)
    {
        return authToken == config.Value.AuthToken;
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
            result.Id = request.Id;

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
                Error = ResponseErrors.TransactionNotFound,
            };

        return new ResultResponseDto<AllowResultDto>() { Result = new AllowResultDto() { Allow = true } };
    }

    public async Task<BaseResponseDto> CreateTransaction(CreateTransactionDto dto)
    {
        var orderId = long.Parse(dto.Account.OrderId);

        var transaction = await dbContext.PaymeTransactions.FirstOrDefaultAsync(x => x.OrderId == orderId) ??
                          throw new NotFoundException("Transaction not found");

        if (transaction.Status != EnumPaymeTransactionStatus.Pending)
        {
            return new ErrorResponseDto()
            {
                Error = ResponseErrors.TransactionCanNotBePerformed,
            };
        }

        if (transaction.ExternalId != null && transaction.ExternalId != dto.Id)
            return new ErrorResponseDto()
            {
                Error = ResponseErrors.TransactionAlreadyCreated,
            };

        if (transaction.CreatedAt.AddHours(12) <= DateTime.Now)
        {
            transaction.Status = EnumPaymeTransactionStatus.PendingCancelled;
            transaction.CancelledAt = DateTime.Now;
            transaction.Reason = 4;

            await dbContext.SaveChangesAsync();

            return new ErrorResponseDto()
            {
                Error = ResponseErrors.TransactionCanNotBePerformed,
            };
        }

        var checkResult = await CheckPerformTransaction(new CheckPerformTransactionDto()
        {
            Amount = dto.Amount,
            Account = dto.Account
        });

        if (checkResult is ErrorResponseDto)
            return checkResult;

        transaction.ExternalId = dto.Id;
        transaction.ExternalCreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(dto.Time).DateTime;
        transaction.Status = EnumPaymeTransactionStatus.Pending;

        await dbContext.SaveChangesAsync();

        return new ResultResponseDto<CreateTransactionResponseDto>()
        {
            Result = new CreateTransactionResponseDto()
            {
                CreateTime = transaction.ExternalCreatedAt.HasValue
                    ? DateTimeOffset.FromFileTime(transaction.ExternalCreatedAt.Value.ToFileTime())
                        .ToUnixTimeMilliseconds()
                    : 0,
                State = (int)transaction.Status,
                Transaction = transaction.Id.ToString()
            }
        };
    }

    public async Task<BaseResponseDto> PerformTransaction(PerformTransactionDto dto)
    {
        var transaction = await dbContext.PaymeTransactions.FirstOrDefaultAsync(x => x.ExternalId == dto.Id);

        if (transaction is null)
            return new ErrorResponseDto()
            {
                Error = ResponseErrors.TransactionNotFound,
            };

        if (transaction.Status != EnumPaymeTransactionStatus.Pending)
        {
            if (transaction.Status != EnumPaymeTransactionStatus.Paid)
                return new ErrorResponseDto()
                {
                    Error = ResponseErrors.TransactionCanNotBePerformed,
                };

            return new ResultResponseDto<PerformResponseDto>()
            {
                Result = new PerformResponseDto()
                {
                    PerformTime = transaction.PerformedAt.HasValue
                        ? DateTimeOffset.FromFileTime(transaction.PerformedAt.Value.ToFileTime())
                            .ToUnixTimeMilliseconds()
                        : 0,
                    State = (int)transaction.Status,
                    Transaction = transaction.Id.ToString()
                }
            };
        }

        if (transaction.CreatedAt.AddHours(12) <= DateTime.Now)
        {
            transaction.Status = EnumPaymeTransactionStatus.PendingCancelled;
            transaction.Reason = 4;
            transaction.CancelledAt = DateTime.Now;
            await dbContext.SaveChangesAsync();

            return new ErrorResponseDto()
            {
                Error = ResponseErrors.TransactionCanNotBePerformed,
            };
        }

        var now = DateTimeOffset.Now;

        transaction.Status = EnumPaymeTransactionStatus.Paid;
        transaction.PerformedAt = now.DateTime;

        await dbContext.SaveChangesAsync();

        return new ResultResponseDto<PerformResponseDto>()
        {
            Result = new PerformResponseDto()
            {
                PerformTime = now.ToUnixTimeMilliseconds(),
                State = (int)transaction.Status,
                Transaction = transaction.Id.ToString()
            }
        };
    }

    public async Task<BaseResponseDto> CancelTransaction(CancelTransactionRequestDto dto)
    {
        var transaction = await dbContext.PaymeTransactions.FirstOrDefaultAsync(x =>
            x.ExternalId == dto.Id);

        if (transaction is null)
            return new ErrorResponseDto()
            {
                Error = ResponseErrors.TransactionNotFound,
            };

        var now = DateTimeOffset.Now;

        if (transaction.Status == EnumPaymeTransactionStatus.Pending)
        {
            transaction.CancelledAt = now.DateTime;
            transaction.Status = EnumPaymeTransactionStatus.PendingCancelled;
            transaction.Reason = dto.Reason;

            await dbContext.SaveChangesAsync();

            return new ResultResponseDto<CancelTransactionResponseDto>()
            {
                Result = new CancelTransactionResponseDto()
                {
                    CancelTime = now.ToUnixTimeMilliseconds(),
                    State = (int)transaction.Status,
                    Transaction = transaction.Id.ToString()
                }
            };
        }

        if (transaction.Status != EnumPaymeTransactionStatus.Paid)
        {
            return new ResultResponseDto<CancelTransactionResponseDto>()
            {
                Result = new CancelTransactionResponseDto()
                {
                    CancelTime = transaction.CancelledAt.HasValue
                        ? DateTimeOffset.FromFileTime(transaction.CancelledAt.Value.ToFileTime())
                            .ToUnixTimeMilliseconds()
                        : 0,
                    State = (int)transaction.Status,
                    Transaction = transaction.Id.ToString()
                }
            };
        }

        //ToDo: implement cancel transaction va it's order

        transaction.CancelledAt = now.DateTime;
        transaction.Status = EnumPaymeTransactionStatus.PaidCancelled;
        transaction.Reason = dto.Reason;

        await dbContext.SaveChangesAsync();

        return new ResultResponseDto<CancelTransactionResponseDto>()
        {
            Result = new CancelTransactionResponseDto()
            {
                CancelTime = now.ToUnixTimeMilliseconds(),
                State = (int)transaction.Status,
                Transaction = transaction.Id.ToString()
            }
        };
    }


    public async Task<BaseResponseDto> CheckTransaction(CheckTransactionRequestDto dto)
    {
        var transaction = await dbContext.PaymeTransactions.FirstOrDefaultAsync(x =>
            x.ExternalId == dto.Id);

        if (transaction is null)
            return new ErrorResponseDto()
            {
                Error = ResponseErrors.TransactionNotFound,
            };

        return new ResultResponseDto<CheckTransactionResponseDto>()
        {
            Result = new CheckTransactionResponseDto()
            {
                CancelTime = transaction.CancelledAt.HasValue
                    ? DateTimeOffset.FromFileTime(transaction.CancelledAt.Value.ToFileTime()).ToUnixTimeMilliseconds()
                    : 0,
                PerformTime = transaction.PerformedAt.HasValue
                    ? DateTimeOffset.FromFileTime(transaction.PerformedAt.Value.ToFileTime()).ToUnixTimeMilliseconds()
                    : 0,
                CreateTime = transaction.ExternalCreatedAt.HasValue
                    ? DateTimeOffset.FromFileTime(transaction.ExternalCreatedAt.Value.ToFileTime())
                        .ToUnixTimeMilliseconds()
                    : 0,
                State = (int)transaction.Status,
                Transaction = transaction.Id.ToString(),
                Reason = transaction.Reason
            }
        };
    }


    public async Task<BaseResponseDto> GetStatement(GetStatementRequestDto dto)
    {
        var fromDate = DateTimeOffset.FromUnixTimeMilliseconds(dto.From);
        var toDate = DateTimeOffset.FromUnixTimeMilliseconds(dto.To);

        var transactions = await dbContext.PaymeTransactions
            .Where(x => x.CreatedAt >= fromDate && x.CreatedAt <= toDate)
            .Select(x => new GetStatementTransaction()
            {
                Id = x.ExternalId!,
                Time = DateTimeOffset.FromFileTime(DateTime.Now.ToFileTime()).ToUnixTimeMilliseconds(),
                Amount = x.Amount,
                AccountBaseDto = new AccountBaseDto()
                {
                    OrderId = x.OrderId.ToString(),
                },
                CreateTime = x.ExternalCreatedAt.HasValue
                    ? DateTimeOffset.FromFileTime(x.ExternalCreatedAt.Value.ToFileTime())
                        .ToUnixTimeMilliseconds()
                    : 0,
                PerformTime = x.PerformedAt.HasValue
                    ? DateTimeOffset.FromFileTime(x.PerformedAt.Value.ToFileTime())
                        .ToUnixTimeMilliseconds()
                    : 0,
                CancelTime = x.CancelledAt.HasValue
                    ? DateTimeOffset.FromFileTime(x.CancelledAt.Value.ToFileTime())
                        .ToUnixTimeMilliseconds()
                    : 0,
                Transaction = x.Id.ToString(),
                State = (int)x.Status,
                Reason = x.Reason
            })
            .ToListAsync();

        return new ResultResponseDto<GetStatementResponseDto>()
        {
            Result = new GetStatementResponseDto()
            {
                Transactions = transactions
            }
        };
    }


    public async Task CreateInternalTransaction(Order order)
    {
        var paymeTransaction = new PaymeTransaction()
            { OrderId = order.Id, Amount = order.Amount, Status = EnumPaymeTransactionStatus.Pending };

        paymeTransaction = dbContext.PaymeTransactions.Add(paymeTransaction).Entity;
        await dbContext.SaveChangesAsync();
    }
}