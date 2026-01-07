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
                Uz = "Ko'rsatma topilmadi."
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
                Uz = "Ma'lumotlarni qayta ishlashda xatolik yuz berdi."
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
                Uz = "Tizim xatosi."
            },
        };

    public static ErrorDto OrderNotFound =>
        new ErrorDto()
        {
            Code = -31050,
            Message = new MessageDto()
            {
                En = "Order not found.",
                Ru = "Заказ не найден.",
                Uz = "Buyurtma topilmadi."
            },
        };

    public static ErrorDto TransactionNotFound =>
        new ErrorDto()
        {
            Code = -31003,
            Message = new MessageDto()
            {
                En = "Transaction not found.",
                Ru = "Транзакция не найдена.",
                Uz = "Amaliyot topilmadi."
            },
        };
    
    public static ErrorDto TransactionAlreadyDone =>
        new ErrorDto()
        {
            Code = -31003,
            Message = new MessageDto()
            {
                En = "Transaction already done.",
                Ru = "Заказ выполнен.",
                Uz = "Amaliyot allaqachon bajarilgan."
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
                Uz = "Ruxsat yo'q."
            },
        };
}