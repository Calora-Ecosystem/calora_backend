namespace Core.Entities.Crm.Enum;

public enum EnumLeadStatus
{
    New = 1,                // Yangi
    FollowUp = 2,           // Qayta aloqa
    Interested = 3,         // O'ylab ko'radi
    PaymentInProgress = 4,  // To'lov jarayonda
    Won = 5,                // Sotuv
    Lost = 6,               // Yo'qotilgan
    // Kanban tartibida "Yangi"dan keyin ko'rsatiladi (tartib KANBAN_STATUSES/funnel bo'yicha,
    // raqamli qiymat bo'yicha emas), shuning uchun mavjud ma'lumotni ko'chirmasdan oxiriga qo'shildi.
    Contacted = 7           // Bog'lanish
}
