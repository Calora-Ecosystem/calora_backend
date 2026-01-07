namespace Core.Services.Billing.Payme.Contracts;

public static class ResponseErrors
{
    public static ErrorDto MethodNotFound =>
        new ErrorDto()
        {
            Code = -32601,
            Message = new MessageDto()
            {
                En = "Method not found",
                Ru = "Запрашиваемый метод не найден. В RPC-запросе имя запрашиваемого метода содержится в поле data",
                Uz = ""
            },
        };

    public static ErrorDto ErrorWhileParsingJson =>
        new ErrorDto()
        {
            Code = -32700,
            Message = new MessageDto()
            {
                En = "Error while parsing JSON.",
                Ru = "Ошибка парсинга JSON.",
                Uz = ""
            },
        };

    public static ErrorDto InternalError =>
        new ErrorDto()
        {
            Code = -32400,
            Message = new MessageDto()
            {
                En = "Internal error.",
                Ru =
                    "Системная (внутренняя ошибка). Ошибку следует использовать в случае системных сбоев: отказа базы данных, отказа файловой системы, неопределенного поведения и т.д.",
                Uz = ""
            },
        };

    public static ErrorDto OrderNotFound =>
        new ErrorDto()
        {
            Code = -31050,
            Message = new MessageDto()
            {
                En = "Order not found.",
                Ru = "",
                Uz = "Buyurtma topilmadi."
            },
        };

    public static ErrorDto WrongAmount =>
        new ErrorDto()
        {
            Code = -31001,
            Message = new MessageDto()
            {
                En = "Wrong amount.",
                Ru = "Неверная сумма.",
                Uz = "Noto'g'ri summa."
            },
        };

    public static ErrorDto Unathorized =>
        new ErrorDto()
        {
            Code = -32504,
            Message = new MessageDto()
            {
                En = "Unauthorized.",
                Ru = "Недостаточно привилегий для выполнения метода.",
                Uz = ""
            },
        };
}